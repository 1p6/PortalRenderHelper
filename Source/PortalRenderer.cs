using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.PortalRenderHelper;

public static class PortalRenderer {

    // in theooory these should be disposed, buuuut we just reuse them for the whole time the game is running, so whatever
    public static VirtualRenderTarget OuterRenderTarget = null;
    public static VirtualRenderTarget InnerRenderTarget = null;
    public static SpriteBatch _Batch = null;
    public static SpriteBatch Batch {get {
        _Batch ??= new SpriteBatch(Engine.Instance.GraphicsDevice);
        return _Batch;
    }}

    public static Vector3 PlayerPos {get; set;} = Vector3.Zero;
    public static readonly DepthStencilState StenciledCopy = new() {
        StencilEnable = true,
        DepthBufferEnable = false,
        StencilFunction = CompareFunction.Less,
        ReferenceStencil = 127,
    };
    public static readonly DepthStencilState MakeStencilCW = new() {
        StencilEnable = true,
        DepthBufferEnable = false,
        StencilPass = StencilOperation.Decrement,
    };
    public static readonly DepthStencilState MakeStencilCCW = new() {
        StencilEnable = true,
        DepthBufferEnable = false,
        StencilPass = StencilOperation.Increment,
    };

    public static Vector2 XY(this Vector3 self) {
        return new(self.X, self.Y);
    }
    public static readonly float MaxDiag = MathF.Sqrt(320*320+180*180);
    public static float PointSegDist(Vector2 p, Vector2 a, Vector2 b) {
        float t = Vector2.Dot(p-a, b-a) / (b-a).LengthSquared();
        t = Calc.Clamp(t, 0, 1);
        return Vector2.Distance(p, a + (b-a)*t);
    }

    public static int LevelRenders = 0;
    public static int MaxLevelRenders = 0;
    public static void DoPartialLevelRender(Level level) {
        LevelRenders++;
        if(LevelRenders > MaxLevelRenders) MaxLevelRenders = LevelRenders;
        level.BeforeRender();

        // from Level.Render
        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        level.GameplayRenderer.Render(level);
        level.Lighting.Render(level);
        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Level);
        Engine.Instance.GraphicsDevice.Clear(level.BackgroundColor);
        level.Background.Render(level);
        Distort.Render((RenderTarget2D)GameplayBuffers.Gameplay, (RenderTarget2D)GameplayBuffers.Displacement, level.Displacement.HasDisplacement(level));
        level.Bloom.Apply(GameplayBuffers.Level, level);
        level.Foreground.Render(level);

