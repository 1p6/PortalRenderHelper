using System;
using Monocle;

namespace Celeste.Mod.PortalRenderHelper;

/*
This struct represents a connected set of angles measured in radians.
See the Contains function for how membership works.
Start and End are arbitrary reals, and an angle is considered inside the interval if there is some multiple of 2*pi that can be added to it that brings it inside the range (Start, End).
As for how Length, emptiness, and fullness work, if Start >= End, then obviously no angle can be brought in the range (Start, End), so the interval is empty. If Start + 2*pi <= End, then the range spans a full 2*pi radians, so there will always be a way to put an angle inside it, making the interval full. As for other intervals, the length is then always End-Start.
This representation allows for intervals less than half a circle, and for intervals greater than half a circle, to both be represented by the struct.
*/
public record struct AngleInterval(float Start, float End) {
    public static readonly AngleInterval EMPTY = new(0, 0);
    public static readonly AngleInterval FULL = new(0, 7);
    public readonly float Length => MathF.Min(MathF.PI*2f, Math.Max(0f, End - Start));
    public readonly bool IsEmpty => End-Start <= 0.001f;
    public readonly bool IsFull => End-Start >= MathF.PI*2f-0.01;

    // Note: Both intersection and union can result in sets consisting of two disconnected intervals. In these cases, these functions will return the smallest interval that contains both of the parts of the actual result.
    public readonly AngleInterval Intersect(AngleInterval other) {
        if(IsEmpty || other.IsEmpty) return EMPTY;
        if(IsFull) return other;
        if(other.IsFull) return this;
        AngleInterval usToThem = new(Start, Calc.WrapAngle(other.End-Start-MathF.PI)+MathF.PI+Start);
        AngleInterval themToUs = new(other.Start, Calc.WrapAngle(End-other.Start-MathF.PI)+MathF.PI+other.Start);
        return (
            Contains(other.Start, 0.001f),
            Contains(other.End, 0.001f),
            other.Contains(Start, 0.001f)
        ) switch {
            (false, false, false) => EMPTY,
            (false, false, true) => this,
            (true, true, false) => other,
            (true, false, _) => themToUs,
            (false, true, _) => usToThem,
            (true, true, true) => usToThem.Length + themToUs.Length < 0.001 ? EMPTY : (Length < other.Length ? this : other),

            // See comment in Union for why the following seemingly impossible cases get resolved by flipping the third boolean of the tuple.

            // (false, true, false) => throw new Exception($"impossible angle interval case FTF! {this.Start} {this.End} ; {other.Start} {other.End}"),
            // (true, false, true) => throw new Exception($"impossible angle interval case TFT! {this.Start} {this.End} ; {other.Start} {other.End}"),
        };
    }
    public readonly AngleInterval Union(AngleInterval other) {
        if(IsFull || other.IsFull) return FULL;
        if(IsEmpty) return other;
        if(other.IsEmpty) return this;
        AngleInterval usToThem = new(Start, Calc.WrapAngle(other.End-Start-MathF.PI)+MathF.PI+Start);
        AngleInterval themToUs = new(other.Start, Calc.WrapAngle(End-other.Start-MathF.PI)+MathF.PI+other.Start);
        return (
            Contains(other.Start, -0.001f),
            Contains(other.End, -0.001f),
            other.Contains(Start, -0.001f)
        ) switch {
            (false, false, false) => usToThem.Length < themToUs.Length ? usToThem : themToUs,
            (false, false, true) => other,
            (true, true, false) => this,
            (true, false, _) => usToThem,
            (false, true, _) => themToUs,
            (true, true, true) => FULL,

            // after further consideration, the impossible cases below occur due to Start either equaling other.Start or other.End, in which case there will be a mismatch between one of the first two bools and the third bool in the tuple. so in general, we can ignore the third bool to resolve the conflict.

            // the following seemingly impossible case occurs when this.Start == other.Start, so just return the larger of the two.
            // (false, true, false) => Length < other.Length ? other : this,

            // (false, true, false) => throw new Exception($"impossible angle interval case FTF! {this.Start} {this.End} ; {other.Start} {other.End}"),
            // (true, false, true) => throw new Exception($"impossible angle interval case TFT! {this.Start} {this.End} ; {other.Start} {other.End}"),
        };
    }
    public readonly AngleInterval Complement { get {
        if(IsFull) return EMPTY;
        if(IsEmpty) return FULL;
        return new(End, Start + MathF.PI*2f);
    }}

    // Positive tolerance shrinks the interval, negative grows it
    public readonly bool Contains(float angle, float tolerance) {
        return Calc.WrapAngle(angle - Start - MathF.PI - tolerance) + MathF.PI < End - Start - tolerance*2f;
    }
}
