using UnityEngine;
using Verse;

namespace ChooseYourOutfit;

public class ChooseYourOutfit : Mod
{
	public static Settings Settings { get; private set; }
	
	public static Mod Mod { get; private set; }

	public ChooseYourOutfit(ModContentPack content) : base(content)
	{
		Settings = GetSettings<Settings>();
		Mod = this;
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		Listing_Standard listingStandard = new();
		listingStandard.Begin(inRect);
		listingStandard.CheckboxLabeled("CYO.Settings.DisableAddedUI".Translate(), ref Settings.disableAddedUI);
		if (!Settings.disableAddedUI)
		{
			listingStandard.CheckboxLabeled("CYO.Settings.CollapseByLayer".Translate(), ref Settings.collapseByLayer);
			listingStandard.CheckboxLabeled("CYO.Settings.SyncFilter".Translate(), ref Settings.syncFilter);
			listingStandard.CheckboxLabeled("CYO.Settings.ApparelListMode".Translate(), ref Settings.apparelListMode);
			if (!Settings.apparelListMode)
			{
				listingStandard.Indent(15f);
				listingStandard.ColumnWidth = inRect.width - 15f;
				if (listingStandard.RadioButton("CYO.Settings.MoveToBottom".Translate(), Settings.moveToBottom))
					Settings.moveToBottom = true;
				if (listingStandard.RadioButton("CYO.Settings.DontMoveIt".Translate(), !Settings.moveToBottom))
					Settings.moveToBottom = false;
				listingStandard.Outdent(15f);
				listingStandard.ColumnWidth = inRect.width;
			}

			listingStandard.CheckboxLabeled("CYO.Settings.HideUnregistrable".Translate(), ref Settings.hideUnregistrable);
			listingStandard.CheckboxLabeled("CYO.Settings.ShowResearchedButton".Translate(),
				ref Settings.showResearchedButton);
			listingStandard.CheckboxLabeled("CYO.Settings.ShowInStorageButton".Translate(), ref Settings.showInStorageButton);
			listingStandard.CheckboxLabeled("CYO.Settings.ShowApparelCount".Translate(), ref Settings.showApparelCount);
			listingStandard.CheckboxLabeled("CYO.Settings.ShowAddBillsButton".Translate(), ref Settings.showAddBillsButton);
			if (Settings.showAddBillsButton)
				listingStandard.CheckboxLabeled("CYO.Settings.IgnoreBillLimit".Translate(), ref Settings.ignoreBillLimit);
			listingStandard.CheckboxLabeled("CYO.Settings.ShowTooltips".Translate(), ref Settings.showTooltips);
			listingStandard.CheckboxLabeled("CYO.Settings.AddFroatMenu".Translate(), ref Settings.addFloatMenu);
		}

		listingStandard.End();
		base.DoSettingsWindowContents(inRect);
	}

	public override string SettingsCategory()
	{
		return "Choose Your Outfit";
	}
}