        level.AfterRender();
    }

    // internal static int DebugCounter = 0;

    public static void RenderPortalsToTarget(Level level, List<PortalRenderPoly> sortedPolys, int depth, Vector2 minBound, Vector2 maxBound, AngleInterval angleBound, float minRadius, float maxRadius) {
        if(InnerRenderTarget == null) throw new Exception("null inner render target!");
        Engine.Instance.GraphicsDevice.SetRenderTarget(InnerRenderTarget);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        if(depth <= 0) return;

        VirtualRenderTarget temp = OuterRenderTarget;
        OuterRenderTarget = InnerRenderTarget;
        VirtualRenderTarget inner = null;
        List<PortalRenderPoly> radiusSortedPolys = new(sortedPolys.Count);
        foreach(PortalRenderPoly poly in sortedPolys) {
            if(poly.Flag.Length != 0 && poly.InvertFlag == level.Session.GetFlag(poly.Flag)) continue;

            poly.UpdateBounds();
            Vector2 newMin = Vector2.Max(minBound, poly.Min);
            Vector2 newMax = Vector2.Min(maxBound, poly.Max);
            AngleInterval newAngle = angleBound.Intersect(poly.AngleSpan);
            float newMinRadius = MathF.Max(minRadius, poly.MinRadius);
            float checkMaxRadius = MathF.Min(maxRadius, poly.MaxRadius);
            if(newMin.X >= newMax.X || newMin.Y >= newMax.Y || newMinRadius >= checkMaxRadius || newAngle.IsEmpty)
                continue;
            radiusSortedPolys.Add(poly);
        }
        radiusSortedPolys.Sort((x, y) => -Comparer.Default.Compare(x.AvgRadius, y.AvgRadius));
        foreach(PortalRenderPoly poly in radiusSortedPolys) {
            // this UpdateBounds call is needed because recursive calls to RenderPortalsToTarget will update the bounds of this same poly using different camera / player positions.
            poly.UpdateBounds();
            Vector2 newMin = Vector2.Max(minBound, poly.Min);
            Vector2 newMax = Vector2.Min(maxBound, poly.Max);
            AngleInterval newAngle = angleBound.Intersect(poly.AngleSpan);
            float newMinRadius = MathF.Max(minRadius, poly.MinRadius+1);
            float newMaxRadius = poly.Closed ? MathF.Min(maxRadius, poly.MaxRadius-1) : maxRadius;
            poly.SetStencil(OuterRenderTarget);

            Vector2 origPos = level.Camera.Position;
            float origAngle = CameraHooks.CameraAngle;
            // floor since the camera pos gets floored later anyways, and doing it now prevents numerical errors caused by adding to the result of lerp smoothing (lerp smoothing gives floats that are almost an integer but not quite 3: )
            Vector2 camCenter = new(160, 90);
            level.Camera.Position = (origPos.Floor() + camCenter - poly.Position).Rotate(poly.Angle).Round() + poly.Target - camCenter;
            CameraHooks.CameraAngle -= poly.Angle;

            Vector3 origPlayerPos = PlayerPos;
            PlayerPos = new((PlayerPos.XY() - poly.Position).Rotate(poly.Angle) + poly.Target, PlayerPos.Z);
            // if(isFirst && DebugCounter % 12 == 0) {
            //     Logger.Log(nameof(PortalRenderHelperModule), $"pos before: {level.Camera.Position}");
            // }
            int newDepth = PortalRenderHelperModule.Settings.IgnoreMapRecursionLimits ? depth-1 : Math.Min(depth-1, poly.RecursionDepth);
            if(newDepth > 0) {
                InnerRenderTarget = (inner ??= RenderTargetPool.Alloc());
                RenderPortalsToTarget(level, sortedPolys, newDepth, newMin, newMax, newAngle, newMinRadius, newMaxRadius);
            } else InnerRenderTarget = null;
            Engine.Instance.GraphicsDevice.SetRenderTarget(null);
            DoPartialLevelRender(level);
            // if(isFirst && DebugCounter % 12 == 0) {
            //     Logger.Log(nameof(PortalRenderHelperModule), $"pos after:  {level.Camera.Position}");
            // }
            // if(isFirst) DebugCounter++;
            // isFirst = false;
            level.Camera.Position = origPos;
            CameraHooks.CameraAngle = origAngle;
            PlayerPos = origPlayerPos;
        }
        if(inner != null) RenderTargetPool.Free(inner);
        InnerRenderTarget = OuterRenderTarget;
        OuterRenderTarget = temp;
    }

    public static void OnRenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        if(self.scene is Level level) {
            PortalRenderEffect effect = null;
            foreach(PortalRenderEffect fx in level.Foreground.GetEach<PortalRenderEffect>()) {
                if(fx.Visible) {
                    effect = fx;
                    break;
                }
            }
            int maxDepth = Math.Min(PortalRenderHelperModule.Settings.MaxRecursionDepth, effect == null ? 0 : (PortalRenderHelperModule.Settings.IgnoreMapRecursionLimits ? int.MaxValue : effect.MaxRecursionDepth));

            if(maxDepth <= 0) {
                if(InnerRenderTarget != null) {
                    RenderTargetPool.Free(InnerRenderTarget);
                    InnerRenderTarget = null;
                }
                orig(self);
                return;
            }

            InnerRenderTarget ??= RenderTargetPool.Alloc();

            // IsRenderingPortals = true;
            // bool isFirst = true;

            // TODO Replace this with actual depth testing
            List<PortalRenderPoly> list = level.Tracker.GetEntities<PortalRenderPoly>().Cast<PortalRenderPoly>().ToList();
            list.Sort((x, y) => Comparer.Default.Compare(x.PortalDepth, y.PortalDepth));

            foreach(Lookout l in level.Tracker.GetEntities<Lookout>()) {
                if(l.interacting) {
                    PlayerPos = new(level.Camera.Position + new Vector2(160, 90), 0);
                    goto skipPlayer;
                }
            }
            Player p = level.Tracker.GetEntity<Player>();
            if(p != null) PlayerPos = new(p.Center, 0);
            skipPlayer:
            // PlayerSpriteMode oldMode = PlayerSpriteMode.Madeline;
            // if(p != null) {
            //     oldMode = p.Sprite.Mode;
            //     p.ResetSprite(PlayerSpriteMode.Badeline);
            // }
            Vector2 screenSpacePlayer = Vector2.Transform(PlayerPos.XY(), level.Camera.Matrix);
            Vector2 furthestCorner = new(screenSpacePlayer.X < 160 ? 320 : 0, screenSpacePlayer.Y < 90 ? 180 : 0);

            int beforeNumActive = RenderTargetPool.NumActiveTargets;
            LevelRenders = 0;
            RenderPortalsToTarget(level, list, maxDepth, new(0,0), new(320,180), AngleInterval.FULL, 0, Vector2.Distance(screenSpacePlayer, furthestCorner));
            if(beforeNumActive != RenderTargetPool.NumActiveTargets)
                throw new Exception("mismatched alloc / free of render targets from pool!");

            // if(p != null) p.ResetSprite(oldMode);

            // IsRenderingPortals = false;
        }
        orig(self);
    }

    public static void DrawPoly<T>(Matrix matrix, T[] vertices, int[] indices, DepthStencilState depth, RasterizerState raster) where T : struct, IVertexType {
        // from GFX.DrawIndexedVertices
        Effect obj = GFX.FxPrimitive;
        Vector2 vector = new(Engine.Graphics.GraphicsDevice.Viewport.Width, Engine.Graphics.GraphicsDevice.Viewport.Height);
        matrix *= Matrix.CreateScale(1f / vector.X * 2f, (0f - 1f / vector.Y) * 2f, 1f);
        matrix *= Matrix.CreateTranslation(-1f, 1f, 0f);
        Engine.Instance.GraphicsDevice.DepthStencilState = depth;
        Engine.Instance.GraphicsDevice.RasterizerState = raster;
        Engine.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        obj.Parameters["World"].SetValue(matrix);
        foreach (EffectPass pass in obj.CurrentTechnique.Passes)
        {
            pass.Apply();
            Engine.Instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, vertices.Length-2);
        }
    }
}

