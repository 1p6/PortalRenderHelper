using System;
using Monocle;

namespace Celeste.Mod.PortalRenderHelper;

public record struct AngleInterval(float Start, float End) {
    public static readonly AngleInterval EMPTY = new(0, 0);
    public readonly float Length => End - Start;
    public readonly bool IsEmpty => Length <= 0f;
    
    public readonly AngleInterval Intersect(AngleInterval other) {
        if(IsEmpty) return this;
        if(other.IsEmpty) return other;
        float start;
        if(Contains(other.Start)) start = other.Start;
        else if(other.Contains(Start)) start = Start;
        else return EMPTY;
        float end;
        if(Contains(other.End)) end = other.End;
        else if(other.Contains(End)) end = End;
        else return EMPTY;
        return new(); // TODO
    }
    public readonly AngleInterval Union(AngleInterval other) {
        if(IsEmpty) return other;
        if(other.IsEmpty) return this;
        return new(); // TODO
    }
    public readonly bool Contains(float angle) {
        return Calc.WrapAngle(angle - Start - MathF.PI) + Math.PI < End - Start;
    }
}
