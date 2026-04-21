using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ZacksMusicianship.Common.Cadences;

namespace ZacksMusicianship.Common.Players
{
	public class SongbookPlayer : ModPlayer
	{
		private ulong unlockedCadenceMask;

		public int UnlockedCadenceCount => CountUnlocked(unlockedCadenceMask);

		public override void Initialize()
		{
			unlockedCadenceMask = 0UL;
		}

		public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
		{
			if (mediumCoreDeath)
				yield break;

			Item book = new();
			book.SetDefaults(ModContent.ItemType<Content.Items.Songbook>());
			yield return book;
		}

		public override void SaveData(TagCompound tag)
		{
			if (unlockedCadenceMask != 0UL)
				tag["UnlockedCadenceMask"] = (long)unlockedCadenceMask;
		}

		public override void LoadData(TagCompound tag)
		{
			unlockedCadenceMask = tag.ContainsKey("UnlockedCadenceMask")
				? (ulong)tag.GetLong("UnlockedCadenceMask")
				: 0UL;
		}

		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendSongbookPacket(toWho, fromWho);

		public bool IsUnlocked(CadenceEntryId id) => (unlockedCadenceMask & CadenceLibrary.GetMask(id)) != 0UL;

		public int GetUnlockedCount(CadenceCategory category)
		{
			int count = 0;
			foreach (CadenceBookEntry entry in CadenceLibrary.GetEntries(category))
			{
				if (IsUnlocked(entry.Id))
					count++;
			}

			return count;
		}

		public bool TryUnlock(CadenceBookEntry entry, bool sync)
		{
			if (entry == null || IsUnlocked(entry.Id))
				return false;

			unlockedCadenceMask |= CadenceLibrary.GetMask(entry.Id);

			if (Main.myPlayer == Player.whoAmI && Main.netMode != NetmodeID.Server)
				NotifyLocalUnlock(entry);

			if (sync && Main.netMode != NetmodeID.SinglePlayer)
				SendSongbookPacket();

			return true;
		}

		internal void ApplySyncedUnlockMask(ulong unlockedMask) => unlockedCadenceMask = unlockedMask;

		private void NotifyLocalUnlock(CadenceBookEntry entry)
		{
			SoundEngine.PlaySound(SoundID.MenuTick, Player.Center);
			CombatText.NewText(Player.Hitbox, new Microsoft.Xna.Framework.Color(255, 220, 120), "Songbook Updated", dramatic: false);
			Main.NewText($"Songbook discovered: {entry.Title}", entry.AccentColor);
		}

		private static int CountUnlocked(ulong mask)
		{
			int count = 0;
			while (mask != 0UL)
			{
				count += (int)(mask & 1UL);
				mask >>= 1;
			}

			return count;
		}

		private void SendSongbookPacket(int toWho = -1, int fromWho = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;

			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)ZacksMusicianship.PacketType.SyncSongbookState);
			packet.Write((byte)Player.whoAmI);
			packet.Write(unlockedCadenceMask);
			packet.Send(toWho, fromWho);
		}
	}
}
