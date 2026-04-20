using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ZacksMusicianship.Common.NPCs
{
	public class MusicGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public int suspendedTimer = 0;

		public override void PostAI(NPC npc)
		{
			if (suspendedTimer <= 0)
				return;

			suspendedTimer--;
			npc.noGravity = true;

			// Resist falling — upward launch is applied on first hit in Woodcord.OnHitNPC
			if (npc.velocity.Y > 1f)
				npc.velocity.Y *= 0.6f;

			if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
				Dust.NewDust(npc.position, npc.width, npc.height, DustID.IceTorch, 0f, -0.5f);
		}
	}
}
