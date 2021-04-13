using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AustralianChallenge
{
	public class AustralianWorld : ModWorld
	{
		public override void PostWorldGen() {
			for (int x = Main.spawnTileX - 2; x < Main.spawnTileX + 2; x++) {
				WorldGen.PlaceTile(x, Main.spawnTileY - 5, TileID.Cloud, forced: true);
			}
		}
	}
}
