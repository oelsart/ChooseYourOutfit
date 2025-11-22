using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit;

[StaticConstructorOnStartup]
public static class ModCompat
{
    static ModCompat()
    {
        foreach (var inner in AccessTools.InnerTypes(typeof(ModCompat)))
        {
            RuntimeHelpers.RunClassConstructor(inner.TypeHandle);
        }
    }

    public static bool AnyNull(params object[] args)
    {
        return args.Any(arg => arg == null);
    }

    public static readonly bool AnimalControls = ModsConfig.IsActive("avilmask.AnimalControls");

    public static readonly bool ABHATweaker = ModsConfig.IsActive("AB.HATweaker");

    public static class ProstheticNoMissingBodyParts
    {
        public static readonly bool Active = ModsConfig.IsActive("Mlie.ProstheticNoMissingBodyParts");

        private static readonly ModSettings Settings;

        private static readonly AccessTools.FieldRef<ModSettings, List<string>> ArmsWhitelist;

        private static readonly AccessTools.FieldRef<ModSettings, List<string>> FeetWhitelist;

        private static readonly AccessTools.FieldRef<ModSettings, List<string>> HandsWhitelist;

        private static readonly AccessTools.FieldRef<ModSettings, List<string>> LegsWhitelist;

        static ProstheticNoMissingBodyParts()
        {
            if (Active)
            {
                try
                {
                    var mod = (Mod)AccessTools.Field("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsMod:mod")?.GetValue(null);
                    Settings = (ModSettings)AccessTools.Field("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsMod:settings")?.GetValue(mod);
                    var t_ProstheticNoMissingBodyPartsSettings = AccessTools.TypeByName("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsSettings");
                    ArmsWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "ArmsWhitelist");
                    FeetWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "FeetWhitelist");
                    HandsWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "HandsWhitelist");
                    LegsWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "LegsWhitelist");
                }
                catch (Exception e)
                {
                    Active = false;
                    Log.Error($"[ChooseYourOutfit] ProstheticNoMissingBodyParts compatibility is broken: {e}");
                    Active = false;
                }
                finally
                {
                    if (AnyNull(Settings, ArmsWhitelist, FeetWhitelist, HandsWhitelist, LegsWhitelist))
                    {
                        Log.Error("[ChooseYourOutfit] ProstheticNoMissingBodyParts compatibility is broken.");
                        Active = false;
                    }
                }
            }
        }

        public static IEnumerable<string> GetWhitelist
        {
            get
            {
                if (!Active) return [];
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
        public static readonly bool Active = ModsConfig.IsActive("savestoragesettings.kv.rw.fishtmp");

        public static readonly MethodInfo Original;

        public static readonly MethodInfo Postfix;

        public static readonly Func<Dialog_ManageApparelPolicies, ApparelPolicy> GetSelectedPolicy;

        public static readonly Action<Dialog_ManageApparelPolicies, ApparelPolicy> SetApparelPolicy;

        public static readonly Type LoadFilterDialog;

        public static readonly Type SaveFilterDialog;

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
                    Active = false;
                    Log.Error($"[ChooseYourOutfit] SaveStorageSettings compatibility is broken: {e}");
                    Active = false;
                }
                finally
                {
                    if (AnyNull(Original, Postfix, GetSelectedPolicy, SetApparelPolicy, LoadFilterDialog, SaveFilterDialog))
                    {
                        Log.Error("[ChooseYourOutfit] SaveStorageSettings compatibility is broken.");
                        Active = false;
                    }
                }
            }
        }

        public static void SaveStorageButtons(Dialog_ManageApparelPolicies dialog, Rect inRect)
        {
            if (!Active) return;

            if (Widgets.ButtonText(new Rect(inRect.xMax - 300f, 15f, 140f, 35f), "SaveStorageSettings.LoadAsNew".Translate(), true, false))
            {
                var apparelPolicy = Current.Game.outfitDatabase.MakeNewOutfit();
                SetApparelPolicy(dialog, apparelPolicy);
                Find.WindowStack.Add(GetDialog(LoadFilterDialog, "Apparel_Management", apparelPolicy.filter));
            }
            var selectedPolicy = GetSelectedPolicy(dialog);
            if (selectedPolicy != null)
            {
                if (Widgets.ButtonText(new Rect(inRect.xMax - 155f, 15f, 75f, 35f), "SaveStorageSettings.LoadOutfit".Translate(), true, false))
                {
                    Find.WindowStack.Add(GetDialog(LoadFilterDialog, "Apparel_Management", selectedPolicy.filter));
                }
                if (Widgets.ButtonText(new Rect(inRect.xMax - 75f, 15f, 75f, 35f), "SaveStorageSettings.SaveOutfit".Translate(), true, false))
                {
                    Find.WindowStack.Add(GetDialog(SaveFilterDialog, "Apparel_Management", selectedPolicy.filter));
                }
            }

            return;

            Window GetDialog(Type type, string str, ThingFilter filter)
            {
                return (Window)Activator.CreateInstance(type, AccessTools.all, null, [str, filter], null);
            }
        }
    }

    public static class Outfitted
    {
        public static readonly bool Active = ModsConfig.IsActive("mitasamodel.Outfitted");

        public static readonly MethodInfo Patch1Original;

        public static readonly MethodInfo Patch1;

        public static readonly MethodInfo Patch2Original;

        public static readonly MethodInfo Patch2;

        public static readonly Action<Dialog_ManagePolicies<ApparelPolicy>, Rect> PostfixDelegate;

        public static readonly FastInvokeHandler DrawOutfittedButtons;

        static Outfitted()
        {
            if (Active)
            {
                try
                {
                    Patch1Original = AccessTools.Method(typeof(Dialog_ManageApparelPolicies), "DoContentsRect");
                    Patch1 = AccessTools.Method("Outfitted.Dialog_ManageApparelPolicies_DoContentsRect_Patch:Prefix");
                    Patch2Original = AccessTools.Method(typeof(Dialog_ManagePolicies<ApparelPolicy>), "DoWindowContents");
                    Patch2 = AccessTools.Method("Outfitted.Dialog_ManagePolicies_DoWindowContents_Patch:Postfix");
                    PostfixDelegate = AccessTools.MethodDelegate<Action<Dialog_ManagePolicies<ApparelPolicy>, Rect>>(Patch2);
                    DrawOutfittedButtons = MethodInvoker.GetHandler(AccessTools.Method("Outfitted.Dialog_Policies:DrawOutfittedButtons"));
                }
                catch (Exception e)
                {
                    Log.Error($"[ChooseYourOutfit] Outfitted compatibility is broken: {e}");
                    Active = false;
                }
                finally
                {
                    if (AnyNull(Patch1Original, Patch1, Patch2Original, Patch2, PostfixDelegate, DrawOutfittedButtons))
                    {
                        Log.Error("[ChooseYourOutfit] Outfitted compatibility is broken.");
                        Active = false;
                    }
                }
            }
        }

        public static void OutfittedButton(Dialog_ManageApparelPoliciesEx dialog, Rect inRect)
        {
            if (!Active) return;

            var dx = SaveStorageSettings.Active ? 445f : 140f;
            var buttonRect = new Rect(inRect.xMax - dx, 15f, 140f, 35f);
            if (Widgets.ButtonText(buttonRect, "Outfitted", true, false))
            {
                var rect = new Rect(UI.MousePositionOnUIInverted, new Vector2(342f, 600f));
                if (rect.xMax > Screen.width)
                {
                    rect.x -= rect.width;
                }
                var dialog2 = new Dialog
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
                    DrawOutfittedButtons(null, rect2.LeftPartPixels(321f).BottomPartPixels(590f), dialog.SelectedPolicy);
                    rect2.y -= 35f;
                    rect2.height += 45f;
                    Widgets.BeginGroup(rect2);
                    var rect3 = rect.LeftPartPixels(332f).AtZero();
                    rect3.height += 75f;
                    PostfixDelegate(dialog, rect3);
                    Widgets.EndGroup();
                };

                Find.WindowStack.Add(dialog2);
            }
        }

        public class Dialog : ImmediateWindow
        {
            protected override void SetInitialSizeAndPosition()
            {
                windowRect = windowRect.Rounded();
            }
        }
    }
}
