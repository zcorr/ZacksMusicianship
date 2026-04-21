using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using ZacksMusicianship.Common.Cadences;
using ZacksMusicianship.Common.Players;
using ZacksMusicianship.Common.Systems;

namespace ZacksMusicianship.Common.UI
{
	public class SongbookUIState : UIState
	{
		private const float PanelWidth = 980f;
		private const float PanelHeight = 690f;
		private const float OuterPadding = 24f;
		private const float SectionGap = 12f;
		private const float ContentWidth = PanelWidth - OuterPadding * 2f;
		private const float ColumnHeight = 522f;
		private const float CategoryColumnWidth = 210f;
		private const float EntryColumnWidth = 258f;
		private const float DetailColumnWidth = ContentWidth - CategoryColumnWidth - EntryColumnWidth - SectionGap * 2f;
		private const float CategoryButtonHeight = 48f;
		private const float EntryButtonHeight = 54f;
		private const float ButtonGap = 10f;

		private readonly UITextPanel<string>[] categoryButtons = new UITextPanel<string>[CadenceLibrary.CategoryCount];
		private readonly UITextPanel<string>[] entryButtons = new UITextPanel<string>[CadenceLibrary.MaxEntriesPerCategory];

		private bool initialized;
		private CadenceCategory selectedCategory = CadenceCategory.Foundations;
		private CadenceEntryId selectedEntry = CadenceEntryId.CircleStep;

		private WrappedTextBlock discoveredText;
		private WrappedTextBlock categoryDescriptionText;
		private WrappedTextBlock detailTitleText;
		private WrappedTextBlock detailStatusText;
		private WrappedTextBlock detailFormulaText;
		private WrappedTextBlock detailSummaryText;
		private WrappedTextBlock detailDescriptionText;
		private WrappedTextBlock detailHintText;
		private WrappedTextBlock detailExampleText;

