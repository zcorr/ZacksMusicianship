using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ZacksMusicianship.Common.UI;

namespace ZacksMusicianship.Common.Systems
{
	public class SongbookSystem : ModSystem
	{
		internal static SongbookUIState SongbookUI;

		public override void Load()
		{
			if (!Main.dedServ)
				SongbookUI = new SongbookUIState();
		}

		public override void Unload()
		{
			SongbookUI = null;
		}

		public static void OpenFor(Player player)
		{
			if (Main.dedServ || player.whoAmI != Main.myPlayer || SongbookUI == null)
				return;

			IngameFancyUI.OpenUIState(SongbookUI);
			SongbookUI.LoadFromPlayer(player);
		}

		public static void Close()
		{
			if (!Main.dedServ)
				IngameFancyUI.Close();
		}
	}
}
