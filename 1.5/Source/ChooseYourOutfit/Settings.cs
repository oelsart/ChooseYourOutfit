using Verse;
using UnityEngine;
using RimWorld;

namespace ChooseYourOutfit
{
    public class Settings : ModSettings
    {
        public bool disableAddedUI;
        public bool collapseByLayer;
        public bool syncFilter;
        public bool apparelListMode;
        public bool selectedApparelListMode;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref disableAddedUI, "disableAddedUI", false);
            Scribe_Values.Look(ref collapseByLayer, "collapseByLayer", true);
            Scribe_Values.Look(ref syncFilter, "syncFilter", true);
            Scribe_Values.Look(ref apparelListMode, "apparelListMode", false);
            Scribe_Values.Look(ref selectedApparelListMode, "selectedApparelListMode", false);
            base.ExposeData();
        }
    }
}