		public override void OnInitialize()
		{
			initialized = true;

			UIPanel panel = new();
			panel.Width.Set(PanelWidth, 0f);
			panel.Height.Set(PanelHeight, 0f);
			panel.HAlign = 0.5f;
			panel.VAlign = 0.5f;
			panel.SetPadding(0f);
			panel.BackgroundColor = new Color(24, 28, 40, 245);
			panel.BorderColor = new Color(114, 96, 76);
			Append(panel);

			UIText title = new("Cadence Songbook", 1.05f, false);
			title.Left.Set(OuterPadding, 0f);
			title.Top.Set(OuterPadding, 0f);
			panel.Append(title);

			WrappedTextBlock subtitle = new("Play saved Woodcord progressions to discover harmonic routes and log them here.", 0.78f);
			subtitle.Width.Set(ContentWidth, 0f);
			subtitle.Height.Set(22f, 0f);
			subtitle.Left.Set(OuterPadding, 0f);
			subtitle.Top.Set(58f, 0f);
			subtitle.TextColor = new Color(218, 220, 228);
			panel.Append(subtitle);

			discoveredText = new WrappedTextBlock("Discovered 0 / 0 cadences", 0.8f);
			discoveredText.Width.Set(ContentWidth, 0f);
			discoveredText.Height.Set(22f, 0f);
			discoveredText.Left.Set(OuterPadding, 0f);
			discoveredText.Top.Set(86f, 0f);
			discoveredText.TextColor = new Color(176, 226, 210);
			panel.Append(discoveredText);

			float contentTop = 122f;

			UIPanel categoryPanel = CreateSectionPanel(OuterPadding, contentTop, CategoryColumnWidth, ColumnHeight);
			panel.Append(categoryPanel);

			WrappedTextBlock categoryHeader = new("Categories", 0.82f);
			categoryHeader.Width.Set(0f, 1f);
			categoryHeader.Height.Set(20f, 0f);
			categoryHeader.TextColor = new Color(232, 224, 210);
			categoryPanel.Append(categoryHeader);

			categoryDescriptionText = new WrappedTextBlock(CadenceLibrary.GetCategoryDescription(selectedCategory), 0.72f);
			categoryDescriptionText.Width.Set(0f, 1f);
			categoryDescriptionText.Height.Set(56f, 0f);
			categoryDescriptionText.Top.Set(24f, 0f);
			categoryDescriptionText.TextColor = new Color(200, 202, 214);
			categoryPanel.Append(categoryDescriptionText);

			for (int i = 0; i < categoryButtons.Length; i++)
			{
				int categoryIndex = i;
				UITextPanel<string> button = new(string.Empty, 0.72f, false);
				button.Width.Set(0f, 1f);
				button.Height.Set(CategoryButtonHeight, 0f);
				button.Top.Set(92f + i * (CategoryButtonHeight + ButtonGap), 0f);
				button.OnLeftClick += (_, _) => SelectCategory(CadenceLibrary.AllCategories[categoryIndex]);
				categoryPanel.Append(button);
				categoryButtons[i] = button;
			}

			float entryLeft = OuterPadding + CategoryColumnWidth + SectionGap;
			UIPanel entryPanel = CreateSectionPanel(entryLeft, contentTop, EntryColumnWidth, ColumnHeight);
			panel.Append(entryPanel);

			WrappedTextBlock entryHeader = new("Entries", 0.82f);
			entryHeader.Width.Set(0f, 1f);
			entryHeader.Height.Set(20f, 0f);
			entryHeader.TextColor = new Color(232, 224, 210);
			entryPanel.Append(entryHeader);

			WrappedTextBlock entryHint = new("Choose a page to inspect it. Locked pages reveal only their discovery hint.", 0.7f);
			entryHint.Width.Set(0f, 1f);
			entryHint.Height.Set(42f, 0f);
			entryHint.Top.Set(24f, 0f);
			entryHint.TextColor = new Color(200, 202, 214);
			entryPanel.Append(entryHint);

			for (int i = 0; i < entryButtons.Length; i++)
			{
				int entryIndex = i;
				UITextPanel<string> button = new(string.Empty, 0.74f, false);
				button.Width.Set(0f, 1f);
				button.Height.Set(EntryButtonHeight, 0f);
				button.Top.Set(78f + i * (EntryButtonHeight + ButtonGap), 0f);
				button.OnLeftClick += (_, _) => SelectEntryByIndex(entryIndex);
				entryPanel.Append(button);
				entryButtons[i] = button;
			}

			float detailLeft = entryLeft + EntryColumnWidth + SectionGap;
			UIPanel detailPanel = CreateSectionPanel(detailLeft, contentTop, DetailColumnWidth, ColumnHeight);
			panel.Append(detailPanel);

			detailTitleText = new WrappedTextBlock("Undiscovered Cadence", 0.96f);
			detailTitleText.Width.Set(0f, 1f);
			detailTitleText.Height.Set(34f, 0f);
			detailTitleText.TextColor = new Color(230, 230, 236);
			detailPanel.Append(detailTitleText);

			detailStatusText = new WrappedTextBlock("Locked page", 0.76f);
			detailStatusText.Width.Set(0f, 1f);
			detailStatusText.Height.Set(18f, 0f);
			detailStatusText.Top.Set(38f, 0f);
			detailPanel.Append(detailStatusText);

			detailFormulaText = new WrappedTextBlock("Formula: ???", 0.8f);
			detailFormulaText.Width.Set(0f, 1f);
			detailFormulaText.Height.Set(22f, 0f);
			detailFormulaText.Top.Set(66f, 0f);
			detailPanel.Append(detailFormulaText);

			detailSummaryText = new WrappedTextBlock("This page is still blank.", 0.78f);
			detailSummaryText.Width.Set(0f, 1f);
			detailSummaryText.Height.Set(44f, 0f);
			detailSummaryText.Top.Set(96f, 0f);
			detailPanel.Append(detailSummaryText);

			detailDescriptionText = new WrappedTextBlock("Play Woodcord progressions to discover this cadence.", 0.76f);
			detailDescriptionText.Width.Set(0f, 1f);
			detailDescriptionText.Height.Set(138f, 0f);
			detailDescriptionText.Top.Set(150f, 0f);
			detailDescriptionText.TextColor = new Color(214, 216, 228);
			detailPanel.Append(detailDescriptionText);

			detailHintText = new WrappedTextBlock("Discovery hint: ???", 0.74f);
			detailHintText.Width.Set(0f, 1f);
			detailHintText.Height.Set(84f, 0f);
			detailHintText.Top.Set(300f, 0f);
			detailHintText.TextColor = new Color(175, 224, 203);
			detailPanel.Append(detailHintText);

			detailExampleText = new WrappedTextBlock("Example: hidden until discovered", 0.74f);
			detailExampleText.Width.Set(0f, 1f);
			detailExampleText.Height.Set(52f, 0f);
			detailExampleText.Top.Set(394f, 0f);
			detailExampleText.TextColor = new Color(206, 194, 166);
			detailPanel.Append(detailExampleText);

			WrappedTextBlock detailFooter = new("Cadences are discovered from the actual chord order you perform with Woodcord, not just from what you save.", 0.72f);
			detailFooter.Width.Set(0f, 1f);
			detailFooter.Height.Set(52f, 0f);
			detailFooter.Top.Set(454f, 0f);
			detailFooter.TextColor = new Color(182, 184, 196);
			detailPanel.Append(detailFooter);

			UITextPanel<string> closeButton = new("Close", 0.84f, false);
			closeButton.Width.Set(110f, 0f);
			closeButton.Height.Set(42f, 0f);
			closeButton.Left.Set(PanelWidth - OuterPadding - 110f, 0f);
			closeButton.Top.Set(PanelHeight - OuterPadding - 42f, 0f);
			closeButton.BackgroundColor = new Color(60, 64, 88);
			closeButton.BorderColor = new Color(108, 118, 160);
			closeButton.OnLeftClick += (_, _) => SongbookSystem.Close();
			panel.Append(closeButton);

			EnsureValidSelection();
			RefreshUi();
		}

