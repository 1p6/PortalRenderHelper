using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.PortalRenderHelper;

[CustomEntity("PortalRenderHelper/SetCameraAngleTrigger")]
public class SetCameraAngleTrigger : Trigger {
    public SetCameraAngleTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        SetAngle = data.Bool("setAngle", false);
        SetTargetAngle = data.Bool("setTargetAngle", true);
        Angle = data.Float("angle") / 180f * (float) Math.PI;
        SetOnEnter = data.Bool("setOnEnter", true);
        SetOnExit = data.Bool("setOnExit", false);
        SetOnStay = data.Bool("setOnStay", false);
        SetOnLoad = data.Bool("setOnLoad", false);
        SetOnUnload = data.Bool("setOnUnload", false);
        Flag = data.Attr("flag");
        Invert = data.Bool("invert", false);
    }

    public bool SetAngle;
    public bool SetTargetAngle;
    public float Angle;
    public bool SetOnEnter;
    public bool SetOnExit;
    public bool SetOnStay;
    public bool SetOnLoad;
    public bool SetOnUnload;
    public string Flag;
    public bool Invert;

    public void SetCamAngle() {
        if(Flag.Length != 0 && Invert == SceneAs<Level>().Session.GetFlag(Flag)) return;

        if(SetAngle) PortalRenderer.CameraAngle = Angle;
        if(SetTargetAngle) PortalRenderer.CameraTargetAngle = Angle;
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        if(SetOnEnter) SetCamAngle();
    }

    public override void OnLeave(Player player)
    {
        base.OnLeave(player);
        if(SetOnExit) SetCamAngle();
    }

    public override void OnStay(Player player)
    {
        base.OnStay(player);
        if(SetOnStay) SetCamAngle();
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        if(SetOnLoad) SetCamAngle();
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        if(SetOnUnload) SetCamAngle();
    }
}
