using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using ZacksMusicianship.Common.Chords;
using ZacksMusicianship.Common.Players;
using ZacksMusicianship.Common.Systems;

namespace ZacksMusicianship.Common.UI
{
	public class ChordComposerUIState : UIState
	{
		private const int MaxSelectedNotes = 3;
		private const float PanelWidth = 760f;
		private const float PanelHeight = 800f;
		private const float OuterPadding = 24f;
		private const float ContentWidth = PanelWidth - OuterPadding * 2f;
		private const float SectionGap = 12f;
		private const float SectionPadding = 14f;
		private const float InnerContentWidth = ContentWidth - SectionPadding * 2f;
		private const float ButtonGap = 8f;
		private const float NoteButtonWidth = (InnerContentWidth - ButtonGap * 5f) / 6f;
		private const float ProgressionSlotWidth = (InnerContentWidth - ButtonGap * 3f) / 4f;
		private const float ActionButtonWidth = (InnerContentWidth - ButtonGap * 4f) / 5f;
		private const float NoteButtonHeight = 40f;
		private const float SlotHeight = 42f;
		private const float ActionButtonHeight = 44f;
		private bool initialized;

		private static readonly string[] OrderedNotes =
		{
			"C", "C#", "D", "D#", "E", "F",
			"F#", "G", "G#", "A", "A#", "B"
		};

		private static readonly Color[] NoteColors =
		{
			new(207, 88, 88),
			new(191, 84, 130),
			new(179, 108, 209),
			new(108, 104, 209),
			new(86, 145, 214),
			new(74, 181, 191),
			new(80, 192, 141),
			new(116, 186, 84),
			new(170, 184, 78),
			new(214, 165, 80),
			new(221, 124, 71),
			new(219, 94, 94),
		};

		private readonly bool[] selectedNotes = new bool[12];
		private readonly UITextPanel<string>[] noteButtons = new UITextPanel<string>[12];
		private readonly UITextPanel<string>[] progressionSlots = new UITextPanel<string>[GuitarSwordPlayer.MaxProgressionLength];

		private WrappedTextBlock selectionText;
		private WrappedTextBlock statusText;
		private WrappedTextBlock effectText;
		private WrappedTextBlock cadenceText;
		private ChordStaffPreview staffPreview;
		private UITextPanel<string> previewButton;
		private UITextPanel<string> addButton;
		private UITextPanel<string> undoButton;
		private UITextPanel<string> resetButton;

