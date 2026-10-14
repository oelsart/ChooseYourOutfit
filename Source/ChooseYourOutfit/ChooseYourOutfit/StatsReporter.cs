using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ChooseYourOutfit;

public class StatsReporter
{
	private StatDrawEntry mousedOverEntry;
	private Vector2 scrollPosition;
	private readonly ScrollPositioner scrollPositioner = new();
	private readonly QuickSearchWidget quickSearchWidget = new();
	private float listHeight;
	private List<StatDrawEntry> cachedDrawEntries = [];
	private readonly List<string> cachedEntryValues = [];
	private readonly List<float> cachedEntryHeights = [];
	private IEnumerable<StatDrawEntry> specialDisplayStats;
	private float titleHeight;
	private readonly Dialog_ManageApparelPoliciesEx dialog;
	public (StatDrawEntry entry, bool descending) SortingEntry;
	private readonly Dictionary<string, bool> collapse = [];
	private readonly List<StatDrawEntry> pinnedEntry = [];

	private static readonly CachedTexture PinTex = new("UI/Icons/Pin");

	public void Reset(float width, ThingDef def, ThingDef stuff, QualityCategory quality)
	{
		scrollPosition = default;
		scrollPositioner.Arm(false);
		mousedOverEntry = null;
		cachedDrawEntries.Clear();
		cachedEntryValues.Clear();
		cachedEntryHeights.Clear();

		BuildableDef buildableDef = def;
		var req = buildableDef != null ? StatRequest.For(buildableDef, stuff, quality) : StatRequest.ForEmpty();
		specialDisplayStats = def.SpecialDisplayStats(req);

		if (cachedDrawEntries.NullOrEmpty())
		{
			var thing = def.GetConcreteExample(stuff);
			if (thing is ThingWithComps thingWithComps && thingWithComps.GetComp<CompQuality>() is { } compQuality)
				compQuality.SetQuality(quality, ArtGenerationContext.Colony);
			cachedDrawEntries.AddRange(specialDisplayStats);
			cachedDrawEntries.AddRange(from r in StatsToDraw(thing)
				where r.ShouldDisplay()
				select r);
			FinalizeCachedDrawEntries(cachedDrawEntries);
		}

		for (var i = 0; i < cachedDrawEntries.Count; i++)
		{
			using (new TextBlock(GameFont.Small))
			{
				cachedEntryHeights.Add(Text.CalcHeight(cachedEntryValues[i], width / 2 - GenUI.ScrollBarWidth - 8f));
			}
		}

		using (new TextBlock(GameFont.Medium))
		{
			titleHeight = Text.CalcHeight(def.label, width) + 5f;
		}
	}

	public StatsReporter(Dialog_ManageApparelPoliciesEx dialog)
	{
		this.dialog = dialog;
		foreach (var statCategory in DefDatabase<StatCategoryDef>.AllDefs)
		{
			collapse[statCategory.defName] = false;
		}
	}

	public StatDrawEntry SelectedEntry { get; private set; }

	private static IEnumerable<StatDrawEntry> StatsToDraw(Thing thing)
	{
		var allDefs = DefDatabase<StatDef>.AllDefs.Where(s => s.Worker.ShouldShowFor(StatRequest.For(thing)));

		foreach (var statDef in allDefs)
		{
			yield return new StatDrawEntry(statDef.category, statDef, thing.GetStatValue(statDef), StatRequest.For(thing));
		}
	}

	private void SelectEntry(StatDrawEntry rec, bool playSound = true)
	{
		dialog.apparelListingRequest = true;
		dialog.layerListingRequest = true;
		if (SelectedEntry == mousedOverEntry && SelectedEntry != null) SelectedEntry = null;
		else SelectedEntry = rec;
		if (playSound)
		{
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		}
	}

