﻿using Verse;

namespace ChooseYourOutfit
{
    public class Settings : ModSettings
    {
        public bool disableAddedUI = false;
        public bool collapseByLayer = true;
        public bool syncFilter = true;
        public bool apparelListMode = false;
        public bool moveToBottom = false;
        public bool showAddBillsButton = true;
        public bool ignoreBillLimit = false;
        public bool showTooltips = true;
        public bool currentlyResearched = false;
        public bool addFroatMenu = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref disableAddedUI, "disableAddedUI", false);
            Scribe_Values.Look(ref collapseByLayer, "collapseByLayer", true);
            Scribe_Values.Look(ref syncFilter, "syncFilter", true);
            Scribe_Values.Look(ref apparelListMode, "apparelListMode", false);
            Scribe_Values.Look(ref moveToBottom, "moveToBottom", false);
            Scribe_Values.Look(ref showAddBillsButton, "showAddBillsButton", true);
            Scribe_Values.Look(ref ignoreBillLimit, "ignoreBillLimit", false);
            Scribe_Values.Look(ref showTooltips, "showTooltips", true);
            Scribe_Values.Look(ref currentlyResearched, "currentlyResearched", false);
            Scribe_Values.Look(ref addFroatMenu, "addFroatMenu", true);
            base.ExposeData();
        }
    }
}