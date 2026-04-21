using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ZacksMusicianship.Common.Cadences;
using ZacksMusicianship.Common.Players;
using ZacksMusicianship.Common.Systems;

namespace ZacksMusicianship.Content.Items
{
	public class Songbook : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 30;
			Item.useTime = 18;
			Item.useAnimation = 18;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTurn = true;
			Item.rare = ItemRarityID.White;
			Item.value = Item.buyPrice(silver: 5);
			Item.noMelee = true;
			Item.UseSound = null;
			Item.maxStack = 1;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player)
		{
			if (Main.myPlayer == player.whoAmI)
			{
				SoundEngine.PlaySound(SoundID.MenuTick, player.Center);
				SongbookSystem.OpenFor(player);
			}

			return false;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			SongbookPlayer songbookPlayer = Main.LocalPlayer.GetModPlayer<SongbookPlayer>();
			tooltips.Add(new TooltipLine(Mod, "CadenceCount",
				$"Cadences discovered: {songbookPlayer.UnlockedCadenceCount}/{CadenceLibrary.EntryCount}")
			{
				OverrideColor = new Microsoft.Xna.Framework.Color(176, 226, 210)
			});

			tooltips.Add(new TooltipLine(Mod, "SongbookHint", "[Left or right-click to open your cadence journal]")
			{
				OverrideColor = Microsoft.Xna.Framework.Color.Gray * 0.85f
			});
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Wood, 1)
				.Register();
		}
	}
}