[CustomEntity("PortalRenderHelper/PortalRenderPoly")]
[Tracked]
public class PortalRenderPoly : Entity {
    public PortalRenderPoly(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Closed = data.Bool("closed", false);
        Flag = data.Attr("flag");
        InvertFlag = data.Bool("invert");
        PortalDepth = data.Float("portalDepth");
        Angle = data.Float("angle") / 180f * (float) Math.PI;
        RecursionDepth = data.Int("recursionDepth");
        // TurningNumberCull = data.Bool("turningNumberCull", true);

        // turns out all the positions in `data` are relative to the current room's coordinates, and offset gives the position of the room in the map's coordinates
        Target = data.Nodes[0] + offset;
        RenderOffset = Target - Position;
        RenderPoly = new VertexPositionColor[data.Nodes.Length * (Closed ? 1 : 2)];
        RenderPoly[0].Position = new Vector3(Position, 0);
        for(int i = 1; i < data.Nodes.Length; ++i) {
            RenderPoly[i].Position = new(data.Nodes[i] + offset, 0);
        }

        RenderPolyIndices = new int[(RenderPoly.Length-2)*3];
        for(int i = 0; i < RenderPoly.Length-2; ++i) {
            RenderPolyIndices[3*i] = 0;
            RenderPolyIndices[3*i+1] = i+1;
            RenderPolyIndices[3*i+2] = i+2;
        }
    }

    public Vector2 Target;
    public Vector2 RenderOffset;
    public float Angle;
    public VertexPositionColor[] RenderPoly;
    public int[] RenderPolyIndices;
    public bool Closed;
    public string Flag;
    public bool InvertFlag;
    public float PortalDepth;
    public int RecursionDepth;

    public Vector2 Min = new(0,0);
    public Vector2 Max = new(320,180);
    public AngleInterval AngleSpan = AngleInterval.FULL;
    public float MinRadius = 0;
    public float MaxRadius = float.PositiveInfinity;
    public float AvgRadius = float.PositiveInfinity;

