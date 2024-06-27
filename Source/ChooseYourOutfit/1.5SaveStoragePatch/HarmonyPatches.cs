using System;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using HarmonyLib;
using SaveStorageSettings;
using RimWorld;
using UnityEngine;

namespace ChooseYourOutfit.SaveStorageSettingsPatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.chooseyouroutfit.savestoragesettingspatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Dialog_ManageApparelPoliciesEx), "DoWindowContents")]
    public static class Dialog_ManageApparelPoliciesEx_DoWindowContents_Patch
    {
        public static void Postfix(Dialog_ManageApparelPoliciesEx __instance, Rect inRect)
        {
            if (Widgets.ButtonText(new Rect(inRect.xMax - 300f, 15f, 140f, 35f), "SaveStorageSettings.LoadAsNew".Translate(), true, false, true, null))
            {
                ApparelPolicy apparelPolicy = Current.Game.outfitDatabase.MakeNewOutfit();
                SetApparelPolicy.Invoke(patch, BindingFlags.NonPublic | BindingFlags.Static, null, new object[] { __instance, apparelPolicy }, null);
                Find.WindowStack.Add((Window)LoadFilterDialog.Invoke(new object[] { "Apparel_Management", apparelPolicy.filter }));
            }
            ApparelPolicy selectedPolicy = (ApparelPolicy)GetSelectedPolicy.Invoke(patch, BindingFlags.NonPublic | BindingFlags.Static, null, new object[] { __instance }, null);
            if (selectedPolicy != null)
            {
                if (Widgets.ButtonText(new Rect(inRect.xMax - 155f, 15f, 75f, 35f), "SaveStorageSettings.LoadOutfit".Translate(), true, false, true, null))
                {
                    Find.WindowStack.Add((Window)LoadFilterDialog.Invoke(new object[] { "Apparel_Management", selectedPolicy.filter })); ;
                }
                if (Widgets.ButtonText(new Rect(inRect.xMax - 75f, 15f, 75f, 35f), "SaveStorageSettings.SaveOutfit".Translate(), true, false, true, null))
                {
                    Find.WindowStack.Add((Window)SaveFilterDialog.Invoke(new object[] { "Apparel_Management", selectedPolicy.filter }));
                }
            }
        }

        static Type patch = AccessTools.TypeByName("Patch_Dialog_ManageApparelPolicies_DoContentsRect");

        static MethodInfo SetApparelPolicy = AccessTools.Method(patch, "SetApparelPolicy");

        static MethodInfo GetSelectedPolicy = AccessTools.Method(patch, "GetSelectedPolicy");

        static ConstructorInfo LoadFilterDialog = AccessTools.TypeByName("LoadFilterDialog").Constructor(new Type[] { typeof(string), typeof(ThingFilter) });

        static ConstructorInfo SaveFilterDialog = AccessTools.TypeByName("SaveFilterDialog").Constructor(new Type[] { typeof(string), typeof(ThingFilter) });
    }
}
