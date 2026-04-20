using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ZacksMusicianship.Content.Projectiles
{
	public class DiminishedOrbProjectile : ModProjectile
	{
		private ref float BounceCount => ref Projectile.ai[0];
		private ref float ChangeTimer  => ref Projectile.ai[1];

		public override void SetDefaults()
		{
			Projectile.width       = 18;
			Projectile.height      = 18;
			Projectile.aiStyle     = 0;
			Projectile.friendly    = true;
			Projectile.DamageType  = DamageClass.Melee;
			Projectile.penetrate   = 1;
			Projectile.timeLeft    = 90;
			Projectile.tileCollide = true;
			Projectile.light       = 0.3f;
		}

		public override void AI()
		{
			ChangeTimer++;

			// Randomly deflect direction every 15 frames to create chaotic movement
			if (ChangeTimer % 15 == 0)
			{
				float newAngle = Projectile.velocity.ToRotation() + Main.rand.NextFloat(-1.2f, 1.2f);
				Projectile.velocity = newAngle.ToRotationVector2() * 6f;
			}

			Projectile.rotation += 0.2f;

			if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.CrimsonTorch);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			BounceCount++;
			if (BounceCount >= 3)
				return true; // Kill projectile after 3 bounces

			if (Math.Abs(Projectile.velocity.X) != Math.Abs(oldVelocity.X))
				Projectile.velocity.X = -oldVelocity.X;
			if (Math.Abs(Projectile.velocity.Y) != Math.Abs(oldVelocity.Y))
				Projectile.velocity.Y = -oldVelocity.Y;

			return false;
		}
	}
}
