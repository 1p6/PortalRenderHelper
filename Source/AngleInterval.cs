using System;

namespace Celeste.Mod.PortalRenderHelper;

/*
This struct represents a connected set of angles measured in integer multiples of 2*pi / DEG_CIRCLE radians.
See the Contains function for how membership works.
Start is the starting angle, and the interval extends from that angle until Start + Length.
Negative lengths correspond to empty intervals, and a length >= DEG_CIRCLE corresponds to a full circle.
This representation allows for intervals less than half a circle, and for intervals greater than half a circle, to both be represented by the struct.
*/
public readonly record struct AngleInterval(int Start, int Length) {
    public readonly int Start = Rem(Start, DEG_CIRCLE);
    public readonly int Length = Math.Clamp(Length, 0, DEG_CIRCLE);

    /*
    1 px / 320 px (aka viewport width) = 0.003 radians
    2*pi / 0.003 = 2010.6 deg per circle
    going with double that, just in case we need the precision
    */
    public const int DEG_CIRCLE = 1 << 12;
    public static readonly AngleInterval EMPTY = new(0, 0);
    public static readonly AngleInterval FULL = new(0, DEG_CIRCLE);
    public readonly bool IsEmpty => Length <= 0;
    public readonly bool IsFull => Length >= DEG_CIRCLE;
    public readonly int End => Start + Length;

    // Note: Both intersection and union can result in sets consisting of two disconnected intervals. In these cases, these functions will return the smallest interval that contains both of the parts of the actual result.
    public readonly AngleInterval Intersect(AngleInterval other) {
        if(IsEmpty || other.IsEmpty) return EMPTY;
        if(IsFull) return other;
        if(other.IsFull) return this;
        AngleInterval usToThem = new(Start, Rem(other.End-Start-1, DEG_CIRCLE)+1);
        AngleInterval themToUs = new(other.Start, Rem(End-other.Start-1, DEG_CIRCLE)+1);
        return (
            Contains(other.Start),
            Contains(other.End-1),
            other.Contains(Start)
        ) switch {
            (false, false, false) => EMPTY,
            (false, false, true) => this,
            (true, true, false) => other,
            (true, false, _) => themToUs,
            (false, true, _) => usToThem,
            (true, true, true) => Length < other.Length ? this : other,

            // See comment in Union for why the following seemingly impossible cases get resolved by flipping the third boolean of the tuple.

            // (false, true, false) => throw new Exception($"impossible angle interval case FTF! {this.Start} {this.End} ; {other.Start} {other.End}"),
            // (true, false, true) => throw new Exception($"impossible angle interval case TFT! {this.Start} {this.End} ; {other.Start} {other.End}"),
        };
    }
    public readonly AngleInterval Union(AngleInterval other) {
        if(IsFull || other.IsFull) return FULL;
        if(IsEmpty) return other;
        if(other.IsEmpty) return this;
        AngleInterval usToThem = new(Start, Rem(other.End-Start-1, DEG_CIRCLE)+1);
        AngleInterval themToUs = new(other.Start, Rem(End-other.Start-1, DEG_CIRCLE)+1);
        return (
            Contains(other.Start),
            Contains(other.End-1),
            other.Contains(Start)
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
    public readonly AngleInterval Complement => new(Start + Length, DEG_CIRCLE - Length);

    public readonly bool Contains(int angle) {
        return Rem(angle - Start, DEG_CIRCLE) < Length;
    }
    public override string ToString() {
        return $"AngleInterval({Start}, {Length})";
    }

    public static int Rem(int a, int b) {
        int res = a % b;
        // could replace this with a & (b-1) if b is a power of two
        return res < 0 ? res + b : res;
    }

    public static int RadToDeg(float radians) {
        return (int)Math.Round(radians / (2*MathF.PI) * DEG_CIRCLE);
    }

    public static void TestCases() {
        AngleInterval a = new(-50, 300);
        AngleInterval b = new(190, 300);
        Logger.Log(nameof(PortalRenderHelperModule), $"case 1 {a.Intersect(b)}");
    }
}
