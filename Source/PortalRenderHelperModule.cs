using System;
using System.Collections.Generic;
using Monocle;
using MonoMod.Core;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.PortalRenderHelper;

public class PortalRenderHelperModule : EverestModule {
    public static PortalRenderHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(PortalRenderHelperModuleSettings);
    public static PortalRenderHelperModuleSettings Settings => (PortalRenderHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(PortalRenderHelperModuleSession);
    public static PortalRenderHelperModuleSession Session => (PortalRenderHelperModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(PortalRenderHelperModuleSaveData);
    public static PortalRenderHelperModuleSaveData SaveData => (PortalRenderHelperModuleSaveData) Instance._SaveData;

    public PortalRenderHelperModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(PortalRenderHelperModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(PortalRenderHelperModule), LogLevel.Info);
#endif
    }

    public static List<IDisposable> Hooks = new();

    public override void Load() {
        // TODO: apply any hooks that should always be active
        Everest.Events.Level.OnLoadBackdrop += OnLoadBackdrop;
        On.Monocle.Engine.RenderCore += PortalRenderer.OnRenderCore;
        // On.Celeste.Level.EnforceBounds += RelativeTeleportTrigger.DisableBounds;
        On.Monocle.Camera.UpdateMatrices += PortalRenderer.UpdateMatrices;
        On.Celeste.Actor.MoveHExact += RelativeTeleportTrigger.HookMoveHExact;
        On.Celeste.Actor.MoveVExact += RelativeTeleportTrigger.HookMoveVExact;
        IL.Celeste.TalkComponent.TalkComponentUI.Render += PortalRenderer.HookTalkComponent;

        Hooks.Add(new Hook(typeof(Camera).GetProperty("Left").GetGetMethod(), PortalRenderer.CameraGetLeft));
        Hooks.Add(new Hook(typeof(Camera).GetProperty("Right").GetGetMethod(), PortalRenderer.CameraGetRight));
        Hooks.Add(new Hook(typeof(Camera).GetProperty("Top").GetGetMethod(), PortalRenderer.CameraGetTop));
        Hooks.Add(new Hook(typeof(Camera).GetProperty("Bottom").GetGetMethod(), PortalRenderer.CameraGetBottom));
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
        Everest.Events.Level.OnLoadBackdrop -= OnLoadBackdrop;
        On.Monocle.Engine.RenderCore -= PortalRenderer.OnRenderCore;
        // On.Celeste.Level.EnforceBounds -= RelativeTeleportTrigger.DisableBounds;
        On.Monocle.Camera.UpdateMatrices -= PortalRenderer.UpdateMatrices;
        On.Celeste.Actor.MoveHExact -= RelativeTeleportTrigger.HookMoveHExact;
        On.Celeste.Actor.MoveVExact -= RelativeTeleportTrigger.HookMoveVExact;
        IL.Celeste.TalkComponent.TalkComponentUI.Render -= PortalRenderer.HookTalkComponent;

        Hooks.ForEach(x => x.Dispose());
    }

    public static Backdrop OnLoadBackdrop(MapData map, BinaryPacker.Element child, BinaryPacker.Element above)
    {
        if(child.Name.Equals(PortalRenderEffect.FXName, StringComparison.InvariantCultureIgnoreCase)) {
            return new PortalRenderEffect();
        }
        return null;
    }
}