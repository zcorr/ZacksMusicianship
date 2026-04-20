using Terraria;
using Terraria.ModLoader;

namespace ZacksMusicianship.Content.Buffs
{
	public class MajorChordBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.moveSpeed       += 0.2f;
			player.runAcceleration += 0.08f;
		}
	}
}
