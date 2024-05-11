﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using System.Text.RegularExpressions;

namespace ChooseYourOutfit
{
    public class StatsReporter
    {
        public void Reset()
        {

            this.scrollPosition = default(Vector2);
            this.scrollPositioner.Arm(false);
            this.mousedOverEntry = null;
            this.cachedDrawEntries.Clear();
            this.cachedEntryValues.Clear();
        }

        public StatsReporter(Dialog_ManageApparelPoliciesEx dialog)
        {
            this.dialog = dialog;
        }

        public void DrawStatsReport(Rect rect, ThingDef def, ThingDef stuff, QualityCategory quality)
        {
            BuildableDef buildableDef = def as BuildableDef;
            StatRequest req = (buildableDef != null) ? StatRequest.For(buildableDef, stuff, quality) : StatRequest.ForEmpty();
            var specialDisplayStats = def.SpecialDisplayStats(req);

            if (this.cachedDrawEntries.NullOrEmpty<StatDrawEntry>())
            {
                ThingWithComps thing = def.GetConcreteExample(stuff) as ThingWithComps;
                CompQuality compQuality = thing.GetComp<CompQuality>();
                compQuality?.SetQuality(quality, ArtGenerationContext.Colony);
                this.cachedDrawEntries.AddRange(specialDisplayStats);
                this.cachedDrawEntries.AddRange(from r in this.StatsToDraw(thing)
                                                              where r.ShouldDisplay()
                                                              select r);
                this.FinalizeCachedDrawEntries(this.cachedDrawEntries);
            }
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, def.label.Truncate(rect.width));
            rect.yMin += Text.LineHeight;

            this.DrawStatsWorker(rect, specialDisplayStats);
        }

        public StatDrawEntry SelectedEntry { get { return this.selectedEntry; } }

        private IEnumerable<StatDrawEntry> StatsToDraw(ThingWithComps thing)
        {
            IEnumerable<StatDef> allDefs = DefDatabase<StatDef>.AllDefs.Where(s => s.Worker.ShouldShowFor(StatRequest.For(thing)));

            foreach (StatDef statDef in allDefs)
            {
                yield return new StatDrawEntry(statDef.category, statDef, thing.GetStatValue(statDef, true,  -1), StatRequest.For(thing), ToStringNumberSense.Undefined, null, false);
            }

            yield break;
        }

        private void SelectEntry(StatDrawEntry rec, bool playSound = true)
        {
            if (this.selectedEntry == this.mousedOverEntry && this.selectedEntry != null) this.selectedEntry = null;
            else this.selectedEntry = rec;
            if (playSound)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
            dialog.apparelListingRequest = true;
            dialog.layerListingRequest = true;
        }

        private void DrawStatsWorker(Rect rect, IEnumerable<StatDrawEntry> specialDisplayStats)
        {
            Rect rect2 = new Rect(rect);
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(0f, 0f, rect2.width - GenUI.ScrollBarWidth, this.listHeight);
            var anyMouseOvered = false;

            Widgets.BeginScrollView(rect2, ref this.scrollPosition, viewRect, true);
            float num = 0f;
            string b = null;

            for (int i = 0; i < this.cachedDrawEntries.Count; i++)
            {
                StatDrawEntry ent = this.cachedDrawEntries[i];

                if (ent.category.LabelCap != b)
                {
                    Widgets.ListSeparator(ref num, viewRect.width, ent.category.LabelCap);
                    b = ent.category.LabelCap;
                }

                var sortButtonRect = new Rect(viewRect.xMax - 24f, num, 24f, 24f);
                var height = ent.Draw(8f, num, viewRect.width, this.selectedEntry == ent, false, false, delegate
                {
                    if (specialDisplayStats.Any(s => s.LabelCap == ent.LabelCap) && !Mouse.IsOver(sortButtonRect))
                    {
                        this.SelectEntry(ent, true);
                    }
                }, delegate
                {
                    anyMouseOvered = true;
                    if (SortingEntry.entry != ent && Regex.IsMatch(ent.ValueString, @"[0-9]") && ent.stat != null)
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

                var statRect = new Rect(8f, num, viewRect.width, height);
                if (specialDisplayStats.Any(s => s.LabelCap == ent.LabelCap) && Mouse.IsOver(statRect))
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
                        TooltipHandler.TipRegion(statRect, tip);
                    }
                    Widgets.DrawRectFast(statRect, new Color(1f, 0.94f, 0.5f, 0.09f));
                }

                num += height;

                if (ent == SortingEntry.entry)
                {
                    GUI.DrawTexture(sortButtonRect, SortingEntry.descending ? TexButton.ReorderDown : TexButton.ReorderUp);
                }
            }
            this.listHeight = num + 100f;
            Widgets.EndScrollView();

            if (anyMouseOvered is false) this.mousedOverEntry = null;
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
               if(ent != null) this.SortingEntry.entry = ent;
            }
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

        private StatDrawEntry selectedEntry;

        private StatDrawEntry mousedOverEntry;

        private Vector2 scrollPosition;

        private ScrollPositioner scrollPositioner = new ScrollPositioner();

        private QuickSearchWidget quickSearchWidget = new QuickSearchWidget();

        private float listHeight;

        private List<StatDrawEntry> cachedDrawEntries = new List<StatDrawEntry>();

        private List<string> cachedEntryValues = new List<string>();

        private Dialog_ManageApparelPoliciesEx dialog;

        public (StatDrawEntry entry, bool descending) SortingEntry;
    }
}
