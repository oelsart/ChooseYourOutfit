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
            foreach (var code in codes)
            {
                yield return code;
            }
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
            foreach (var code in codes)
            {
                yield return code;
            }
        }
    }

    //Policy編集ボタン3つを右端からポリシー名の横に変える
    [HarmonyPatch(typeof(Dialog_ManagePolicies<ApparelPolicy>), nameof(Dialog_ManagePolicies<ApparelPolicy>.DoWindowContents))]
    static class Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List <CodeInstruction> codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Ldstr) && c.operand.Equals("DeletePolicyTip"));
            var window = AccessTools.CreateInstance<Dialog_ManageApparelPolicies>();
            var margin = (float)AccessTools.Property(window.GetType(), "Margin").GetValue(window);

            //魂のIL手打ちゾーン
            List<CodeInstruction> addCodes = new List<CodeInstruction>()
            {
                CodeInstruction.LoadLocal(7, true),
                CodeInstruction.LoadField(typeof(Text), "fontStyles"),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Ldelem_Ref),
                CodeInstruction.LoadLocal(12),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(GUIContent), new Type[] { typeof(string) })),
                CodeInstruction.Call(typeof(GUIStyle), "CalcSize", new Type[] { typeof(GUIContent) }),
                CodeInstruction.LoadField(typeof(Vector2), "x"),
                new CodeInstruction(OpCodes.Ldc_R4, margin),
                new CodeInstruction(OpCodes.Ldc_R4, 200f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Add),
                CodeInstruction.StoreField(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), "tmpLocal"),
                CodeInstruction.LoadField(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), "tmpLocal"),
                CodeInstruction.Call(typeof(Rect), "set_x", new Type[] { typeof(float) }),
                CodeInstruction.LoadLocal(6, true),
                CodeInstruction.LoadField(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), "tmpLocal"),
                new CodeInstruction(OpCodes.Ldc_R4, 42f),
                new CodeInstruction(OpCodes.Add),
                CodeInstruction.Call(typeof(Rect), "set_x", new Type[] { typeof(float) }),
                CodeInstruction.LoadLocal(5, true),
                CodeInstruction.LoadField(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), "tmpLocal"),
                new CodeInstruction(OpCodes.Ldc_R4, 84f),
                new CodeInstruction(OpCodes.Add),
                CodeInstruction.Call(typeof(Rect), "set_x", new Type[] { typeof(float) })
            };

            codes.InsertRange(pos, addCodes);

            foreach(var code in codes)
            {
                yield return code;
            }
        }

        public static float tmpLocal = 0f;
    }
}   