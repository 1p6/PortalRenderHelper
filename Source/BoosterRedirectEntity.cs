using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.PortalRenderHelper;

[CustomEntity("PortalRenderHelper/BoosterRedirect")]
class BoosterRedirect : Trigger {
    public Image Arrow;
    public Hitbox RedirHitbox;
    public float Angle;
    public VertexLight Light;
    public BoosterRedirect(EntityData data, Vector2 offset)
            : base(data, offset) {
        Visible = true;
        Depth = 10000;
        MTexture baseImg = GFX.Game["Flynx/PortalRenderHelper/objects/BoosterRedirect/base"];
        for(int y = 0; y < Height/8; ++y) {
            for(int x = 0; x < Width/8; ++x) {
                Image img = new(baseImg.GetSubtexture(
                    x == 0 ? 0 : (x < Width/8-1 ? 8 : 16),
                    y == 0 ? 0 : (y < Height/8-1 ? 8 : 16),
                    8, 8
                ));
                img.Position = new Vector2(x, y)*8;
                Add(img);
            }
        }
        Arrow = new(GFX.Game["Flynx/PortalRenderHelper/objects/BoosterRedirect/arrow"]);
        Arrow.CenterOrigin();
        Arrow.Position = new(Width/2, Height/2);
        Arrow.Rotation = Angle = data.Float("direction")/180*MathF.PI;
        Add(Arrow);
        RedirHitbox = new(Width-14, Height-14, 7, 7);
        RedirHitbox.Added(this);
        Add(Light = new VertexLight(new(8, 8), Color.White, 1.0f, 12, 16));
        Add(Light = new VertexLight(new(Width-8, 8), Color.White, 1.0f, 12, 16));
        Add(Light = new VertexLight(new(8, Height-8), Color.White, 1.0f, 12, 16));
        Add(Light = new VertexLight(new(Width-8, Height-8), Color.White, 1.0f, 12, 16));
    }

    public const int PLAYER_DASH_STATE = 2;
    public const int PLAYER_RED_DASH_STATE = 5;

    public override void OnStay(Player player)
    {
        base.OnStay(player);
        switch(player.StateMachine.State) {
            case PLAYER_DASH_STATE:
                Vector2 aim = player.DashDir;
                if(aim.LengthSquared() > 0.01) {
                    Angle = aim.Angle();
                }
                break;
            case PLAYER_RED_DASH_STATE:
                if(!RedirHitbox.Collide(player.Collider)) break;
                if(Calc.AbsAngleDiff(Angle, player.DashDir.Angle()) < 0.01) break;
                player.Speed = Calc.AngleToVector(Angle, player.Speed.Length());
                player.DashDir = Calc.AngleToVector(Angle, player.DashDir.Length());
                if(MathF.Abs(player.DashDir.X) > 0.05)
                    player.Facing = (Facings) MathF.Sign(player.DashDir.X);
                player.NaiveMove(player.Center.Clamp(Left+8, Top+8, Right-8, Bottom-8) - player.Center);

                break;
        }
    }

    public override void Update()
    {
        base.Update();
        Arrow.Rotation = Calc.AngleLerp(Arrow.Rotation, Angle, 6*Engine.DeltaTime);
    }

    public override void DebugRender(Camera camera)
    {
        base.DebugRender(camera);
        RedirHitbox.Render(camera, Color.Purple);
    }
}
