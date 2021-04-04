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
		public override bool Autoload(ref string name)
		{
			IL.Terraria.Player.ctor += HookPlayer;
			IL.Terraria.Player.BordersMovement += HookBordersMovement;
			IL.Terraria.Player.Update += HookUpdate;
			IL.Terraria.Player.UpdateDead += HookUpdateDead;
			return base.Autoload(ref name);
		}

		private void HookPlayer(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(i => i.MatchStfld(typeof(Player), nameof(Player.gravDir)));
			c.Index--;
			c.Remove();
			c.Emit(OpCodes.Ldc_R4, -1f);
		}

		private void HookBordersMovement(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(i => i.MatchStfld(typeof(Player), nameof(Player.gravDir)));
			c.Index--;
			c.RemoveRange(2);
			c.EmitDelegate<Action<Player>>(player => {
				if (player.gravDir == -1f)
				{
					player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " was swallowed by the sky."), 10.0, 0);
				}
			});
		}

		private void HookUpdate(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(i => i.MatchCall(typeof(Player), nameof(Player.JumpMovement)));
			c.GotoPrev(i => i.MatchStfld(typeof(Player), nameof(Player.gravDir)));
			c.Index--;
			c.Remove();
			c.Emit(OpCodes.Ldc_R4, -1f);
		}

		private void HookUpdateDead(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(i => i.MatchStfld(typeof(Player), nameof(Player.gravDir)));
			c.Index--;
			c.Remove();
			c.Emit(OpCodes.Ldc_R4, -1f);
		}
	}
}
