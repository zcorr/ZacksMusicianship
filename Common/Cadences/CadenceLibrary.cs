using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ZacksMusicianship.Common.Chords;

namespace ZacksMusicianship.Common.Cadences
{
	public enum CadenceCategory
	{
		Foundations = 0,
		MajorRoutes = 1,
		MinorRoutes = 2,
	}

	public enum CadenceEntryId
	{
		CircleStep = 0,
		SuspensionRelease = 1,
		LeadingToneRelease = 2,
		AuthenticCadence = 3,
		PlagalCadence = 4,
		TwoFiveOne = 5,
		OneFourFiveOne = 6,
		SixTwoFiveOne = 7,
		MinorAuthentic = 8,
		MinorPlagal = 9,
		TwoDimFiveOne = 10,
		OneFourFiveOneMinor = 11,
	}

	public readonly struct PlayedChord
	{
		public int Root { get; }
		public ChordQuality Quality { get; }

		public PlayedChord(int root, ChordQuality quality)
		{
			Root = ChordMath.NormalizePitchClass(root);
			Quality = quality;
		}
	}

	public sealed class CadenceBookEntry
	{
		public required CadenceEntryId Id { get; init; }
		public required CadenceCategory Category { get; init; }
		public required int CadenceGain { get; init; }
		public required int Priority { get; init; }
		public required string Title { get; init; }
		public required string Formula { get; init; }
		public required string Summary { get; init; }
		public required string Description { get; init; }
		public required string Hint { get; init; }
		public required string Example { get; init; }
		public required Color AccentColor { get; init; }
		public required Func<IReadOnlyList<PlayedChord>, bool> Matches { get; init; }
	}

	public static class CadenceLibrary
	{
		private static readonly CadenceCategory[] Categories =
		{
			CadenceCategory.Foundations,
			CadenceCategory.MajorRoutes,
			CadenceCategory.MinorRoutes,
		};

