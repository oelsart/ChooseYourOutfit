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
                    try
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
                    catch (Exception e)
                    {
                        Log.Error($"[ChooseYourOutfit] ProstheticNoMissingBodyParts compatibility is broken: {e}");
                    }
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
                    try
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
                    catch (Exception e)
                    {
                        Log.Error($"[ChooseYourOutfit] SaveStorageSettings compatibility is broken: {e}");
                    }
                }
            }

            public static void SaveStorageButtons(Dialog_ManageApparelPolicies dialog, Rect inRect)
            {
                if (!Active) return;

                Window GetDialog(Type type, string str, ThingFilter filter)
                {
                    return (Window)Activator.CreateInstance(type, AccessTools.all, null, new object[] { str, filter }, null);
                }

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

        public static class Outfitted
        {
            public static bool Active = ModsConfig.IsActive("homiru.Outfitted");

            public static Type Dialog_ManageApparelPolicies_DoContentsRect_Patch;

            public static MethodInfo Original;

            public static MethodInfo Postfix;

            static Outfitted()
            {
                if (Active)
                {
                    try
                    {
                        Dialog_ManageApparelPolicies_DoContentsRect_Patch = GenTypes.GetTypeInAnyAssembly("Outfitted.Dialog_ManageApparelPolicies_DoContentsRect_Patch", "Outfitted");
                        Original = AccessTools.Method(typeof(Dialog_ManageApparelPolicies), "DoContentsRect");
                        Postfix = AccessTools.Method(Dialog_ManageApparelPolicies_DoContentsRect_Patch, "Postfix");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[ChooseYourOutfit] Outfitted compatibility is broken: {e}");
                    }
                }
            }

            public static void OutfittedButton(Dialog_ManageApparelPolicies dialog, Rect inRect)
            {
                if (!Active) return;

                var dx = SaveStorageSettings.Active ? 445f : 140f;
                var buttonRect = new Rect(inRect.xMax - dx, 15f, 140f, 35f);
                if (Widgets.ButtonText(buttonRect, "Outfitted", true, false, true, null))
                {
                    var rect = new Rect(UI.MousePositionOnUIInverted, new Vector2(320f, 450f));
                    if (rect.xMax > Screen.width)
                    {
                        rect.x -= rect.width;
                    }
                    var dialog2 = new Dialog()
                    {
                        windowRect = rect,
                        draggable = true,
                        closeOnCancel = true,
                        closeOnClickedOutside = true,
                        forceCatchAcceptAndCancelEventEvenIfUnfocused = true
                    };
                    dialog2.doWindowFunc = () =>
                    {
                        var rect2 = dialog2.windowRect.AtZero();
                        rect2.y -= 70f;
                        rect2.height += 80f;
                        Widgets.BeginGroup(rect2);
                        ReversePatch_Dialog_ManageApparelPolicies_DoContentsRect_Patch_Postfix.DoContent(rect.LeftPart(0f).AtZero(), dialog);
                        Widgets.EndGroup();
                    };

                    Find.WindowStack.Add(dialog2);
                }
            }

            public class Dialog : ImmediateWindow
            {
                protected override void SetInitialSizeAndPosition()
                {
                    this.windowRect = this.windowRect.Rounded();
                }
            }
        }
    }
}
