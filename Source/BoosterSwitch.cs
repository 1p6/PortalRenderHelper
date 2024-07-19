using System.Threading;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.PortalRenderHelper;

[CustomEntity("PortalRenderHelper/BoosterSwitch")]
public class BoosterSwitch : DashSwitch
{
    public BoosterSwitch(Vector2 position, Sides side, bool persistent, bool allGates, EntityID id, string spriteName) : base(position, side, persistent, allGates, id, spriteName) {}

    public BoosterSwitch(EntityData data, Vector2 offset, EntityID id)
            : this(
                data.Position+offset,
                data.Enum<Sides>("side"),
                data.Bool("persistent"),
                data.Bool("allGates"),
                id, data.Attr("sprite", "default")
            ) {
        sprite.Color = data.HexColor("color", new Color(0xff, 0xbb, 0xbb));
    }
    
    public static DashCollisionResults HookOnDashed(On.Celeste.DashSwitch.orig_OnDashed orig, DashSwitch self, Player player, Vector2 direction) {
        if(self is BoosterSwitch && (player == null || player.StateMachine.State != BoosterRedirect.PLAYER_RED_DASH_STATE))
            return DashCollisionResults.NormalCollision;
        return orig(self, player, direction);
    }
    public static void HookUpdate(ILContext ctx) {
        ILCursor cur = new(ctx);
        if(cur.TryGotoNext(MoveType.After, insn => insn.MatchCallvirt<Player>("get_Holding"), insn => insn.MatchBrfalse(out _))) {
            // if we're a boosterswitch, pretend the player isn't holding anything. normally standing on a dash switch whiling holding something causes the switch to be fully pressed. we want it to instead just partially press it without fully triggering the switch.
            cur.Prev.MatchBrfalse(out var label);
            cur.EmitLdarg0();
            cur.EmitIsinst(typeof(BoosterSwitch));
            cur.EmitBrtrue(label);
        }
    }
}
