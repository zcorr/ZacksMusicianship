using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ZacksMusicianship.Common.Chords;
using ZacksMusicianship.Common.Players;

namespace ZacksMusicianship
{
	public class ZacksMusicianship : Mod
	{
		internal enum PacketType : byte
		{
			SyncProgressionState = 0,
			SyncSongbookState = 1,
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			switch ((PacketType)reader.ReadByte())
			{
				case PacketType.SyncProgressionState:
				{
					int playerIndex = reader.ReadByte();
					int progressionCount = reader.ReadByte();
					int activeIndex = reader.ReadByte();
					int activeStrumPatternIndex = reader.ReadByte();
					int activeStrumStepIndex = reader.ReadByte();
					int cadenceCharge = reader.ReadByte();
					int[] roots = new int[GuitarSwordPlayer.MaxProgressionLength];
					ChordQuality[] qualities = new ChordQuality[GuitarSwordPlayer.MaxProgressionLength];

					for (int i = 0; i < GuitarSwordPlayer.MaxProgressionLength; i++)
					{
						roots[i] = reader.ReadByte();
						qualities[i] = (ChordQuality)reader.ReadByte();
					}

					if (playerIndex < 0 || playerIndex >= Main.maxPlayers || Main.player[playerIndex] == null)
						return;

					GuitarSwordPlayer chordPlayer = Main.player[playerIndex].GetModPlayer<GuitarSwordPlayer>();
					chordPlayer.ApplySyncedProgressionState(progressionCount, activeIndex, activeStrumPatternIndex, activeStrumStepIndex,
						cadenceCharge, roots, qualities);

					if (Main.netMode == NetmodeID.Server)
					{
						ModPacket packet = GetPacket();
						packet.Write((byte)PacketType.SyncProgressionState);
						packet.Write((byte)playerIndex);
						packet.Write((byte)progressionCount);
						packet.Write((byte)activeIndex);
						packet.Write((byte)activeStrumPatternIndex);
						packet.Write((byte)activeStrumStepIndex);
						packet.Write((byte)cadenceCharge);

						for (int i = 0; i < GuitarSwordPlayer.MaxProgressionLength; i++)
						{
							packet.Write((byte)roots[i]);
							packet.Write((byte)qualities[i]);
						}

						packet.Send(-1, whoAmI);
					}

					break;
				}

				case PacketType.SyncSongbookState:
				{
					int playerIndex = reader.ReadByte();
					ulong unlockedMask = reader.ReadUInt64();

					if (playerIndex < 0 || playerIndex >= Main.maxPlayers || Main.player[playerIndex] == null)
						return;

					SongbookPlayer songbookPlayer = Main.player[playerIndex].GetModPlayer<SongbookPlayer>();
					songbookPlayer.ApplySyncedUnlockMask(unlockedMask);

					if (Main.netMode == NetmodeID.Server)
					{
						ModPacket packet = GetPacket();
						packet.Write((byte)PacketType.SyncSongbookState);
						packet.Write((byte)playerIndex);
						packet.Write(unlockedMask);
						packet.Send(-1, whoAmI);
					}

					break;
				}
			}
		}
	}
}
