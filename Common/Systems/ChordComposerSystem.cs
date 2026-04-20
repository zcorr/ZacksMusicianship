using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ZacksMusicianship.Common.UI;

namespace ZacksMusicianship.Common.Systems
{
	public class ChordComposerSystem : ModSystem
	{
		internal static ChordComposerUIState ComposerUI;

		public override void Load()
		{
			if (!Main.dedServ)
				ComposerUI = new ChordComposerUIState();
		}

		public override void Unload()
		{
			ComposerUI = null;
		}

		public static void OpenFor(Player player)
		{
			if (Main.dedServ || player.whoAmI != Main.myPlayer || ComposerUI == null)
				return;

			IngameFancyUI.OpenUIState(ComposerUI);
			ComposerUI.LoadFromPlayer(player);
		}

		public static void Close()
		{
			if (!Main.dedServ)
				IngameFancyUI.Close();
		}
	}
}
