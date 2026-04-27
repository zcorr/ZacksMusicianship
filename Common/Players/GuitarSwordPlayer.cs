using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ZacksMusicianship.Common.Cadences;
using ZacksMusicianship.Common.Chords;
using ZacksMusicianship.Common.Rhythm;

	namespace ZacksMusicianship.Common.Players
	{
		public class GuitarSwordPlayer : ModPlayer
		{
			public const int MaxProgressionLength = 4;
			private const int StrumSessionGapTicks = 32;

			private readonly int[] progressionRoots = new int[MaxProgressionLength];
			private readonly ChordQuality[] progressionQualities = new ChordQuality[MaxProgressionLength];
			private readonly List<PlayedChord> heldCadencePhrase = new();
			private int activeStrumPatternIndex = StrumPatternLibrary.DefaultPatternIndex;

			public int ProgressionCount { get; private set; }
			public int ActiveProgressionIndex { get; private set; }
			public int ChordRoot => ProgressionCount > 0
				? progressionRoots[Utils.Clamp(ActiveProgressionIndex, 0, ProgressionCount - 1)]
				: 0;
			public ChordQuality CurrentQuality => ProgressionCount > 0
				? progressionQualities[Utils.Clamp(ActiveProgressionIndex, 0, ProgressionCount - 1)]
				: ChordQuality.Major;
			public int CadenceCharge { get; private set; }
			public int ActiveStrumPatternIndex => activeStrumPatternIndex;
			public int ActiveStrumStepIndex { get; private set; }
			public StrumPattern CurrentStrumPattern => StrumPatternLibrary.GetPattern(activeStrumPatternIndex);
			public StrumStroke CurrentStrumStroke => CurrentStrumPattern.GetStroke(ActiveStrumStepIndex);
			public int CurrentStrumSubdivisionSpan => CurrentStrumPattern.GetSubdivisionSpanAfter(ActiveStrumStepIndex);

			internal bool CadencePrimedThisUse { get; private set; }
			private long lastStrumUseTick;
			private bool strumSessionActive;
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

				if (ActiveStrumPatternIndex != StrumPatternLibrary.DefaultPatternIndex)
					tag["ActiveStrumPatternIndex"] = ActiveStrumPatternIndex;

				if (ActiveStrumStepIndex > 0)
					tag["ActiveStrumStepIndex"] = ActiveStrumStepIndex;

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

				activeStrumPatternIndex = tag.ContainsKey("ActiveStrumPatternIndex")
					? StrumPatternLibrary.NormalizePatternIndex(tag.GetInt("ActiveStrumPatternIndex"))
					: StrumPatternLibrary.DefaultPatternIndex;
				ActiveStrumStepIndex = Utils.Clamp(
					tag.ContainsKey("ActiveStrumStepIndex") ? tag.GetInt("ActiveStrumStepIndex") : 0,
					0,
					CurrentStrumPattern.StepCount - 1);
				CadenceCharge = tag.ContainsKey("CadenceCharge") ? Utils.Clamp(tag.GetInt("CadenceCharge"), 0, 3) : 0;
				CadencePrimedThisUse = false;
			}

			public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendProgressionPacket(toWho, fromWho);

			public override void PostUpdate()
			{
				if (!strumSessionActive && !cadenceSessionActive)
					return;

				bool localRelease = Player.whoAmI == Main.myPlayer && !Player.controlUseItem;
				bool itemChanged = Player.HeldItem == null || Player.HeldItem.type != ModContent.ItemType<Content.Items.Woodcord>();
				bool activelyUsingWoodcord = !itemChanged && (Player.itemAnimation > 0 || (Player.whoAmI == Main.myPlayer && Player.controlUseItem));
				bool timedOut = !activelyUsingWoodcord && Main.GameUpdateCount - lastStrumUseTick > StrumSessionGapTicks;

				if (Player.dead || !Player.active || localRelease || itemChanged || timedOut)
					FinalizePerformanceSession(sync: true);
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

			public int GetSlowestProgressionUseTime(System.Func<ChordQuality, int> getUseTime)
			{
				if (getUseTime == null)
					return 1;

				int slowestUseTime = System.Math.Max(1, getUseTime(CurrentQuality));

				for (int i = 0; i < ProgressionCount; i++)
					slowestUseTime = System.Math.Max(slowestUseTime, getUseTime(progressionQualities[i]));

				return slowestUseTime;
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

			public void RegisterStrumUse()
			{
				strumSessionActive = true;
				lastStrumUseTick = Main.GameUpdateCount;
			}

			public void RegisterChordUseForCadence(Player player, int root, ChordQuality quality, bool sync)
			{
				RegisterStrumUse();
				cadenceSessionActive = true;

				if (heldCadencePhrase.Count >= MaxProgressionLength)
					return;

				heldCadencePhrase.Add(new PlayedChord(root, quality));
			}

			public bool AdvanceStrumPatternAfterUse(bool sync)
			{
				if (CadencePrimedThisUse)
					CadenceCharge = 0;

				CadencePrimedThisUse = false;
				bool patternComplete = AdvanceStrumStep();

				if (patternComplete)
					AdvanceProgressionIndex();

				if (sync && Main.netMode != NetmodeID.SinglePlayer)
					SendProgressionPacket();

				return patternComplete;
			}

			public void SetStrumPattern(int patternIndex, bool sync)
			{
				int normalizedPatternIndex = StrumPatternLibrary.NormalizePatternIndex(patternIndex);

				if (activeStrumPatternIndex == normalizedPatternIndex && ActiveStrumStepIndex == 0)
					return;

				activeStrumPatternIndex = normalizedPatternIndex;
				ActiveStrumStepIndex = 0;
				ClearStrumSession();
				ClearCadencePhraseSession();

				if (sync && Main.netMode != NetmodeID.SinglePlayer)
					SendProgressionPacket();
			}

			internal void ApplySyncedProgressionState(int progressionCount, int activeIndex, int activeStrumPatternIndex,
				int activeStrumStepIndex, int cadenceCharge, int[] roots, ChordQuality[] qualities)
			{
				ResetState();
				ProgressionCount = Utils.Clamp(progressionCount, 0, MaxProgressionLength);

				for (int i = 0; i < ProgressionCount; i++)
				{
					progressionRoots[i] = ChordMath.NormalizePitchClass(roots[i]);
					progressionQualities[i] = qualities[i];
				}

				ActiveProgressionIndex = ProgressionCount == 0 ? 0 : Utils.Clamp(activeIndex, 0, ProgressionCount - 1);
				this.activeStrumPatternIndex = StrumPatternLibrary.NormalizePatternIndex(activeStrumPatternIndex);
				ActiveStrumStepIndex = Utils.Clamp(activeStrumStepIndex, 0, CurrentStrumPattern.StepCount - 1);
				CadenceCharge = Utils.Clamp(cadenceCharge, 0, 3);
				CadencePrimedThisUse = false;
			}

			private void ResetEditedProgressionState()
			{
				ActiveProgressionIndex = 0;
				ActiveStrumStepIndex = 0;
				CadenceCharge = 0;
				CadencePrimedThisUse = false;
				ClearStrumSession();
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
				activeStrumPatternIndex = StrumPatternLibrary.DefaultPatternIndex;
				ActiveStrumStepIndex = 0;
				CadenceCharge = 0;
				CadencePrimedThisUse = false;
				ClearStrumSession();
				ClearCadencePhraseSession();
			}

			private bool AdvanceStrumStep()
			{
				StrumPattern pattern = CurrentStrumPattern;
				int currentStep = Utils.Clamp(ActiveStrumStepIndex, 0, pattern.StepCount - 1);
				int nextStep = currentStep + 1;
				bool patternComplete = nextStep >= pattern.StepCount;

				ActiveStrumStepIndex = patternComplete ? 0 : nextStep;
				return patternComplete;
			}

			private void AdvanceProgressionIndex()
			{
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
			}

			private void FinalizePerformanceSession(bool sync)
			{
				if (!strumSessionActive && !cadenceSessionActive)
					return;

				ActiveProgressionIndex = 0;
				ActiveStrumStepIndex = 0;
				ClearStrumSession();

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
				cadenceSessionActive = false;
			}

			private void ClearStrumSession()
			{
				lastStrumUseTick = 0;
				strumSessionActive = false;
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
				packet.Write((byte)ActiveStrumPatternIndex);
				packet.Write((byte)ActiveStrumStepIndex);
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
