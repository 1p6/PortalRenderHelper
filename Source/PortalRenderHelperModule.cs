using System;

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

    public override void Load() {
        // TODO: apply any hooks that should always be active
        Everest.Events.Level.OnLoadBackdrop += OnLoadBackdrop;
        On.Monocle.Engine.RenderCore += PortalRenderEffect.OnRenderCore;
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
        Everest.Events.Level.OnLoadBackdrop -= OnLoadBackdrop;
        On.Monocle.Engine.RenderCore -= PortalRenderEffect.OnRenderCore;
    }

    public static Backdrop OnLoadBackdrop(MapData map, BinaryPacker.Element child, BinaryPacker.Element above)
    {
        if(child.Name.Equals(PortalRenderEffect.FXName, StringComparison.InvariantCultureIgnoreCase)) {
            return new PortalRenderEffect();
        }
        return null;
    }
}