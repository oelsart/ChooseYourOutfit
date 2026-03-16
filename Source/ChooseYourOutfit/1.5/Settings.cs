using Verse;

namespace ChooseYourOutfit;

public class Settings : ModSettings
{
    public bool disableAddedUI;
    public bool collapseByLayer = true;
    public bool syncFilter = true;
    public bool apparelListMode;
    public bool moveToBottom;
    public bool hideUnregistrable;
    public bool showAddBillsButton = true;
    public bool ignoreBillLimit;
    public bool showTooltips = true;
    public bool showResearchedButton = true;
    public bool currentlyResearched;
    public bool showInStorageButton = true;
    public bool currentlyInStorage;
    public bool applyHitPoints;
    public bool applyQuality;
    public bool showApparelCount = true;
    public bool addFloatMenu = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref disableAddedUI, "disableAddedUI");
        Scribe_Values.Look(ref collapseByLayer, "collapseByLayer", true);
        Scribe_Values.Look(ref syncFilter, "syncFilter", true);
        Scribe_Values.Look(ref apparelListMode, "apparelListMode");
        Scribe_Values.Look(ref moveToBottom, "moveToBottom");
        Scribe_Values.Look(ref hideUnregistrable, "hideUnregistrable");
        Scribe_Values.Look(ref showAddBillsButton, "showAddBillsButton", true);
        Scribe_Values.Look(ref ignoreBillLimit, "ignoreBillLimit");
        Scribe_Values.Look(ref showTooltips, "showTooltips", true);
        Scribe_Values.Look(ref currentlyResearched, "currentlyResearched");
        Scribe_Values.Look(ref showResearchedButton, "showResearchedButton", true);
        Scribe_Values.Look(ref currentlyInStorage, "currentlyInStorage");
        Scribe_Values.Look(ref applyHitPoints, "applyHitPoints");
        Scribe_Values.Look(ref applyQuality, "applyQuality");
        Scribe_Values.Look(ref showInStorageButton, "showInStorageButton", true);
        Scribe_Values.Look(ref showApparelCount, "showApparelCount", true);
        Scribe_Values.Look(ref addFloatMenu, "addFloatMenu", true);
        base.ExposeData();
    }
}