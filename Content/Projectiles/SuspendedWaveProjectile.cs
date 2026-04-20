using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ZacksMusicianship.Content.Projectiles
{
	public class SuspendedWaveProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width       = 22;
			Projectile.height      = 22;
			Projectile.aiStyle     = 0;
			Projectile.friendly    = true;
			Projectile.DamageType  = DamageClass.Melee;
			Projectile.penetrate   = 1;
			Projectile.timeLeft    = 180;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light       = 0.6f;
		}

		public override void AI()
		{
			NPC target = FindClosestNPC(400f * 400f);
			if (target != null)
			{
				Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, toTarget * 9f, 0.09f);
			}

			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch);
				d.velocity *= 0.3f;
				d.scale     = 0.7f;
			}
		}

		private NPC FindClosestNPC(float maxDistSq)
		{
			NPC closest = null;
			float closestDist = maxDistSq;

			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (!npc.CanBeChasedBy()) continue;
				float dist = Vector2.DistanceSquared(npc.Center, Projectile.Center);
				if (dist < closestDist)
				{
					closestDist = dist;
					closest = npc;
				}
			}

			return closest;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Frostburn, 180);
		}
	}
}
