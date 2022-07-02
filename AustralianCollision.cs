using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AustralianChallenge
{
	internal class AustralianCollision : ModSystem
	{
		public override void Load() {
			IL.Terraria.Collision.TileCollision += HookTileCollision;
		}

		private void HookTileCollision(ILContext il) {
			var arg = (
				fallThrough: (byte)4,
				fall2:       (byte)5,
				gravDir:     (byte)6
			);
			var loc = (
				x:          (byte)13,
				y:          (byte)14,
				tileHeight: (byte)15
			);

			// halve height of platform tiles
			{
				var c = new ILCursor(il);
				var label = default(ILLabel);
				c.GotoNext(
					i => i.MatchCall<Tile>("halfBrick"), // internal
					i => i.MatchBrfalse(out label));
				c.GotoLabel(label);
				c.Emit(OpCodes.Ldloc_S, loc.tileHeight);
				c.Emit(OpCodes.Ldloc_S, loc.x);
				c.Emit(OpCodes.Ldloc_S, loc.y);
				c.EmitDelegate<Func<int, int, int, int>>((tileHeight, x, y) => {
					if (TileID.Sets.Platforms[Main.tile[x, y].TileType])
						tileHeight -= 8;
					return tileHeight;
				});
				c.Emit(OpCodes.Stloc_S, loc.tileHeight);
			}

			// disable collision with solid-topped tiles while inverted
			{
				var c = new ILCursor(il);
				var label = default(ILLabel);
				c.GotoNext(MoveType.After,
					i => i.MatchBgtUn(out label),
					i => i.MatchLdcI4(1),
					i => i.MatchStsfld<Collision>(nameof(Collision.down)));
				c.Emit(OpCodes.Ldarg_S, arg.gravDir);
				c.Emit(OpCodes.Ldloc_S, loc.x);
				c.Emit(OpCodes.Ldloc_S, loc.y);
				c.EmitDelegate<Func<int, int, int, bool>>((gravDir, x, y) =>
					Main.tileSolidTop[Main.tile[x, y].TileType] && gravDir == -1f);
				c.Emit(OpCodes.Brtrue, label);
			}

			// add inverted platform collision logic
			{
				var c = new ILCursor(il);
				c.GotoNext(
					i => i.MatchBrtrue(out _),
					i => i.MatchLdcI4(1),
					i => i.MatchStsfld<Collision>(nameof(Collision.up)));
				c.Emit(OpCodes.Ldarg_S, arg.gravDir);
				c.Emit(OpCodes.Ldarg_S, arg.fallThrough);
				c.Emit(OpCodes.Ldarg_S, arg.fall2);
				c.Emit(OpCodes.Ldloc_S, loc.x);
				c.Emit(OpCodes.Ldloc_S, loc.y);
				c.EmitDelegate<Func<bool, int, bool, bool, int, int, bool>>((tileSolidTop, gravDir, fallThrough, fall2, x, y) =>
					tileSolidTop && (gravDir == 1f || !TileID.Sets.Platforms[Main.tile[x, y].TileType] || fallThrough || fall2));
			}
		}
	}
}
