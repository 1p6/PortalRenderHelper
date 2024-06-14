using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.PortalRenderHelper;

public class PortalRenderer {

    // in theooory these should be disposed, buuuut we just reuse them for the whole time the game is running, so whatever
    public static VirtualRenderTarget _RenderTarget = null;
    public static VirtualRenderTarget RenderTarget {get {
        _RenderTarget ??= VirtualContent.CreateRenderTarget("PortalRenderHelper/PortalRenderer", 320, 180, true);
        return _RenderTarget;
    }}
    public static SpriteBatch _Batch = null;
    public static SpriteBatch Batch {get {
        _Batch ??= new SpriteBatch(Engine.Instance.GraphicsDevice);
        return _Batch;
    }}

    public static bool IsRenderingPortals {get; set;} = false;

    public static Vector3 PlayerPos {get; set;} = Vector3.Zero;
    public static float _CameraAngle = 0f;
    public static float CameraAngle {
        get => _CameraAngle;
        set {
            _CameraAngle = value;
            Scene s = Engine.Instance.scene;
            if(s is Level l && l.Camera != null) l.Camera.changed = true;
        }
    }
    public static float CameraTargetAngle { get; set; } = 0f;
    public static Vector2 CameraMinBound = new(0,0);
    public static Vector2 CameraMaxBound = new(320, 180);
    public static bool _CameraBoundsChanged = true;
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

    public static void UpdateMatrices(On.Monocle.Camera.orig_UpdateMatrices orig, Camera self) {
        orig(self);
        Vector3 center = new(160, 90, 0);
        self.matrix = self.matrix * Matrix.CreateTranslation(-center) * Matrix.CreateRotationZ(CameraAngle) * Matrix.CreateTranslation(center);
        self.inverse = Matrix.CreateTranslation(-center) * Matrix.CreateRotationZ(-CameraAngle) * Matrix.CreateTranslation(center) * self.inverse;
        _CameraBoundsChanged = true;
    }
    public static void TryUpdateCameraBounds(Camera self) {
        if(self.changed || _CameraBoundsChanged) {
            if(self.changed) self.UpdateMatrices();
            Vector2[] corners = new Vector2[4];
            int i = 0;
            for(int y = 0; y <= 180; y += 180) {
                for(int x = 0; x <= 320; x += 320) {
                    corners[i++] = Vector2.Transform(new(x, y), self.Inverse);
                }
            }
            Vector2 min = corners[0], max = corners[0];
            for(i = 1; i < 4; i++) {
                min.X = Math.Min(min.X, corners[i].X);
                min.Y = Math.Min(min.Y, corners[i].Y);
                max.X = Math.Max(max.X, corners[i].X);
                max.Y = Math.Max(max.Y, corners[i].Y);
            }
            CameraMinBound = min;
            CameraMaxBound = max;
            _CameraBoundsChanged = false;
        }
    }
    public static float CameraGetLeft(Camera self) {
        TryUpdateCameraBounds(self);
        return CameraMinBound.X;
    }
    public static float CameraGetRight(Camera self) {
        TryUpdateCameraBounds(self);
        return CameraMaxBound.X;
    }
    public static float CameraGetTop(Camera self) {
        TryUpdateCameraBounds(self);
        return CameraMinBound.Y;
    }
    public static float CameraGetBottom(Camera self) {
        TryUpdateCameraBounds(self);
        return CameraMaxBound.Y;
    }

    public static void HookTalkComponent(ILContext ctx) {
        ILCursor cur = new(ctx);
        cur.GotoNext(MoveType.Before, instr => instr.MatchStloc2());
        cur.EmitPop().EmitLdarg0().EmitLdloc1();
        cur.EmitDelegate(ModifyTalkComponentPos);
    }
    public static Vector2 ModifyTalkComponentPos(TalkComponent.TalkComponentUI self, Vector2 camPos) {
        Vector2 center = new(160, 90);
        return center + (self.Handler.Entity.Position - camPos - center).Rotate(CameraAngle) + self.Handler.DrawAt;
    }