		public override void OnInitialize()
		{
			initialized = true;

			UIPanel panel = new();
			panel.Width.Set(PanelWidth, 0f);
			panel.Height.Set(PanelHeight, 0f);
			panel.HAlign = 0.5f;
			panel.VAlign = 0.5f;
			panel.SetPadding(0f);
			panel.BackgroundColor = new Color(25, 29, 43, 240);
			panel.BorderColor = new Color(113, 94, 73);
			Append(panel);

			float cursorY = OuterPadding;

			UIText title = new("Woodcord Chord Builder", 1.06f, false);
			title.Left.Set(OuterPadding, 0f);
			title.Top.Set(cursorY, 0f);
			panel.Append(title);
			cursorY += 34f;

			WrappedTextBlock instructions = new("Pick exactly 3 notes. Save up to 4 chords, then Woodcord advances to the next chord after each full strum pattern.", 0.78f);
			instructions.Width.Set(ContentWidth, 0f);
			instructions.Height.Set(24f, 0f);
			instructions.Left.Set(OuterPadding, 0f);
			instructions.Top.Set(cursorY, 0f);
			instructions.TextColor = new Color(225, 225, 230);
			panel.Append(instructions);
			cursorY += 32f;

			UIPanel staffPanel = CreateSectionPanel(cursorY, 152f);
			panel.Append(staffPanel);

			staffPreview = new ChordStaffPreview();
			staffPreview.Width.Set(0f, 1f);
			staffPreview.Height.Set(0f, 1f);
			staffPanel.Append(staffPreview);
			cursorY += 152f + SectionGap;

			UIPanel notePanel = CreateSectionPanel(cursorY, 146f);
			panel.Append(notePanel);

			UIText noteTitle = CreateSectionHeader("Choose Notes");
			notePanel.Append(noteTitle);

			for (int i = 0; i < OrderedNotes.Length; i++)
			{
				int index = i;
				int row = i / 6;
				int col = i % 6;

				UITextPanel<string> button = new(OrderedNotes[i], 0.9f, false);
				button.Width.Set(NoteButtonWidth, 0f);
				button.Height.Set(NoteButtonHeight, 0f);
				button.Left.Set(col * (NoteButtonWidth + ButtonGap), 0f);
				button.Top.Set(28f + row * (NoteButtonHeight + ButtonGap), 0f);
				button.BackgroundColor = new Color(49, 57, 79);
				button.BorderColor = NoteColors[i] * 0.75f;
				button.OnLeftClick += (_, _) => ToggleNote(index);
				notePanel.Append(button);
				noteButtons[i] = button;
			}
			cursorY += 146f + SectionGap;

			UIPanel progressionPanel = CreateSectionPanel(cursorY, 102f);
			panel.Append(progressionPanel);

			UIText progressionTitle = CreateSectionHeader("Saved Progression");
			progressionPanel.Append(progressionTitle);

			for (int i = 0; i < progressionSlots.Length; i++)
			{
				int slotIndex = i;
				UITextPanel<string> slot = new($"{i + 1}. Empty", 0.72f, false);
				slot.Width.Set(ProgressionSlotWidth, 0f);
				slot.Height.Set(SlotHeight, 0f);
				slot.Left.Set(i * (ProgressionSlotWidth + ButtonGap), 0f);
				slot.Top.Set(28f, 0f);
				slot.BackgroundColor = new Color(49, 57, 79);
				slot.BorderColor = new Color(96, 96, 96);
				slot.OnLeftClick += (_, _) => LoadSavedSlot(slotIndex);
				progressionPanel.Append(slot);
				progressionSlots[i] = slot;
			}
			cursorY += 102f + SectionGap;

			UIPanel infoPanel = CreateSectionPanel(cursorY, 168f);
			panel.Append(infoPanel);

			UIText infoTitle = CreateSectionHeader("Current State");
			infoPanel.Append(infoTitle);

			selectionText = new WrappedTextBlock("Selection: none", 0.84f);
			selectionText.Width.Set(0f, 1f);
			selectionText.Height.Set(20f, 0f);
			selectionText.Top.Set(26f, 0f);
			infoPanel.Append(selectionText);

			statusText = new WrappedTextBlock("Chord: none", 0.82f);
			statusText.Width.Set(0f, 1f);
			statusText.Height.Set(20f, 0f);
			statusText.Top.Set(48f, 0f);
			infoPanel.Append(statusText);

			effectText = new WrappedTextBlock("Loop: none", 0.8f);
			effectText.Width.Set(0f, 1f);
			effectText.Height.Set(40f, 0f);
			effectText.Top.Set(72f, 0f);
			infoPanel.Append(effectText);

			cadenceText = new WrappedTextBlock("Cadence: 0/3", 0.8f);
			cadenceText.Width.Set(0f, 1f);
			cadenceText.Height.Set(26f, 0f);
			cadenceText.Top.Set(114f, 0f);
			infoPanel.Append(cadenceText);
			cursorY += 168f + SectionGap;

			previewButton = CreateActionButton("Preview", () => PreviewSelectedChord());
			previewButton.Left.Set(OuterPadding, 0f);
			previewButton.Top.Set(cursorY, 0f);
			panel.Append(previewButton);

			addButton = CreateActionButton("Add Step", () => AddSelectedChord());
			addButton.Left.Set(OuterPadding + 1 * (ActionButtonWidth + ButtonGap), 0f);
			addButton.Top.Set(cursorY, 0f);
			panel.Append(addButton);

			undoButton = CreateActionButton("Undo Step", () => UndoLastChord());
			undoButton.Left.Set(OuterPadding + 2 * (ActionButtonWidth + ButtonGap), 0f);
			undoButton.Top.Set(cursorY, 0f);
			undoButton.BackgroundColor = new Color(87, 72, 60);
			panel.Append(undoButton);

			resetButton = CreateActionButton("Reset Loop", () => ResetProgression());
			resetButton.Left.Set(OuterPadding + 3 * (ActionButtonWidth + ButtonGap), 0f);
			resetButton.Top.Set(cursorY, 0f);
			resetButton.BackgroundColor = new Color(79, 54, 54);
			panel.Append(resetButton);

			UITextPanel<string> closeButton = CreateActionButton("Close", () => ChordComposerSystem.Close());
			closeButton.Left.Set(OuterPadding + 4 * (ActionButtonWidth + ButtonGap), 0f);
			closeButton.Top.Set(cursorY, 0f);
			closeButton.BackgroundColor = new Color(60, 64, 88);
			panel.Append(closeButton);
			cursorY += ActionButtonHeight + SectionGap;

			WrappedTextBlock footer = new("Preview plays the selected chord. Add Step saves it. Click a saved step to load it back into the builder.", 0.72f);
			footer.Width.Set(ContentWidth, 0f);
			footer.Height.Set(30f, 0f);
			footer.Left.Set(OuterPadding, 0f);
			footer.Top.Set(cursorY, 0f);
			footer.TextColor = new Color(188, 188, 198);
			panel.Append(footer);

			RefreshUi();
		}

