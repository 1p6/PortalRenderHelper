namespace Celeste.Mod.PortalRenderHelper;

public class PortalRenderHelperModuleSettings : EverestModuleSettings {
    public bool EnableDebugInfo {get; set;} = false;

    [SettingRange(0,50,true)]
    public int MaxRecursionDepth {get; set;} = 50;

    public bool IgnoreMapRecursionLimits {get; set;} = false;
}