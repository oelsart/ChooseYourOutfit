using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit;

public class Dialog_AddBillsToWorkTables : Window
{
    public Dialog_AddBillsToWorkTables(IEnumerable<ThingDef> apparels, Dictionary<ThingDef, ThingDef> stuff)
    {
        forcePause = true;
        doCloseX = true;
        doCloseButton = true;
        closeOnClickedOutside = true;

        Apparels = [.. apparels.OrderByDescending(a => a.label)];
        RecipeExist = [.. Apparels.Where(a => DefDatabase<RecipeDef>.AllDefs.Where(r => r.AvailableNow).Any(r => r.ProducedThingDef == a))];
        Stuff = stuff;
        tryAddResult = TryAddBillsToWorkTables();
    }

    private List<ThingDef> Apparels { get; }

    private Dictionary<ThingDef, ThingDef> Stuff { get; }

    private HashSet<ThingDef> RecipeExist { get; }

    private IEnumerable<Building_WorkTable> AllWorkTables => Find.CurrentMap.listerBuildings.AllColonistBuildingsOfType<Building_WorkTable>();

    public override void DoWindowContents(Rect inRect)
    {
        var outRect = inRect;
        outRect.yMax -= Margin + CloseButSize.y;
        var itemRect = new Rect(outRect.x, outRect.y, outRect.width, Text.LineHeight);
        var viewRect = new Rect(outRect.x, outRect.y, outRect.width, 0f)
        {
            height = tryAddResult.Select(a => 1 + a.Value.Count).Sum() * itemRect.height
        };

        Widgets.AdjustRectsForScrollView(inRect, ref outRect, ref viewRect);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        foreach (var result in tryAddResult)
        {
            Widgets.DrawTitleBG(itemRect);
            Widgets.Label(itemRect, ("CYO.AddBills." + result.Key).Translate());
            itemRect.y += itemRect.height;
            var color = result.Key == TryAddBillsResult.Success ? new Color(0f, 0.5f, 0f, 0.15f) : new Color(0.5f, 0f, 0f, 0.15f);
            foreach (var apparel in result.Value)
            {
                Widgets.DrawRectFast(itemRect, color);
                TaggedString text = apparel.apparel.label + (apparel.worktable != null ? " ---> " + apparel.worktable.Label : "");
                Widgets.Label(itemRect, text.Truncate(itemRect.width));
                TooltipHandler.TipRegion(itemRect, text);
                itemRect.y += itemRect.height;
            }
        }
        Widgets.EndScrollView();
    }

    private Dictionary<TryAddBillsResult, HashSet<(ThingDef, Building_WorkTable)>> TryAddBillsToWorkTables()
    {
        var result = new Dictionary<TryAddBillsResult, HashSet<(ThingDef, Building_WorkTable)>>();
        foreach (TryAddBillsResult r in Enum.GetValues(typeof(TryAddBillsResult))) result.Add(r, []);

        foreach (var apparel in Apparels)
        {
            if (!RecipeExist.Contains(apparel))
            {
                result[TryAddBillsResult.NoRecipes].Add((apparel, null));
                continue;
            }

            var success = false;
            var recipeFound = false;
            var registered = AllWorkTables.Any(w => w.BillStack.Bills.Any(b => b.recipe.ProducedThingDef == apparel));
            if (!registered || Dialog_AddBillsConfirm.forceRegister)
            {
                foreach (var worktable in AllWorkTables)
                {
                    var recipe = worktable.def.AllRecipes.FirstOrDefault(r => r.ProducedThingDef == apparel);
                    if (recipe != null)
                    {
                        recipeFound = true;
                        if (worktable.BillStack.Count < BillStack.MaxCount || ChooseYourOutfit.settings.ignoreBillLimit)
                        {
                            var bill = recipe.MakeNewBill();
                            if (Dialog_AddBillsConfirm.restrictToPreviewedStuffs)
                            {
                                foreach (var stuff in GenStuff.AllowedStuffsFor(apparel)) bill.ingredientFilter.SetAllow(stuff, false);
                                bill.ingredientFilter.SetAllow(Stuff[apparel], true);
                            }
                            worktable.BillStack.AddBill(bill);
                            result[TryAddBillsResult.Success].Add((apparel, worktable));
                            success = true;
                            break;
                        }
                    }
                }
            }

            if (!success)
            {
                if (registered && !Dialog_AddBillsConfirm.forceRegister)
                {
                    result[TryAddBillsResult.AlreadyRegistered].Add((apparel, null));
                }
                else if (recipeFound)
                {
                    result[TryAddBillsResult.TooManyBills].Add((apparel, null));
                }
                else
                {
                    result[TryAddBillsResult.NoWorkTables].Add((apparel, null));
                }
            }
        }

        result.RemoveAll(r => r.Value.Count == 0);
        return result;
    }

    private readonly Dictionary<TryAddBillsResult, HashSet<(ThingDef apparel, Building_WorkTable worktable)>> tryAddResult;

    private Vector2 scrollPosition;

    private enum TryAddBillsResult
    {
        NoRecipes,
        NoWorkTables,
        TooManyBills,
        AlreadyRegistered,
        Success
    }
}