    public static void DoPartialLevelRender(Level level) {
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

    public static void OnRenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        if(self.scene is Level level) {

            self.GraphicsDevice.SetRenderTarget(PortalRenderer.RenderTarget);
            self.GraphicsDevice.Clear(Color.Transparent);

            IsRenderingPortals = true;
            // bool isFirst = true;

            List<PortalRenderPoly> list = level.Tracker.GetEntities<PortalRenderPoly>().ConvertAll(x => (PortalRenderPoly) x);
            list.Sort((x, y) => Comparer.Default.Compare(x.PortalDepth, y.PortalDepth));

            Player p = level.Tracker.GetEntity<Player>();
            // PlayerSpriteMode oldMode = PlayerSpriteMode.Madeline;
            // if(p != null) {
            //     oldMode = p.Sprite.Mode;
            //     p.ResetSprite(PlayerSpriteMode.Badeline);
            // }

            foreach(PortalRenderPoly poly in list) {
                if(poly.Flag.Length != 0 && poly.InvertFlag == level.Session.GetFlag(poly.Flag)) continue;

                foreach(Lookout l in level.Tracker.GetEntities<Lookout>()) {
                    if(l.interacting) {
                        PlayerPos = new(level.Camera.Position + new Vector2(160, 90), 0);
                        goto setStencil;
                    }
                }
                // if(p != null) PlayerPos = new(p.Position - Vector2.UnitY * (p.Ducking ? 4f : 7.5f), 0);
                if(p != null) PlayerPos = new(p.Center, 0);
                setStencil:
                poly.SetStencil();

                self.GraphicsDevice.SetRenderTarget(null);
                Vector2 origPos = level.Camera.Position;
                float origAngle = CameraAngle;
                // floor since the camera pos gets floored later anyways, and doing it now prevents numerical errors caused by adding to the result of lerp smoothing (lerp smoothing gives floats that are almost an integer but not quite 3: )
                Vector2 camCenter = new(160, 90);
                level.Camera.Position = (origPos.Floor() + camCenter - poly.Position).Rotate(poly.Angle).Round() + poly.Target - camCenter;
                CameraAngle -= poly.Angle;
                // if(isFirst && DebugCounter % 12 == 0) {
                //     Logger.Log(nameof(PortalRenderHelperModule), $"pos before: {level.Camera.Position}");
                // }
                DoPartialLevelRender(level);
                // if(isFirst && DebugCounter % 12 == 0) {
                //     Logger.Log(nameof(PortalRenderHelperModule), $"pos after:  {level.Camera.Position}");
                // }
                // if(isFirst) DebugCounter++;
                // isFirst = false;
                level.Camera.Position = origPos;
                CameraAngle = origAngle;
            }

            // if(p != null) p.ResetSprite(oldMode);

            IsRenderingPortals = false;
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

    public void SetStencil() {
        Level level = SceneAs<Level>();
        if(!Closed) {
            // hopefully this is large enough
            float farAway = 10000.0f;
            // Add points "at infinity" that go back through the polygon in reverse order. This properly generalizes the old behaviour to polygons defined with more than two finite vertices.
            for(int i = 0; i < RenderPoly.Length/2; ++i) {
                RenderPoly[^(i+1)].Position = PortalRenderer.PlayerPos + (RenderPoly[i].Position - PortalRenderer.PlayerPos) * farAway;
            }
        }

        Engine.Instance.GraphicsDevice.SetRenderTarget(PortalRenderer.RenderTarget);
        Engine.Instance.GraphicsDevice.Clear(ClearOptions.Stencil, Color.White, 0, 0);
        PortalRenderer.DrawPoly(level.Camera.Matrix, RenderPoly, RenderPolyIndices, PortalRenderer.MakeStencilCW, RasterizerState.CullCounterClockwise);
        PortalRenderer.DrawPoly(level.Camera.Matrix, RenderPoly, RenderPolyIndices, PortalRenderer.MakeStencilCCW, RasterizerState.CullClockwise);
        Engine.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
    }
}

public class PortalRenderEffect : Backdrop {
    public const string FXName = "PortalRenderHelper/PortalRenderEffect";

    public override void Ended(Scene scene)
    {
        // RenderTarget?.Dispose();
        // Batch?.Dispose();
        PortalRenderer.CameraAngle = PortalRenderer.CameraTargetAngle = 0f;
    }

    public override void Update(Scene scene)
    {
        base.Update(scene);
        PortalRenderer.CameraAngle = Calc.AngleLerp(PortalRenderer.CameraAngle, PortalRenderer.CameraTargetAngle, 0.05f);
    }

    public override void Render(Scene scene)
    {
        if(PortalRenderer.IsRenderingPortals) {
            // if(PortalRenderer.DebugCounter % 12 == 0)
            //     Logger.Log(nameof(PortalRenderHelperModule), $"pos mid:    {(scene as Level).Camera.Position}");
            Engine.Instance.GraphicsDevice.SetRenderTarget(PortalRenderer.RenderTarget);
            PortalRenderer.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, PortalRenderer.StenciledCopy, RasterizerState.CullNone);
            PortalRenderer.Batch.Draw(GameplayBuffers.Level, Vector2.Zero, Color.White);
            PortalRenderer.Batch.End();
            Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Level);
        } else {
            Draw.SpriteBatch.Draw(PortalRenderer.RenderTarget, Vector2.Zero, Color.White);
        }
    }
}
