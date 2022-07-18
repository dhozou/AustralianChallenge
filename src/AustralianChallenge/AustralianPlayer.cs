using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AustralianChallenge
{
	internal class AustralianPlayer : ModSystem
	{
		public override void Load() {
			IL.Terraria.GameContent.PlayerSleepingHelper.StartSleeping += il => {
				// preserve gravity while sleeping
				var c = new ILCursor(il);
				c.GotoNext(
					i => i.MatchLdarg(1),
					i => i.MatchLdcR4(1),
					i => i.MatchStfld<Player>(nameof(Player.gravDir)));
				c.RemoveRange(3);
			};

			IL.Terraria.Player.BordersMovement += il => {
				// kill players that fall into space
				var c = new ILCursor(il);
				c.GotoNext(
					i => i.MatchLdcR4(1f),
					i => i.MatchStfld<Player>(nameof(Player.gravDir)));
				c.RemoveRange(2);
				c.EmitDelegate<Action<Player>>(player => {
					if (player.gravDir == -1f)
						player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " was swallowed by the sky."), 10.0, 0);
				});
			};

			// WORKAROUND tModLoader/tModLoader#2724
			// can't directly hook Player.PlaceThing_Tiles_PlaceIt so add
			// this logic to the start of an existing nested method instead
			IL.Terraria.Player.PlaceThing_Tiles_PlaceIt_SpinSmartPlatform += il => {
				// place platforms upside down
				var c = new ILCursor(il);
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Action<Player>>(player => {
					var tileID = player.inventory[player.selectedItem].createTile;
					if (player.gravDir == 1f || tileID < 0 || !TileID.Sets.Platforms[tileID])
						return;
					WorldGen.PoundTile(Player.tileTargetX, Player.tileTargetY);
					if (Main.netMode == 1)
						NetMessage.SendData(msgType: MessageID.TileManipulation, number: 7, number2: Player.tileTargetX, number3: Player.tileTargetY, number4: 1f);
				});
			};

			IL.Terraria.Player.Spawn += il => {
				// invert gravity by default
				var c = new ILCursor(il);
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Action<Player>>(player => {
					player.gravDir = -1f;
				});
			};

			IL.Terraria.Player.Update += il => {
				var loc = (
					ignorePlats: (byte)12,
					fallThrough: (byte)13
				);

				// invert gravity by default
				{
					var c = new ILCursor(il);
					var label = default(ILLabel);
					c.GotoNext(
						i => i.MatchLdfld<Player>(nameof(Player.gravControl2)),
						i => i.MatchBrfalse(out label));
					c.GotoLabel(label);
					c.GotoNext(
						i => i.MatchLdcR4(1),
						i => i.MatchStfld<Player>(nameof(Player.gravDir)));
					c.RemoveRange(2);
					c.EmitDelegate<Action<Player>>(player => {
						player.gravDir = -1f;
					});
				}

				// don't fall through platforms while inverted
				{
					var c = new ILCursor(il);
					c.GotoNext(MoveType.After,
						i => i.MatchLdfld<Player>(nameof(Player.controlDown)),
						i => i.MatchStloc(loc.fallThrough),
						i => i.MatchLdarg(0),
						i => i.MatchLdfld<Player>(nameof(Player.gravDir)),
						i => i.MatchLdcR4(-1f),
						i => i.MatchCeq());
					c.Index -= 3;
					c.RemoveRange(3);
					c.EmitDelegate<Func<Player, bool>>(player =>
						player.gravDir == -1f && player.velocity.Y > 0f);
					c.Emit(OpCodes.Stloc_S, loc.ignorePlats);
					c.Emit(OpCodes.Ldc_I4_0);
				}
			};

			IL.Terraria.Player.UpdateDead += il => {
				// preserve gravity on death screen
				var c = new ILCursor(il);
				c.GotoNext(
					i => i.MatchLdarg(0),
					i => i.MatchLdcR4(1f),
					i => i.MatchStfld<Player>(nameof(Player.gravDir)));
				c.RemoveRange(3);
			};

			IL.Terraria.WorldGen.ReplaceTile_DoActualReplacement_Single += il => {
				// place platforms upside down when replacing
				var c = new ILCursor(il);
				var label = default(ILLabel);
				c.GotoNext(
					i => i.MatchLdloc(1),
					i => i.MatchBrfalse(out label));
				c.GotoLabel(label);
				c.Emit(OpCodes.Ldloc_0);
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldarg_S, (byte)4);
				c.EmitDelegate<Action<int, int, Tile>>((oldTileID, newTileID, tile) => {
					if (Main.LocalPlayer.gravDir == -1f && !TileID.Sets.Platforms[oldTileID] && TileID.Sets.Platforms[newTileID] && tile.Slope == SlopeType.Solid)
						tile.IsHalfBlock = true;
				});
			};
		}
	}
}
