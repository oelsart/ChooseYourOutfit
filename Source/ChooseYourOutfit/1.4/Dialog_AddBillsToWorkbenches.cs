using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit
{
    public class Dialog_AddBillsToWorkbenches : Window
    {
        public Dialog_AddBillsToWorkbenches(HashSet<ThingDef> selectedApparels)
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.closeOnClickedOutside = true;

            this.SelectedApparels = selectedApparels.OrderByDescending(a => a.label).ToHashSet();
            this.tryAddResult = TryAddBillsToWorkBenches();
        }

        private HashSet<ThingDef> SelectedApparels
        {
            get
            {
                return selApparelsInt;
            }
            set
            {
                selApparelsInt = value;
            }
        }

        private HashSet<ThingDef> RecipeExist
        {
            get
            {
                return this.SelectedApparels.Where(a => DefDatabase<RecipeDef>.AllDefs.Where(r => r.AvailableNow).Any(r => r.ProducedThingDef == a)).ToHashSet();
            }
        }

        private HashSet<ThingDef> NoRecipesAvailableNow
        {
            get
            {
                return this.SelectedApparels.Where(a => DefDatabase<RecipeDef>.AllDefs.Where(r => r.AvailableNow).All(r => r.ProducedThingDef != a)).ToHashSet();
            }
        }

        private IEnumerable<Building_WorkTable> AllWorkBenches
        {
            get
            {
                return Find.CurrentMap.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var outRect = inRect;
            var itemRect = new Rect(outRect.x, outRect.y, outRect.width, Text.LineHeight);
            var viewRect = new Rect(outRect.x, outRect.y, outRect.width, 0f);
            viewRect.height = this.tryAddResult.Select(a => 1 + a.Value.Count).Sum() * itemRect.height;
            viewRect.width -= GenUI.ScrollBarWidth + 1f;

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
                    Widgets.Label(itemRect, apparel.label);
                    itemRect.y += itemRect.height;
                }
            }
            Widgets.EndScrollView();
        }

        private Dictionary<TryAddBillsResult, HashSet<ThingDef>> TryAddBillsToWorkBenches()
        {
            var result = new Dictionary<TryAddBillsResult, HashSet<ThingDef>>();
            foreach (TryAddBillsResult r in Enum.GetValues(typeof(TryAddBillsResult))) result.Add(r, new HashSet<ThingDef>());

            foreach (var apparel in this.SelectedApparels)
            {
                if (!RecipeExist.Contains(apparel))
                {
                    result[TryAddBillsResult.NoRecipes].Add(apparel);
                    continue;
                }

                var success = false;
                var recipeFound = false;
                foreach (var workbench in AllWorkBenches)
                {
                    var recipe = workbench.def.AllRecipes.FirstOrDefault(r => r.ProducedThingDef == apparel);
                    if (recipe != null)
                    {
                        recipeFound = true;
                        if (workbench.BillStack.Count < BillStack.MaxCount)
                        {
                            workbench.BillStack.AddBill(BillUtility.MakeNewBill(recipe));
                            result[TryAddBillsResult.Success].Add(apparel);
                            success = true;
                            break;
                        }
                    }
                }

                if (success is false)
                {
                    if (recipeFound is true)
                    {
                        result[TryAddBillsResult.TooManyBills].Add(apparel);
                    }
                    else
                    {
                        result[TryAddBillsResult.NoWorkbenches].Add(apparel);
                    }
                }
            }

            result.RemoveAll(r => r.Value.Count == 0);
            return result;
        }

        private HashSet<ThingDef> selApparelsInt;

        private Dictionary<TryAddBillsResult, HashSet<ThingDef>> tryAddResult;

        private Vector2 scrollPosition;
        private enum TryAddBillsResult
        {
            NoRecipes,
            NoWorkbenches,
            TooManyBills,
            Success
        }
    }
}