	public void DrawStatsWorker(Rect rect)
	{
		var rect2 = rect;
		rect2.yMin += titleHeight;
		var viewRect = new Rect(0f, 0f, rect2.width - GenUI.ScrollBarWidth - 8f, listHeight);
		var anyMouseOvered = false;

		var num = 0f;
		string b = null;
		Widgets.BeginScrollView(rect2, ref scrollPosition, viewRect);

		foreach (var group in cachedDrawEntries.GroupBy(pinnedEntry.Contains).OrderByDescending(g => g.Key))
		{
			foreach (var ent in group)
			{
				if (ent?.category is null)
					continue;
				var index = cachedDrawEntries.IndexOf(ent);

				if (!group.Key && ent.category.defName != b)
				{
					var tmp = num;
					ListSeparator(tmp, viewRect.width, ent.category);
					b = ent.category.defName;
					num += Widgets.ListSeparatorHeight;
				}

				if (collapse[ent.category.defName]) continue;

				var statRect = new Rect(8f, num, viewRect.width, cachedEntryHeights[index]);
				if (Mouse.IsOver(statRect) && specialDisplayStats.Any(e => e.Same(ent)))
				{
					mousedOverEntry = ent;
					if (ChooseYourOutfit.Settings.showTooltips)
					{
						var tip = "CYO.Tip.SpecialStat".Translate() + "\n";
						if (ent.category == StatCategoryDefOf.EquippedStatOffsets) tip += "CYO.Tip.FilterByLabel".Translate();
						else tip += "CYO.Tip.FilterByValue".Translate();
						if (ChooseYourOutfit.Settings.showTooltips) TooltipHandler.TipRegion(statRect, tip);
					}

					Widgets.DrawRectFast(statRect, new Color(1f, 0.94f, 0.5f, 0.09f));
				}

				var pinRect = new Rect(viewRect.width * 0.55f - 24f, num, 24f, 24f);
				var sortButtonRect = new Rect(viewRect.xMax - 24f, num, 24f, 24f);
				Draw(ent, 8f, num, viewRect.width, SelectedEntry == ent, false, false, () =>
				{
					if (specialDisplayStats.Any(e => e.Same(ent)) && !Mouse.IsOver(sortButtonRect))
					{
						SelectEntry(ent);
					}
				}, () =>
				{
					anyMouseOvered = true;

					if (!pinnedEntry.Contains(ent))
						GUI.DrawTexture(pinRect.ContractedBy(2f), PinTex.Texture, ScaleMode.ScaleToFit, true, 1f,
							new Color(1f, 1f, 1f, 0.5f), 0f, 0f);
					if (ChooseYourOutfit.Settings.showTooltips)
						TooltipHandler.TipRegion(pinRect, "CYO.Tip.PinButton".Translate());

					if (Widgets.ButtonInvisible(pinRect) && !Find.UIRoot.windows.IsOpen<FloatMenu>())
					{
						if (!pinnedEntry.Remove(ent)) pinnedEntry.Add(ent);
					}

					if (ent.stat != null)
					{
						if (ChooseYourOutfit.Settings.showTooltips)
							TooltipHandler.TipRegion(sortButtonRect, "CYO.Tip.SortButton".Translate());
						if (ent != SortingEntry.entry)
							GUI.DrawTexture(sortButtonRect, TexButton.ReorderDown, ScaleMode.ScaleToFit, true, 1f,
								new Color(1f, 1f, 1f, 0.5f), 0f, 0f);
						if (Widgets.ButtonInvisible(sortButtonRect) && !Find.UIRoot.windows.IsOpen<FloatMenu>())
						{
							if (SortingEntry.entry != ent)
							{
								SortingEntry.entry = ent;
								SortingEntry.descending = true;
							}
							else if (SortingEntry.descending) SortingEntry.descending = false;
							else SortingEntry.entry = null;

							dialog.apparelListingRequest = true;
						}
					}
				}, scrollPosition, rect2, cachedEntryValues[index]);

				if (group.Key)
				{
					GUI.DrawTexture(pinRect.ContractedBy(2f), PinTex.Texture);
				}

				if (ent == SortingEntry.entry)
				{
					GUI.DrawTexture(sortButtonRect, SortingEntry.descending ? TexButton.ReorderDown : TexButton.ReorderUp);
				}

				num += cachedEntryHeights[index];
			}
		}

		listHeight = num;
		Widgets.EndScrollView();

		if (!anyMouseOvered) mousedOverEntry = null;
	}