		public void LoadFromPlayer(Player player)
		{
			ClearSelection();

			if (initialized)
				RefreshUi();
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			Main.LocalPlayer.mouseInterface = true;

			if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
				ChordComposerSystem.Close();
		}

		private void ToggleNote(int pitchClass)
		{
			if (!selectedNotes[pitchClass] && selectedNotes.Count(selected => selected) >= MaxSelectedNotes)
				return;

			selectedNotes[pitchClass] = !selectedNotes[pitchClass];
			RefreshUi();
		}

		private void ClearSelection()
		{
			for (int i = 0; i < selectedNotes.Length; i++)
				selectedNotes[i] = false;
		}

		private void RefreshUi()
		{
			if (!initialized || selectionText == null || statusText == null || effectText == null || cadenceText == null || staffPreview == null || addButton == null)
				return;

			GuitarSwordPlayer currentPlayerState = Main.LocalPlayer.GetModPlayer<GuitarSwordPlayer>();

			for (int i = 0; i < noteButtons.Length; i++)
			{
				bool selected = selectedNotes[i];
				Color noteColor = NoteColors[i];
				UITextPanel<string> button = noteButtons[i];
				if (button == null)
					continue;
				button.BackgroundColor = selected ? noteColor : new Color(49, 57, 79);
				button.BorderColor = selected ? Color.White : noteColor * 0.75f;
			}

			List<string> selectedNames = new();
			for (int i = 0; i < selectedNotes.Length; i++)
			{
				if (selectedNotes[i])
					selectedNames.Add(ChordMath.GetNoteName(i));
			}

			selectionText.SetText(selectedNames.Count == 0
				? "Selection: none"
				: $"Selection: {string.Join(", ", selectedNames)}");
			selectionText.TextColor = selectedNames.Count == 0 ? Color.Silver : Color.White;

			if (ChordMath.TryRecognize(selectedNotes, out int root, out ChordQuality quality))
			{
				statusText.SetText(BuildStatusText(currentPlayerState, root, quality));
				statusText.TextColor = currentPlayerState.ProgressionCount >= GuitarSwordPlayer.MaxProgressionLength
					? new Color(230, 185, 125)
					: ChordMath.GetColor(quality);

				effectText.SetText(BuildEffectText(currentPlayerState, true, root, quality));
				effectText.TextColor = new Color(225, 225, 230);
				cadenceText.SetText(BuildCadenceText(currentPlayerState, true, root, quality));
				cadenceText.TextColor = currentPlayerState.CadenceCharge >= 3 ? new Color(255, 180, 80) : new Color(150, 230, 210);

				previewButton.SetText($"Preview {GetCompactChordName(root, quality)}");
				previewButton.BackgroundColor = ChordMath.GetColor(quality) * 0.65f;
				previewButton.BorderColor = ChordMath.GetColor(quality);

				addButton.SetText(currentPlayerState.ProgressionCount >= GuitarSwordPlayer.MaxProgressionLength
					? "Progression Full"
					: $"Add Step {currentPlayerState.ProgressionCount + 1}");
				addButton.BackgroundColor = currentPlayerState.ProgressionCount >= GuitarSwordPlayer.MaxProgressionLength
					? new Color(62, 62, 62)
					: ChordMath.GetColor(quality) * 0.5f;
				addButton.BorderColor = currentPlayerState.ProgressionCount >= GuitarSwordPlayer.MaxProgressionLength
					? new Color(96, 96, 96)
					: Color.White;

				UpdateProgressionSlots(currentPlayerState, hasPreview: true, root, quality);
				staffPreview.SetPreview(selectedNotes, recognized: true, root, quality);
			}
			else
			{
				string message = selectedNames.Count switch
				{
					0 => "Chord: none",
					< MaxSelectedNotes => "Chord: choose 3 notes",
					_ => "Chord: unsupported voicing",
				};

				statusText.SetText(message);
				statusText.TextColor = Color.Silver;
				effectText.SetText(BuildEffectText(currentPlayerState, false, 0, ChordQuality.Major));
				effectText.TextColor = new Color(225, 225, 230);
				cadenceText.SetText(BuildCadenceText(currentPlayerState, false, 0, ChordQuality.Major));
				cadenceText.TextColor = currentPlayerState.CadenceCharge >= 3 ? new Color(255, 180, 80) : new Color(150, 230, 210);

				previewButton.SetText("Preview");
				previewButton.BackgroundColor = new Color(62, 62, 62);
				previewButton.BorderColor = new Color(96, 96, 96);

				addButton.SetText(currentPlayerState.ProgressionCount >= GuitarSwordPlayer.MaxProgressionLength ? "Progression Full" : "Add Step");
				addButton.BackgroundColor = new Color(62, 62, 62);
				addButton.BorderColor = new Color(96, 96, 96);

				UpdateProgressionSlots(currentPlayerState, hasPreview: false, 0, ChordQuality.Major);
				staffPreview.SetPreview(selectedNotes, recognized: false, 0, ChordQuality.Major);
			}

			undoButton.BackgroundColor = currentPlayerState.ProgressionCount > 0 ? new Color(87, 72, 60) : new Color(62, 62, 62);
			undoButton.BorderColor = currentPlayerState.ProgressionCount > 0 ? new Color(170, 140, 110) : new Color(96, 96, 96);
			resetButton.BackgroundColor = currentPlayerState.ProgressionCount > 0 ? new Color(79, 54, 54) : new Color(62, 62, 62);
			resetButton.BorderColor = currentPlayerState.ProgressionCount > 0 ? new Color(184, 104, 104) : new Color(96, 96, 96);
		}

