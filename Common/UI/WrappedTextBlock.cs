using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ZacksMusicianship.Common.UI
{
	public class WrappedTextBlock : UIElement
	{
		private readonly List<string> wrappedLines = new();
		private string text = string.Empty;
		private float lastWrapWidth = -1f;
		private bool needsWrap = true;

		public float TextScale { get; set; }

		public Color TextColor { get; set; } = Color.White;

		public WrappedTextBlock(string text, float textScale = 1f)
		{
			TextScale = textScale;
			SetText(text);
		}

		public void SetText(string newText)
		{
			newText ??= string.Empty;
			if (text == newText)
				return;

			text = newText;
			needsWrap = true;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			RebuildLines();

			CalculatedStyle dimensions = GetDimensions();
			float lineHeight = FontAssets.MouseText.Value.MeasureString("Ag").Y * TextScale;
			Vector2 drawPosition = new(dimensions.X, dimensions.Y);

			foreach (string line in wrappedLines)
			{
				Utils.DrawBorderString(spriteBatch, line, drawPosition, TextColor, TextScale);
				drawPosition.Y += lineHeight;
			}
		}

		private void RebuildLines()
		{
			float wrapWidth = Math.Max(1f, GetDimensions().Width);
			if (!needsWrap && Math.Abs(wrapWidth - lastWrapWidth) < 0.5f)
				return;

			lastWrapWidth = wrapWidth;
			needsWrap = false;
			wrappedLines.Clear();

			string[] paragraphs = text.Replace("\r", string.Empty).Split('\n');
			foreach (string paragraph in paragraphs)
			{
				if (string.IsNullOrWhiteSpace(paragraph))
				{
					wrappedLines.Add(string.Empty);
					continue;
				}

				string currentLine = string.Empty;
				foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
				{
					string candidate = string.IsNullOrEmpty(currentLine)
						? word
						: $"{currentLine} {word}";

					if (MeasureWidth(candidate) <= wrapWidth)
					{
						currentLine = candidate;
						continue;
					}

					if (!string.IsNullOrEmpty(currentLine))
					{
						wrappedLines.Add(currentLine);
						currentLine = string.Empty;
					}

					if (MeasureWidth(word) <= wrapWidth)
					{
						currentLine = word;
						continue;
					}

					foreach (string chunk in BreakLongToken(word, wrapWidth))
						wrappedLines.Add(chunk);
				}

				if (!string.IsNullOrEmpty(currentLine))
					wrappedLines.Add(currentLine);
			}

			if (wrappedLines.Count == 0)
				wrappedLines.Add(string.Empty);
		}

		private IEnumerable<string> BreakLongToken(string token, float wrapWidth)
		{
			string currentChunk = string.Empty;

			foreach (char character in token)
			{
				string candidate = currentChunk + character;
				if (!string.IsNullOrEmpty(currentChunk) && MeasureWidth(candidate) > wrapWidth)
				{
					yield return currentChunk;
					currentChunk = character.ToString();
				}
				else
				{
					currentChunk = candidate;
				}
			}

			if (!string.IsNullOrEmpty(currentChunk))
				yield return currentChunk;
		}

		private float MeasureWidth(string value) => FontAssets.MouseText.Value.MeasureString(value).X * TextScale;
	}
}
