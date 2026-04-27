using System;
using System.Collections.Generic;

namespace ZacksMusicianship.Common.Rhythm
{
	public sealed class StrumPattern
	{
		private readonly StrumStroke[] strokes;

		public StrumPattern(string name, string displayName, int subdivisionCount, params StrumStroke[] strokes)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Strum pattern name cannot be empty.", nameof(name));

			if (string.IsNullOrWhiteSpace(displayName))
				throw new ArgumentException("Strum pattern display name cannot be empty.", nameof(displayName));

			if (subdivisionCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(subdivisionCount), "Strum pattern subdivision count must be positive.");

			if (strokes == null || strokes.Length == 0)
				throw new ArgumentException("Strum pattern must contain at least one stroke.", nameof(strokes));

			this.strokes = (StrumStroke[])strokes.Clone();
			Name = name;
			DisplayName = displayName;
			SubdivisionCount = subdivisionCount;

			ValidateStrokes();
		}

		public string Name { get; }
		public string DisplayName { get; }
		public int SubdivisionCount { get; }
		public int StepCount => strokes.Length;
		public IReadOnlyList<StrumStroke> Strokes => strokes;

		public StrumStroke GetStroke(int stepIndex) => strokes[NormalizeStepIndex(stepIndex)];

		public int GetSubdivisionSpanAfter(int stepIndex)
		{
			int normalizedStep = NormalizeStepIndex(stepIndex);
			int currentSubdivision = strokes[normalizedStep].Subdivision;
			int nextSubdivision = strokes[(normalizedStep + 1) % strokes.Length].Subdivision;
			int span = nextSubdivision - currentSubdivision;

			if (span <= 0)
				span += SubdivisionCount;

			return Math.Max(1, span);
		}

		public string GetDirectionDisplay()
		{
			string[] labels = new string[strokes.Length];

			for (int i = 0; i < strokes.Length; i++)
				labels[i] = strokes[i].ShortDirectionLabel;

			return string.Join(" ", labels);
		}

		public string GetBeatDisplay()
		{
			string[] labels = new string[strokes.Length];

			for (int i = 0; i < strokes.Length; i++)
				labels[i] = strokes[i].BeatLabel;

			return string.Join(", ", labels);
		}

		private int NormalizeStepIndex(int stepIndex)
		{
			int normalized = stepIndex % strokes.Length;
			return normalized < 0 ? normalized + strokes.Length : normalized;
		}

		private void ValidateStrokes()
		{
			int previousSubdivision = -1;

			for (int i = 0; i < strokes.Length; i++)
			{
				int subdivision = strokes[i].Subdivision;

				if (subdivision < 0 || subdivision >= SubdivisionCount)
					throw new ArgumentOutOfRangeException(nameof(strokes), "Strum stroke subdivision is outside the pattern.");

				if (subdivision <= previousSubdivision)
					throw new ArgumentException("Strum strokes must be ordered by unique subdivisions.", nameof(strokes));

				previousSubdivision = subdivision;
			}
		}
	}
}
