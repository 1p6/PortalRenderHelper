using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.PortalRenderHelper;

[CustomEntity("PortalRenderHelper/RelativeTeleportTrigger")]
public class RelativeTeleportTrigger : Trigger {
    public RelativeTeleportTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        TeleportOffset = data.Nodes[0] + offset - Position;
        enableFlag = data.Attr("flag");
        invertFlag = data.Bool("invert");
    }

    public Vector2 TeleportOffset;
    public string enableFlag;
    public bool invertFlag;

    // public static bool CurrentlyTeleporting {get; private set;} = false;

    // public static void DisableBounds(On.Celeste.Level.orig_EnforceBounds orig, Level self, Player player){
    //     if(CurrentlyTeleporting) return;
    //     orig(self, player);
    // }

    public void DoTeleport(Player player) {
        // Audio.Play("event:/game/general/diamond_touch");
        // Vector2 preCameraTarget = player.CameraTarget;
        // Logger.Log(nameof(PortalRenderHelperModule), "teleport!");
        player.level.OnEndOfFrame += delegate {
            player.Position += TeleportOffset;
            // Logger.Log(nameof(PortalRenderHelperModule), $"pos before: {player.level.Camera.Position}");
            // there was an issue with how the camera was appearing, seems it's more an issue in the base game itself having to do with the player entering and immediately entering a camera target trigger
            // NVM LOL I guess I did the math wrong at some point? anyway, since the camera target trigger points to the center of the screen, and the screen is 180 pixels / 22.5 tiles tall, it should point at 22.5/2 or 11.25 tiles below the top of the level. I forgot the 0.25 tiles / 2 pixels when positioning the camera target.
            // player.level.Camera.Position = player.level.Camera.Position.Round() + TeleportOffset;
            // Logger.Log(nameof(PortalRenderHelperModule), $"target camera pos: {player.CameraTarget}");
            player.level.Camera.Position += TeleportOffset;
            // player.CameraAnchor += TeleportOffset;
            // Logger.Log(nameof(PortalRenderHelperModule), $"pos after: {player.level.Camera.Position}");
            if(!player.level.Bounds.Contains(player.Collider.Bounds)) {
                LevelData mapdata = player.level.Session.MapData.GetAt(player.Position);
                // Logger.Log(nameof(PortalRenderHelperModule), $"mapdata: {mapdata}");
                if(mapdata != null && mapdata != player.level.Session.LevelData) {
                    // from Level.NextLevel
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
