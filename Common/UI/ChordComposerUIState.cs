using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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

		private WrappedTextBlock selectionText;
		private WrappedTextBlock statusText;
		private WrappedTextBlock effectText;
		private WrappedTextBlock cadenceText;
		private ChordStaffPreview staffPreview;
		private UITextPanel<string> previewButton;
		private UITextPanel<string> commitButton;

		public override void OnInitialize()
		{
			initialized = true;

			UIPanel panel = new();
			panel.Width.Set(700f, 0f);
			panel.Height.Set(612f, 0f);
			panel.HAlign = 0.5f;
			panel.VAlign = 0.5f;
			panel.BackgroundColor = new Color(25, 29, 43, 240);
			panel.BorderColor = new Color(113, 94, 73);
			Append(panel);

			UIText title = new("Woodcord Chord Builder", 1f, true);
			title.Left.Set(24f, 0f);
			title.Top.Set(18f, 0f);
			panel.Append(title);

			WrappedTextBlock instructions = new("Pick exactly 3 notes. Supported chord qualities: major, minor, diminished, and sus4.", 0.85f);
			instructions.Width.Set(648f, 0f);
			instructions.Height.Set(34f, 0f);
			instructions.Left.Set(24f, 0f);
			instructions.Top.Set(56f, 0f);
			instructions.TextColor = new Color(225, 225, 230);
			panel.Append(instructions);

			UIPanel staffPanel = new();
			staffPanel.Width.Set(648f, 0f);
			staffPanel.Height.Set(138f, 0f);
			staffPanel.Left.Set(24f, 0f);
			staffPanel.Top.Set(96f, 0f);
			staffPanel.BackgroundColor = new Color(31, 36, 53, 220);
			staffPanel.BorderColor = new Color(95, 82, 69);
			panel.Append(staffPanel);

			staffPreview = new ChordStaffPreview();
			staffPreview.Width.Set(-20f, 1f);
			staffPreview.Height.Set(-20f, 1f);
			staffPreview.Left.Set(10f, 0f);
			staffPreview.Top.Set(10f, 0f);
			staffPanel.Append(staffPreview);

			for (int i = 0; i < OrderedNotes.Length; i++)
			{
				int index = i;
				int row = i / 6;
				int col = i % 6;

				UITextPanel<string> button = new(OrderedNotes[i], 0.95f, false);
				button.Width.Set(92f, 0f);
				button.Height.Set(40f, 0f);
				button.Left.Set(28f + col * 104f, 0f);
				button.Top.Set(252f + row * 52f, 0f);
				button.BackgroundColor = new Color(49, 57, 79);
				button.BorderColor = NoteColors[i] * 0.75f;
				button.OnLeftClick += (_, _) => ToggleNote(index);
				panel.Append(button);
				noteButtons[i] = button;
			}

			selectionText = new WrappedTextBlock("Selected notes: none", 0.92f);
			selectionText.Width.Set(648f, 0f);
			selectionText.Height.Set(28f, 0f);
			selectionText.Left.Set(24f, 0f);
			selectionText.Top.Set(360f, 0f);
			panel.Append(selectionText);

			statusText = new WrappedTextBlock("Recognized chord: none", 0.92f);
			statusText.Width.Set(648f, 0f);
			statusText.Height.Set(28f, 0f);
			statusText.Left.Set(24f, 0f);
			statusText.Top.Set(390f, 0f);
			panel.Append(statusText);

			effectText = new WrappedTextBlock("Build a valid triad to arm the weapon with that chord quality.", 0.84f);
			effectText.Width.Set(648f, 0f);
			effectText.Height.Set(42f, 0f);
			effectText.Left.Set(24f, 0f);
			effectText.Top.Set(420f, 0f);
			panel.Append(effectText);

			cadenceText = new WrappedTextBlock("Cadence: 0/3", 0.8f);
			cadenceText.Width.Set(648f, 0f);
			cadenceText.Height.Set(46f, 0f);
			cadenceText.Left.Set(24f, 0f);
			cadenceText.Top.Set(462f, 0f);
			panel.Append(cadenceText);

			previewButton = new UITextPanel<string>("Preview Chord", 0.9f, false);
			previewButton.Width.Set(150f, 0f);
			previewButton.Height.Set(44f, 0f);
			previewButton.Left.Set(24f, 0f);
			previewButton.Top.Set(516f, 0f);
			previewButton.OnLeftClick += (_, _) => PreviewSelectedChord();
			panel.Append(previewButton);

			commitButton = new UITextPanel<string>("Commit Chord", 0.9f, false);
			commitButton.Width.Set(160f, 0f);
			commitButton.Height.Set(44f, 0f);
			commitButton.Left.Set(188f, 0f);
			commitButton.Top.Set(516f, 0f);
			commitButton.OnLeftClick += (_, _) => CommitSelectedChord();
			panel.Append(commitButton);

			UITextPanel<string> clearButton = new("Clear", 0.9f, false);
			clearButton.Width.Set(110f, 0f);
			clearButton.Height.Set(44f, 0f);
			clearButton.Left.Set(366f, 0f);
			clearButton.Top.Set(516f, 0f);
			clearButton.BackgroundColor = new Color(79, 54, 54);
			clearButton.OnLeftClick += (_, _) =>
			{
				ClearSelection();
				RefreshUi();
			};
			panel.Append(clearButton);

			UITextPanel<string> closeButton = new("Close", 0.9f, false);
			closeButton.Width.Set(110f, 0f);
			closeButton.Height.Set(44f, 0f);
			closeButton.Left.Set(492f, 0f);
			closeButton.Top.Set(516f, 0f);
			closeButton.BackgroundColor = new Color(60, 64, 88);
			closeButton.OnLeftClick += (_, _) => ChordComposerSystem.Close();
			panel.Append(closeButton);

			WrappedTextBlock footer = new("Right-click the weapon to reopen this builder at any time.", 0.74f);
			footer.Width.Set(648f, 0f);
			footer.Height.Set(24f, 0f);
			footer.Left.Set(24f, 0f);
			footer.Top.Set(568f, 0f);
			footer.TextColor = new Color(188, 188, 198);
			panel.Append(footer);

			RefreshUi();
		}

		public void LoadFromPlayer(Player player)
		{
			ClearSelection();

			GuitarSwordPlayer chordPlayer = player.GetModPlayer<GuitarSwordPlayer>();
			foreach (int pitchClass in ChordMath.GetPitchClasses(chordPlayer.ChordRoot, chordPlayer.CurrentQuality))
				selectedNotes[pitchClass] = true;

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
			if (!initialized || selectionText == null || statusText == null || effectText == null || cadenceText == null || staffPreview == null)
				return;

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
				? "Selected notes: none"
				: $"Selected notes: {string.Join(", ", selectedNames)}");

			if (ChordMath.TryRecognize(selectedNotes, out int root, out ChordQuality quality))
			{
				GuitarSwordPlayer currentPlayerState = Main.LocalPlayer.GetModPlayer<GuitarSwordPlayer>();
				string chordName = ChordMath.GetDisplayName(root, quality);
				statusText.SetText($"Recognized chord: {chordName}");
				effectText.SetText($"{ChordMath.GetDescription(quality)}  Notes: {ChordMath.GetNotesDisplay(root, quality)}");

				int cadenceGain = ChordMath.EvaluateCadenceGain(currentPlayerState.ChordRoot, currentPlayerState.CurrentQuality, root, quality, out string cadenceName);
				if (cadenceGain > 0)
				{
					int projectedCharge = Utils.Clamp(currentPlayerState.CadenceCharge + cadenceGain, 0, 3);
					string encoreText = projectedCharge >= 3 ? "  ->  Encore Ready" : string.Empty;
					cadenceText.SetText($"Cadence preview: {cadenceName} +{cadenceGain}  ({currentPlayerState.CadenceCharge}/3 -> {projectedCharge}/3){encoreText}");
					cadenceText.TextColor = ChordMath.GetColor(quality);
				}
				else
				{
					cadenceText.SetText(currentPlayerState.CadenceCharge >= 3
						? "Cadence: Encore Ready"
						: $"Cadence: {currentPlayerState.CadenceCharge}/3");
					cadenceText.TextColor = Color.Silver;
				}

				previewButton.SetText($"Preview {chordName}");
				previewButton.BackgroundColor = ChordMath.GetColor(quality) * 0.65f;
				previewButton.BorderColor = ChordMath.GetColor(quality);

				commitButton.SetText($"Commit {chordName}");
				commitButton.BackgroundColor = ChordMath.GetColor(quality) * 0.5f;
				commitButton.BorderColor = Color.White;

				staffPreview.SetPreview(selectedNotes, recognized: true, root, quality);
			}
			else
			{
				string message = selectedNames.Count switch
				{
					0 => "Recognized chord: none",
					< MaxSelectedNotes => "Recognized chord: choose 3 notes",
					_ => "Recognized chord: unsupported voicing",
				};

				statusText.SetText(message);
				effectText.SetText("This weapon currently supports 3-note major, minor, diminished, and sus4 chords.");
				GuitarSwordPlayer currentPlayerState = Main.LocalPlayer.GetModPlayer<GuitarSwordPlayer>();
				cadenceText.SetText(currentPlayerState.CadenceCharge >= 3
					? "Cadence: Encore Ready"
					: $"Cadence: {currentPlayerState.CadenceCharge}/3");
				cadenceText.TextColor = Color.Silver;

				previewButton.SetText("Preview Chord");
				previewButton.BackgroundColor = new Color(62, 62, 62);
				previewButton.BorderColor = new Color(96, 96, 96);

				commitButton.SetText("Commit Chord");
				commitButton.BackgroundColor = new Color(62, 62, 62);
				commitButton.BorderColor = new Color(96, 96, 96);

				staffPreview.SetPreview(selectedNotes, recognized: false, 0, ChordQuality.Major);
			}
		}

		private void PreviewSelectedChord()
		{
			if (!ChordMath.TryRecognize(selectedNotes, out int root, out ChordQuality quality))
				return;

			SoundEngine.PlaySound(ChordMath.GetSoundStyle(root, quality), Main.LocalPlayer.Center);
		}

		private void CommitSelectedChord()
		{
			if (!ChordMath.TryRecognize(selectedNotes, out int root, out ChordQuality quality))
				return;

			Player player = Main.LocalPlayer;
			GuitarSwordPlayer chordPlayer = player.GetModPlayer<GuitarSwordPlayer>();
			int cadenceGain = chordPlayer.CommitChord(root, quality, sync: true, out string cadenceName, out bool encoreReady);

			SoundEngine.PlaySound(ChordMath.GetSoundStyle(root, quality), player.Center);
			CombatText.NewText(player.Hitbox, ChordMath.GetColor(quality), ChordMath.GetDisplayName(root, quality), dramatic: true);

			if (cadenceGain > 0)
			{
				CombatText.NewText(player.Hitbox, new Color(130, 255, 220), $"{cadenceName} +{cadenceGain}", dramatic: false);
			}

			if (encoreReady)
			{
				CombatText.NewText(player.Hitbox, new Color(255, 180, 80), "Encore Ready!", dramatic: true);
			}

			ChordComposerSystem.Close();
		}
	}
}