		private void PreviewSelectedChord()
		{
			if (!ChordMath.TryRecognize(selectedNotes, out int root, out ChordQuality quality))
				return;

			SoundEngine.PlaySound(ChordMath.GetSoundStyle(root, quality), Main.LocalPlayer.Center);
		}

		private void AddSelectedChord()
		{
			if (!ChordMath.TryRecognize(selectedNotes, out int root, out ChordQuality quality))
				return;

			Player player = Main.LocalPlayer;
			GuitarSwordPlayer chordPlayer = player.GetModPlayer<GuitarSwordPlayer>();
			if (!chordPlayer.TryAddChordToProgression(root, quality, sync: true, out int slotIndex, out bool progressionComplete))
				return;

			SoundEngine.PlaySound(ChordMath.GetSoundStyle(root, quality), player.Center);
			CombatText.NewText(player.Hitbox, ChordMath.GetColor(quality), $"Saved Step {slotIndex + 1}", dramatic: false);

			if (progressionComplete)
				CombatText.NewText(player.Hitbox, new Color(255, 180, 80), "4-Chord Progression Ready", dramatic: true);

			ClearSelection();
			RefreshUi();
		}

		private void UndoLastChord()
		{
			Player player = Main.LocalPlayer;
			GuitarSwordPlayer chordPlayer = player.GetModPlayer<GuitarSwordPlayer>();
			if (!chordPlayer.RemoveLastChord(sync: true))
				return;

			ClearSelection();
			CombatText.NewText(player.Hitbox, new Color(240, 210, 170), "Removed Last Step", dramatic: false);
			RefreshUi();
		}

