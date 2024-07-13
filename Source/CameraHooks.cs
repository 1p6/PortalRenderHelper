using System;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.PortalRenderHelper;

public static class CameraHooks {

    public static float CameraTargetAngle { get; set; } = 0f;
    public static Vector2 _CameraMinBound = new(0,0);
    public static Vector2 _CameraMaxBound = new(320, 180);
    public static Vector2 CameraMinBound {
        get {
            Scene s = Engine.Instance.scene;
            if(s is Level l && l.Camera != null) TryUpdateCameraBounds(l.Camera);
            return _CameraMinBound;
        }
    }
    public static Vector2 CameraMaxBound {
        get {
            Scene s = Engine.Instance.scene;
            if(s is Level l && l.Camera != null) TryUpdateCameraBounds(l.Camera);
            return _CameraMaxBound;
        }
    }
    public static bool _CameraBoundsChanged = true;


    public static float _CameraAngle = 0f;
    public static float CameraAngle {
        get => _CameraAngle;
        set {
            _CameraAngle = value;
            Scene s = Engine.Instance.scene;
            if(s is Level l && l.Camera != null) l.Camera.changed = true;
        }
    }


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
                min = Vector2.Min(min, corners[i]);
                max = Vector2.Max(max, corners[i]);
            }
            _CameraMinBound = min;
            _CameraMaxBound = max;
            _CameraBoundsChanged = false;
        }
    }
    public static float CameraGetLeft(Camera self) {
        return CameraMinBound.X;
    }
    public static float CameraGetRight(Camera self) {
        return CameraMaxBound.X;
    }
    public static float CameraGetTop(Camera self) {
        return CameraMinBound.Y;
    }
    public static float CameraGetBottom(Camera self) {
        return CameraMaxBound.Y;
    }

    public static void HookTalkComponent(ILContext ctx) {
        ILCursor cur = new(ctx);
        if(cur.TryGotoNext(MoveType.Before, instr => instr.MatchStloc2())) {
            cur.EmitPop().EmitLdarg0().EmitLdloc1();
            cur.EmitDelegate(ModifyTalkComponentPos);
        }
    }
    public static Vector2 ModifyTalkComponentPos(TalkComponent.TalkComponentUI self, Vector2 camPos) {
        Vector2 center = new(160, 90);
        return center + (self.Handler.Entity.Position - camPos - center).Rotate(CameraAngle) + self.Handler.DrawAt;
    }

    public static void HookInput(On.Monocle.MInput.orig_UpdateVirtualInputs orig) {
        orig();
        var settings = PortalRenderHelperModule.Settings;

        // feather movement always uses same controls as walking
        Vector2 rotatedFeather = Input.Feather.Value.Rotate(-CameraAngle);

        if(settings.InputSettings.RotateWalk) {
            // attempt to match vanilla's varying thresholds for different direction movements
            Vector2 sign = rotatedFeather.Sign();
            Vector2 abs = rotatedFeather.Abs();
            Input.MoveX.Value = abs.X >= 0.3 ? (int)sign.X : 0;
            Input.GliderMoveY.Value = abs.Y >= 0.3 ? (int)sign.Y : 0;
            Input.MoveY.Value = abs.Y >= 0.7 ? (int)sign.Y : 0;
        }

        if(settings.InputSettings.RotateDash) Input.Aim.Value = Input.Aim.Value.Rotate(-CameraAngle);
        if(settings.InputSettings.RotateFeather) Input.Feather.Value = rotatedFeather;
    }
}
