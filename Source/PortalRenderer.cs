using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.PortalRenderHelper;

[CustomEntity("PortalRenderHelper/PortalRenderPoly")]
[Tracked]
public class PortalRenderPoly : Entity {
    public PortalRenderPoly(EntityData data, Vector2 offset) : base(data.Position + offset) {
        RenderOffset = data.Nodes[0] - Position;
        RenderPoly = new VertexPositionColor[data.Nodes.Length];
        RenderPoly[0].Position = new Vector3(Position.X, Position.Y, 0);

        RenderPolyIndices = new int[(RenderPoly.Length-2)*3];
        for(int i = 1; i < RenderPoly.Length; ++i) {
            if(i < RenderPoly.Length-1) {
                RenderPolyIndices[3*i-3] = 0;
                RenderPolyIndices[3*i-2] = i;
                RenderPolyIndices[3*i-1] = i+1;
            }
            RenderPoly[i].Position = new(data.Nodes[i].X, data.Nodes[i].Y, 0);
        }

        closed = data.Bool("closed", false);
        flag = data.Attr("flag");
        invertFlag = data.Bool("invert");
    }

    public Vector2 RenderOffset;
    public VertexPositionColor[] RenderPoly;
    public int[] RenderPolyIndices;
    public bool closed;
    public string flag;
    public bool invertFlag;
}

public class PortalRenderEffect : Backdrop {
    public const string FXName = "PortalRenderHelper/PortalRenderEffect";

    public static VirtualRenderTarget RenderTarget {get; set;} = null;
    public static SpriteBatch Batch {get; set;} = null;

    public override void Ended(Scene scene)
    {
        // RenderTarget?.Dispose();
        // Batch?.Dispose();
    }

    public static bool IsRenderingPortals {get; set;} = false;
    public static readonly DepthStencilState StenciledCopy = new() {
        StencilEnable = true,
        DepthBufferEnable = false,
        StencilFunction = CompareFunction.NotEqual,
        ReferenceStencil = 0,
    };
    public static readonly DepthStencilState MakeStencil = new() {
        StencilEnable = true,
        DepthBufferEnable = false,
        StencilPass = StencilOperation.Invert,
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
            RenderTarget ??= VirtualContent.CreateRenderTarget(FXName, 320, 180, true);
            Batch ??= new SpriteBatch(self.GraphicsDevice);

            self.GraphicsDevice.SetRenderTarget(RenderTarget);
            self.GraphicsDevice.Clear(Color.Transparent);

            IsRenderingPortals = true;

            foreach(PortalRenderPoly poly in level.Tracker.GetEntities<PortalRenderPoly>()) {
                self.GraphicsDevice.SetRenderTarget(RenderTarget);
                self.GraphicsDevice.Clear(ClearOptions.Stencil, Color.White, 0, 0);
                self.GraphicsDevice.DepthStencilState = MakeStencil;
                GFX.DrawIndexedVertices(level.Camera.Matrix, poly.RenderPoly, poly.RenderPoly.Length, poly.RenderPolyIndices, poly.RenderPoly.Length-2);
                self.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
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

    public override void Render(Scene scene)
    {
        if(RenderTarget == null || Batch == null) return;
        if(IsRenderingPortals) {
            Engine.Instance.GraphicsDevice.SetRenderTarget(RenderTarget);
            Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, StenciledCopy, RasterizerState.CullNone);
            Batch.Draw(GameplayBuffers.Level, Vector2.Zero, Color.White);
            Batch.End();
            Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Level);
        } else {
            Draw.SpriteBatch.Draw(RenderTarget, Vector2.Zero, Color.White);
        }
    }
}
