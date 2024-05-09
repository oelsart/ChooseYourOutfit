using Verse;
using UnityEngine;
using RimWorld;

namespace ChooseYourOutfit
{
    public class Settings : ModSettings
    {
        public bool disableAddedUI = false;
        public bool collapseByLayer = true;
        public bool syncFilter = true;
        public bool filterLoadTiming = false;
        public bool apparelListMode = false;
        public bool moveToBottom = false;
        public bool showAddBillsButton = true;
        public bool ignoreBillLimit = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref disableAddedUI, "disableAddedUI", true);
            Scribe_Values.Look(ref collapseByLayer, "collapseByLayer", true);
            Scribe_Values.Look(ref syncFilter, "syncFilter", true);
            Scribe_Values.Look(ref filterLoadTiming, "filterLoadTiming", true);
            Scribe_Values.Look(ref apparelListMode, "apparelListMode", true);
            Scribe_Values.Look(ref moveToBottom, "moveToBottom", true);
            Scribe_Values.Look(ref showAddBillsButton, "showAddBillsButton", true);
            Scribe_Values.Look(ref ignoreBillLimit, "ignoreBillLimit", true);
            base.ExposeData();
        }
    }
}