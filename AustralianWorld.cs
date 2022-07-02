using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace AustralianChallenge
{
	internal class AustralianWorld : ModSystem
	{
		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight) {
			var index = tasks.FindIndex(pass => pass.Name == "Spawn Point");
			if (index == -1)
				throw new Exception("Failed to insert \"Spawn Platform\" generation pass.");

			tasks.Insert(index + 1, new PassLegacy("Spawn Platform", (_, _) => {
				for (int x = Main.spawnTileX - 2; x <= Main.spawnTileX + 2; x++)
					WorldGen.PlaceTile(x, Main.spawnTileY - 5, TileID.Cloud, forced: true);
			}));
		}
	}
}