		private void ResetProgression()
		{
			Player player = Main.LocalPlayer;
			GuitarSwordPlayer chordPlayer = player.GetModPlayer<GuitarSwordPlayer>();
			if (chordPlayer.ProgressionCount <= 0)
				return;

			chordPlayer.ClearProgression(sync: true);
			ClearSelection();
			CombatText.NewText(player.Hitbox, new Color(230, 140, 140), "Progression Cleared", dramatic: false);
			RefreshUi();
		}

		private void LoadSavedSlot(int slotIndex)
		{
			GuitarSwordPlayer chordPlayer = Main.LocalPlayer.GetModPlayer<GuitarSwordPlayer>();
			if (!chordPlayer.TryGetProgressionChord(slotIndex, out int root, out ChordQuality quality))
				return;

			ClearSelection();
			foreach (int pitchClass in ChordMath.GetPitchClasses(root, quality))
				selectedNotes[pitchClass] = true;

			RefreshUi();
		}

		private void UpdateProgressionSlots(GuitarSwordPlayer chordPlayer, bool hasPreview, int previewRoot, ChordQuality previewQuality)
		{
			for (int i = 0; i < progressionSlots.Length; i++)
			{
				UITextPanel<string> slot = progressionSlots[i];
				if (slot == null)
					continue;

				if (chordPlayer.TryGetProgressionChord(i, out int root, out ChordQuality quality))
				{
					bool active = chordPlayer.ProgressionCount > 0 && i == chordPlayer.ActiveProgressionIndex;
					slot.SetText($"{i + 1}. {GetCompactChordName(root, quality)}");
					slot.BackgroundColor = active ? ChordMath.GetColor(quality) * 0.62f : ChordMath.GetColor(quality) * 0.34f;
					slot.BorderColor = active ? Color.White : ChordMath.GetColor(quality);
					continue;
				}

				if (hasPreview && i == chordPlayer.ProgressionCount && chordPlayer.ProgressionCount < GuitarSwordPlayer.MaxProgressionLength)
				{
					slot.SetText($"{i + 1}. + {GetCompactChordName(previewRoot, previewQuality)}");
					slot.BackgroundColor = ChordMath.GetColor(previewQuality) * 0.26f;
					slot.BorderColor = ChordMath.GetColor(previewQuality);
					continue;
				}

				slot.SetText($"{i + 1}. Empty");
				slot.BackgroundColor = new Color(49, 57, 79);
				slot.BorderColor = new Color(96, 96, 96);
			}
		}

		private string BuildStatusText(GuitarSwordPlayer chordPlayer, int root, ChordQuality quality)
		{
			string chordName = ChordMath.GetDisplayName(root, quality);
			if (chordPlayer.ProgressionCount >= GuitarSwordPlayer.MaxProgressionLength)
				return $"Chord: {chordName}. Progression full.";

			if (chordPlayer.ProgressionCount == 0)
				return $"Chord: {chordName}. Add as step 1/4.";

			return $"Chord: {chordName}. Add as step {chordPlayer.ProgressionCount + 1}/4.";
		}

