using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit
{
    public class Dialog_AddBillsToWorkTables : Window
    {
        public Dialog_AddBillsToWorkTables(HashSet<ThingDef> apparels, bool forceRegister)
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.closeOnClickedOutside = true;

            this.Apparels = apparels.OrderByDescending(a => a.label).ToHashSet();
            this.forceRegister = forceRegister;
            this.tryAddResult = TryAddBillsToWorkTables();
        }

        private HashSet<ThingDef> Apparels
        {
            get
            {
                return apparelsInt;
            }
            set
            {
                apparelsInt = value;
            }
        }

        private HashSet<ThingDef> RecipeExist
        {
            get
            {
                return this.Apparels.Where(a => DefDatabase<RecipeDef>.AllDefs.Where(r => r.AvailableNow).Any(r => r.ProducedThingDef == a)).ToHashSet();
            }
        }

        private IEnumerable<Building_WorkTable> AllWorkTables
        {
            get
            {
                return Find.CurrentMap.listerBuildings.AllColonistBuildingsOfType<Building_WorkTable>();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var outRect = inRect;
            outRect.yMax -= Margin + CloseButSize.y;
            var itemRect = new Rect(outRect.x, outRect.y, outRect.width, Text.LineHeight);
            var viewRect = new Rect(outRect.x, outRect.y, outRect.width, 0f);
            viewRect.height = this.tryAddResult.Select(a => 1 + a.Value.Count).Sum() * itemRect.height;

            Widgets.AdjustRectsForScrollView(inRect, ref outRect, ref viewRect);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            foreach (var result in tryAddResult)
            {
                Widgets.DrawTitleBG(itemRect);
                Widgets.Label(itemRect, ("CYO.AddBills." + result.Key.ToString()).Translate());
                itemRect.y += itemRect.height;
                var color = result.Key == TryAddBillsResult.Success ? new Color(0f, 0.5f, 0f, 0.15f) : new Color(0.5f, 0f, 0f, 0.15f);
                foreach (var apparel in result.Value)
                {
                    Widgets.DrawRectFast(itemRect, color);
                    TaggedString text = apparel.apparel.label + (apparel.worktable != null ? " to " + apparel.worktable.Label : "");
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
            foreach (TryAddBillsResult r in Enum.GetValues(typeof(TryAddBillsResult))) result.Add(r, new HashSet<(ThingDef, Building_WorkTable)>());

            foreach (var apparel in this.Apparels)
            {
                if (!RecipeExist.Contains(apparel))
                {
                    result[TryAddBillsResult.NoRecipes].Add((apparel, null));
                    continue;
                }

                var success = false;
                var recipeFound = false;
                var registered = false;
                registered = AllWorkTables.Any(w => w.BillStack.Bills.Any(b => b.recipe.ProducedThingDef == apparel));
                if (!registered || this.forceRegister)
                {
                    foreach (var worktable in AllWorkTables)
                    {
                        var recipe = worktable.def.AllRecipes.FirstOrDefault(r => r.ProducedThingDef == apparel);
                        if (recipe != null)
                        {
                            recipeFound = true;
                            if (worktable.BillStack.Count < BillStack.MaxCount || ChooseYourOutfit.settings.ignoreBillLimit)
                            {
                                worktable.BillStack.AddBill(BillUtility.MakeNewBill(recipe));
                                result[TryAddBillsResult.Success].Add((apparel, worktable));
                                success = true;
                                break;
                            }
                        }
                    }
                }

                if (success is false)
                {
                    if (registered is true && this.forceRegister is false)
                    {
                        result[TryAddBillsResult.AlreadyRegistered].Add((apparel, null));
                    }
                    else if (recipeFound is true)
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

        private HashSet<ThingDef> apparelsInt;

        private Dictionary<TryAddBillsResult, HashSet<(ThingDef apparel, Building_WorkTable worktable)>> tryAddResult;

        private Vector2 scrollPosition;

        private bool forceRegister;

        private enum TryAddBillsResult
        {
            NoRecipes,
            NoWorkTables,
            TooManyBills,
            AlreadyRegistered,
            Success
        }
    }
}
