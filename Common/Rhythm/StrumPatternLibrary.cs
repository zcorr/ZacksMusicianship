namespace ZacksMusicianship.Common.Rhythm
{
	public static class StrumPatternLibrary
	{
		public const int DefaultPatternIndex = 0;

		private static readonly StrumPattern[] Patterns =
		{
			new(
				"folk_down_down_up_up_down_up",
				"Down Down Up Up Down Up",
				8,
				new StrumStroke(StrumDirection.Down, 0, "1"),
				new StrumStroke(StrumDirection.Down, 2, "2"),
				new StrumStroke(StrumDirection.Up, 3, "& of 2"),
				new StrumStroke(StrumDirection.Up, 5, "& of 3"),
				new StrumStroke(StrumDirection.Down, 6, "4"),
				new StrumStroke(StrumDirection.Up, 7, "& of 4")),
		};

		public static int PatternCount => Patterns.Length;
		public static StrumPattern DefaultPattern => Patterns[DefaultPatternIndex];

		public static StrumPattern GetPattern(int index) => Patterns[NormalizePatternIndex(index)];

		public static int NormalizePatternIndex(int index)
		{
			return index >= 0 && index < Patterns.Length ? index : DefaultPatternIndex;
		}
	}
}
