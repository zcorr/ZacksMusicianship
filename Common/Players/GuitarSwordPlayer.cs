using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ZacksMusicianship.Common.Chords;

namespace ZacksMusicianship.Common.Players
{
	public class GuitarSwordPlayer : ModPlayer
	{
		public int ChordRoot { get; private set; } = 0;
		public ChordQuality CurrentQuality { get; private set; } = ChordQuality.Major;
		public int CadenceCharge { get; private set; }

		internal bool CadencePrimedThisUse { get; private set; }

		public override void Initialize()
		{
			ChordRoot = 0;
			CurrentQuality = ChordQuality.Major;
			CadenceCharge = 0;
			CadencePrimedThisUse = false;
		}

		public override void SaveData(TagCompound tag)
		{
			if (ChordRoot != 0)
				tag["ChordRoot"] = ChordRoot;

			if (CurrentQuality != ChordQuality.Major)
				tag["ChordQuality"] = (byte)CurrentQuality;

			if (CadenceCharge > 0)
				tag["CadenceCharge"] = CadenceCharge;
		}

		public override void LoadData(TagCompound tag)
		{
			ChordRoot = tag.ContainsKey("ChordRoot") ? ChordMath.NormalizePitchClass(tag.GetInt("ChordRoot")) : 0;
			CurrentQuality = tag.ContainsKey("ChordQuality") ? (ChordQuality)tag.GetByte("ChordQuality") : ChordQuality.Major;
			CadenceCharge = tag.ContainsKey("CadenceCharge") ? Utils.Clamp(tag.GetInt("CadenceCharge"), 0, 3) : 0;
		}

		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendChordPacket(toWho, fromWho);

		public int CommitChord(int root, ChordQuality quality, bool sync, out string cadenceName, out bool encoreReady)
		{
			root = ChordMath.NormalizePitchClass(root);
			int previousCharge = CadenceCharge;
			int gain = ChordMath.EvaluateCadenceGain(ChordRoot, CurrentQuality, root, quality, out cadenceName);

			ApplySyncedChord(root, quality);

			if (gain > 0)
				CadenceCharge = Utils.Clamp(CadenceCharge + gain, 0, 3);

			encoreReady = previousCharge < 3 && CadenceCharge >= 3;

			if (sync && Main.netMode != NetmodeID.SinglePlayer)
				SendChordPacket();

			return gain;
		}

		public bool PrepareCadenceUse()
		{
			CadencePrimedThisUse = CadenceCharge >= 3;
			return CadencePrimedThisUse;
		}

		public void ConsumeCadenceCharge(bool sync)
		{
			if (!CadencePrimedThisUse && CadenceCharge <= 0)
				return;

			CadenceCharge = 0;
			CadencePrimedThisUse = false;

			if (sync && Main.netMode != NetmodeID.SinglePlayer)
				SendChordPacket();
		}

		internal void ApplySyncedChord(int root, ChordQuality quality)
		{
			ChordRoot = ChordMath.NormalizePitchClass(root);
			CurrentQuality = quality;
		}

		internal void ApplySyncedCadenceCharge(int cadenceCharge)
		{
			CadenceCharge = Utils.Clamp(cadenceCharge, 0, 3);
		}

		private void SendChordPacket(int toWho = -1, int fromWho = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;

			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)ZacksMusicianship.PacketType.SyncChordSelection);
			packet.Write((byte)Player.whoAmI);
			packet.Write((byte)ChordRoot);
			packet.Write((byte)CurrentQuality);
			packet.Write((byte)CadenceCharge);
			packet.Send(toWho, fromWho);
		}
	}
}
