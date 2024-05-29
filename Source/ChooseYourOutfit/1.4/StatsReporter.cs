using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace ChooseYourOutfit
{
    public class StatsReporter
    {
        public void Reset(float width, ThingDef def, ThingDef stuff, QualityCategory quality)
        {

            this.scrollPosition = default(Vector2);
            this.scrollPositioner.Arm(false);
            this.mousedOverEntry = null;
            this.cachedDrawEntries.Clear();
            this.cachedEntryValues.Clear();
            this.cachedEntryHeights.Clear();

            BuildableDef buildableDef = def as BuildableDef;
            StatRequest req = (buildableDef != null) ? StatRequest.For(buildableDef, stuff, quality) : StatRequest.ForEmpty();
            this.specialDisplayStats = def.SpecialDisplayStats(req);

            if (this.cachedDrawEntries.NullOrEmpty<StatDrawEntry>())
            {
                ThingWithComps thing = def.GetConcreteExample(stuff) as ThingWithComps;
                CompQuality compQuality = thing.GetComp<CompQuality>();
                compQuality?.SetQuality(quality, ArtGenerationContext.Colony);
                this.cachedDrawEntries.AddRange(specialDisplayStats);
                this.cachedDrawEntries.AddRange(from r in this.StatsToDraw(thing)
                                                where r.ShouldDisplay
                                                select r);
                this.FinalizeCachedDrawEntries(this.cachedDrawEntries);
            }

            for (int i = 0; i < this.cachedDrawEntries.Count; i++)
            {
                using (new TextBlock(GameFont.Small))
                {
                    this.cachedEntryHeights.Add(Text.CalcHeight(this.cachedEntryValues[i], width / 2 - GenUI.ScrollBarWidth - 8f));
                }
            }

            using (new TextBlock(GameFont.Medium))
            {
                this.titleHeight = Text.CalcHeight(def.label, width) + 5f;
            }
        }

        public StatsReporter(Dialog_ManageOutfitsEx dialog)
        {
            this.dialog = dialog;
            foreach (var statCategory in DefDatabase<StatCategoryDef>.AllDefs)
            {
                collapse[statCategory.LabelCap] = false;
            }
        }

        public StatDrawEntry SelectedEntry { get { return this.selectedEntry; } }

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
            if (this.selectedEntry == this.mousedOverEntry && this.selectedEntry != null) this.selectedEntry = null;
            else this.selectedEntry = rec;
            if (playSound)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
        }

        public ConcurrentQueue<Action> DrawStatsWorker(Rect rect)
        {
            var drawer = new ConcurrentQueue<Action>();
            Rect rect2 = new Rect(rect);
            rect2.yMin += this.titleHeight;
            Rect viewRect = new Rect(0f, 0f, rect2.width - GenUI.ScrollBarWidth - 8f, this.listHeight);
            var anyMouseOvered = false;

            float num = 0f;
            string b = null;
            drawer.Enqueue(() => Widgets.BeginScrollView(rect2, ref this.scrollPosition, viewRect, true));

            foreach (var group in this.cachedDrawEntries.GroupBy(e => pinnedEntry.Contains(e)).OrderByDescending(g => g.Key == true))
            {
                foreach (var ent in group)
                {
                    var i = this.cachedDrawEntries.IndexOf(ent);

                    if (group.Key == false && ent.category.LabelCap != b)
                    {
                        var tmp = num;
                        drawer.Enqueue(() => this.ListSeparator(tmp, viewRect.width, ent.category));
                        b = ent.category.LabelCap;
                        num += Widgets.ListSeparatorHeight;
                    }

                    if (collapse[ent.category.LabelCap]) continue;

                    var statRect = new Rect(8f, num, viewRect.width, this.cachedEntryHeights[i]);
                    drawer.Enqueue(() =>
                    {
                        if (Mouse.IsOver(statRect) && specialDisplayStats.Any(s => s.LabelCap == ent.LabelCap))
                        {
                            this.mousedOverEntry = ent;
                            if (ChooseYourOutfit.settings.showTooltips)
                            {
                                var tip = "CYO.Tip.SpecialStat".Translate() + "\n";
                                if (ent.LabelCap == "Stat_Source_Label".Translate() ||
                                    ent.LabelCap == "Stat_Thing_Apparel_CountsAsClothingNudity_Name".Translate() ||
                                    ent.LabelCap == "Layer".Translate() ||
                                    ent.LabelCap == "Covers".Translate() ||
                                    ent.LabelCap == "CreatedAt".Translate() ||
                                    ent.LabelCap == "Ingredients".Translate() ||
                                    ent.LabelCap == "Stat_Thing_Apparel_ValidLifestage".Translate()) tip += "CYO.Tip.FilterByValue".Translate();
                                else tip += "CYO.Tip.FilterByLabel".Translate();
                                if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(statRect, tip);
                            }
                            Widgets.DrawRectFast(statRect, new Color(1f, 0.94f, 0.5f, 0.09f));
                        }
                    });

                    var pinRect = new Rect(viewRect.width * 0.55f - 24f, num, 24f, 24f);
                    var sortButtonRect = new Rect(viewRect.xMax - 24f, num, 24f, 24f);
                    var drawResult = this.Draw(ent, 8f, num, viewRect.width, this.selectedEntry == ent, false, false, delegate
                    {
                        if (specialDisplayStats.Any(s => s.LabelCap == ent.LabelCap) && !Mouse.IsOver(sortButtonRect))
                        {
                            Input.ResetInputAxes();
                            this.SelectEntry(ent, true);
                        }
                    }, delegate
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
                    }, this.scrollPosition, rect2, this.cachedEntryValues[i]);

                    foreach (var draw in drawResult) drawer.Enqueue(draw);

                    if (group.Key == true)
                    {
                        drawer.Enqueue(() => GUI.DrawTexture(pinRect.ContractedBy(2f), PinTex));
                    }

                    if (ent == SortingEntry.entry)
                    {
                        drawer.Enqueue(() => GUI.DrawTexture(sortButtonRect, SortingEntry.descending ? TexButton.ReorderDown : TexButton.ReorderUp));
                    }

                    num += this.cachedEntryHeights[i];
                }
            }
            this.listHeight = num + 100f;
            drawer.Enqueue(() => Widgets.EndScrollView());

            if (anyMouseOvered is false) this.mousedOverEntry = null;

            return drawer;
        }

        private void FinalizeCachedDrawEntries(IEnumerable<StatDrawEntry> original)
        {
            this.cachedDrawEntries = (from sd in original
                                      orderby sd.category.displayOrder, sd.DisplayPriorityWithinCategory descending, sd.LabelCap
                                      select sd).ToList<StatDrawEntry>();
            this.quickSearchWidget.noResultsMatched = !this.cachedDrawEntries.Any<StatDrawEntry>();
            foreach (StatDrawEntry statDrawEntry in this.cachedDrawEntries)
            {
                this.cachedEntryValues.Add(statDrawEntry.ValueString);
                var ent = this.pinnedEntry.FirstOrDefault((StatDrawEntry e) => e.Same(statDrawEntry));
                if (ent != null) this.pinnedEntry.Replace(ent, statDrawEntry);
            }
            if (this.selectedEntry != null)
            {
                this.selectedEntry = this.cachedDrawEntries.FirstOrDefault((StatDrawEntry e) => e.Same(this.selectedEntry));
            }
            if (this.quickSearchWidget.filter.Active)
            {
                foreach (StatDrawEntry sd2 in this.cachedDrawEntries)
                {
                    if (this.Matches(sd2))
                    {
                        this.selectedEntry = sd2;
                        this.scrollPositioner.Arm(true);
                        break;
                    }
                }
            }
            if (this.SortingEntry.entry != null)
            {
                var ent = this.cachedDrawEntries.FirstOrDefault((StatDrawEntry e) => e.Same(this.SortingEntry.entry));
                if (ent != null) this.SortingEntry.entry = ent;
            }
        }

        private ConcurrentQueue<Action> Draw(StatDrawEntry entry, float x, float y, float width, bool selected, bool highlightLabel, bool lowlightLabel, Action clickedCallback, Action mousedOverCallback, Vector2 scrollPosition, Rect scrollOutRect, string valueCached = null)
        {
            var drawer = new ConcurrentQueue<Action>();
            float num = width * 0.45f;
            string text = valueCached ?? entry.ValueString;
            Rect rect = new Rect(x, y, width, cachedEntryHeights[this.cachedDrawEntries.IndexOf(entry)]);
            if (y - scrollPosition.y + rect.height >= 0f && y - scrollPosition.y <= scrollOutRect.height)
            {
                GUI.color = Color.white;
                if (selected)
                {
                    drawer.Enqueue(() => Widgets.DrawHighlightSelected(rect));
                }
                drawer.Enqueue(() =>
                {
                    if (Mouse.IsOver(rect))
                    {
                        Widgets.DrawHighlight(rect);
                    }
                });
                if (highlightLabel)
                {
                    drawer.Enqueue(() => Widgets.DrawTextHighlight(rect, 4f, null));
                }
                if (lowlightLabel)
                {
                    GUI.color = Color.grey;
                }
                Rect rect2 = rect;
                rect2.width -= num + 26f;
                drawer.Enqueue(() =>
                {
                    Widgets.Label(rect2, entry.LabelCap.Truncate(rect2.width));
                    if (Text.CalcSize(entry.LabelCap).x > rect2.width) TooltipHandler.TipRegion(rect2, entry.LabelCap);
                });
                Rect rect3 = rect;
                rect3.x = rect2.xMax + 26f;
                rect3.width = num;
                drawer.Enqueue(() => Widgets.Label(rect3, text));
                GUI.color = Color.white;
                drawer.Enqueue(() =>
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
                });
            }
            return drawer;
        }

        public bool Matches(StatDrawEntry sd)
        {
            return this.quickSearchWidget.filter.Matches(sd.LabelCap);
        }

        public void SelectEntry(int index)
        {
            if (index < 0 || index > this.cachedDrawEntries.Count)
            {
                return;
            }
            this.SelectEntry(this.cachedDrawEntries[index], true);
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

        private List<StatDrawEntry> cachedDrawEntries = new List<StatDrawEntry>();

        private List<string> cachedEntryValues = new List<string>();

        private List<float> cachedEntryHeights = new List<float>();

        private IEnumerable<StatDrawEntry> specialDisplayStats;

        private float titleHeight;

        private Dialog_ManageOutfitsEx dialog;

        public (StatDrawEntry entry, bool descending) SortingEntry;

        private Dictionary<string, bool> collapse = new Dictionary<string, bool>();

        private List<StatDrawEntry> pinnedEntry = new List<StatDrawEntry>();

        private readonly Texture2D PinTex = ContentFinder<Texture2D>.Get("UI/Icons/Pin", true);
    }
}
