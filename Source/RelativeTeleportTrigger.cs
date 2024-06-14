using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.PortalRenderHelper;

[CustomEntity("PortalRenderHelper/RelativeTeleportTrigger")]
[Tracked]
public class RelativeTeleportTrigger : Trigger {
    public RelativeTeleportTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Target = data.Nodes[0] + offset;
        TeleportOffset = Target - Position;
        enableFlag = data.Attr("flag");
        invertFlag = data.Bool("invert");
        Angle = data.Float("angle") / 180f * (float) Math.PI;
        ChangeTargetAngle = data.Bool("changeTargetAngle", true);
    }

    public Vector2 TeleportOffset;
    public Vector2 Target;
    public float Angle;
    public bool ChangeTargetAngle;
    public string enableFlag;
    public bool invertFlag;

    // public static bool CurrentlyTeleporting {get; private set;} = false;

    // public static void DisableBounds(On.Celeste.Level.orig_EnforceBounds orig, Level self, Player player){
    //     if(CurrentlyTeleporting) return;
    //     orig(self, player);
    // }

    public static bool HookMoveHExact(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int moveH, Collision onCollide, Solid pusher) {
        // TODO: implement teleports for all actors
        if(self is not Player) return orig(self, moveH, onCollide, pusher);
        // cancels collision if we end up in a relative teleport trigger
        bool teleCollide = false;
        bool res = orig(self, moveH, (hitData) => {
            if(self.CollideCheck<RelativeTeleportTrigger>())
                teleCollide = true;
            else onCollide?.Invoke(hitData);
        }, pusher);
        return res && !teleCollide;
    }
    public static bool HookMoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int moveH, Collision onCollide, Solid pusher) {
        if(self is not Player) return orig(self, moveH, onCollide, pusher);
        bool teleCollide = false;
        bool res = orig(self, moveH, (hitData) => {
            if(self.CollideCheck<RelativeTeleportTrigger>())
                teleCollide = true;
            else onCollide?.Invoke(hitData);
        }, pusher);
        return res && !teleCollide;
    }

    public void DoTeleport(Player player) {
        // Audio.Play("event:/game/general/diamond_touch");
        // Vector2 preCameraTarget = player.CameraTarget;
        // Logger.Log(nameof(PortalRenderHelperModule), "teleport!");
        player.level.OnEndOfFrame += delegate {
            Vector2 oldPos = player.Position;
            Vector2 oldCounter = player.movementCounter;
            player.NaiveMove(Target + (player.Center - Position).Rotate(Angle) - player.Center);
            if(player.CollideCheck<Solid>()) {
                // cancel teleport if something is in the way
                player.Position = oldPos;
                player.movementCounter = oldCounter;
                return;
            }
            // Logger.Log(nameof(PortalRenderHelperModule), $"player speed before: {player.Speed}");
            player.Speed = player.Speed.Rotate(Angle);
            // Logger.Log(nameof(PortalRenderHelperModule), $"player speed after: {player.Speed}");
            // Logger.Log(nameof(PortalRenderHelperModule), $"pos before: {player.level.Camera.Position}");
            // there was an issue with how the camera was appearing, seems it's more an issue in the base game itself having to do with the player entering and immediately entering a camera target trigger
            // NVM LOL I guess I did the math wrong at some point? anyway, since the camera target trigger points to the center of the screen, and the screen is 180 pixels / 22.5 tiles tall, it should point at 22.5/2 or 11.25 tiles below the top of the level. I forgot the 0.25 tiles / 2 pixels when positioning the camera target.
            // player.level.Camera.Position = player.level.Camera.Position.Round() + TeleportOffset;
            // Logger.Log(nameof(PortalRenderHelperModule), $"target camera pos: {player.CameraTarget}");
            Vector2 cameraCenter = new(160, 90);
            player.level.Camera.Position = Target + (player.level.Camera.Position + cameraCenter - Position).Rotate(Angle) - cameraCenter;
            PortalRenderer.CameraAngle -= Angle;
            if(ChangeTargetAngle) PortalRenderer.CameraTargetAngle -= Angle;
            // player.CameraAnchor += TeleportOffset;
            // Logger.Log(nameof(PortalRenderHelperModule), $"pos after: {player.level.Camera.Position}");
            if(!player.level.Bounds.Contains(player.Collider.Bounds)) {
                LevelData mapdata = player.level.Session.MapData.GetAt(player.Position);
                // Logger.Log(nameof(PortalRenderHelperModule), $"mapdata: {mapdata}");
                if(mapdata != null && mapdata != player.level.Session.LevelData) {
                    // from Level.NextLevel
                    foreach(TheoCrystal c in player.level.Tracker.GetEntities<TheoCrystal>()) {
                        // fix crash from theo crystals lol, there miiight be a better way, but whatever. why do theo crystals even have this tag? none of the other holdables do??
                        if(!c.Hold.IsHeld) c.RemoveTag(Tags.TransitionUpdate);
                    }
                    Engine.TimeRate = 1f;
                    Distort.Anxiety = 0f;
                    Distort.GameRate = 1f;
                    player.level.TransitionTo(mapdata, Vector2.Zero);
                }
            }
        };
        // player.Position += TeleportOffset;
        // player.level.Camera.Position += TeleportOffset;
        // if(!player.level.Bounds.Contains(player.Collider.Bounds)) {
        //     // by doing this level transition ourselves, we prevent level transition effects such as dying on the bottom edge, getting an upward boost on the top edge, etc.
        //     // disabling bounds enforcement for the remainder of the frame prevents the player update code from initiating the transition itself after seeing the position change.
        //     CurrentlyTeleporting = true;
        //     // player.EnforceLevelBounds = false;
        //     player.level.OnEndOfFrame += delegate {
        //         CurrentlyTeleporting = false;
        //         // player.EnforceLevelBounds = true;
        //     };
        //     player.level.NextLevel(player.Position, Vector2.Zero);
        // }
        // Vector2 postCameraTarget = player.CameraTarget;
        // player.level.Camera.Position += postCameraTarget - preCameraTarget;
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        // if(enableFlag.Length == 0) {
        //     doTeleport(player);
        // } else {
        //     if(player.level.Session.GetFlag(enableFlag)) {
        //         doTeleport(player);
        //     } else {
        //         teleportOnFlagSet = true;
        //     }
        // }
    }

    public override void OnLeave(Player player)
    {
        base.OnLeave(player);
        // Logger.Log(nameof(PortalRenderHelperModule), "player leave!!");
        // Audio.Play("event:/game/general/diamond_return");
    }

    public override void OnStay(Player player)
    {
        base.OnStay(player);
        if(enableFlag.Length == 0 || (invertFlag != player.level.Session.GetFlag(enableFlag))) {
            DoTeleport(player);
        }
    }
}
