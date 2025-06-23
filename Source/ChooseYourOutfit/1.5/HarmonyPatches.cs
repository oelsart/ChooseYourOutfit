using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using static ChooseYourOutfit.ModCompat;

namespace ChooseYourOutfit
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Instance = new Harmony("com.harmony.rimworld.chooseyouroutfit");
            Instance.PatchAllUncategorized();
            if (Outfitted.Active)
            {
                Instance.PatchCategory("OELS.ChooseYourOutfit.Outfitted");
            }
        }

        public static Harmony Instance { get; private set; }
    }

    //new Dialog_ManageApparelPoliciesをnew Dialog_ManageApparelPoliciesExに置き換える
    [HarmonyPatch(typeof(PawnColumnWorker_Outfit), nameof(PawnColumnWorker_Outfit.DoHeader))]
    static class Patch_PawnColumnWorker_Outfit_DoHeader
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            //置き換え後のoperandとしてDialog_ManageApparelPoliciesExのコンストラクタを取得
            var operand = AccessTools.Constructor(typeof(Dialog_ManageApparelPoliciesEx), new Type[] { typeof(Pawn) });
            //Dialog_ManageApparelPoliciesのコンストラクタをoperandに持つNewobjの場所を検索
            int pos = codes.FindIndex(c => c.opcode == OpCodes.Newobj && ((ConstructorInfo)c.operand).DeclaringType.Equals(typeof(Dialog_ManageApparelPolicies)));
            //新しいoperandに置き換え
            codes[pos].operand = operand;
            return codes;
        }
    }

    //new Dialog_ManageApparelPoliciesをnew Dialog_ManageApparelPoliciesExに置き換え、渡すのをpawnにする
    [HarmonyPatch]
    static class Patch_PawnColumnWorker_Outfit_Button_GenerateMenu
    {
        static MethodInfo TargetMethod()
        {
            var m_WindowStack_Add = AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add));
            return AccessTools.FindIncludingInnerTypes(typeof(PawnColumnWorker_Outfit),
                t => t.GetMethods(AccessTools.all).FirstOrDefault(m =>
                {
                    return m.Name.Contains("<Button_GenerateMenu>") &&
                    PatchProcessor.ReadMethodBody(m).Any(c => m_WindowStack_Add.Equals(c.Value));
                }));
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            //置き換え後のoperandとしてDialog_ManageApparelPoliciesExのコンストラクタを取得
            var operand = AccessTools.Constructor(typeof(Dialog_ManageApparelPoliciesEx), new Type[] { typeof(Pawn) });
            //Dialog_ManageApparelPoliciesのコンストラクタをoperandに持つNewobjの場所を検索
            var pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Newobj) && ((ConstructorInfo)c.operand).DeclaringType.Equals(typeof(Dialog_ManageApparelPolicies)));
            //新しいoperandに置き換え
            codes[pos].operand = operand;
            //(pawn以下の).outfits.CurrentApparelPolicyを削除
            codes.RemoveAt(pos - 1);
            codes.RemoveAt(pos - 2);
            return codes;
        }
    }

    //Policy編集ボタン3つを右端からポリシー名の横に変える
    [HarmonyPatch]
    static class Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents
    {
        static MethodBase TargetMethod()
        {
            Type generic = AnimalControls ? typeof(Dialog_ManagePolicies<FoodPolicy>) : typeof(Dialog_ManagePolicies<ApparelPolicy>);
            return AccessTools.Method(generic, "DoWindowContents");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var m_LabelEllipses = AccessTools.Method(typeof(Widgets), "LabelEllipses");
            var pos = codes.FindIndex(c => c.Calls(m_LabelEllipses));
            var window = AccessTools.CreateInstance<Dialog_ManageApparelPoliciesEx>();
            var margin = (float)AccessTools.Property(window.GetType(), "Margin").GetValue(window);
            var root = generator.DeclareLocal(typeof(float));

            var label = generator.DefineLabel();
            codes[pos].labels.Add(label);
            codes.InsertRange(pos, new[]
            {
                CodeInstruction.LoadArgument(0),
                CodeInstruction.Call(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), nameof(MoveButtons)),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Dup),
                CodeInstruction.Call(typeof(Text), nameof(Text.CalcSize)),
                CodeInstruction.LoadField(typeof(Vector2), "x"),
                new CodeInstruction(OpCodes.Ldc_R4, margin),
                new CodeInstruction(OpCodes.Ldc_R4, 194f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Ldc_R4, margin + 488f),
                CodeInstruction.Call(typeof(Math), "Min", new Type[] { typeof(float), typeof(float) }),
                new CodeInstruction(OpCodes.Stloc_S, root),
                CodeInstruction.LoadLocal(4, true),
                new CodeInstruction(OpCodes.Ldc_R4, margin + 488f),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(Rect), nameof(Rect.xMax)))
            });

            var pos2 = codes.FindIndex(pos, c => c.LoadsConstant("DeletePolicyTip"));
            var label2 = generator.DefineLabel();
            var label3 = generator.DefineLabel();
            var offset = generator.DeclareLocal(typeof(float));
            var m_OffsetButton = AccessTools.Method(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), nameof(OffsetButton));
            codes[pos2].labels.Add(label2);
            codes.InsertRange(pos2, new[]
            {
                CodeInstruction.LoadArgument(0),
                CodeInstruction.Call(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), nameof(MoveButtons)),
                new CodeInstruction(OpCodes.Brfalse_S, label2),
                new CodeInstruction(OpCodes.Ldc_R4, 0f),
                new CodeInstruction(OpCodes.Stloc_S, offset),
                CodeInstruction.LoadLocal(13),
                new CodeInstruction(OpCodes.Brtrue_S, label3),
                CodeInstruction.LoadLocal(8, true),
                new CodeInstruction(OpCodes.Ldloc_S, root),
                new CodeInstruction(OpCodes.Ldloca_S, offset),
                new CodeInstruction(opcode: OpCodes.Call, m_OffsetButton),
                CodeInstruction.LoadLocal(7, true).WithLabels(label3),
                new CodeInstruction(OpCodes.Ldloc_S, root),
                new CodeInstruction(OpCodes.Ldloca_S, offset),
                new CodeInstruction(opcode: OpCodes.Call, m_OffsetButton),
                CodeInstruction.LoadLocal(6, true),
                new CodeInstruction(OpCodes.Ldloc_S, root),
                new CodeInstruction(OpCodes.Ldloca_S, offset),
                new CodeInstruction(opcode: OpCodes.Call, m_OffsetButton),
                CodeInstruction.LoadLocal(5, true),
                new CodeInstruction(OpCodes.Ldloc_S, root),
                new CodeInstruction(OpCodes.Ldloca_S, offset),
                new CodeInstruction(opcode: OpCodes.Call, m_OffsetButton),
            });
            return codes;
        }

        static bool MoveButtons(Dialog_ManagePolicies<ApparelPolicy> dialog)
        {
            return !ChooseYourOutfit.settings.disableAddedUI && dialog is Dialog_ManageApparelPoliciesEx;
        }

        static void OffsetButton(ref Rect rect, float root, ref float offset)
        {
            rect.x = root + offset;
            offset += 42f;
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.GetFloatMenuOptions))]
    public static class Patch_Thing_GetFloatMenuOptions
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> values, Thing __instance, Pawn selPawn)
        {
            foreach (var value in values)
            {
                yield return value;
            }
            if (ChooseYourOutfit.settings.addFroatMenu && __instance.def.IsApparel)
            {
                var apparel = __instance.def;
                var allows = selPawn.outfits.CurrentApparelPolicy.filter.Allows(apparel);
                var key = string.Format(allows ? "CYO.RemoveApparelFromFilter".Translate() : "CYO.AddApparelToFilter".Translate(), apparel.label, selPawn.outfits.CurrentApparelPolicy.label);
                yield return new FloatMenuOption(key, () =>
                {
                    selPawn.outfits.CurrentApparelPolicy.filter.SetAllow(apparel, !allows);
                });
            }
        }
    }

    [HarmonyPatch]
    [HarmonyAfter("AB.HATweaker")]
    static class Patch_PawnRenderTree_SetupApparelNodes
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.FindIncludingInnerTypes(typeof(DynamicPawnRenderNodeSetup_Apparel), t =>
            {
                if (!t.Name.Contains("<GetDynamicNodes>")) return null;
                return t.GetDeclaredMethods().FirstOrDefault(m => m.Name.Contains("MoveNext"));
            });
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            var label = generator.DefineLabel();
            var window = generator.DeclareLocal(typeof(Dialog_ManageApparelPoliciesEx));

            var windowOfTypeGeneric = AccessTools.Method(typeof(WindowStack), "WindowOfType").MakeGenericMethod(typeof(Dialog_ManageApparelPoliciesEx));

            codes[0].labels.Add(label);
            codes.InsertRange(0, new[]
            {
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Find), nameof(Find.WindowStack))),
                new CodeInstruction(OpCodes.Callvirt, windowOfTypeGeneric),
                new CodeInstruction(OpCodes.Stloc_S, window),
                new CodeInstruction(OpCodes.Ldloc_S, window),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldloc_S, window),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "inDialogPortraitRequest"),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldloc_S, window),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "preApparelsApparel"),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<Apparel>), nameof(List<Apparel>.Count))),
                new CodeInstruction(OpCodes.Brtrue_S, label),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret)
            });

            var m_GetEnumerator = AccessTools.Method(typeof(List<Apparel>), "GetEnumerator");
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.OperandIs(m_GetEnumerator));
            var g_WornApparel = AccessTools.PropertyGetter(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel));
            var pos2 = codes.FindLastIndex(pos, c => c.opcode == OpCodes.Callvirt && c.OperandIs(g_WornApparel)) - 3;

            var label2 = generator.DefineLabel();
            var label3 = generator.DefineLabel();

            codes[pos].labels.Add(label2);
            codes[pos2].labels.Add(label3);
            codes.InsertRange(pos2, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_S, window),
                new CodeInstruction(OpCodes.Brfalse_S, label3),
                new CodeInstruction(OpCodes.Ldloc_S, window),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "inDialogPortraitRequest"),
                new CodeInstruction(OpCodes.Brfalse_S, label3),
                new CodeInstruction(OpCodes.Ldloc_S, window),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "preApparelsApparel"),
                new CodeInstruction(OpCodes.Br_S, label2),
            });

            if (ABHATweaker)
            {
                var label4 = generator.DefineLabel();
                codes[pos2].labels.Add(label4);
                var pos3 = codes.FindLastIndex(pos2, c => c.opcode == OpCodes.Stloc_2) + 1;
                var label5 = generator.DefineLabel();
                codes[pos3].labels.Add(label5);

                codes.InsertRange(pos3, new[]
                {
                    new CodeInstruction(OpCodes.Ldloc_S, window),
                    new CodeInstruction(OpCodes.Brfalse_S, label5),
                    new CodeInstruction(OpCodes.Ldloc_S, window),
                    CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "inDialogPortraitRequest"),
                    new CodeInstruction(OpCodes.Brtrue_S, label4),
                });
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
    [HarmonyAfter("net.velc.rimworld.mod.hds", "AB.HATweaker")]
    static class Patch_PawnRenderTree_AdjustParms
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();

            int pos;
            if (ABHATweaker)
            {
                var m_GetApparel_1 = AccessTools.Method(AccessTools.TypeByName("HeadApparelTweaker.HarmonyPatchA5"), "GetApparel_1");
                pos = codes.FindIndex(c => c.Calls(m_GetApparel_1)) - 2;
            }
            else
            {
                var m_GetEnumerator = AccessTools.Method(typeof(List<Apparel>), "GetEnumerator");
                pos = codes.FindIndex(c => c.Calls(m_GetEnumerator));
            }
            var windowOfTypeGeneric = AccessTools.Method(typeof(WindowStack), "WindowOfType").MakeGenericMethod(typeof(Dialog_ManageApparelPoliciesEx));

            var labelPop = generator.DefineLabel();
            var labelEnum = generator.DefineLabel();

            var g_WornApparel = AccessTools.PropertyGetter(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel));
            var pos2 = codes.FindLastIndex(pos, c => c.Calls(g_WornApparel)) - 3;
            codes[pos].WithLabels(labelEnum);
            codes.InsertRange(pos2, new List<CodeInstruction>
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

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    static class Patch_Pawn_GetGizmos
    {
        static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.IsFreeColonist)
            {
                __result = __result.AddItem(new Command_OpenCYODialog());
            }
        }
    }

    [HarmonyPatchCategory("OELS.ChooseYourOutfit.Outfitted")]
    [HarmonyPatch("Outfitted.Dialog_ManageApparelPolicies_DoContentsRect_Patch", "Postfix")]
    public static class ReversePatch_Dialog_ManageApparelPolicies_DoContentsRect_Patch_Postfix
    {
        [HarmonyReversePatch]
        public static void DoContent(Rect rect, Dialog_ManageApparelPolicies __instance)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var original = AccessTools.Method(Outfitted.Dialog_ManageApparelPolicies_DoContentsRect_Patch, "DrawCloneButton");
                var patch = AccessTools.Method(typeof(ReversePatch_Dialog_ManageApparelPolicies_DoContentsRect_Patch_DrawCloneButton), "DrawCloneButton");
                return instructions.MethodReplacer(original, patch);
            }
            _ = Transpiler(null);
            throw new NotImplementedException();
        }
    }

    [HarmonyPatchCategory("OELS.ChooseYourOutfit.Outfitted")]
    [HarmonyPatch("Outfitted.Dialog_ManageApparelPolicies_DoContentsRect_Patch", "DrawCloneButton")]
    public static class ReversePatch_Dialog_ManageApparelPolicies_DoContentsRect_Patch_DrawCloneButton
    {
        [HarmonyReversePatch]
        public static void DrawCloneButton(ApparelPolicy selectedOutfit)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = instructions.FirstOrDefault(c => c.LoadsConstant(700f));
                if (code != null) code.operand = 10f;
                code = instructions.FirstOrDefault(c => c.LoadsConstant(0f));
                if (code != null) code.operand = 80f;
                return instructions;
            }
            _ = Transpiler(null);
            throw new NotImplementedException();
        }
    }
}