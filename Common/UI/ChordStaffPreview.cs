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
		private const float ClefVerticalBias = -8f; // visual nudge to seat the glyph on the staff

		private const float HeaderPaddingLeft = 12f;
		private const float HeaderPaddingTop = 8f;
		private const float SubheadingOffset = 20f;
		private const float StaffOffsetFromTop = 54f;
		private const float HeadingScale = 0.82f;
		private const float SubheadingScale = 0.7f;
		private const float EmptyLabelScale = 0.78f;

		private static readonly Color StaffLineColor = new(153, 132, 104);
		private static readonly Color EmptyLabelColor = new(170, 170, 178);

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
				detailText = "Select up to 3 notes to preview the voicing.";
			else if (recognized)
				detailText = $"Notes: {ChordMath.GetNotesDisplay(root, quality)}";
			else
				detailText = $"Selected: {string.Join(", ", displayNotes.Select(n => n.Label))}";
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dimensions = GetDimensions();
			Rectangle bounds = dimensions.ToRectangle();
			Texture2D pixel = TextureAssets.MagicPixel.Value;

			float blockLeft = bounds.X + (bounds.Width - (StaffWidth + ClefWidth)) * 0.5f;
			float staffLeft = blockLeft + ClefWidth;
			float staffRight = staffLeft + StaffWidth;
			float staffTop = bounds.Y + StaffOffsetFromTop;
			float staffBottom = staffTop + LineSpacing * 4f;
			float stackX = (staffLeft + staffRight) * 0.5f;

			Vector2 headerPos = new(bounds.X + HeaderPaddingLeft, bounds.Y + HeaderPaddingTop);
			Vector2 clefTopLeft = CalculateClefPosition(blockLeft, staffTop);

			DrawHeader(spriteBatch, headerPos);
			DrawTrebleClef(spriteBatch, pixel, clefTopLeft, StaffLineColor);
			DrawStaffLines(spriteBatch, pixel, staffLeft, staffRight, staffTop);

			if (displayNotes.Count == 0)
			{
				Utils.DrawBorderString(spriteBatch, "No notes selected", new Vector2(stackX - 74f, staffTop + 18f), EmptyLabelColor, EmptyLabelScale);
				return;
			}

			List<DrawNoteState> noteStates = BuildDrawStates(stackX, staffBottom, displayNotes);
			DrawNoteGroup(spriteBatch, pixel, noteStates, staffBottom);
		}

		private void DrawHeader(SpriteBatch spriteBatch, Vector2 position)
		{
			Utils.DrawBorderString(spriteBatch, headingText, position, accentColor, HeadingScale);
			Utils.DrawBorderString(spriteBatch, detailText, position + new Vector2(0f, SubheadingOffset), Color.Silver, SubheadingScale);
		}

		private static Vector2 CalculateClefPosition(float blockLeft, float staffTop)
		{
			float clefWidth = TrebleClefPixels[0].Length * ClefPixelScale;
			float clefHeight = TrebleClefPixels.Length * ClefPixelScale;
			return new Vector2(
				blockLeft + (ClefWidth - clefWidth) * 0.5f,
				staffTop + (LineSpacing * 4f - clefHeight) * 0.5f + ClefVerticalBias);
		}

		private static void DrawStaffLines(SpriteBatch spriteBatch, Texture2D pixel, float staffLeft, float staffRight, float staffTop)
		{
			int lineWidth = (int)(staffRight - staffLeft);
			for (int lineIndex = 0; lineIndex < 5; lineIndex++)
			{
				int y = (int)(staffTop + lineIndex * LineSpacing);
				spriteBatch.Draw(pixel, new Rectangle((int)staffLeft, y, lineWidth, 2), StaffLineColor);
			}
		}

		private void DrawNoteGroup(SpriteBatch spriteBatch, Texture2D pixel, List<DrawNoteState> noteStates, float staffBottom)
		{
			foreach (DrawNoteState noteState in noteStates)
			{
				DrawLedgerLines(spriteBatch, pixel, noteState, staffBottom, StaffLineColor * 0.85f);

				if (noteState.Note.IsSharp)
					Utils.DrawBorderString(spriteBatch, "#", new Vector2(noteState.X - 17f, noteState.Y - 10f), Color.White, 0.72f);

				DrawNoteHead(spriteBatch, pixel, noteState.X, noteState.Y, accentColor);
				DrawStem(spriteBatch, pixel, noteState.X, noteState.Y, noteState.Note.StaffOffset, accentColor);
			}
		}

		private static List<int> BuildRecognizedMidiNotes(int root, ChordQuality quality)
		{
			int rootPitchClass = ChordMath.NormalizePitchClass(root);
			int rootMidi = RootMidiBase + rootPitchClass;
			return ChordMath.GetPitchClasses(root, quality)
				.Select(pc => rootMidi + (pc - rootPitchClass + 12) % 12)
				.OrderBy(midi => midi)
				.ToList();
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
			List<DisplayNote> sortedNotes = notes.OrderBy(n => n.StaffOffset).ToList();

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
