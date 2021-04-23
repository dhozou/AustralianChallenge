using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AustralianChallenge
{
	public class AustralianPlayer : ModPlayer
	{
		public override bool Autoload(ref string name) {
			IL.Terraria.Player.BordersMovement += HookBordersMovement;
			IL.Terraria.Player.Spawn += HookSpawn;
			IL.Terraria.Player.Update += HookUpdate;
			IL.Terraria.Player.UpdateDead += HookUpdateDead;

			return base.Autoload(ref name);
		}

		private void HookBordersMovement(ILContext il) {
			var c = new ILCursor(il);
			c.GotoNext(
				i => i.MatchLdcR4(1f),
				i => i.MatchStfld(typeof(Player), nameof(Player.gravDir)));
			c.RemoveRange(2);
			c.EmitDelegate<Action<Player>>(player => {
				if (player.gravDir == -1f)
					player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " was swallowed by the sky."), 10.0, 0);
			});
		}

		private void HookSpawn(ILContext il) {
			var c = new ILCursor(il);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<Player>>(player => {
				player.gravDir = -1f;
			});
		}

		private void HookUpdate(ILContext il) {
			var c = new ILCursor(il);
			var loc = (
				ignorePlats: (byte)11,
				fallThrough: (byte)12
			);

			var label = default(ILLabel);
			c.GotoNext(
				i => i.MatchLdfld(typeof(Player), nameof(Player.gravControl2)),
				i => i.MatchBrfalse(out label));
			c.GotoLabel(label);
			c.GotoNext(
				i => i.MatchLdcR4(1),
				i => i.MatchStfld(typeof(Player), nameof(Player.gravDir)));
			c.RemoveRange(2);
			c.EmitDelegate<Action<Player>>(player => {
				player.gravDir = -1f;
			});

			c.GotoNext(MoveType.After,
				i => i.MatchLdfld(typeof(Player), nameof(Player.controlDown)),
				i => i.MatchStloc(loc.fallThrough),
				i => i.MatchLdarg(0),
				i => i.MatchLdfld(typeof(Player), nameof(Player.gravDir)),
				i => i.MatchLdcR4(-1f),
				i => i.MatchBeq(out _));
			c.Index -= 3;
			c.RemoveRange(3);
			c.EmitDelegate<Func<Player, bool>>(player => player.gravDir == -1f && player.velocity.Y > 0f);
			c.Emit(OpCodes.Stloc_S, loc.ignorePlats);
		}

		private void HookUpdateDead(ILContext il) {
			var c = new ILCursor(il);
			c.GotoNext(
				i => i.MatchLdcR4(1f),
				i => i.MatchStfld(typeof(Player), nameof(Player.gravDir)));
			c.RemoveRange(2);
			c.EmitDelegate<Action<Player>>(player => {
				player.gravDir = -1f;
			});
		}
	}
}
