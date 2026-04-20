using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ZacksMusicianship.Content.Projectiles
{
	public class MajorNoteProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width       = 14;
			Projectile.height      = 14;
			Projectile.aiStyle     = 0;
			Projectile.friendly    = true;
			Projectile.DamageType  = DamageClass.Melee;
			Projectile.penetrate   = 1;
			Projectile.timeLeft    = 14;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.light       = 0.32f;
		}

		public override void AI()
		{
			Projectile.rotation += 0.25f;

			if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame);
		}
	}
}