    public void UpdateBounds() {
        if(!Closed) {
            // hopefully this is large enough
            float farAway = 10000.0f;
            // Add points "at infinity" that go back through the polygon in reverse order. This properly generalizes the old behaviour to polygons defined with more than two finite vertices.
            for(int i = 0; i < RenderPoly.Length/2; ++i) {
                RenderPoly[^(i+1)].Position = PortalRenderer.PlayerPos + (RenderPoly[i].Position - PortalRenderer.PlayerPos) * farAway;
                // RenderPoly[^(i+1)].Position.Z = -1;
            }
        }
        // rectangular coordinate bounds
        Min = new(float.PositiveInfinity);
        Max = new(float.NegativeInfinity);
        foreach(VertexPositionColor point in RenderPoly) {
            Vector2 pos = Vector2.Transform(point.Position.XY(), SceneAs<Level>().Camera.Matrix).Round();
            Min = Vector2.Min(Min, pos);
            Max = Vector2.Max(Max, pos);
        }
        // polar coordinate bounds
        Vector2 playerPos = PortalRenderer.PlayerPos.XY();
        MinRadius = float.PositiveInfinity;
        if(Closed) {
            AngleSpan = AngleInterval.EMPTY;
            Vector2 prevPos = RenderPoly[^1].Position.XY();
            int prev = AngleInterval.RadToDeg(Calc.Angle(playerPos, prevPos) + CameraHooks.CameraAngle);
            MaxRadius = 0;
            foreach(VertexPositionColor point in RenderPoly) {
                Vector2 currPos = point.Position.XY();
                int current = AngleInterval.RadToDeg(Calc.Angle(playerPos, currPos) + CameraHooks.CameraAngle);
                int diff = AngleInterval.Rem(current-prev + AngleInterval.DEG_CIRCLE/2, AngleInterval.DEG_CIRCLE) - AngleInterval.DEG_CIRCLE/2;
                AngleSpan = AngleSpan.Union(diff < 0 ? new(prev+diff, -diff) : new(prev, diff));
                prev = current;

                float currDist = Vector2.Distance(playerPos, currPos);
                MinRadius = MathF.Min(MinRadius, PortalRenderer.PointSegDist(playerPos, prevPos, currPos));
                MaxRadius = MathF.Max(MaxRadius, currDist);
                prevPos = currPos;
            }
            AvgRadius = float.PositiveInfinity;
        } else {
            Vector2 prevPos = RenderPoly[0].Position.XY();
            int end = AngleInterval.RadToDeg(Calc.Angle(playerPos, prevPos) + CameraHooks.CameraAngle);
            int start = end, prev = end;
            MaxRadius = AvgRadius = Vector2.Distance(playerPos, prevPos);
            for(int i = 1; i < RenderPoly.Length/2; i++) {
                Vector2 currPos = RenderPoly[i].Position.XY();
                int current = AngleInterval.RadToDeg(Calc.Angle(playerPos, currPos) + CameraHooks.CameraAngle);
                start += AngleInterval.Rem(current-prev + AngleInterval.DEG_CIRCLE/2, AngleInterval.DEG_CIRCLE) - AngleInterval.DEG_CIRCLE/2;
                prev = current;

                float currDist = Vector2.Distance(playerPos, currPos);
                MinRadius = MathF.Min(MinRadius, PortalRenderer.PointSegDist(playerPos, prevPos, currPos));
                MaxRadius = MathF.Max(MaxRadius, currDist);
                AvgRadius += currDist;
                prevPos = currPos;
            }
            AngleSpan = new(start, end-start);
            AvgRadius /= RenderPoly.Length/2;
        }
    }

    public void SetStencil(VirtualRenderTarget rt) {
        Level level = SceneAs<Level>();
        Engine.Instance.GraphicsDevice.SetRenderTarget(rt);
        Engine.Instance.GraphicsDevice.Clear(ClearOptions.Stencil, Color.White, 0, 0);
        PortalRenderer.DrawPoly(level.Camera.Matrix, RenderPoly, RenderPolyIndices, PortalRenderer.MakeStencilCW, RasterizerState.CullCounterClockwise);
        PortalRenderer.DrawPoly(level.Camera.Matrix, RenderPoly, RenderPolyIndices, PortalRenderer.MakeStencilCCW, RasterizerState.CullClockwise);
        Engine.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
    }
}

public class PortalRenderEffect : Backdrop {
    public const string FXName = "PortalRenderHelper/PortalRenderEffect";

    public PortalRenderEffect(BinaryPacker.Element data) {
        MaxRecursionDepth = data.AttrInt("maxRecursionDepth", 4);
    }

    public int MaxRecursionDepth;

    public override void Ended(Scene scene)
    {
        // RenderTarget?.Dispose();
        // Batch?.Dispose();
        CameraHooks.CameraAngle = CameraHooks.CameraTargetAngle = 0f;
    }

    public override void Update(Scene scene)
    {
        base.Update(scene);
        CameraHooks.CameraAngle = Calc.AngleLerp(CameraHooks.CameraAngle, CameraHooks.CameraTargetAngle, 0.05f);
    }

    public override void Render(Scene scene)
    {
        if(PortalRenderer.InnerRenderTarget != null) {
            Draw.SpriteBatch.Draw(PortalRenderer.InnerRenderTarget, Vector2.Zero, Color.White);
        }
        if(PortalRenderer.OuterRenderTarget != null) {
            // if(PortalRenderer.DebugCounter % 12 == 0)
            //     Logger.Log(nameof(PortalRenderHelperModule), $"pos mid:    {(scene as Level).Camera.Position}");
            Renderer.EndSpritebatch();
            Engine.Instance.GraphicsDevice.SetRenderTarget(PortalRenderer.OuterRenderTarget);
            PortalRenderer.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, PortalRenderer.StenciledCopy, RasterizerState.CullNone);
            PortalRenderer.Batch.Draw(GameplayBuffers.Level, Vector2.Zero, Color.White);
            PortalRenderer.Batch.End();
            Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Level);
        }
    }
}
