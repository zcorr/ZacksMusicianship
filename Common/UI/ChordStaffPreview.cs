using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using ZacksMusicianship.Common.Chords;

namespace ZacksMusicianship.Common.UI
{
	public class ChordStaffPreview : UIElement
	{
		private const float LineSpacing = 12f;
		private const float StepHeight = LineSpacing / 2f;
		private const int BottomLineAbsoluteStep = 30; // E4 on treble clef staff
		private const int RootMidiBase = 60; // C4
		private const float StaffWidth = 378f;
		private const float ClefWidth = 58f;
		private const float ClefPixelScale = 1.8f;

		private static readonly string[] TrebleClefPixels =
		{
			"........##..........",
			".......###..........",
			".......###..........",
			"........##..........",
			"........##..........",
			"........##..........",
			".......###..........",
			"......####..........",
			".....##.##..........",
			".....#..###.........",
			"....##...##.........",
			"....#....##.........",
			"....#....##.........",
			"....##...##.........",
			".....##..##.........",
			"......#####.........",
			".....##..###........",
			"....##.....##.......",
			"....#.......#.......",
			"....#.......#.......",
			"....##.....##.......",
			".....###..##........",
			".......####.........",
			"........##..........",
			"........##..........",
			"........##..........",
			"........##..........",
			".......###..........",
			".....####...........",
			"....##.##...........",
			"....#..##...........",
			"....##.##...........",
			".....###............",
		};

		private static readonly int[] QualityIntervalsMajor = { 0, 4, 7 };
		private static readonly int[] QualityIntervalsMinor = { 0, 3, 7 };
		private static readonly int[] QualityIntervalsDiminished = { 0, 3, 6 };
		private static readonly int[] QualityIntervalsSuspended = { 0, 5, 7 };

		private readonly List<DisplayNote> displayNotes = new();
		private string headingText = "Treble Staff Preview";
		private string detailText = "Select up to 3 notes to preview the voicing.";
		private Color accentColor = new(205, 205, 215);

