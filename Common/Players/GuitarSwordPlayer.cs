using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ZacksMusicianship.Common.Cadences;
using ZacksMusicianship.Common.Chords;

	namespace ZacksMusicianship.Common.Players
	{
		public class GuitarSwordPlayer : ModPlayer
		{
			public const int MaxProgressionLength = 4;
			private const int CadenceSessionGapTicks = 32;

			private readonly int[] progressionRoots = new int[MaxProgressionLength];
			private readonly ChordQuality[] progressionQualities = new ChordQuality[MaxProgressionLength];
			private readonly List<PlayedChord> heldCadencePhrase = new();

			public int ProgressionCount { get; private set; }
			public int ActiveProgressionIndex { get; private set; }
			public int ChordRoot => ProgressionCount > 0
				? progressionRoots[Utils.Clamp(ActiveProgressionIndex, 0, ProgressionCount - 1)]
				: 0;
			public ChordQuality CurrentQuality => ProgressionCount > 0
				? progressionQualities[Utils.Clamp(ActiveProgressionIndex, 0, ProgressionCount - 1)]
				: ChordQuality.Major;
			public int CadenceCharge { get; private set; }

			internal bool CadencePrimedThisUse { get; private set; }
			private long lastCadencePhraseUseTick;
			private bool cadenceSessionActive;

			public override void Initialize()
			{
				ResetState();
			}

			public override void SaveData(TagCompound tag)
			{
				if (ProgressionCount > 0)
				{
					List<int> roots = new();
					List<byte> qualities = new();

					for (int i = 0; i < ProgressionCount; i++)
					{
						roots.Add(progressionRoots[i]);
						qualities.Add((byte)progressionQualities[i]);
					}

					tag["ProgressionRoots"] = roots;
					tag["ProgressionQualities"] = qualities;
				}

				if (ActiveProgressionIndex > 0)
					tag["ActiveProgressionIndex"] = ActiveProgressionIndex;

				if (CadenceCharge > 0)
					tag["CadenceCharge"] = CadenceCharge;
			}

			public override void LoadData(TagCompound tag)
			{
				ResetState();

				if (tag.ContainsKey("ProgressionRoots") && tag.ContainsKey("ProgressionQualities"))
				{
					IList<int> roots = tag.GetList<int>("ProgressionRoots");
					IList<byte> qualities = tag.GetList<byte>("ProgressionQualities");

					ProgressionCount = Utils.Clamp(System.Math.Min(roots.Count, qualities.Count), 0, MaxProgressionLength);

					for (int i = 0; i < ProgressionCount; i++)
					{
						progressionRoots[i] = ChordMath.NormalizePitchClass(roots[i]);
						progressionQualities[i] = (ChordQuality)qualities[i];
					}

					ActiveProgressionIndex = ProgressionCount == 0
						? 0
						: Utils.Clamp(tag.ContainsKey("ActiveProgressionIndex") ? tag.GetInt("ActiveProgressionIndex") : 0, 0, ProgressionCount - 1);
				}
				else if (tag.ContainsKey("ChordRoot") || tag.ContainsKey("ChordQuality"))
				{
					progressionRoots[0] = tag.ContainsKey("ChordRoot") ? ChordMath.NormalizePitchClass(tag.GetInt("ChordRoot")) : 0;
					progressionQualities[0] = tag.ContainsKey("ChordQuality") ? (ChordQuality)tag.GetByte("ChordQuality") : ChordQuality.Major;
					ProgressionCount = 1;
					ActiveProgressionIndex = 0;
				}

				CadenceCharge = tag.ContainsKey("CadenceCharge") ? Utils.Clamp(tag.GetInt("CadenceCharge"), 0, 3) : 0;
				CadencePrimedThisUse = false;
			}

			public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendProgressionPacket(toWho, fromWho);

			public override void PostUpdate()
			{
				if (!cadenceSessionActive)
					return;

				bool localRelease = Player.whoAmI == Main.myPlayer && !Player.controlUseItem;
				bool itemChanged = Player.HeldItem == null || Player.HeldItem.type != ModContent.ItemType<Content.Items.Woodcord>();
				bool timedOut = Main.GameUpdateCount - lastCadencePhraseUseTick > CadenceSessionGapTicks;

				if (Player.dead || !Player.active || localRelease || itemChanged || timedOut)
					FinalizeCadencePhrase(sync: true);
			}

			public bool TryGetProgressionChord(int slot, out int root, out ChordQuality quality)
			{
				if (slot < 0 || slot >= ProgressionCount)
				{
					root = 0;
					quality = ChordQuality.Major;
					return false;
				}

				root = progressionRoots[slot];
				quality = progressionQualities[slot];
				return true;
			}

			public string GetProgressionDisplay(string separator = " -> ")
			{
				if (ProgressionCount <= 0)
					return "none";

				List<string> steps = new();
				for (int i = 0; i < ProgressionCount; i++)
					steps.Add(ChordMath.GetDisplayName(progressionRoots[i], progressionQualities[i]));

				return string.Join(separator, steps);
			}

			public bool TryAddChordToProgression(int root, ChordQuality quality, bool sync, out int slotIndex, out bool progressionComplete)
			{
				slotIndex = ProgressionCount;
				progressionComplete = false;

				if (ProgressionCount >= MaxProgressionLength)
					return false;

				progressionRoots[slotIndex] = ChordMath.NormalizePitchClass(root);
				progressionQualities[slotIndex] = quality;
				ProgressionCount++;
				progressionComplete = ProgressionCount >= MaxProgressionLength;

				ResetEditedProgressionState();

				if (sync && Main.netMode != NetmodeID.SinglePlayer)
					SendProgressionPacket();

				return true;
			}

			public bool RemoveLastChord(bool sync)
			{
				if (ProgressionCount <= 0)
					return false;

				ProgressionCount--;
				progressionRoots[ProgressionCount] = 0;
				progressionQualities[ProgressionCount] = ChordQuality.Major;
				ResetEditedProgressionState();

				if (sync && Main.netMode != NetmodeID.SinglePlayer)
					SendProgressionPacket();

				return true;
			}

			public void ClearProgression(bool sync)
			{
				ResetState();

				if (sync && Main.netMode != NetmodeID.SinglePlayer)
					SendProgressionPacket();
			}

			public bool PrepareCadenceUse()
			{
				CadencePrimedThisUse = CadenceCharge >= 3;
				return CadencePrimedThisUse;
			}

			public void RegisterChordUseForCadence(Player player, int root, ChordQuality quality, bool sync)
			{
				cadenceSessionActive = true;
				lastCadencePhraseUseTick = Main.GameUpdateCount;

				if (heldCadencePhrase.Count >= MaxProgressionLength)
					return;

				heldCadencePhrase.Add(new PlayedChord(root, quality));
			}

			public void AdvanceProgressionAfterUse(bool sync)
			{
				if (CadencePrimedThisUse)
					CadenceCharge = 0;

				CadencePrimedThisUse = false;

				if (ProgressionCount > 1)
				{
					int currentIndex = ActiveProgressionIndex;
					int nextIndex = (currentIndex + 1) % ProgressionCount;
					ActiveProgressionIndex = nextIndex;
				}
				else
				{
					ActiveProgressionIndex = 0;
				}

				if (sync && Main.netMode != NetmodeID.SinglePlayer)
					SendProgressionPacket();
			}

			internal void ApplySyncedProgressionState(int progressionCount, int activeIndex, int cadenceCharge, int[] roots, ChordQuality[] qualities)
			{
				ResetState();
				ProgressionCount = Utils.Clamp(progressionCount, 0, MaxProgressionLength);

				for (int i = 0; i < ProgressionCount; i++)
				{
					progressionRoots[i] = ChordMath.NormalizePitchClass(roots[i]);
					progressionQualities[i] = qualities[i];
				}

				ActiveProgressionIndex = ProgressionCount == 0 ? 0 : Utils.Clamp(activeIndex, 0, ProgressionCount - 1);
				CadenceCharge = Utils.Clamp(cadenceCharge, 0, 3);
				CadencePrimedThisUse = false;
			}

			private void ResetEditedProgressionState()
			{
				ActiveProgressionIndex = 0;
				CadenceCharge = 0;
				CadencePrimedThisUse = false;
				ClearCadencePhraseSession();
			}

			private void ResetState()
			{
				for (int i = 0; i < MaxProgressionLength; i++)
				{
					progressionRoots[i] = 0;
					progressionQualities[i] = ChordQuality.Major;
				}

				ProgressionCount = 0;
				ActiveProgressionIndex = 0;
				CadenceCharge = 0;
				CadencePrimedThisUse = false;
				ClearCadencePhraseSession();
			}

			private void FinalizeCadencePhrase(bool sync)
			{
				if (!cadenceSessionActive)
					return;

				ActiveProgressionIndex = 0;

				if (heldCadencePhrase.Count >= 2 && CadenceLibrary.TryMatchPhrase(heldCadencePhrase, out CadenceBookEntry entry))
				{
					int chargeBefore = CadenceCharge;
					CadenceCharge = Utils.Clamp(CadenceCharge + entry.CadenceGain, 0, 3);
					bool encoreReady = chargeBefore < 3 && CadenceCharge >= 3;

					Player.GetModPlayer<SongbookPlayer>().TryUnlock(entry, sync);

					if (Main.myPlayer == Player.whoAmI && Main.netMode != NetmodeID.Server)
					{
						CombatText.NewText(Player.Hitbox, entry.AccentColor, $"{entry.Title} +{entry.CadenceGain}", dramatic: false);

						if (encoreReady)
							CombatText.NewText(Player.Hitbox, new Microsoft.Xna.Framework.Color(255, 180, 80), "Encore Ready!", dramatic: true);
					}
				}

				ClearCadencePhraseSession();

				if (sync && Main.netMode != NetmodeID.SinglePlayer)
					SendProgressionPacket();
			}

			private void ClearCadencePhraseSession()
			{
				heldCadencePhrase.Clear();
				lastCadencePhraseUseTick = 0;
				cadenceSessionActive = false;
			}

			private void SendProgressionPacket(int toWho = -1, int fromWho = -1)
			{
				if (Main.netMode == NetmodeID.SinglePlayer)
					return;

				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)ZacksMusicianship.PacketType.SyncProgressionState);
				packet.Write((byte)Player.whoAmI);
				packet.Write((byte)ProgressionCount);
				packet.Write((byte)ActiveProgressionIndex);
				packet.Write((byte)CadenceCharge);

				for (int i = 0; i < MaxProgressionLength; i++)
				{
					packet.Write((byte)progressionRoots[i]);
					packet.Write((byte)progressionQualities[i]);
				}

				packet.Send(toWho, fromWho);
			}
		}
	}
