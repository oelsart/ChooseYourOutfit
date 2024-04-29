using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;

namespace ChooseYourOutfit
{
    public static class CYO_StatsReporter
    {
        public static void Reset()
        {
            scrollPosition = default(Vector2);
            selectedEntry = null;
            scrollPositioner.Arm(false);
            mousedOverEntry = null;
            cachedDrawEntries.Clear();
            cachedEntryValues.Clear();
        }

        public static void DrawStatsReport(Rect rect, ThingDef def, ThingDef stuff, QualityCategory quality)
        {
            if (cachedDrawEntries.NullOrEmpty<StatDrawEntry>())
            {
                BuildableDef buildableDef = def as BuildableDef;
                StatRequest req = (buildableDef != null) ? StatRequest.For(buildableDef, stuff, quality) : StatRequest.ForEmpty();
                ThingWithComps thing = def.GetConcreteExample(stuff) as ThingWithComps;
                CompQuality compQuality = thing.GetComp<CompQuality>();
                compQuality?.SetQuality(quality, ArtGenerationContext.Colony);
                cachedDrawEntries.AddRange(def.SpecialDisplayStats(req));
                cachedDrawEntries.AddRange(from r in StatsToDraw(thing)
                                                              where r.ShouldDisplay
                                                              select r);
                FinalizeCachedDrawEntries(cachedDrawEntries);
            }
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, def.label);
            rect.yMin += Text.LineHeight;

            DrawStatsWorker(rect);
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
            selectedEntry = rec;
            if (playSound)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
        }

        private static void DrawStatsWorker(Rect rect)
        {
            Rect rect2 = new Rect(rect);
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(0f, 0f, rect2.width - 16f, listHeight);
            Widgets.BeginScrollView(rect2, ref scrollPosition, viewRect, true);
            float num = 0f;
            string b = null;
            mousedOverEntry = null;
            for (int i = 0; i < cachedDrawEntries.Count; i++)
            {
                StatDrawEntry ent = cachedDrawEntries[i];
                if (ent.category.LabelCap != b)
                {
                    Widgets.ListSeparator(ref num, viewRect.width, ent.category.LabelCap);
                    b = ent.category.LabelCap;
                }

                num += ent.Draw(8f, num, viewRect.width, selectedEntry == ent, false, false, delegate
                {
                    SelectEntry(ent, true);
                }, delegate
                {
                    mousedOverEntry = ent;
                }, scrollPosition, rect2, cachedEntryValues[i]);
            }

            listHeight = num + 100f;
            Widgets.EndScrollView();
        }

        private static void FinalizeCachedDrawEntries(IEnumerable<StatDrawEntry> original)
        {
            cachedDrawEntries = (from sd in original
                                 orderby sd.category.displayOrder, sd.DisplayPriorityWithinCategory descending, sd.LabelCap
                                 select sd).ToList<StatDrawEntry>();
            quickSearchWidget.noResultsMatched = !cachedDrawEntries.Any<StatDrawEntry>();
            foreach (StatDrawEntry statDrawEntry in cachedDrawEntries)
            {
                cachedEntryValues.Add(statDrawEntry.ValueString);
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
        }

        private static bool Matches(StatDrawEntry sd)
        {
            return quickSearchWidget.filter.Matches(sd.LabelCap);
        }

        public static void SelectEntry(int index)
        {
            if (index < 0 || index > cachedDrawEntries.Count)
            {
                return;
            }
            SelectEntry(cachedDrawEntries[index], true);
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