		public void SetPreview(bool[] selectedNotes, bool recognized, int root, ChordQuality quality)
		{
			accentColor = recognized ? ChordMath.GetColor(quality) : new Color(205, 205, 215);
			headingText = recognized ? ChordMath.GetDisplayName(root, quality) : "Treble Staff Preview";

			List<int> midiNotes = recognized
				? BuildRecognizedMidiNotes(root, quality)
				: BuildSelectedMidiNotes(selectedNotes);

			displayNotes.Clear();
			foreach (int midi in midiNotes)
				displayNotes.Add(ToDisplayNote(midi));

			if (displayNotes.Count == 0)
			{
				detailText = "Select up to 3 notes to preview the voicing.";
			}
			else if (recognized)
			{
				detailText = $"Notes: {ChordMath.GetNotesDisplay(root, quality)}";
			}
			else
			{
				detailText = $"Selected: {string.Join(", ", displayNotes.Select(note => note.Label))}";
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dimensions = GetDimensions();
			Rectangle bounds = dimensions.ToRectangle();
			Texture2D pixel = TextureAssets.MagicPixel.Value;

			Color staffColor = new Color(153, 132, 104);
			float headerX = bounds.X + 12f;
			float headerY = bounds.Y + 8f;
			float blockLeft = bounds.X + (bounds.Width - (StaffWidth + ClefWidth)) * 0.5f;
			float staffLeft = blockLeft + ClefWidth;
			float staffRight = staffLeft + StaffWidth;
			float staffTop = bounds.Y + 54f;
			float staffBottom = staffTop + LineSpacing * 4f;
			float stackX = (staffLeft + staffRight) * 0.5f;
			float clefWidth = TrebleClefPixels[0].Length * ClefPixelScale;
			float clefHeight = TrebleClefPixels.Length * ClefPixelScale;
			Vector2 clefTopLeft = new(
				blockLeft + (ClefWidth - clefWidth) * 0.5f,
				staffTop + (LineSpacing * 4f - clefHeight) * 0.5f - 8f);

			Utils.DrawBorderString(spriteBatch, headingText, new Vector2(headerX, headerY), accentColor, 0.82f);
			Utils.DrawBorderString(spriteBatch, detailText, new Vector2(headerX, headerY + 20f), Color.Silver, 0.7f);

			DrawTrebleClef(spriteBatch, pixel, clefTopLeft, staffColor);

			for (int lineIndex = 0; lineIndex < 5; lineIndex++)
			{
				int y = (int)(staffTop + lineIndex * LineSpacing);
				spriteBatch.Draw(pixel, new Rectangle((int)staffLeft, y, (int)(staffRight - staffLeft), 2), staffColor);
			}

			if (displayNotes.Count == 0)
			{
				Utils.DrawBorderString(spriteBatch, "No notes selected", new Vector2(stackX - 74f, staffTop + 18f), new Color(170, 170, 178), 0.78f);
				return;
			}

			List<DrawNoteState> noteStates = BuildDrawStates(stackX, staffBottom);
			foreach (DrawNoteState noteState in noteStates)
			{
				DrawLedgerLines(spriteBatch, pixel, noteState, staffBottom, staffColor * 0.85f);

				if (noteState.Note.IsSharp)
					Utils.DrawBorderString(spriteBatch, "#", new Vector2(noteState.X - 17f, noteState.Y - 10f), Color.White, 0.72f);

				DrawNoteHead(spriteBatch, pixel, noteState.X, noteState.Y, accentColor);
				DrawStem(spriteBatch, pixel, noteState.X, noteState.Y, noteState.Note.StaffOffset, accentColor);
			}
		}

		private static List<int> BuildRecognizedMidiNotes(int root, ChordQuality quality)
		{
			int[] intervals = quality switch
			{
				ChordQuality.Major => QualityIntervalsMajor,
				ChordQuality.Minor => QualityIntervalsMinor,
				ChordQuality.Diminished => QualityIntervalsDiminished,
				ChordQuality.Suspended => QualityIntervalsSuspended,
				_ => QualityIntervalsMajor,
			};

			int rootMidi = RootMidiBase + ChordMath.NormalizePitchClass(root);
			return intervals.Select(interval => rootMidi + interval).ToList();
		}

		private static List<int> BuildSelectedMidiNotes(bool[] selectedNotes)
		{
			List<int> midiNotes = new();
			for (int pitchClass = 0; pitchClass < selectedNotes.Length; pitchClass++)
			{
				if (selectedNotes[pitchClass])
					midiNotes.Add(RootMidiBase + pitchClass);
			}

			midiNotes.Sort();
			return midiNotes;
		}

		private static DisplayNote ToDisplayNote(int midi)
		{
			int pitchClass = ChordMath.NormalizePitchClass(midi);
			int octave = (midi / 12) - 1;

			(int letterIndex, bool isSharp) = pitchClass switch
			{
				0 => (0, false),  // C
				1 => (0, true),   // C#
				2 => (1, false),  // D
				3 => (1, true),   // D#
				4 => (2, false),  // E
				5 => (3, false),  // F
				6 => (3, true),   // F#
				7 => (4, false),  // G
				8 => (4, true),   // G#
				9 => (5, false),  // A
				10 => (5, true),  // A#
				11 => (6, false), // B
				_ => (0, false),
			};

			int absoluteStep = octave * 7 + letterIndex;
			return new DisplayNote
			{
				StaffOffset = absoluteStep - BottomLineAbsoluteStep,
				IsSharp = isSharp,
				Label = ChordMath.GetNoteName(pitchClass),
			};
		}

		private static List<DrawNoteState> BuildDrawStates(float stackX, float staffBottom, List<DisplayNote> notes)
		{
			List<DrawNoteState> states = new();
			List<DisplayNote> sortedNotes = notes.OrderBy(note => note.StaffOffset).ToList();

			for (int i = 0; i < sortedNotes.Count; i++)
			{
				DisplayNote note = sortedNotes[i];
				float x = stackX;

				if (i > 0 && note.StaffOffset - sortedNotes[i - 1].StaffOffset <= 1)
					x += 9f;

				states.Add(new DrawNoteState
				{
					Note = note,
					X = x,
					Y = staffBottom - note.StaffOffset * StepHeight,
				});
			}

			return states;
		}

		private List<DrawNoteState> BuildDrawStates(float stackX, float staffBottom) => BuildDrawStates(stackX, staffBottom, displayNotes);

		private static void DrawLedgerLines(SpriteBatch spriteBatch, Texture2D pixel, DrawNoteState noteState, float staffBottom, Color color)
		{
			if (noteState.Note.StaffOffset <= -2)
			{
				for (int offset = -2; offset >= noteState.Note.StaffOffset; offset -= 2)
				{
					int y = (int)(staffBottom - offset * StepHeight);
					spriteBatch.Draw(pixel, new Rectangle((int)(noteState.X - 12f), y, 28, 2), color);
				}
			}

			if (noteState.Note.StaffOffset >= 10)
			{
				for (int offset = 10; offset <= noteState.Note.StaffOffset; offset += 2)
				{
					int y = (int)(staffBottom - offset * StepHeight);
					spriteBatch.Draw(pixel, new Rectangle((int)(noteState.X - 12f), y, 28, 2), color);
				}
			}
		}

		private static void DrawNoteHead(SpriteBatch spriteBatch, Texture2D pixel, float x, float y, Color color)
		{
			Color fill = color * 0.92f;
			int startX = (int)x - 5;
			int startY = (int)y - 4;

			spriteBatch.Draw(pixel, new Rectangle(startX + 2, startY, 6, 1), fill);
			spriteBatch.Draw(pixel, new Rectangle(startX + 1, startY + 1, 8, 1), fill);
			spriteBatch.Draw(pixel, new Rectangle(startX, startY + 2, 10, 2), fill);
			spriteBatch.Draw(pixel, new Rectangle(startX + 1, startY + 4, 8, 1), fill);
			spriteBatch.Draw(pixel, new Rectangle(startX + 2, startY + 5, 6, 1), fill);
		}

		private static void DrawStem(SpriteBatch spriteBatch, Texture2D pixel, float x, float y, int staffOffset, Color color)
		{
			bool stemDown = staffOffset >= 4; // B4 / middle line and above
			int stemX = stemDown ? (int)x - 5 : (int)x + 4;
			int stemY = stemDown ? (int)y - 2 : (int)y - 26;
			spriteBatch.Draw(pixel, new Rectangle(stemX, stemY, 2, 24), color * 0.9f);
		}

		private static void DrawTrebleClef(SpriteBatch spriteBatch, Texture2D pixel, Vector2 topLeft, Color color)
		{
			for (int y = 0; y < TrebleClefPixels.Length; y++)
			{
				string row = TrebleClefPixels[y];
				for (int x = 0; x < row.Length; x++)
				{
					if (row[x] == '.')
						continue;

					Rectangle pixelRect = new(
						(int)(topLeft.X + x * ClefPixelScale),
						(int)(topLeft.Y + y * ClefPixelScale),
						(int)(ClefPixelScale + 0.75f),
						(int)(ClefPixelScale + 0.75f));
					spriteBatch.Draw(pixel, pixelRect, color);
				}
			}
		}

		private struct DisplayNote
		{
			public int StaffOffset;
			public bool IsSharp;
			public string Label;
		}

		private struct DrawNoteState
		{
			public DisplayNote Note;
			public float X;
			public float Y;
		}
	}
}
