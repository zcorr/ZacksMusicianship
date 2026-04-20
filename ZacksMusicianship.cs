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
			SyncChordSelection = 0,
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			switch ((PacketType)reader.ReadByte())
			{
				case PacketType.SyncChordSelection:
				{
					int playerIndex = reader.ReadByte();
					int root = reader.ReadByte();
					ChordQuality quality = (ChordQuality)reader.ReadByte();
					int cadenceCharge = reader.ReadByte();

					if (playerIndex < 0 || playerIndex >= Main.maxPlayers || Main.player[playerIndex] == null)
						return;

					GuitarSwordPlayer chordPlayer = Main.player[playerIndex].GetModPlayer<GuitarSwordPlayer>();
					chordPlayer.ApplySyncedChord(root, quality);
					chordPlayer.ApplySyncedCadenceCharge(cadenceCharge);

					if (Main.netMode == NetmodeID.Server)
					{
						ModPacket packet = GetPacket();
						packet.Write((byte)PacketType.SyncChordSelection);
						packet.Write((byte)playerIndex);
						packet.Write((byte)root);
						packet.Write((byte)quality);
						packet.Write((byte)cadenceCharge);
						packet.Send(-1, whoAmI);
					}

					break;
				}
			}
		}
	}
}
