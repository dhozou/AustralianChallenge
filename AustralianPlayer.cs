using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace AustralianChallenge
{
	public class AustralianPlayer : ModPlayer
	{
		public override bool Autoload(ref string name)
		{
			IL.Terraria.Player.Update += HookUpdate;
			return base.Autoload(ref name);
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
	}
}
