using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ZacksMusicianship.Common.Chords;
using ZacksMusicianship.Common.NPCs;
using ZacksMusicianship.Common.Players;
using ZacksMusicianship.Common.Rhythm;
using ZacksMusicianship.Common.Systems;
using ZacksMusicianship.Content.Buffs;
using ZacksMusicianship.Content.Projectiles;

namespace ZacksMusicianship.Content.Items
{
	public class Woodcord : ModItem
	{
		private const int BaseDamage = 9;
		private const int BaseUseTime = 19;
		private const float BaseKnock = 4f;

		private readonly struct ChordModeStats
		{
			public ChordModeStats(int damage, int useTime, float knockBack)
			{
				Damage = damage;
				UseTime = useTime;
				KnockBack = knockBack;
			}

			public int Damage { get; }
			public int UseTime { get; }
			public float KnockBack { get; }
		}

		public override void SetDefaults()
		{
			Item.damage = BaseDamage;
			Item.DamageType = DamageClass.Melee;
			Item.width = 44;
			Item.height = 46;
			Item.useTime = BaseUseTime;
			Item.useAnimation = BaseUseTime;
			Item.useStyle = ItemUseStyleID.Guitar;
			Item.knockBack = BaseKnock;
			Item.value = Item.buyPrice(silver: 20);
			Item.rare = ItemRarityID.White;
			Item.UseSound = null;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.noUseGraphic = false;
			Item.shoot = ModContent.ProjectileType<WoodcordSlashProjectile>();
			Item.shootSpeed = 12f;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player)
		{
			if (player.altFunctionUse == 2)
			{
				if (Main.myPlayer == player.whoAmI)
					ChordComposerSystem.OpenFor(player);

				return false;
			}

			if (player.ownedProjectileCounts[ModContent.ProjectileType<WoodcordSlashProjectile>()] > 0)
				return false;

			GuitarSwordPlayer chordPlayer = player.GetModPlayer<GuitarSwordPlayer>();
			ChordQuality quality = chordPlayer.CurrentQuality;
			bool encoreThisUse = chordPlayer.PrepareCadenceUse();

			ChordModeStats modeStats = GetModeStats(quality);
			Item.damage = modeStats.Damage;
			Item.knockBack = modeStats.KnockBack;
			Item.useTime = GetStrumAdjustedUseTime(chordPlayer, chordPlayer.GetSlowestProgressionUseTime(GetModeUseTime));
			Item.useAnimation = Item.useTime;

			if (encoreThisUse)
			{
				Item.damage = (int)(Item.damage * 1.35f);
				Item.knockBack += 2f;
			}

			return true;
		}

		public override bool? UseItem(Player player)
		{
			if (player.altFunctionUse == 2)
				return false;

			if (Main.myPlayer == player.whoAmI)
			{
				GuitarSwordPlayer chordPlayer = player.GetModPlayer<GuitarSwordPlayer>();
				SoundEngine.PlaySound(GetStrumSoundStyle(chordPlayer.ChordRoot, chordPlayer.CurrentQuality,
					chordPlayer.CurrentStrumStroke.Direction), player.Center);

				if (chordPlayer.CadencePrimedThisUse)
					CombatText.NewText(player.Hitbox, new Color(255, 180, 80), "Encore!", dramatic: true);
			}

			return true;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
			Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (player.altFunctionUse == 2)
				return false;

			GuitarSwordPlayer chordPlayer = player.GetModPlayer<GuitarSwordPlayer>();
			int root = chordPlayer.ChordRoot;
			ChordQuality quality = chordPlayer.CurrentQuality;
			StrumStroke strumStroke = chordPlayer.CurrentStrumStroke;

			if (velocity == Vector2.Zero)
				velocity = new Vector2(player.direction, 0f);

			Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction),
				ModContent.ProjectileType<WoodcordSlashProjectile>(), damage, knockback, player.whoAmI,
				(float)quality, (float)strumStroke.Direction);

			switch (quality)
			{
				case ChordQuality.Major:
					if (player.ownedProjectileCounts[ModContent.ProjectileType<MajorNoteProjectile>()] == 0)
					{
						Projectile.NewProjectile(source, position, velocity * 0.9f,
							ModContent.ProjectileType<MajorNoteProjectile>(),
							(int)(damage * 0.5f), knockback * 0.3f, player.whoAmI);
					}
					break;

				case ChordQuality.Minor:
					break;

				case ChordQuality.Diminished:
					Vector2 chaos = velocity.RotatedByRandom(0.8f) * 0.9f;
					Projectile.NewProjectile(source, position, chaos,
						ModContent.ProjectileType<DiminishedOrbProjectile>(),
						(int)(damage * 0.35f), 0f, player.whoAmI);
					break;

				case ChordQuality.Suspended:
					Projectile.NewProjectile(source, position, velocity * 0.6f,
						ModContent.ProjectileType<SuspendedWaveProjectile>(),
						(int)(damage * 0.65f), knockback, player.whoAmI);
					break;
			}

