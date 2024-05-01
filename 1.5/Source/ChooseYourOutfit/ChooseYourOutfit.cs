using Verse;
using UnityEngine;

namespace ChooseYourOutfit
{
    public class ChooseYourOutfit : Mod
    {
        /// <summary>
        /// A reference to our settings.
        /// </summary>
        public static Settings settings;

        public static ModContentPack content;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public ChooseYourOutfit(ModContentPack content) : base(content)
        {
            ChooseYourOutfit.settings = GetSettings<Settings>();
            ChooseYourOutfit.content = content;
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("CYO.Settings.DisableAddedUI".Translate(), ref settings.disableAddedUI);
            if (!settings.disableAddedUI)
            {
                listingStandard.CheckboxLabeled("CYO.Settings.CollapseByLayer".Translate(), ref settings.collapseByLayer);
                listingStandard.CheckboxLabeled("CYO.Settings.SyncFilter".Translate(), ref settings.syncFilter);
                listingStandard.CheckboxLabeled("CYO.Settings.DrawSelectedApparelList".Translate(), ref settings.drawSelectedApparelList);
            }
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            return "ChooseYourOutfit";
        }
    }
}