	private void FinalizeCachedDrawEntries(IEnumerable<StatDrawEntry> original)
	{
		cachedDrawEntries =
		[
			.. from sd in original
			orderby sd.category.displayOrder, sd.DisplayPriorityWithinCategory descending, sd.LabelCap
			select sd
		];
		quickSearchWidget.noResultsMatched = !cachedDrawEntries.Any();
		foreach (var statDrawEntry in cachedDrawEntries)
		{
			cachedEntryValues.Add(statDrawEntry.ValueString);
			var ent = pinnedEntry.FirstOrDefault(e => e.Same(statDrawEntry));
			if (ent != null) pinnedEntry.Replace(ent, statDrawEntry);
		}

		if (SelectedEntry != null)
		{
			SelectedEntry = cachedDrawEntries.FirstOrDefault(e => e.Same(SelectedEntry));
		}

		if (quickSearchWidget.filter.Active)
		{
			foreach (var sd2 in cachedDrawEntries)
			{
				if (Matches(sd2))
				{
					SelectedEntry = sd2;
					scrollPositioner.Arm();
					break;
				}
			}
		}

		if (SortingEntry.entry != null)
		{
			var ent = cachedDrawEntries.FirstOrDefault(e => e.Same(SortingEntry.entry));
			if (ent != null) SortingEntry.entry = ent;
		}
	}

	private void Draw(StatDrawEntry entry, float x, float y, float width, bool selected,
		bool highlightLabel, bool lowlightLabel, Action clickedCallback, Action mousedOverCallback, Vector2 scrollPos,
		Rect scrollOutRect, string valueCached = null)
	{
		var num = width * 0.45f;
		var text = valueCached ?? entry.ValueString;
		Rect rect = new(x, y, width, cachedEntryHeights[cachedDrawEntries.IndexOf(entry)]);
		if (y - scrollPos.y + rect.height >= 0f && y - scrollPos.y <= scrollOutRect.height)
		{
			GUI.color = Color.white;
			if (selected)
			{
				Widgets.DrawHighlightSelected(rect);
			}

			Widgets.DrawHighlightIfMouseover(rect);
			if (highlightLabel)
			{
				Widgets.DrawTextHighlight(rect);
			}

			if (lowlightLabel)
			{
				GUI.color = Color.grey;
			}

			var rect2 = rect;
			rect2.width -= num + 26f;
			Widgets.Label(rect2, entry.LabelCap.Truncate(rect2.width));
			if (Text.CalcSize(entry.LabelCap).x > rect2.width) TooltipHandler.TipRegion(rect2, entry.LabelCap);
			var rect3 = rect;
			rect3.x = rect2.xMax + 26f;
			rect3.width = num;
			Widgets.Label(rect3, text);
			GUI.color = Color.white;
			if (Mouse.IsOver(rect))
			{
				mousedOverCallback();
				if (entry.stat != null)
				{
					var localStat = entry.stat;
					TooltipHandler.TipRegion(rect,
						new TipSignal(() => localStat.LabelCap + ": " + localStat.description, entry.stat.GetHashCode()));
				}

				if (Event.current is { type: EventType.mouseUp, button: 0 })
				{
					clickedCallback();
				}
			}
		}
	}

	public bool Matches(StatDrawEntry sd)
	{
		return quickSearchWidget.filter.Matches(sd.LabelCap);
	}

	public void SelectEntry(int index)
	{
		if (index < 0 || index > cachedDrawEntries.Count)
		{
			return;
		}

		SelectEntry(cachedDrawEntries[index]);
	}

	public void ListSeparator(float curY, float width, StatCategoryDef category)
	{
		var color = GUI.color;
		curY += 3f;
		GUI.color = Widgets.SeparatorLabelColor;
		Rect rect1 = new(0f, curY, 20f, 20f);
		Rect rect2 = new(25f, curY, width - 25f, 30f);
		Text.Anchor = TextAnchor.UpperLeft;
		var tex = collapse[category.defName] ? TexButton.Reveal : TexButton.Collapse;
		if (Widgets.ButtonInvisible(rect1))
		{
			collapse[category.defName] = !collapse[category.defName];
		}

		Widgets.DrawTextureFitted(rect1, tex, 1f);
		Widgets.Label(rect2, category.defName);
		curY += 20f;
		GUI.color = Widgets.SeparatorLineColor;
		Widgets.DrawLineHorizontal(0f, curY, width);
		GUI.color = color;
	}
}