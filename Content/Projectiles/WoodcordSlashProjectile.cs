using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ZacksMusicianship.Common.Chords;
using ZacksMusicianship.Common.NPCs;
using ZacksMusicianship.Common.Players;
using ZacksMusicianship.Common.Rhythm;
using ZacksMusicianship.Content.Buffs;

namespace ZacksMusicianship.Content.Projectiles
{
	public class WoodcordSlashProjectile : ModProjectile
	{
		private ChordQuality Quality => (ChordQuality)(int)Projectile.ai[0];
		private StrumDirection StrokeDirection => (StrumDirection)(int)Projectile.ai[1];

		private ref float Initialized => ref Projectile.localAI[0];
		private ref float BaseRotation => ref Projectile.localAI[1];

		public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Arkhalis}";

		public override void SetDefaults()
		{
			Projectile.width = 96;
			Projectile.height = 96;
			Projectile.aiStyle = 0;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.ownerHitCheck = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.hide = false;
			Projectile.timeLeft = 360;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Player player = Main.player[Projectile.owner];
			if (!player.active || player.dead || player.itemAnimationMax <= 0)
				return false;

			Texture2D texture = TextureAssets.Projectile[ProjectileID.Arkhalis].Value;
			Vector2 center = player.RotatedRelativePoint(player.MountedCenter, true);
			float progress = GetProgress(player);
			Color slashColor = Color.Lerp(Color.White, ChordMath.GetColor(Quality), 0.68f);
			int frameCount = Math.Max(1, Main.projFrames[ProjectileID.Arkhalis]);
			int frameHeight = texture.Height / frameCount;

			for (int trailIndex = 3; trailIndex >= 0; trailIndex--)
			{
				float sampleProgress = Utils.Clamp(progress - trailIndex * 0.08f, 0f, 1f);
				float sampleRotation = GetSlashRotation(player, sampleProgress);
				float sampleReach = GetSlashReach(sampleProgress);
				Vector2 drawPosition = center + sampleRotation.ToRotationVector2() * sampleReach - Main.screenPosition;
				float opacity = GetSlashOpacity(sampleProgress) * (1f - trailIndex * 0.15f);
				float scale = GetSlashScale(sampleProgress) * (1f - trailIndex * 0.04f);
				int frameIndex = Utils.Clamp((int)(sampleProgress * (frameCount - 1)), 0, frameCount - 1);
				Rectangle frame = new(0, frameIndex * frameHeight, texture.Width, frameHeight);
				Vector2 origin = new(frame.Width * 0.5f, frame.Height * 0.5f);

				Main.EntitySpriteDraw(texture, drawPosition, frame, slashColor * opacity, sampleRotation,
					origin, scale, SpriteEffects.None, 0);
			}

			return false;
		}

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

			float progress = GetProgress(player);
			float rotation = GetSlashRotation(player, progress);
			float reach = GetSlashReach(progress);

			Vector2 center = player.RotatedRelativePoint(player.MountedCenter, true);
			Projectile.Center = center + rotation.ToRotationVector2() * reach;
			Projectile.rotation = rotation;
			Projectile.scale = GetSlashScale(progress);
			Projectile.spriteDirection = player.direction;

			float armRotation = rotation - (player.direction == 1 ? MathHelper.PiOver4 : MathHelper.Pi * 0.75f);
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
			player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, armRotation - player.direction * 0.18f);

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

			float progress = GetProgress(player);
			return progress is > 0.08f and < 0.88f;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player player = Main.player[Projectile.owner];
			Vector2 start = player.RotatedRelativePoint(player.MountedCenter, true);
			Vector2 end = Projectile.Center;
			float collisionPoint = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 32f, ref collisionPoint);
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

		private float GetProgress(Player player) => 1f - player.itemAnimation / (float)player.itemAnimationMax;

		private float GetSlashRotation(Player player, float progress)
		{
			float start = StrokeDirection == StrumDirection.Up ? 1.05f : -1.2f;
			float end = StrokeDirection == StrumDirection.Up ? -1.2f : 1.05f;
			float sweep = MathHelper.Lerp(start, end, progress) * player.direction;
			return BaseRotation + sweep * 0.92f;
		}

		private float GetSlashReach(float progress)
		{
			float outward = MathHelper.Lerp(22f, 44f, Utils.GetLerpValue(0f, 0.4f, progress, clamped: true));
			float pulse = (float)Math.Sin(progress * MathHelper.Pi) * 8f;
			return outward + pulse;
		}

		private float GetSlashScale(float progress)
		{
			float body = MathHelper.Lerp(0.82f, 1.06f, Utils.GetLerpValue(0.05f, 0.55f, progress, clamped: true));
			return body + (float)Math.Sin(progress * MathHelper.Pi) * 0.12f;
		}

		private float GetSlashOpacity(float progress)
		{
			float fadeIn = Utils.GetLerpValue(0.02f, 0.18f, progress, clamped: true);
			float fadeOut = Utils.GetLerpValue(1f, 0.72f, progress, clamped: true);
			return fadeIn * fadeOut * 0.95f;
		}
	}
}