		private string BuildEffectText(GuitarSwordPlayer chordPlayer, bool hasRecognizedChord, int root, ChordQuality quality)
		{
			if (hasRecognizedChord)
			{
				string selectedEffect = ChordMath.GetDescription(quality);
				if (chordPlayer.ProgressionCount > 0)
				{
					string activeChord = ChordMath.GetDisplayName(chordPlayer.ChordRoot, chordPlayer.CurrentQuality);
					return $"Loop: {chordPlayer.GetProgressionDisplay()}. Active {chordPlayer.ActiveProgressionIndex + 1}/{chordPlayer.ProgressionCount}: {activeChord}. Preview: {selectedEffect}";
				}

				return $"Loop: none. Preview: {selectedEffect}";
			}

			if (chordPlayer.ProgressionCount > 0)
			{
				string activeChord = ChordMath.GetDisplayName(chordPlayer.ChordRoot, chordPlayer.CurrentQuality);
				return $"Loop: {chordPlayer.GetProgressionDisplay()}. Active {chordPlayer.ActiveProgressionIndex + 1}/{chordPlayer.ProgressionCount}: {activeChord}.";
			}

			return "Loop: none. Unsaved state uses C Major.";
		}

		private string BuildCadenceText(GuitarSwordPlayer chordPlayer, bool hasRecognizedChord, int previewRoot, ChordQuality previewQuality)
		{
			string meterText = chordPlayer.CadenceCharge >= 3 ? "Encore Ready" : $"{chordPlayer.CadenceCharge}/3";
			List<int> roots = new();
			List<ChordQuality> qualities = new();
			CopyProgression(chordPlayer, roots, qualities);

			if (hasRecognizedChord && chordPlayer.ProgressionCount < GuitarSwordPlayer.MaxProgressionLength)
			{
				roots.Add(previewRoot);
				qualities.Add(previewQuality);
				ChordMath.EvaluateProgressionCadenceTotal(roots, qualities, roots.Count, out string previewSummary);
				return $"Cadence: {meterText}. Release after holding this {roots.Count}-chord phrase. {ShortenCadenceSummary(previewSummary)}";
			}

			ChordMath.EvaluateProgressionCadenceTotal(roots, qualities, roots.Count, out string currentSummary);
			return $"Cadence: {meterText}. Release after a 2-4 chord phrase. {ShortenCadenceSummary(currentSummary)}";
		}

		private void CopyProgression(GuitarSwordPlayer chordPlayer, List<int> roots, List<ChordQuality> qualities)
		{
			for (int i = 0; i < chordPlayer.ProgressionCount; i++)
			{
				if (!chordPlayer.TryGetProgressionChord(i, out int root, out ChordQuality quality))
					continue;

				roots.Add(root);
				qualities.Add(quality);
			}
		}

		private string GetCompactChordName(int root, ChordQuality quality)
		{
			string qualityLabel = quality switch
			{
				ChordQuality.Major => "Maj",
				ChordQuality.Minor => "Min",
				ChordQuality.Diminished => "Dim",
				ChordQuality.Suspended => "Sus4",
				_ => "Chord",
			};

			return $"{ChordMath.GetNoteName(root)} {qualityLabel}";
		}

		private static string ShortenCadenceSummary(string summary) =>
			summary
				.Replace("Phrase cadence: ", string.Empty);

		private static UIText CreateSectionHeader(string text)
		{
			UIText header = new(text, 0.82f, false);
			header.Top.Set(0f, 0f);
			header.TextColor = new Color(230, 223, 210);
			return header;
		}

		private static UITextPanel<string> CreateActionButton(string text, Action onClick)
		{
			UITextPanel<string> button = new(text, 0.84f, false);
			button.Width.Set(ActionButtonWidth, 0f);
			button.Height.Set(ActionButtonHeight, 0f);
			button.BackgroundColor = new Color(62, 62, 62);
			button.BorderColor = new Color(96, 96, 96);
			button.OnLeftClick += (_, _) => onClick();
			return button;
		}

		private static UIPanel CreateSectionPanel(float top, float height)
		{
			UIPanel section = new();
			section.Width.Set(ContentWidth, 0f);
			section.Height.Set(height, 0f);
			section.Left.Set(OuterPadding, 0f);
			section.Top.Set(top, 0f);
			section.SetPadding(SectionPadding);
			section.BackgroundColor = new Color(31, 36, 53, 220);
			section.BorderColor = new Color(95, 82, 69);
			return section;
		}
	}
}
