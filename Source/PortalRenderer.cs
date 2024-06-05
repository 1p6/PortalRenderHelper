using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

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

    public static void OnRenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        if(self.scene is Level level) {

            self.GraphicsDevice.SetRenderTarget(PortalRenderer.RenderTarget);
            self.GraphicsDevice.Clear(Color.Transparent);

            IsRenderingPortals = true;

            foreach(PortalRenderPoly poly in level.Tracker.GetEntities<PortalRenderPoly>()) {
                poly.SetStencil();

                self.GraphicsDevice.SetRenderTarget(null);
                Vector2 origPos = level.Camera.Position;
                level.Camera.Position += poly.RenderOffset;
                DoPartialLevelRender(level);
                level.Camera.Position = origPos;
            }

            IsRenderingPortals = false;
        }
        orig(self);
    }

    public static void DrawPoly<T>(Matrix matrix, T[] vertices, int[] indices, DepthStencilState depth, RasterizerState raster) where T : struct, IVertexType {
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

        RenderOffset = data.Nodes[0] - Position;
        RenderPoly = new VertexPositionColor[data.Nodes.Length + (Closed ? 0 : 2)];
        RenderPoly[0].Position = new Vector3(Position, 0);
        for(int i = 1; i < data.Nodes.Length; ++i) {
            RenderPoly[i].Position = new(data.Nodes[i], 0);
        }

        RenderPolyIndices = new int[(RenderPoly.Length-2)*3];
        for(int i = 0; i < RenderPoly.Length-2; ++i) {
            RenderPolyIndices[3*i] = 0;
            RenderPolyIndices[3*i+1] = i+1;
            RenderPolyIndices[3*i+2] = i+2;
        }
    }

    public Vector2 RenderOffset;
    public VertexPositionColor[] RenderPoly;
    public int[] RenderPolyIndices;
    public bool Closed;
    public string Flag;
    public bool InvertFlag;

    public void SetStencil() {
        Level level = SceneAs<Level>();
        if(!Closed) {
            Player p = level.Tracker.GetEntity<Player>();
            Vector3 playerPos = new(p.Position - Vector2.UnitY * (p.Ducking ? 4f : 7.5f), 0);
            // hopefully this is large enough
            float farAway = 1000.0f;
            RenderPoly[^2].Position = playerPos + (RenderPoly[^3].Position - playerPos) * farAway;
            RenderPoly[^1].Position = playerPos + (RenderPoly[0].Position - playerPos) * farAway;
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
    }

    public override void Render(Scene scene)
    {
        if(PortalRenderer.IsRenderingPortals) {
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