			if (chordPlayer.CadencePrimedThisUse)
				ReleaseEncore(player, source, position, velocity, damage, knockback, quality);

			chordPlayer.RegisterStrumUse();
			bool completedPattern = chordPlayer.AdvanceStrumPatternAfterUse(sync: true);

			if (completedPattern)
				chordPlayer.RegisterChordUseForCadence(player, root, quality, sync: true);

			return false;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			ChordQuality quality = player.GetModPlayer<GuitarSwordPlayer>().CurrentQuality;

			switch (quality)
			{
				case ChordQuality.Major:
					player.AddBuff(ModContent.BuffType<MajorChordBuff>(), 180);
					break;

				case ChordQuality.Minor:
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

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (!Main.rand.NextBool(3))
				return;

			GuitarSwordPlayer chordPlayer = player.GetModPlayer<GuitarSwordPlayer>();
			ChordQuality quality = chordPlayer.CurrentQuality;
			Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, ChordMath.GetDustId(quality));

			if (chordPlayer.CadenceCharge >= 3 && Main.rand.NextBool(2))
			{
				int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.GemTopaz);
				Main.dust[dust].velocity *= 0.2f;
				Main.dust[dust].noGravity = true;
			}
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			GuitarSwordPlayer chordPlayer = Main.LocalPlayer.GetModPlayer<GuitarSwordPlayer>();
			string chordName = ChordMath.GetDisplayName(chordPlayer.ChordRoot, chordPlayer.CurrentQuality);
			StrumPattern strumPattern = chordPlayer.CurrentStrumPattern;
			StrumStroke strumStroke = chordPlayer.CurrentStrumStroke;
			int strumTempoUseTime = chordPlayer.GetSlowestProgressionUseTime(GetModeUseTime);

			tooltips.Add(new TooltipLine(Mod, "CurrentChord",
				chordPlayer.ProgressionCount > 0
					? $"Current Step: {chordPlayer.ActiveProgressionIndex + 1}/{chordPlayer.ProgressionCount}  —  {chordName}  —  {ChordMath.GetDescription(chordPlayer.CurrentQuality)}"
					: $"Current Chord: {chordName}  —  {ChordMath.GetDescription(chordPlayer.CurrentQuality)}")
			{
				OverrideColor = ChordMath.GetColor(chordPlayer.CurrentQuality)
			});

			tooltips.Add(new TooltipLine(Mod, "SavedProgression",
				chordPlayer.ProgressionCount > 0
					? $"Progression: {chordPlayer.GetProgressionDisplay()}"
					: "Progression: empty  —  right-click to add up to 4 saved chords")
			{
				OverrideColor = chordPlayer.ProgressionCount > 0 ? new Color(214, 214, 230) : Color.Gray
			});

			tooltips.Add(new TooltipLine(Mod, "StrumPattern",
				$"Strum: {strumPattern.DisplayName} ({strumPattern.GetDirectionDisplay()})  —  next {strumStroke.DirectionLabel} on {strumStroke.BeatLabel} ({chordPlayer.ActiveStrumStepIndex + 1}/{strumPattern.StepCount})")
			{
				OverrideColor = new Color(190, 220, 255)
			});

			tooltips.Add(new TooltipLine(Mod, "StrumBeats",
				$"Strum beats: {strumPattern.GetBeatDisplay()}  —  chord changes after the pattern")
			{
				OverrideColor = Color.Gray * 0.9f
			});

			tooltips.Add(new TooltipLine(Mod, "StrumTempo",
				chordPlayer.ProgressionCount > 1
					? $"Strum tempo: locked to slowest saved chord ({strumTempoUseTime} base ticks)"
					: $"Strum tempo: {strumTempoUseTime} base ticks")
			{
				OverrideColor = Color.Gray * 0.9f
			});

			tooltips.Add(new TooltipLine(Mod, "CurrentNotes",
				$"Notes: {ChordMath.GetNotesDisplay(chordPlayer.ChordRoot, chordPlayer.CurrentQuality)}")
			{
				OverrideColor = Color.Silver
			});

			tooltips.Add(new TooltipLine(Mod, "CadenceMeter",
				chordPlayer.CadenceCharge >= 3
					? "Cadence: Encore Ready  —  your next strike releases an empowered echo"
					: $"Cadence: {chordPlayer.CadenceCharge}/3  —  release after a 2-4 chord phrase to count one cadence")
			{
				OverrideColor = chordPlayer.CadenceCharge >= 3 ? new Color(255, 180, 80) : new Color(150, 230, 210)
			});

			tooltips.Add(new TooltipLine(Mod, "RightClickHint",
				"[Right-click to edit saved progression]")
			{
				OverrideColor = Color.Gray * 0.85f
			});
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.CopperBar, 12)
				.AddIngredient(ItemID.Wood, 8)
				.AddTile(TileID.Anvils)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.TinBar, 12)
				.AddIngredient(ItemID.Wood, 8)
				.AddTile(TileID.Anvils)
				.Register();
		}

		private static int GetStrumAdjustedUseTime(GuitarSwordPlayer chordPlayer, int baseUseTime)
		{
			StrumPattern pattern = chordPlayer.CurrentStrumPattern;
			int span = chordPlayer.CurrentStrumSubdivisionSpan;
			int duration = (int)System.Math.Round(baseUseTime * pattern.StepCount * span / (float)pattern.SubdivisionCount);

			return System.Math.Max(6, duration);
		}

		private static ChordModeStats GetModeStats(ChordQuality quality)
		{
			return quality switch
			{
				ChordQuality.Major => new ChordModeStats(6, 16, 2f),
				ChordQuality.Minor => new ChordModeStats(14, 26, 8f),
				_ => new ChordModeStats(BaseDamage, BaseUseTime, BaseKnock),
			};
		}

		private static int GetModeUseTime(ChordQuality quality) => GetModeStats(quality).UseTime;

		private static SoundStyle GetStrumSoundStyle(int root, ChordQuality quality, StrumDirection direction)
		{
			SoundStyle soundStyle = ChordMath.GetSoundStyle(root, quality);

			if (direction == StrumDirection.Up)
				soundStyle.Volume *= 0.72f;

			return soundStyle;
		}

		private void ReleaseEncore(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
			int damage, float knockback, ChordQuality quality)
		{
			switch (quality)
			{
				case ChordQuality.Major:
					SpawnMajorEncore(player, source, position, velocity, damage, knockback);
					break;

				case ChordQuality.Minor:
					player.AddBuff(BuffID.Titan, 180);
					CreateDustBurst(player, DustID.Shadowflame);
					break;

				case ChordQuality.Diminished:
					SpawnDiminishedEncore(player, source, position, velocity, damage);
					break;

				case ChordQuality.Suspended:
					SpawnSuspendedEncore(player, source, position, velocity, damage, knockback);
					break;
			}
		}

		private void SpawnMajorEncore(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
			int damage, float knockback)
		{
			for (int i = -1; i <= 1; i++)
			{
				Vector2 noteVelocity = velocity.RotatedBy(MathHelper.ToRadians(12f * i)) * 0.85f;
				Projectile.NewProjectile(source, position, noteVelocity,
					ModContent.ProjectileType<MajorNoteProjectile>(),
					(int)(damage * 0.4f), knockback * 0.35f, player.whoAmI);
			}

			CreateDustBurst(player, DustID.GoldFlame);
		}

		private void SpawnDiminishedEncore(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int damage)
		{
			for (int i = -1; i <= 1; i++)
			{
				Vector2 chaosVelocity = velocity.RotatedBy(MathHelper.ToRadians(20f * i)) * 0.85f;
				Projectile.NewProjectile(source, position, chaosVelocity,
					ModContent.ProjectileType<DiminishedOrbProjectile>(),
					(int)(damage * 0.3f), 0f, player.whoAmI);
			}

			CreateDustBurst(player, DustID.CrimsonTorch);
		}

		private void SpawnSuspendedEncore(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
			int damage, float knockback)
		{
			for (int i = -1; i <= 1; i++)
			{
				Vector2 waveVelocity = velocity.RotatedBy(MathHelper.ToRadians(16f * i)) * 0.65f;
				Projectile.NewProjectile(source, position, waveVelocity,
					ModContent.ProjectileType<SuspendedWaveProjectile>(),
					(int)(damage * 0.45f), knockback, player.whoAmI);
			}

			CreateDustBurst(player, DustID.IceTorch);
		}

		private void CreateDustBurst(Player player, int dustType)
		{
			for (int i = 0; i < 12; i++)
			{
				Vector2 velocity = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 12f) * Main.rand.NextFloat(1.5f, 3f);
				Dust dust = Dust.NewDustPerfect(player.Center, dustType, velocity, Scale: 1.1f);
				dust.noGravity = true;
			}
		}
	}
}
