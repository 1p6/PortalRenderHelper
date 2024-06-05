using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.PortalRenderHelper;

[CustomEntity("PortalRenderHelper/RelativeTeleportTrigger")]
public class RelativeTeleportTrigger : Trigger {
    public RelativeTeleportTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        TeleportOffset = data.Nodes[0] - Position;
        enableFlag = data.Attr("flag");
        invertFlag = data.Bool("invert");
    }

    public Vector2 TeleportOffset;
    public string enableFlag;
    public bool invertFlag;

    public void DoTeleport(Player player) {
        Audio.Play("event:/game/general/diamond_touch");
        Vector2 preCameraTarget = player.CameraTarget;
        player.Position += TeleportOffset;
        Vector2 postCameraTarget = player.CameraTarget;
        player.level.Camera.Position += postCameraTarget - preCameraTarget;
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
