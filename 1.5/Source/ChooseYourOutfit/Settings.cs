using Verse;
using UnityEngine;
using RimWorld;

namespace ChooseYourOutfit
{
    public class Settings : ModSettings
    {
        /// <summary>
        /// The three settings our mod has.
        /// </summary>
        public bool disableAddedUI = false;
        public bool collapseByLayer = true;
        public bool syncFilter = true;
        public bool selectedApparelListMode = false;

        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref disableAddedUI, "disableAddedUI", true);
            Scribe_Values.Look(ref collapseByLayer, "collapseByLayer", true);
            Scribe_Values.Look(ref syncFilter, "syncFilter", true);
            Scribe_Values.Look(ref selectedApparelListMode, "selectedApparelListMode", true);
            base.ExposeData();
        }
    }
}