		public void LoadFromPlayer(Player player)
		{
			if (!initialized)
				return;

			EnsureValidSelection();
			RefreshUi();
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			Main.LocalPlayer.mouseInterface = true;

			if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
				SongbookSystem.Close();
		}

		private void SelectCategory(CadenceCategory category)
		{
			selectedCategory = category;
			EnsureValidSelection();
			RefreshUi();
		}

		private void SelectEntryByIndex(int entryIndex)
		{
			IReadOnlyList<CadenceBookEntry> entries = CadenceLibrary.GetEntries(selectedCategory);
			if (entryIndex < 0 || entryIndex >= entries.Count)
				return;

			selectedEntry = entries[entryIndex].Id;
			RefreshUi();
		}

		private void EnsureValidSelection()
		{
			IReadOnlyList<CadenceBookEntry> entries = CadenceLibrary.GetEntries(selectedCategory);
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries[i].Id == selectedEntry)
					return;
			}

			selectedEntry = entries[0].Id;
		}

		private void RefreshUi()
		{
			if (!initialized || discoveredText == null || detailTitleText == null)
				return;

			SongbookPlayer playerState = Main.LocalPlayer.GetModPlayer<SongbookPlayer>();
			discoveredText.SetText($"Discovered {playerState.UnlockedCadenceCount} / {CadenceLibrary.EntryCount} cadences");

			for (int i = 0; i < categoryButtons.Length; i++)
			{
				CadenceCategory category = CadenceLibrary.AllCategories[i];
				UITextPanel<string> button = categoryButtons[i];
				IReadOnlyList<CadenceBookEntry> categoryEntries = CadenceLibrary.GetEntries(category);
				int unlockedInCategory = playerState.GetUnlockedCount(category);
				bool selected = category == selectedCategory;
				Color accent = GetCategoryColor(category);

				button.SetText($"{CadenceLibrary.GetCategoryTitle(category)} {unlockedInCategory}/{categoryEntries.Count}");
				button.BackgroundColor = selected ? accent * 0.45f : new Color(46, 54, 76);
				button.BorderColor = selected ? accent : new Color(95, 102, 128);
			}

			categoryDescriptionText.SetText(CadenceLibrary.GetCategoryDescription(selectedCategory));
			categoryDescriptionText.TextColor = new Color(200, 202, 214);

			IReadOnlyList<CadenceBookEntry> entries = CadenceLibrary.GetEntries(selectedCategory);
			for (int i = 0; i < entryButtons.Length; i++)
			{
				UITextPanel<string> button = entryButtons[i];
				if (i >= entries.Count)
				{
					button.SetText(string.Empty);
					button.BackgroundColor = new Color(28, 32, 44, 140);
					button.BorderColor = new Color(52, 56, 68);
					button.IgnoresMouseInteraction = true;
					continue;
				}

				CadenceBookEntry entry = entries[i];
				bool unlocked = playerState.IsUnlocked(entry.Id);
				bool selected = entry.Id == selectedEntry;

				button.IgnoresMouseInteraction = false;
				button.SetText(unlocked ? entry.Title : $"Locked Page {i + 1}");
				button.BackgroundColor = selected
					? entry.AccentColor * (unlocked ? 0.46f : 0.22f)
					: unlocked ? entry.AccentColor * 0.22f : new Color(44, 48, 62);
				button.BorderColor = selected
					? Color.White
					: unlocked ? entry.AccentColor : new Color(96, 96, 108);
			}

			CadenceBookEntry selectedDefinition = CadenceLibrary.GetEntry(selectedEntry);
			bool isUnlocked = playerState.IsUnlocked(selectedDefinition.Id);
			Color detailColor = isUnlocked ? selectedDefinition.AccentColor : new Color(188, 188, 196);

			detailTitleText.SetText(isUnlocked ? selectedDefinition.Title : "Undiscovered Cadence");
			detailTitleText.TextColor = detailColor;

			detailStatusText.SetText(isUnlocked
				? $"Unlocked in {CadenceLibrary.GetCategoryTitle(selectedDefinition.Category)}"
				: $"Locked page in {CadenceLibrary.GetCategoryTitle(selectedDefinition.Category)}");
			detailStatusText.TextColor = isUnlocked ? new Color(214, 214, 224) : new Color(162, 162, 174);

			detailFormulaText.SetText(isUnlocked
				? $"Formula: {selectedDefinition.Formula}"
				: "Formula: ???");
			detailFormulaText.TextColor = isUnlocked ? new Color(226, 214, 176) : new Color(156, 156, 168);

			detailSummaryText.SetText(isUnlocked
				? selectedDefinition.Summary
				: "This page has not been written into your songbook yet.");
			detailSummaryText.TextColor = isUnlocked ? new Color(220, 222, 232) : new Color(174, 174, 184);

			detailDescriptionText.SetText(isUnlocked
				? selectedDefinition.Description
				: "Keep performing saved Woodcord progressions to uncover this harmonic route.");

			detailHintText.SetText($"Discovery hint: {selectedDefinition.Hint}");
			detailHintText.TextColor = new Color(175, 224, 203);

			detailExampleText.SetText(isUnlocked
				? $"Example: {selectedDefinition.Example}"
				: "Example: hidden until discovered");
			detailExampleText.TextColor = isUnlocked ? new Color(206, 194, 166) : new Color(144, 144, 156);
		}

		private static UIPanel CreateSectionPanel(float left, float top, float width, float height)
		{
			UIPanel section = new();
			section.Left.Set(left, 0f);
			section.Top.Set(top, 0f);
			section.Width.Set(width, 0f);
			section.Height.Set(height, 0f);
			section.SetPadding(14f);
			section.BackgroundColor = new Color(31, 36, 53, 220);
			section.BorderColor = new Color(94, 82, 68);
			return section;
		}

		private static Color GetCategoryColor(CadenceCategory category) => category switch
		{
			CadenceCategory.Foundations => new Color(194, 178, 112),
			CadenceCategory.MajorRoutes => new Color(122, 206, 146),
			CadenceCategory.MinorRoutes => new Color(170, 122, 232),
			_ => Color.White,
		};
	}
}
