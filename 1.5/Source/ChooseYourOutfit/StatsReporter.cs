using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace ChooseYourOutfit
{
    public static class StatsReporter
    {
        public static void Reset()
        {
            StatsReporter.scrollPosition = default(Vector2);
            StatsReporter.selectedEntry = null;
            StatsReporter.scrollPositioner.Arm(false);
            StatsReporter.mousedOverEntry = null;
            StatsReporter.cachedDrawEntries.Clear();
            StatsReporter.cachedEntryValues.Clear();
        }

        public static void DrawStatsReport(Rect rect, ThingDef def, ThingDef stuff, QualityCategory quality)
        {
            if (StatsReporter.cachedDrawEntries.NullOrEmpty<StatDrawEntry>())
            {
                BuildableDef buildableDef = def as BuildableDef;
                StatRequest req = (buildableDef != null) ? StatRequest.For(buildableDef, stuff, quality) : StatRequest.ForEmpty();
                ThingWithComps thing = def.GetConcreteExample(stuff) as ThingWithComps;
                CompQuality compQuality = thing.GetComp<CompQuality>();
                compQuality?.SetQuality(quality, ArtGenerationContext.Colony);
                StatsReporter.cachedDrawEntries.AddRange(def.SpecialDisplayStats(req));
                StatsReporter.cachedDrawEntries.AddRange(from r in StatsReporter.StatsToDraw(thing)
                                                              where r.ShouldDisplay()
                                                              select r);
                StatsReporter.FinalizeCachedDrawEntries(StatsReporter.cachedDrawEntries);
            }
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, def.label.Truncate(rect.width));
            rect.yMin += Text.LineHeight;

            StatsReporter.DrawStatsWorker(rect);
        }

        private static IEnumerable<StatDrawEntry> StatsToDraw(ThingWithComps thing)
        {
            IEnumerable<StatDef> allDefs = DefDatabase<StatDef>.AllDefs.Where(s => s.Worker.ShouldShowFor(StatRequest.For(thing)));

            foreach (StatDef statDef in allDefs)
            {
                yield return new StatDrawEntry(statDef.category, statDef, thing.GetStatValue(statDef, true,  -1), StatRequest.For(thing), ToStringNumberSense.Undefined, null, false);
            }

            yield break;
        }

        private static void SelectEntry(StatDrawEntry rec, bool playSound = true)
        {
            StatsReporter.selectedEntry = rec;
            if (playSound)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
        }

        private static void DrawStatsWorker(Rect rect)
        {
            Rect rect2 = new Rect(rect);
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(0f, 0f, rect2.width - 16f, StatsReporter.listHeight);
            Widgets.BeginScrollView(rect2, ref StatsReporter.scrollPosition, viewRect, true);
            float num = 0f;
            string b = null;
            StatsReporter.mousedOverEntry = null;
            for (int i = 0; i < StatsReporter.cachedDrawEntries.Count; i++)
            {
                StatDrawEntry ent = StatsReporter.cachedDrawEntries[i];
                if (ent.category.LabelCap != b)
                {
                    Widgets.ListSeparator(ref num, viewRect.width, ent.category.LabelCap);
                    b = ent.category.LabelCap;
                }

                num += ent.Draw(8f, num, viewRect.width, StatsReporter.selectedEntry == ent, false, false, delegate
                {
                    StatsReporter.SelectEntry(ent, true);
                }, delegate
                {
                    StatsReporter.mousedOverEntry = ent;
                }, StatsReporter.scrollPosition, rect2, StatsReporter.cachedEntryValues[i]);
            }

            StatsReporter.listHeight = num + 100f;
            Widgets.EndScrollView();
        }

        private static void FinalizeCachedDrawEntries(IEnumerable<StatDrawEntry> original)
        {
            StatsReporter.cachedDrawEntries = (from sd in original
                                 orderby sd.category.displayOrder, sd.DisplayPriorityWithinCategory descending, sd.LabelCap
                                 select sd).ToList<StatDrawEntry>();
            StatsReporter.quickSearchWidget.noResultsMatched = !StatsReporter.cachedDrawEntries.Any<StatDrawEntry>();
            foreach (StatDrawEntry statDrawEntry in StatsReporter.cachedDrawEntries)
            {
                StatsReporter.cachedEntryValues.Add(statDrawEntry.ValueString);
            }
            if (StatsReporter.selectedEntry != null)
            {
                StatsReporter.selectedEntry = StatsReporter.cachedDrawEntries.FirstOrDefault((StatDrawEntry e) => e.Same(StatsReporter.selectedEntry));
            }
            if (StatsReporter.quickSearchWidget.filter.Active)
            {
                foreach (StatDrawEntry sd2 in StatsReporter.cachedDrawEntries)
                {
                    if (StatsReporter.Matches(sd2))
                    {
                        StatsReporter.selectedEntry = sd2;
                        StatsReporter.scrollPositioner.Arm(true);
                        break;
                    }
                }
            }
        }

        private static bool Matches(StatDrawEntry sd)
        {
            return StatsReporter.quickSearchWidget.filter.Matches(sd.LabelCap);
        }

        public static void SelectEntry(int index)
        {
            if (index < 0 || index > StatsReporter.cachedDrawEntries.Count)
            {
                return;
            }
            StatsReporter.SelectEntry(StatsReporter.cachedDrawEntries[index], true);
        }

        private static StatDrawEntry selectedEntry;

        private static StatDrawEntry mousedOverEntry;

        private static Vector2 scrollPosition;

        private static ScrollPositioner scrollPositioner = new ScrollPositioner();

        private static QuickSearchWidget quickSearchWidget = new QuickSearchWidget();

        private static float listHeight;

        private static List<StatDrawEntry> cachedDrawEntries = new List<StatDrawEntry>();

        private static List<string> cachedEntryValues = new List<string>();
    }
}