		private static readonly CadenceBookEntry[] Entries =
		{
			new()
			{
				Id = CadenceEntryId.CircleStep,
				Category = CadenceCategory.Foundations,
				CadenceGain = 1,
				Priority = 10,
				Title = "Circle Step",
				Formula = "stable -> stable (+4th / -5th)",
				Summary = "A stable chord rises by a fourth into another stable chord.",
				Description = "Circle motion is the engine under countless progressions. When a major or minor chord rises by a perfect fourth, the harmony starts to feel like it is traveling somewhere with purpose.",
				Hint = "Play two major or minor chords where the second root is a fourth above the first.",
				Example = "Dm -> G or G -> C",
				AccentColor = new Color(198, 205, 120),
				Matches = history => MatchTail(history, (previous, current) =>
					IsStable(previous.Quality) &&
					IsStable(current.Quality) &&
					ChordMath.NormalizePitchClass(current.Root - previous.Root) == 5),
			},
			new()
			{
				Id = CadenceEntryId.SuspensionRelease,
				Category = CadenceCategory.Foundations,
				CadenceGain = 1,
				Priority = 10,
				Title = "Suspension Release",
				Formula = "sus4 -> I / i",
				Summary = "Suspended tension resolves to a stable chord on the same root.",
				Description = "A suspended chord withholds the third, then finally releases it. This is the cleanest way to hear tension and release inside a single root.",
				Hint = "Play a suspended chord, then resolve it to a major or minor chord without changing the root.",
				Example = "Csus4 -> C or Csus4 -> Cm",
				AccentColor = new Color(132, 190, 255),
				Matches = history => MatchTail(history, (previous, current) =>
					previous.Quality == ChordQuality.Suspended &&
					IsStable(current.Quality) &&
					previous.Root == current.Root),
			},
			new()
			{
				Id = CadenceEntryId.LeadingToneRelease,
				Category = CadenceCategory.Foundations,
				CadenceGain = 2,
				Priority = 12,
				Title = "Leading Tone Release",
				Formula = "vii° -> I / i",
				Summary = "A diminished chord climbs by semitone into a tonic.",
				Description = "The diminished leading-tone chord is all pull and no rest. When it rises by a semitone into a stable tonic, the arrival feels immediate and inevitable.",
				Hint = "Resolve a diminished chord upward by one semitone into a major or minor tonic.",
				Example = "Bdim -> C or D#dim -> Em",
				AccentColor = new Color(255, 145, 96),
				Matches = history => MatchTail(history, (previous, current) =>
					previous.Quality == ChordQuality.Diminished &&
					IsStable(current.Quality) &&
					ChordMath.NormalizePitchClass(current.Root - previous.Root) == 1),
			},
			new()
			{
				Id = CadenceEntryId.AuthenticCadence,
				Category = CadenceCategory.Foundations,
				CadenceGain = 1,
				Priority = 14,
				Title = "Authentic Cadence",
				Formula = "V -> I",
				Summary = "Dominant pressure resolves into a major tonic.",
				Description = "This is the classic close in a major key. The dominant chord points directly at home, and the tonic answers it with a full arrival.",
				Hint = "End a major route by moving from a major V chord into a major I chord.",
				Example = "G -> C",
				AccentColor = Color.Gold,
				Matches = history => MatchesFinalTonic(history, new[] { 7, 0 }, new[] { ChordQuality.Major, ChordQuality.Major }),
			},
			new()
			{
				Id = CadenceEntryId.PlagalCadence,
				Category = CadenceCategory.MajorRoutes,
				CadenceGain = 1,
				Priority = 14,
				Title = "Plagal Cadence",
				Formula = "IV -> I",
				Summary = "The subdominant falls softly into the tonic.",
				Description = "Where the authentic cadence feels decisive, the plagal cadence feels grounded and warm. It still comes home, just without the same edge of dominance.",
				Hint = "Move from a major IV chord into a major I chord.",
				Example = "F -> C",
				AccentColor = new Color(124, 214, 180),
				Matches = history => MatchesFinalTonic(history, new[] { 5, 0 }, new[] { ChordQuality.Major, ChordQuality.Major }),
			},
			new()
			{
				Id = CadenceEntryId.TwoFiveOne,
				Category = CadenceCategory.MajorRoutes,
				CadenceGain = 2,
				Priority = 24,
				Title = "ii-V-I",
				Formula = "ii -> V -> I",
				Summary = "The core major-key preparation, tension, and arrival route.",
				Description = "ii-V-I is one of the most recognizable cadence chains in tonal music. It prepares the dominant, charges the dominant, then lands on the tonic with complete clarity.",
				Hint = "Play a minor ii chord, then major V, then major I.",
				Example = "Dm -> G -> C",
				AccentColor = new Color(116, 170, 255),
				Matches = history => MatchesFinalTonic(history, new[] { 2, 7, 0 }, new[] { ChordQuality.Minor, ChordQuality.Major, ChordQuality.Major }),
			},
			new()
			{
				Id = CadenceEntryId.OneFourFiveOne,
				Category = CadenceCategory.MajorRoutes,
				CadenceGain = 2,
				Priority = 26,
				Title = "I-IV-V-I",
				Formula = "I -> IV -> V -> I",
				Summary = "A full major-key loop that leaves home, builds, and returns.",
				Description = "This route expands the tonic into a broader journey. It departs from home, broadens through the subdominant, sharpens into the dominant, and closes back on I.",
				Hint = "Play a four-step major route built from I, IV, V, and back to I.",
				Example = "C -> F -> G -> C",
				AccentColor = new Color(155, 208, 114),
				Matches = history => MatchesFinalTonic(history, new[] { 0, 5, 7, 0 }, new[] { ChordQuality.Major, ChordQuality.Major, ChordQuality.Major, ChordQuality.Major }),
			},
			new()
			{
				Id = CadenceEntryId.SixTwoFiveOne,
				Category = CadenceCategory.MajorRoutes,
				CadenceGain = 2,
				Priority = 28,
				Title = "vi-ii-V-I",
				Formula = "vi -> ii -> V -> I",
				Summary = "A longer circle route that leans all the way into tonic.",
				Description = "This extended progression lets circle motion do the work. Each chord hands momentum to the next until the tonic finally receives the accumulated pull.",
				Hint = "Build a four-step major loop using vi, ii, V, and I in order.",
				Example = "Am -> Dm -> G -> C",
				AccentColor = new Color(200, 170, 255),
				Matches = history => MatchesFinalTonic(history, new[] { 9, 2, 7, 0 }, new[] { ChordQuality.Minor, ChordQuality.Minor, ChordQuality.Major, ChordQuality.Major }),
			},
			new()
			{
				Id = CadenceEntryId.MinorAuthentic,
				Category = CadenceCategory.MinorRoutes,
				CadenceGain = 1,
				Priority = 14,
				Title = "Minor Authentic Cadence",
				Formula = "V -> i",
				Summary = "Dominant tension resolves into a minor tonic.",
				Description = "The arrival is darker, but no less final. A major dominant sharpening into a minor tonic creates a dramatic pull that still reads as home.",
				Hint = "Resolve a major V chord into a minor tonic.",
				Example = "E -> Am",
				AccentColor = new Color(186, 112, 255),
				Matches = history => MatchesFinalTonic(history, new[] { 7, 0 }, new[] { ChordQuality.Major, ChordQuality.Minor }),
			},
			new()
			{
				Id = CadenceEntryId.MinorPlagal,
				Category = CadenceCategory.MinorRoutes,
				CadenceGain = 1,
				Priority = 14,
				Title = "Minor Plagal Cadence",
				Formula = "iv -> i",
				Summary = "The minor subdominant sinks back into tonic.",
				Description = "This is a gentler minor close. Instead of dominant pressure, the sound darkens and settles inward as iv returns to i.",
				Hint = "Move from a minor iv chord into a minor tonic.",
				Example = "Dm -> Am",
				AccentColor = new Color(120, 162, 255),
				Matches = history => MatchesFinalTonic(history, new[] { 5, 0 }, new[] { ChordQuality.Minor, ChordQuality.Minor }),
			},
			new()
			{
				Id = CadenceEntryId.TwoDimFiveOne,
				Category = CadenceCategory.MinorRoutes,
				CadenceGain = 2,
				Priority = 24,
				Title = "ii°-V-i",
				Formula = "ii° -> V -> i",
				Summary = "The minor-key pre-dominant chain funnels straight into tonic.",
				Description = "This route lets the diminished supertonic sharpen the approach before the dominant completes the pull into minor tonic. It feels tense, narrow, and focused.",
				Hint = "Play a diminished ii chord, then major V, then minor i.",
				Example = "Bdim -> E -> Am",
				AccentColor = new Color(255, 122, 138),
				Matches = history => MatchesFinalTonic(history, new[] { 2, 7, 0 }, new[] { ChordQuality.Diminished, ChordQuality.Major, ChordQuality.Minor }),
			},
			new()
			{
				Id = CadenceEntryId.OneFourFiveOneMinor,
				Category = CadenceCategory.MinorRoutes,
				CadenceGain = 2,
				Priority = 26,
				Title = "i-iv-V-i",
				Formula = "i -> iv -> V -> i",
				Summary = "A full minor loop that opens, darkens, and resolves.",
				Description = "This minor route stretches the tonic into a full phrase. The iv deepens the color, the dominant intensifies the return, and the final i closes the circuit.",
				Hint = "Play a four-step minor loop built from i, iv, V, and back to i.",
				Example = "Am -> Dm -> E -> Am",
				AccentColor = new Color(214, 132, 255),
				Matches = history => MatchesFinalTonic(history, new[] { 0, 5, 7, 0 }, new[] { ChordQuality.Minor, ChordQuality.Minor, ChordQuality.Major, ChordQuality.Minor }),
			},
		};

