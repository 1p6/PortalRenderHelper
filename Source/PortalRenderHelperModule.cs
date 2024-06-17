using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
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
        On.Monocle.Camera.UpdateMatrices += CameraHooks.UpdateMatrices;
        On.Celeste.Actor.MoveHExact += RelativeTeleportTrigger.HookMoveHExact;
        On.Celeste.Actor.MoveVExact += RelativeTeleportTrigger.HookMoveVExact;
        IL.Celeste.TalkComponent.TalkComponentUI.Render += CameraHooks.HookTalkComponent;
        IL.Monocle.Commands.Render += HookDebugConsoleRender;

        Hooks.Add(new Hook(typeof(Camera).GetProperty("Left").GetGetMethod(), CameraHooks.CameraGetLeft));
        Hooks.Add(new Hook(typeof(Camera).GetProperty("Right").GetGetMethod(), CameraHooks.CameraGetRight));
        Hooks.Add(new Hook(typeof(Camera).GetProperty("Top").GetGetMethod(), CameraHooks.CameraGetTop));
        Hooks.Add(new Hook(typeof(Camera).GetProperty("Bottom").GetGetMethod(), CameraHooks.CameraGetBottom));
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
        Everest.Events.Level.OnLoadBackdrop -= OnLoadBackdrop;
        On.Monocle.Engine.RenderCore -= PortalRenderer.OnRenderCore;
        // On.Celeste.Level.EnforceBounds -= RelativeTeleportTrigger.DisableBounds;
        On.Monocle.Camera.UpdateMatrices -= CameraHooks.UpdateMatrices;
        On.Celeste.Actor.MoveHExact -= RelativeTeleportTrigger.HookMoveHExact;
        On.Celeste.Actor.MoveVExact -= RelativeTeleportTrigger.HookMoveVExact;
        IL.Celeste.TalkComponent.TalkComponentUI.Render -= CameraHooks.HookTalkComponent;
        IL.Monocle.Commands.Render -= HookDebugConsoleRender;

        Hooks.ForEach(x => x.Dispose());
    }

    public static Backdrop OnLoadBackdrop(MapData map, BinaryPacker.Element child, BinaryPacker.Element above)
    {
        if(child.Name.Equals(PortalRenderEffect.FXName, StringComparison.InvariantCultureIgnoreCase)) {
            return new PortalRenderEffect(child);
        }
        return null;
    }

    public static void HookDebugConsoleRender(ILContext ctx) {
        ILCursor cur = new(ctx);
        if(cur.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt(typeof(SpriteBatch), "Begin"))) {
            cur.EmitLdloc(7);
            cur.EmitDelegate(AddDebugText);
            cur.EmitStloc(7);
        }
    }

    public static string AddDebugText(string text) {
        if(!Settings.EnableDebugInfo) return text;
        return $@"{text}
PortalRenderHelper:
 Allocated render targets: {RenderTargetPool.NumAllocdTargets}
 Level renders per frame: {PortalRenderer.LevelRenders}
 Max level renders per frame: {PortalRenderer.MaxLevelRenders}";
    }

    [Command("portal_render_helper_clear_cache", "Clears the render target pool and the max rendered levels counter")]
    public static void ClearCacheCommand() {
        RenderTargetPool.Clear();
        PortalRenderer.MaxLevelRenders = PortalRenderer.LevelRenders;
    }
}