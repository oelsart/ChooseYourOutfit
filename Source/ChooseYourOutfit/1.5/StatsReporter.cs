using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ChooseYourOutfit
{
    public class StatsReporter
    {
        public void Reset(float width, ThingDef def, ThingDef stuff, QualityCategory quality)
        {

            scrollPosition = default;
            scrollPositioner.Arm(false);
            mousedOverEntry = null;
            cachedDrawEntries.Clear();
            cachedEntryValues.Clear();
            cachedEntryHeights.Clear();

            BuildableDef buildableDef = def;
            StatRequest req = (buildableDef != null) ? StatRequest.For(buildableDef, stuff, quality) : StatRequest.ForEmpty();
            specialDisplayStats = def.SpecialDisplayStats(req);

            if (cachedDrawEntries.NullOrEmpty<StatDrawEntry>())
            {
                ThingWithComps thing = def.GetConcreteExample(stuff) as ThingWithComps;
                CompQuality compQuality = thing.GetComp<CompQuality>();
                compQuality?.SetQuality(quality, ArtGenerationContext.Colony);
                cachedDrawEntries.AddRange(specialDisplayStats);
                cachedDrawEntries.AddRange(from r in StatsToDraw(thing)
                                           where r.ShouldDisplay()
                                           select r);
                FinalizeCachedDrawEntries(cachedDrawEntries);
            }

            for (int i = 0; i < cachedDrawEntries.Count; i++)
            {
                using (new TextBlock(GameFont.Small))
                {
                    cachedEntryHeights.Add(Text.CalcHeight(cachedEntryValues[i], (width / 2) - GenUI.ScrollBarWidth - 8f));
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
                if (statCategory.LabelCap == null) continue;
                collapse[statCategory.LabelCap] = false;
            }
        }

        public StatDrawEntry SelectedEntry => selectedEntry;

        private IEnumerable<StatDrawEntry> StatsToDraw(ThingWithComps thing)
        {
            IEnumerable<StatDef> allDefs = DefDatabase<StatDef>.AllDefs.Where(s => s.Worker.ShouldShowFor(StatRequest.For(thing)));

            foreach (StatDef statDef in allDefs)
            {
                yield return new StatDrawEntry(statDef.category, statDef, thing.GetStatValue(statDef, true, -1), StatRequest.For(thing), ToStringNumberSense.Undefined, null, false);
            }

            yield break;
        }

        private void SelectEntry(StatDrawEntry rec, bool playSound = true)
        {
            dialog.apparelListingRequest = true;
            dialog.layerListingRequest = true;
            if (selectedEntry == mousedOverEntry && selectedEntry != null) selectedEntry = null;
            else selectedEntry = rec;
            if (playSound)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
        }

        public IEnumerable<Action> DrawStatsWorker(Rect rect)
        {
            Rect rect2 = new Rect(rect);
            rect2.yMin += titleHeight;
            Rect viewRect = new Rect(0f, 0f, rect2.width - GenUI.ScrollBarWidth - 8f, listHeight);
            var anyMouseOvered = false;

            float num = 0f;
            string b = null;
            yield return () => Widgets.BeginScrollView(rect2, ref scrollPosition, viewRect, true);

            foreach (var group in cachedDrawEntries.GroupBy(e => pinnedEntry.Contains(e)).OrderByDescending(g => g.Key == true))
            {
                foreach (var ent in group)
                {
                    var i = cachedDrawEntries.IndexOf(ent);

                    if (group.Key == false && ent.category.LabelCap != b)
                    {
                        var tmp = num;
                        yield return () => ListSeparator(tmp, viewRect.width, ent.category);
                        b = ent.category.LabelCap;
                        num += Widgets.ListSeparatorHeight;
                    }

                    if (collapse[ent.category.LabelCap]) continue;

                    var statRect = new Rect(8f, num, viewRect.width, cachedEntryHeights[i]);
                    yield return () =>
                    {
                        if (Mouse.IsOver(statRect) && specialDisplayStats.Any(s => s.LabelCap == ent.LabelCap))
                        {
                            mousedOverEntry = ent;
                            if (ChooseYourOutfit.settings.showTooltips)
                            {
                                var tip = "CYO.Tip.SpecialStat".Translate() + "\n";
                                if (ent.category == StatCategoryDefOf.EquippedStatOffsets) tip += "CYO.Tip.FilterByLabel".Translate();
                                else tip += "CYO.Tip.FilterByValue".Translate();
                                if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(statRect, tip);
                            }
                            Widgets.DrawRectFast(statRect, new Color(1f, 0.94f, 0.5f, 0.09f));
                        }
                    };

                    var pinRect = new Rect((viewRect.width * 0.55f) - 24f, num, 24f, 24f);
                    var sortButtonRect = new Rect(viewRect.xMax - 24f, num, 24f, 24f);
                    var drawResult = Draw(ent, 8f, num, viewRect.width, selectedEntry == ent, false, false, () =>
                    {
                        if (specialDisplayStats.Any(s => s.LabelCap == ent.LabelCap) && !Mouse.IsOver(sortButtonRect))
                        {
                            Input.ResetInputAxes();
                            SelectEntry(ent, true);
                        }
                    }, () =>
                    {
                        anyMouseOvered = true;

                        if (!pinnedEntry.Contains(ent)) GUI.DrawTexture(pinRect.ContractedBy(2f), PinTex, ScaleMode.ScaleToFit, true, 1f, new Color(1f, 1f, 1f, 0.5f), 0f, 0f);
                        if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(pinRect, "CYO.Tip.PinButton".Translate());
                        if (Mouse.IsOver(pinRect) && Input.GetMouseButtonDown(0) && !Find.UIRoot.windows.IsOpen<FloatMenu>())
                        {
                            Input.ResetInputAxes();
                            if (pinnedEntry.Contains(ent)) pinnedEntry.Remove(ent);
                            else pinnedEntry.Add(ent);
                        }

                        if (ent.stat != null)
                        {
                            if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(sortButtonRect, "CYO.Tip.SortButton".Translate());
                            if (ent != SortingEntry.entry) GUI.DrawTexture(sortButtonRect, TexButton.ReorderDown, ScaleMode.ScaleToFit, true, 1f, new Color(1f, 1f, 1f, 0.5f), 0f, 0f);
                            if (Mouse.IsOver(sortButtonRect) && Input.GetMouseButtonDown(0) && !Find.UIRoot.windows.IsOpen<FloatMenu>())
                            {
                                Input.ResetInputAxes();
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
                    }, scrollPosition, rect2, cachedEntryValues[i]);

                    foreach (var draw in drawResult) yield return draw;

                    if (group.Key == true)
                    {
                        yield return () => GUI.DrawTexture(pinRect.ContractedBy(2f), PinTex);
                    }

                    if (ent == SortingEntry.entry)
                    {
                        yield return () => GUI.DrawTexture(sortButtonRect, SortingEntry.descending ? TexButton.ReorderDown : TexButton.ReorderUp);
                    }

                    num += cachedEntryHeights[i];
                }
            }
            listHeight = num;
            yield return () => Widgets.EndScrollView();

            if (anyMouseOvered is false) mousedOverEntry = null;
        }

        private void FinalizeCachedDrawEntries(IEnumerable<StatDrawEntry> original)
        {
            cachedDrawEntries = [.. from sd in original
                                      orderby sd.category.displayOrder, sd.DisplayPriorityWithinCategory descending, sd.LabelCap
                                      select sd];
            quickSearchWidget.noResultsMatched = !cachedDrawEntries.Any<StatDrawEntry>();
            foreach (StatDrawEntry statDrawEntry in cachedDrawEntries)
            {
                cachedEntryValues.Add(statDrawEntry.ValueString);
                var ent = pinnedEntry.FirstOrDefault((StatDrawEntry e) => e.Same(statDrawEntry));
                if (ent != null) pinnedEntry.Replace(ent, statDrawEntry);
            }
            if (selectedEntry != null)
            {
                selectedEntry = cachedDrawEntries.FirstOrDefault((StatDrawEntry e) => e.Same(selectedEntry));
            }
            if (quickSearchWidget.filter.Active)
            {
                foreach (StatDrawEntry sd2 in cachedDrawEntries)
                {
                    if (Matches(sd2))
                    {
                        selectedEntry = sd2;
                        scrollPositioner.Arm(true);
                        break;
                    }
                }
            }
            if (SortingEntry.entry != null)
            {
                var ent = cachedDrawEntries.FirstOrDefault((StatDrawEntry e) => e.Same(SortingEntry.entry));
                if (ent != null) SortingEntry.entry = ent;
            }
        }

        private IEnumerable<Action> Draw(StatDrawEntry entry, float x, float y, float width, bool selected, bool highlightLabel, bool lowlightLabel, Action clickedCallback, Action mousedOverCallback, Vector2 scrollPosition, Rect scrollOutRect, string valueCached = null)
        {
            float num = width * 0.45f;
            string text = valueCached ?? entry.ValueString;
            Rect rect = new Rect(x, y, width, cachedEntryHeights[cachedDrawEntries.IndexOf(entry)]);
            if (y - scrollPosition.y + rect.height >= 0f && y - scrollPosition.y <= scrollOutRect.height)
            {
                GUI.color = Color.white;
                if (selected)
                {
                    yield return () => Widgets.DrawHighlightSelected(rect);
                }
                yield return () =>
                {
                    if (Mouse.IsOver(rect))
                    {
                        Widgets.DrawHighlight(rect);
                    }
                };
                if (highlightLabel)
                {
                    yield return () => Widgets.DrawTextHighlight(rect, 4f, null);
                }
                if (lowlightLabel)
                {
                    GUI.color = Color.grey;
                }
                Rect rect2 = rect;
                rect2.width -= num + 26f;
                yield return () =>
                {
                    Widgets.Label(rect2, entry.LabelCap.Truncate(rect2.width));
                    if (Text.CalcSize(entry.LabelCap).x > rect2.width) TooltipHandler.TipRegion(rect2, entry.LabelCap);
                };
                Rect rect3 = rect;
                rect3.x = rect2.xMax + 26f;
                rect3.width = num;
                yield return () => Widgets.Label(rect3, text);
                GUI.color = Color.white;
                yield return () =>
                {
                    if (Mouse.IsOver(rect))
                    {
                        mousedOverCallback();
                        if (entry.stat != null)
                        {
                            StatDef localStat = entry.stat;
                            TooltipHandler.TipRegion(rect, new TipSignal(() => localStat.LabelCap + ": " + localStat.description, entry.stat.GetHashCode()));
                        }
                        if (Input.GetMouseButtonUp(0))
                        {
                            clickedCallback();
                        }
                    }
                };
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
            SelectEntry(cachedDrawEntries[index], true);
        }

        public void ListSeparator(float curY, float width, StatCategoryDef category)
        {
            Color color = GUI.color;
            curY += 3f;
            GUI.color = Widgets.SeparatorLabelColor;
            Rect rect1 = new Rect(0f, curY, 20f, 20f);
            Rect rect2 = new Rect(25f, curY, width - 25f, 30f);
            Text.Anchor = TextAnchor.UpperLeft;
            Texture2D tex = collapse[category.LabelCap] ? TexButton.Reveal : TexButton.Collapse;
            if (Mouse.IsOver(rect1) && Input.GetMouseButtonUp(0))
            {
                Input.ResetInputAxes();
                collapse[category.LabelCap] = !collapse[category.LabelCap];
            }
            Widgets.DrawTextureFitted(rect1, tex, 1f);
            Widgets.Label(rect2, category.LabelCap);
            curY += 20f;
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(0f, curY, width);
            GUI.color = color;
        }

        private StatDrawEntry selectedEntry;

        private StatDrawEntry mousedOverEntry;

        private Vector2 scrollPosition;

        private ScrollPositioner scrollPositioner = new ScrollPositioner();

        private QuickSearchWidget quickSearchWidget = new QuickSearchWidget();

        private float listHeight;

        private List<StatDrawEntry> cachedDrawEntries = [];

        private List<string> cachedEntryValues = [];

        private List<float> cachedEntryHeights = [];

        private IEnumerable<StatDrawEntry> specialDisplayStats;

        private float titleHeight;

        private Dialog_ManageApparelPoliciesEx dialog;

        public (StatDrawEntry entry, bool descending) SortingEntry;

        private Dictionary<string, bool> collapse = [];

        private List<StatDrawEntry> pinnedEntry = [];

        private readonly Texture2D PinTex = ContentFinder<Texture2D>.Get("UI/Icons/Pin", true);
    }
}