		private static readonly Dictionary<CadenceCategory, CadenceBookEntry[]> EntriesByCategory = Entries
			.GroupBy(entry => entry.Category)
			.ToDictionary(group => group.Key, group => group.ToArray());

		public static IReadOnlyList<CadenceCategory> AllCategories => Categories;
		public static IReadOnlyList<CadenceBookEntry> AllEntries => Entries;
		public static int EntryCount => Entries.Length;
		public static int CategoryCount => Categories.Length;
		public static int MaxEntriesPerCategory => EntriesByCategory.Values.Max(entries => entries.Length);

		public static IReadOnlyList<CadenceBookEntry> GetEntries(CadenceCategory category) => EntriesByCategory[category];

		public static CadenceBookEntry GetEntry(CadenceEntryId id) => Entries[(int)id];

		public static ulong GetMask(CadenceEntryId id) => 1UL << (int)id;

		public static string GetCategoryTitle(CadenceCategory category) => category switch
		{
			CadenceCategory.Foundations => "Foundations",
			CadenceCategory.MajorRoutes => "Major Routes",
			CadenceCategory.MinorRoutes => "Minor Routes",
			_ => "Cadences",
		};

		public static string GetCategoryDescription(CadenceCategory category) => category switch
		{
			CadenceCategory.Foundations => "Basic tension-and-release gestures and root motion unlocks.",
			CadenceCategory.MajorRoutes => "Progressions that resolve into a major tonic.",
			CadenceCategory.MinorRoutes => "Progressions that resolve into a minor tonic.",
			_ => "Musical discoveries written into the songbook.",
		};

		public static bool TryMatchPhrase(IReadOnlyList<PlayedChord> phrase, out CadenceBookEntry entry)
		{
			entry = null;

			for (int i = 0; i < Entries.Length; i++)
			{
				CadenceBookEntry candidate = Entries[i];
				if (!candidate.Matches(phrase))
					continue;

				if (entry == null || candidate.Priority > entry.Priority)
					entry = candidate;
			}

			return entry != null;
		}

		private static bool IsStable(ChordQuality quality) => quality is ChordQuality.Major or ChordQuality.Minor;

		private static bool MatchTail(IReadOnlyList<PlayedChord> history, Func<PlayedChord, PlayedChord, bool> matcher)
		{
			if (history.Count < 2)
				return false;

			return matcher(history[^2], history[^1]);
		}

		private static bool MatchesFinalTonic(IReadOnlyList<PlayedChord> history, int[] relativeRoots, ChordQuality[] qualities)
		{
			if (history.Count < relativeRoots.Length || relativeRoots.Length != qualities.Length)
				return false;

			int offset = history.Count - relativeRoots.Length;
			int tonic = history[^1].Root;

			for (int i = 0; i < relativeRoots.Length; i++)
			{
				PlayedChord chord = history[offset + i];
				if (chord.Quality != qualities[i])
					return false;

				if (ChordMath.NormalizePitchClass(chord.Root - tonic) != relativeRoots[i])
					return false;
			}

			return true;
		}
	}
}
