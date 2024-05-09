﻿using Verse;
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
            Listing_Tree listingTree = new Listing_Tree();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("CYO.Settings.DisableAddedUI".Translate(), ref settings.disableAddedUI);
            if (!settings.disableAddedUI)
            {
                listingStandard.CheckboxLabeled("CYO.Settings.CollapseByLayer".Translate(), ref settings.collapseByLayer);
                listingStandard.CheckboxLabeled("CYO.Settings.SyncFilter".Translate(), ref settings.syncFilter);
                if (settings.syncFilter) listingStandard.CheckboxLabeled("CYO.Settings.FilterLoadTiming".Translate(), ref settings.filterLoadTiming);
                listingStandard.CheckboxLabeled("CYO.Settings.ApparelListMode".Translate(), ref settings.apparelListMode);
                if (!settings.apparelListMode)
                {
                    listingStandard.Indent(15f);
                    listingStandard.ColumnWidth = inRect.width - 15f;
                    if(listingStandard.RadioButton("CYO.Settings.MoveToBottom".Translate(), settings.moveToBottom)) settings.moveToBottom = true;
                    if(listingStandard.RadioButton("CYO.Settings.DontMoveIt".Translate(), !settings.moveToBottom)) settings.moveToBottom = false;
                    listingStandard.Outdent(15f);
                    listingStandard.ColumnWidth = inRect.width;
                }
                listingStandard.CheckboxLabeled("CYO.Settings.ShowAddBillsButton".Translate(), ref settings.showAddBillsButton);
                if (settings.showAddBillsButton) listingStandard.CheckboxLabeled("CYO.Settings.IgnoreBillLimit".Translate(), ref settings.ignoreBillLimit);
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