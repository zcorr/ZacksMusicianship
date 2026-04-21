using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio;
using Terraria.ID;
using ZacksMusicianship.Common.Cadences;

namespace ZacksMusicianship.Common.Chords
{
	public static class ChordMath
	{
		private static readonly string[] NoteNames =
		{
			"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
		};

		private static readonly string[] AssetNoteNames =
		{
			"C", "CSharp", "D", "DSharp", "E", "F", "FSharp", "G", "GSharp", "A", "ASharp", "B"
		};

		private static readonly Dictionary<ChordQuality, int[]> QualityIntervals = new()
		{
			[ChordQuality.Major] = new[] { 0, 4, 7 },
			[ChordQuality.Minor] = new[] { 0, 3, 7 },
			[ChordQuality.Diminished] = new[] { 0, 3, 6 },
			[ChordQuality.Suspended] = new[] { 0, 5, 7 },
		};

		public static int NormalizePitchClass(int pitchClass) => ((pitchClass % 12) + 12) % 12;

		public static int[] GetPitchClasses(int root, ChordQuality quality)
		{
			root = NormalizePitchClass(root);
			return QualityIntervals[quality]
				.Select(interval => NormalizePitchClass(root + interval))
				.ToArray();
		}

		public static bool TryRecognize(bool[] selectedNotes, out int root, out ChordQuality quality)
		{
			List<int> selectedPitchClasses = new();

			for (int i = 0; i < selectedNotes.Length; i++)
			{
				if (selectedNotes[i])
					selectedPitchClasses.Add(i);
			}

			if (selectedPitchClasses.Count != 3)
			{
				root = 0;
				quality = ChordQuality.Major;
				return false;
			}

			int[] normalized = selectedPitchClasses.OrderBy(note => note).ToArray();

			foreach (int candidateRoot in normalized)
			{
				foreach (ChordQuality candidateQuality in Enum.GetValues<ChordQuality>())
				{
					int[] candidateNotes = GetPitchClasses(candidateRoot, candidateQuality)
						.OrderBy(note => note)
						.ToArray();

					if (candidateNotes.SequenceEqual(normalized))
					{
						root = candidateRoot;
						quality = candidateQuality;
						return true;
					}
				}
			}

			root = 0;
			quality = ChordQuality.Major;
			return false;
		}

		public static string GetNoteName(int pitchClass) => NoteNames[NormalizePitchClass(pitchClass)];

		public static string GetDisplayName(int root, ChordQuality quality) => $"{GetNoteName(root)} {GetQualityDisplayName(quality)}";

		public static string GetQualityDisplayName(ChordQuality quality) => quality switch
		{
			ChordQuality.Major => "Major",
			ChordQuality.Minor => "Minor",
			ChordQuality.Diminished => "Diminished",
			ChordQuality.Suspended => "Sus4",
			_ => "Unknown",
		};

		public static string GetDescription(ChordQuality quality) => quality switch
		{
			ChordQuality.Major => "Swift strikes; a hit grants a brief speed burst",
			ChordQuality.Minor => "Crushing blows with high knockback",
			ChordQuality.Diminished => "33% chance to Confuse on hit",
			ChordQuality.Suspended => "Suspends enemies in the air for 2 seconds",
			_ => "Unknown chord behavior",
		};

		public static Color GetColor(ChordQuality quality) => quality switch
		{
			ChordQuality.Major => Color.Gold,
			ChordQuality.Minor => new Color(160, 80, 220),
			ChordQuality.Diminished => Color.OrangeRed,
			ChordQuality.Suspended => Color.CornflowerBlue,
			_ => Color.White,
		};

		public static int GetDustId(ChordQuality quality) => quality switch
		{
			ChordQuality.Major => DustID.GoldFlame,
			ChordQuality.Minor => DustID.Shadowflame,
			ChordQuality.Diminished => DustID.CrimsonTorch,
			ChordQuality.Suspended => DustID.IceTorch,
			_ => DustID.Smoke,
		};

		public static string GetNotesDisplay(int root, ChordQuality quality) => string.Join(", ", GetPitchClasses(root, quality).Select(GetNoteName));

		public static int EvaluateCadenceGain(int previousRoot, ChordQuality previousQuality, int newRoot, ChordQuality newQuality, out string cadenceName)
		{
			previousRoot = NormalizePitchClass(previousRoot);
			newRoot = NormalizePitchClass(newRoot);

			bool newIsStableTriad = newQuality is ChordQuality.Major or ChordQuality.Minor;

			if (previousQuality == ChordQuality.Suspended && newIsStableTriad && previousRoot == newRoot)
			{
				cadenceName = "Resolution";
				return 1;
			}

			if (previousQuality == ChordQuality.Diminished && newIsStableTriad && NormalizePitchClass(newRoot - previousRoot) == 1)
			{
				cadenceName = "Leading Tone Release";
				return 2;
			}

			if (newIsStableTriad && NormalizePitchClass(newRoot - previousRoot) == 5)
			{
				cadenceName = "Circle Step";
				return 1;
			}

			cadenceName = string.Empty;
			return 0;
		}

		public static int EvaluateProgressionCadenceTotal(IReadOnlyList<int> roots, IReadOnlyList<ChordQuality> qualities, int count, out string summary)
		{
			if (count <= 0)
			{
				summary = "Build a saved progression to form a cadence phrase.";
				return 0;
			}

			if (count == 1)
			{
				summary = "Single saved chord: cadence needs a 2-4 chord phrase.";
				return 0;
			}

			List<PlayedChord> phrase = new();
			for (int i = 0; i < count; i++)
				phrase.Add(new PlayedChord(roots[i], qualities[i]));

			if (CadenceLibrary.TryMatchPhrase(phrase, out CadenceBookEntry entry))
			{
				summary = $"Phrase cadence: {entry.Title} +{entry.CadenceGain}. {entry.Summary}";
				return entry.CadenceGain;
			}

			summary = "Phrase cadence: no named 2-4 chord cadence detected yet.";
			return 0;
		}

		public static SoundStyle GetSoundStyle(int root, ChordQuality quality)
		{
			string qualityAssetName = quality switch
			{
				ChordQuality.Major => "Major",
				ChordQuality.Minor => "Minor",
				ChordQuality.Diminished => "Diminished",
				ChordQuality.Suspended => "Suspended",
				_ => "Major",
			};

			return new SoundStyle($"ZacksMusicianship/Assets/Sounds/ChordBank/Chord_{AssetNoteNames[NormalizePitchClass(root)]}_{qualityAssetName}")
			{
				Volume = 0.8f,
				PitchVariance = 0f,
				MaxInstances = 0,
			};
		}
	}
}
