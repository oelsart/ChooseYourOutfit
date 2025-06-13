using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit
{
    public static class ModCompat
    {
        public static bool AnimalControls = ModsConfig.IsActive("avilmask.AnimalControls");

        public static bool ABHATweaker = ModsConfig.IsActive("AB.HATweaker");

        public static class ProstheticNoMissingBodyParts
        {
            public static bool Active = ModsConfig.IsActive("Mlie.ProstheticNoMissingBodyParts");

            private static Mod Mod;

            private static ModSettings Settings;

            private static AccessTools.FieldRef<ModSettings, List<string>> ArmsWhitelist;

            private static AccessTools.FieldRef<ModSettings, List<string>> FeetWhitelist;

            private static AccessTools.FieldRef<ModSettings, List<string>> HandsWhitelist;

            private static AccessTools.FieldRef<ModSettings, List<string>> LegsWhitelist;

            static ProstheticNoMissingBodyParts()
            {
                if (Active)
                {
                    Mod = (Mod)AccessTools.Field("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsMod:_mod")?.GetValue(null);
                    if (Mod == null)
                    {
                        Log.Warning("[ChooseYourOutfit] Not found: ProstheticNoMissingBodyPartsMod");
                        return;
                    }
                    Settings = (ModSettings)AccessTools.Field("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsMod:_settings")?.GetValue(Mod);
                    if (Settings == null)
                    {
                        Log.Warning("[ChooseYourOutfit] Not found: ProstheticNoMissingBodyPartsSettings");
                        return;
                    }
                    var t_ProstheticNoMissingBodyPartsSettings = AccessTools.TypeByName("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsSettings");
                    ArmsWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "ArmsWhitelist");
                    FeetWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "FeetWhitelist");
                    HandsWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "HandsWhitelist");
                    LegsWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "LegsWhitelist");
                }
            }

            public static IEnumerable<string> GetWhitelist
            {
                get
                {
                    return ArmsWhitelist(Settings)
                        .Concat(FeetWhitelist(Settings))
                        .Concat(HandsWhitelist(Settings))
                        .Concat(LegsWhitelist(Settings))
                        .Where(s => s != null);
                }
            }
        }

        public static class SaveStorageSettings
        {
            public static bool Active = ModsConfig.IsActive("savestoragesettings.kv.rw.fishtmp");

            public static MethodInfo Original;

            public static MethodInfo Postfix;

            public static Func<Dialog_ManageApparelPolicies, ApparelPolicy> GetSelectedPolicy;

            public static Action<Dialog_ManageApparelPolicies, ApparelPolicy> SetApparelPolicy;

            public static Type LoadFilterDialog;

            public static Type SaveFilterDialog;

            static SaveStorageSettings()
            {
                if (Active)
                {
                    Original = AccessTools.Method(typeof(Dialog_ManageApparelPolicies), "DoContentsRect");
                    var t_Patch = AccessTools.TypeByName("SaveStorageSettings.Patch_Dialog_ManageApparelPolicies_DoContentsRect");
                    Postfix = AccessTools.Method(t_Patch, "Postfix");
                    GetSelectedPolicy = AccessTools.MethodDelegate<Func<Dialog_ManageApparelPolicies, ApparelPolicy>>(
                        AccessTools.Method(t_Patch, "GetSelectedPolicy"));
                    SetApparelPolicy = AccessTools.MethodDelegate<Action<Dialog_ManageApparelPolicies, ApparelPolicy>>(
                        AccessTools.Method(t_Patch, "SetApparelPolicy"));
                    LoadFilterDialog = AccessTools.TypeByName("SaveStorageSettings.Dialog.LoadFilterDialog");
                    SaveFilterDialog = AccessTools.TypeByName("SaveStorageSettings.Dialog.SaveFilterDialog");
                }
            }

            public static void SaveStorageButtons(Dialog_ManageApparelPolicies dialog, Rect inRect)
            {
                Window GetDialog(Type type, string str, ThingFilter filter)
                {
                    return (Window)Activator.CreateInstance(type, AccessTools.all, null, new object[] { str, filter }, null);
                }

                if (!Active) return;

                if (Widgets.ButtonText(new Rect(inRect.xMax - 300f, 15f, 140f, 35f), "SaveStorageSettings.LoadAsNew".Translate(), true, false, true, null))
                {
                    ApparelPolicy apparelPolicy = Current.Game.outfitDatabase.MakeNewOutfit();
                    SetApparelPolicy(dialog, apparelPolicy);
                    Find.WindowStack.Add(GetDialog(LoadFilterDialog, "Apparel_Management", apparelPolicy.filter));
                }
                ApparelPolicy selectedPolicy = GetSelectedPolicy(dialog);
                if (selectedPolicy != null)
                {
                    if (Widgets.ButtonText(new Rect(inRect.xMax - 155f, 15f, 75f, 35f), "SaveStorageSettings.LoadOutfit".Translate(), true, false, true, null))
                    {
                        Find.WindowStack.Add(GetDialog(LoadFilterDialog, "Apparel_Management", selectedPolicy.filter));
                    }
                    if (Widgets.ButtonText(new Rect(inRect.xMax - 75f, 15f, 75f, 35f), "SaveStorageSettings.SaveOutfit".Translate(), true, false, true, null))
                    {
                        Find.WindowStack.Add(GetDialog(SaveFilterDialog, "Apparel_Management", selectedPolicy.filter));
                    }
                }
            }
        }
    }
}
