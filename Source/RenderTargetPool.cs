using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.PortalRenderHelper;

public static class RenderTargetPool {
    public static Stack<VirtualRenderTarget> _Targets = new();
    public static int NumTargets = 0;
    public static int NumActiveTargets = 0;
    public static int NumAllocdTargets = 0;

    public static VirtualRenderTarget Alloc() {
        NumActiveTargets++;
        if(_Targets.Count > 0) {
            return _Targets.Pop();
        }
        NumAllocdTargets++;
        return VirtualContent.CreateRenderTarget($"PortalRenderHelper/Portal{NumTargets++}", 320, 180, true);
    }

    public static void Free(VirtualRenderTarget target) {
        NumActiveTargets--;
        if(NumActiveTargets < 0) throw new Exception("double free from portal render target pool");
        _Targets.Push(target);
    }

    public static void Clear() {
        foreach(VirtualRenderTarget rt in _Targets) {
            rt.Dispose();
        }
        NumAllocdTargets -= _Targets.Count;
        _Targets.Clear();
    }
}
