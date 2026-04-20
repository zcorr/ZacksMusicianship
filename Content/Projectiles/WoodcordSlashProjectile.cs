using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ZacksMusicianship.Common.Chords;
using ZacksMusicianship.Common.NPCs;
using ZacksMusicianship.Common.Players;
using ZacksMusicianship.Content.Buffs;

namespace ZacksMusicianship.Content.Projectiles
{
	public class WoodcordSlashProjectile : ModProjectile
	{
		private ChordQuality Quality => (ChordQuality)(int)Projectile.ai[0];

		private ref float Initialized => ref Projectile.localAI[0];
		private ref float BaseRotation => ref Projectile.localAI[1];

		public override string Texture => "ZacksMusicianship/Content/Items/Woodcord";

		public override void SetDefaults()
		{
			Projectile.width = 56;
			Projectile.height = 56;
			Projectile.aiStyle = 0;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.ownerHitCheck = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.hide = true;
			Projectile.timeLeft = 360;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool PreDraw(ref Color lightColor) => false;

		public override void AI()
		{
			Player player = Main.player[Projectile.owner];
			if (!player.active || player.dead)
			{
				Projectile.Kill();
				return;
			}

			if (Initialized == 0f)
			{
				BaseRotation = Projectile.velocity.ToRotation();
				Initialized = 1f;
			}

			if (player.itemAnimation <= 0 || player.HeldItem.type != ModContent.ItemType<Items.Woodcord>())
			{
				Projectile.Kill();
				return;
			}

			player.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);
			player.heldProj = Projectile.whoAmI;
			player.itemTime = player.itemAnimation;

			float progress = 1f - player.itemAnimation / (float)player.itemAnimationMax;
			float sweep = MathHelper.Lerp(-0.95f, 0.95f, progress) * player.direction;
			float rotation = BaseRotation + sweep * 0.85f;
			float reach = 34f;

			Vector2 center = player.RotatedRelativePoint(player.MountedCenter, true);
			Projectile.Center = center + rotation.ToRotationVector2() * reach;
			Projectile.rotation = rotation;
			Projectile.spriteDirection = player.direction;

			Projectile.timeLeft = 2;

			if (Main.rand.NextBool(2))
			{
				int dustType = ChordMath.GetDustId(Quality);
				Vector2 dustPos = Vector2.Lerp(center, Projectile.Center, Main.rand.NextFloat());
				Dust dust = Dust.NewDustPerfect(dustPos, dustType, rotation.ToRotationVector2() * Main.rand.NextFloat(0.4f, 1.4f));
				dust.noGravity = true;
				dust.scale = 0.9f;
			}
		}

		public override bool? CanDamage()
		{
			Player player = Main.player[Projectile.owner];
			if (!player.active || player.itemAnimationMax <= 0)
				return false;

			float progress = 1f - player.itemAnimation / (float)player.itemAnimationMax;
			return progress is > 0.12f and < 0.92f;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player player = Main.player[Projectile.owner];
			Vector2 start = player.RotatedRelativePoint(player.MountedCenter, true);
			Vector2 end = Projectile.Center;
			float collisionPoint = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 28f, ref collisionPoint);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Player player = Main.player[Projectile.owner];

			switch (Quality)
			{
				case ChordQuality.Major:
					player.AddBuff(ModContent.BuffType<MajorChordBuff>(), 180);
					break;

				case ChordQuality.Diminished:
					if (Main.rand.NextFloat() < 0.33f)
					{
						target.buffImmune[BuffID.Confused] = false;
						target.AddBuff(BuffID.Confused, 240);
					}
					break;

				case ChordQuality.Suspended:
					target.GetGlobalNPC<MusicGlobalNPC>().suspendedTimer = 120;
					target.velocity = new Vector2(target.velocity.X * 0.25f, -9f);
					break;
			}
		}
	}
}
