namespace ZacksMusicianship.Common.Rhythm
{
	public readonly struct StrumStroke
	{
		public StrumStroke(StrumDirection direction, int subdivision, string beatLabel)
		{
			Direction = direction;
			Subdivision = subdivision;
			BeatLabel = beatLabel;
		}

		public StrumDirection Direction { get; }
		public int Subdivision { get; }
		public string BeatLabel { get; }

		public string DirectionLabel => Direction == StrumDirection.Down ? "Down" : "Up";
		public string ShortDirectionLabel => Direction == StrumDirection.Down ? "D" : "U";
	}
}
