using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace ChooseYourOutfit
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.chooseyouroutfit");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(PawnColumnWorker_Outfit), nameof(PawnColumnWorker_Outfit.DoHeader))]
    //new Dialog_ManageApparelPoliciesをnew Dialog_ManageApparelPoliciesExに置き換える
    static class Patch_PawnColumnWorker_Outfit_DoHeader
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            //置き換え後のoperandとしてDialog_ManageApparelPoliciesExのコンストラクタを取得
            var operand = AccessTools.Constructor(typeof(Dialog_ManageApparelPoliciesEx), new Type[] { typeof(Pawn) });
            //Dialog_ManageApparelPoliciesのコンストラクタをoperandに持つNewobjの場所を検索
            int pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Newobj) && (c.operand as ConstructorInfo).DeclaringType.Equals(typeof(Dialog_ManageApparelPolicies)));
            //新しいoperandに置き換え
            codes[pos].operand = operand;
            return codes;
        }
    }

    //new Dialog_ManageApparelPoliciesをnew Dialog_ManageApparelPoliciesExに置き換え、渡すのをpawnにする
    [HarmonyPatch()]
    static class Patch_PawnColumnWorker_Outfit_Button_GenerateMenu
    {
        static MethodInfo TargetMethod()
        {
            return typeof(PawnColumnWorker_Outfit)
                .GetNestedType("<>c__DisplayClass4_0", BindingFlags.NonPublic)
                .GetMethod("<Button_GenerateMenu>b__1", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            //置き換え後のoperandとしてDialog_ManageApparelPoliciesExのコンストラクタを取得
            var operand = AccessTools.Constructor(typeof(Dialog_ManageApparelPoliciesEx), new Type[] { typeof(Pawn) });
            //Dialog_ManageApparelPoliciesのコンストラクタをoperandに持つNewobjの場所を検索
            var pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Newobj) && (c.operand as ConstructorInfo).DeclaringType.Equals(typeof(Dialog_ManageApparelPolicies)));
            //新しいoperandに置き換え
            codes[pos].operand = operand;
            //(pawn以下の).outfits.CurrentApparelPolicyを削除
            codes.RemoveAt(pos - 1);
            codes.RemoveAt(pos - 2);
            return codes;
        }
    }

    //Policy編集ボタン3つを右端からポリシー名の横に変える
    [HarmonyPatch()]
    static class Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents
    {
        static MethodBase TargetMethod()
        {
            Type generic = ModsConfig.IsActive("avilmask.AnimalControls") ? typeof(Dialog_ManagePolicies<FoodPolicy>) : typeof(Dialog_ManagePolicies<ApparelPolicy>);
            return AccessTools.Method(generic, "DoWindowContents");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Call && c.operand.Equals(AccessTools.Method(typeof(Widgets), "LabelEllipses"))) - 2;
            var window = AccessTools.CreateInstance<Dialog_ManageApparelPolicies>();
            var margin = (float)AccessTools.Property(window.GetType(), "Margin").GetValue(window);

            List<CodeInstruction> addCodes = new List<CodeInstruction>
            {
                CodeInstruction.LoadLocal(12),
                CodeInstruction.Call(typeof(Text), "CalcSize"),
                CodeInstruction.LoadField(typeof(Vector2), "x"),
                new CodeInstruction(OpCodes.Ldc_R4, margin),
                new CodeInstruction(OpCodes.Ldc_R4, 194f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Ldc_R4, margin + 488f),
                CodeInstruction.Call(typeof(Math), "Min", new Type[] { typeof(float), typeof(float) }),
                CodeInstruction.StoreField(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), "tmpLocal"),
                CodeInstruction.LoadLocal(4, true),
                new CodeInstruction(OpCodes.Ldc_R4, margin + 488f),
                CodeInstruction.Call(typeof(Rect), "set_xMax")
            };

            List<CodeInstruction> addCodes2 = new List<CodeInstruction>
            {
                CodeInstruction.LoadLocal(7, true),
                CodeInstruction.LoadField(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), "tmpLocal"),
                CodeInstruction.Call(typeof(Rect), "set_x"),
                CodeInstruction.LoadLocal(6, true),
                CodeInstruction.LoadField(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), "tmpLocal"),
                new CodeInstruction(OpCodes.Ldc_R4, 42f),
                new CodeInstruction(OpCodes.Add),
                CodeInstruction.Call(typeof(Rect), "set_x"),
                CodeInstruction.LoadLocal(5, true),
                CodeInstruction.LoadField(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), "tmpLocal"),
                new CodeInstruction(OpCodes.Ldc_R4, 84f),
                new CodeInstruction(OpCodes.Add),
                CodeInstruction.Call(typeof(Rect), "set_x")
            };

            var pos2 = codes.FindIndex(c => c.opcode == OpCodes.Ldstr && c.operand.Equals("DeletePolicyTip"));

            codes.InsertRange(pos2, addCodes2);
            codes.InsertRange(pos, addCodes);
            return codes;
        }

        public static float tmpLocal = 0f;
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    static class Patch_FloatMenuMakerMap_AddHumanlikeOrders
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Ldloc_S) && (c.operand as LocalBuilder).LocalIndex.Equals(139)) - 1;

            List<CodeInstruction> addCodes = new List<CodeInstruction>()
            {
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadLocal(135),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.Call(typeof(Patch_FloatMenuMakerMap_AddHumanlikeOrders), "AddFilterDesignationOption", new Type[] { typeof(Pawn), typeof(Thing), typeof(List<FloatMenuOption>) })
            };

            codes.InsertRange(pos, addCodes);
            return codes;
        }

        public static void AddFilterDesignationOption(Pawn pawn, Thing apparel, List<FloatMenuOption> opts)
        {
            if (ChooseYourOutfit.settings.addFroatMenu)
            {
                var allows = pawn.outfits.CurrentApparelPolicy.filter.Allows(apparel);
                var key = string.Format(allows ? "CYO.RemoveApparelFromFilter".Translate() : "CYO.AddApparelToFilter".Translate(), apparel.def.label, pawn.outfits.CurrentApparelPolicy.label);
                opts.Add(new FloatMenuOption(key, delegate ()
                {
                    pawn.outfits.CurrentApparelPolicy.filter.SetAllow(apparel.def, !allows);
                }));
            }
        }
    }

    [HarmonyPatch(typeof(PawnRenderTree), "SetupApparelNodes")]
    static class Patch_PawnRenderTree_SetupApparelNodes
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILGenerator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            var labelPop = ILGenerator.DefineLabel();
            var labelPop2 = ILGenerator.DefineLabel();

            var windowOfTypeGeneric = AccessTools.Method(typeof(WindowStack), "WindowOfType").MakeGenericMethod(typeof(Dialog_ManageApparelPoliciesEx));

            codes.Insert(0, new CodeInstruction(OpCodes.Pop).WithLabels(labelPop2));
            codes.InsertRange(0, new List<CodeInstruction> {
                CodeInstruction.Call(typeof(Find), "get_WindowStack"),
                new CodeInstruction(OpCodes.Callvirt, windowOfTypeGeneric),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Brfalse_S, labelPop),
                new CodeInstruction(OpCodes.Dup),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "inDialogPortraitRequest"),
                new CodeInstruction(OpCodes.Brfalse_S, labelPop),
                new CodeInstruction(OpCodes.Dup),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "preApparelsApparel"),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<Apparel>), "Count")),
                new CodeInstruction(OpCodes.Brfalse_S, labelPop2),
                new CodeInstruction(OpCodes.Pop).WithLabels(labelPop)
            });

            var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.operand.Equals(AccessTools.Method(typeof(List<Apparel>), "GetEnumerator")));

            labelPop = ILGenerator.DefineLabel();
            var labelEnum = ILGenerator.DefineLabel();

            codes[pos] = codes[pos].WithLabels(labelEnum);
            codes.InsertRange(pos - 4, new List<CodeInstruction>
            {
                CodeInstruction.Call(typeof(Find), "get_WindowStack"),
                new CodeInstruction(OpCodes.Callvirt, windowOfTypeGeneric),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Brfalse_S, labelPop),
                new CodeInstruction(OpCodes.Dup),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "inDialogPortraitRequest"),
                new CodeInstruction(OpCodes.Brfalse_S, labelPop),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "preApparelsApparel"),
                new CodeInstruction(OpCodes.Br_S, labelEnum),
                new CodeInstruction(OpCodes.Pop).WithLabels(labelPop)
            });

            return codes;
        }
    }

    [HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
    [HarmonyAfter("net.velc.rimworld.mod.hds")]
    static class Patch_PawnRenderTree_AdjustParms
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILGenerator)
        {
            List<CodeInstruction> codes = instructions.ToList();

            var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.operand.Equals(AccessTools.Method(typeof(List<Apparel>), "GetEnumerator")));
            var windowOfTypeGeneric = AccessTools.Method(typeof(WindowStack), "WindowOfType").MakeGenericMethod(typeof(Dialog_ManageApparelPoliciesEx));

            var labelPop = ILGenerator.DefineLabel();
            var labelEnum = ILGenerator.DefineLabel();

            codes[pos].WithLabels(labelEnum);
            codes.InsertRange(pos - 4, new List<CodeInstruction>
            {
                CodeInstruction.Call(typeof(Find), "get_WindowStack"),
                new CodeInstruction(OpCodes.Callvirt, windowOfTypeGeneric),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Brfalse_S, labelPop),
                new CodeInstruction(OpCodes.Dup),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "inDialogPortraitRequest"),
                new CodeInstruction(OpCodes.Brfalse_S, labelPop),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "preApparelsApparel"),
                new CodeInstruction(OpCodes.Br_S, labelEnum),
                new CodeInstruction(OpCodes.Pop).WithLabels(labelPop)
            });

            return codes;
        }
    }
}