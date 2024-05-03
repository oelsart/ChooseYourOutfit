using Verse;
using UnityEngine;

namespace ChooseYourOutfit
{
    public class ChooseYourOutfit : Mod
    {
        public static Settings settings;

        public static ModContentPack content;

        public ChooseYourOutfit(ModContentPack content) : base(content)
        {
            ChooseYourOutfit.settings = GetSettings<Settings>();
            ChooseYourOutfit.content = content;
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("CYO.Settings.DisableAddedUI".Translate(), ref settings.disableAddedUI);
            if (!settings.disableAddedUI)
            {
                listingStandard.CheckboxLabeled("CYO.Settings.CollapseByLayer".Translate(), ref settings.collapseByLayer);
                listingStandard.CheckboxLabeled("CYO.Settings.SyncFilter".Translate(), ref settings.syncFilter);
                listingStandard.CheckboxLabeled("CYO.Settings.apparelListMode".Translate(), ref settings.apparelListMode);
                listingStandard.CheckboxLabeled("CYO.Settings.SelectedApparelListMode".Translate(), ref settings.selectedApparelListMode);
            }
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "ChooseYourOutfit";
        }
    }
}