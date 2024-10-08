using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using AwesomeInventory.Loadout;

namespace ChooseYourOutfit
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.chooseyouroutfit");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            /*if (ModsConfig.IsActive("AB.HATweaker"))
            {
                var from = AccessTools.Method(AccessTools.TypeByName("HeadApparelTweaker.HarmonyPatchA5"), "PreProcessApparel");
                var to = AccessTools.Method(typeof(Patch_HeadApparelTweaker_PreProcessApparel), "Prefix");
                harmony.Patch(from, to);
            }*/
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

    [HarmonyDebug]
    [HarmonyPatch(typeof(PawnRenderTree), "SetupApparelNodes")]
    static class Patch_PawnRenderTree_SetupApparelNodes
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILGenerator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            var label = ILGenerator.DefineLabel();
            var window = ILGenerator.DeclareLocal(typeof(Dialog_ManageApparelPoliciesEx));

            var windowOfTypeGeneric = AccessTools.Method(typeof(WindowStack), "WindowOfType").MakeGenericMethod(typeof(Dialog_ManageApparelPoliciesEx));

            codes[0].labels.Add(label);
            codes.InsertRange(0, new List<CodeInstruction> {
                CodeInstruction.Call(typeof(Find), "get_WindowStack"),
                new CodeInstruction(OpCodes.Callvirt, windowOfTypeGeneric),
                new CodeInstruction(OpCodes.Stloc_S, window),
                new CodeInstruction(OpCodes.Ldloc_S, window),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldloc_S, window),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "inDialogPortraitRequest"),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldloc_S, window),
                CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx), "preApparelsApparel"),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<Apparel>), "Count")),
                new CodeInstruction(OpCodes.Brtrue_S, label),
                new CodeInstruction(OpCodes.Ret)
            });

            var m_GetEnumerator = AccessTools.Method(typeof(List<Apparel>), "GetEnumerator");
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.OperandIs(m_GetEnumerator));
            var g_WornApparel = AccessTools.PropertyGetter(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel));
            var pos2 = codes.FindLastIndex(pos, c => c.opcode == OpCodes.Callvirt && c.OperandIs(g_WornApparel)) - 3;

            var label2 = ILGenerator.DefineLabel();
            var label3 = ILGenerator.DefineLabel();

            codes[pos].labels.Add(label2);
            codes[pos2].labels.Add(label3);
            codes.InsertRange(pos2, new List<CodeInstruction>
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

            if (ModsConfig.IsActive("AB.HATweaker"))
            {
                var label4 = ILGenerator.DefineLabel();
                codes[pos2].labels.Add(label4);
                var pos3 = codes.FindLastIndex(pos2, c => c.opcode == OpCodes.Stloc_2);
                var label5 = ILGenerator.DefineLabel();
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
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILGenerator)
        {
            List<CodeInstruction> codes = instructions.ToList();

            int pos;
            if (ModsConfig.IsActive("AB.HATweaker"))
            {
                var m_GetApparel_1 = AccessTools.Method(AccessTools.TypeByName("HeadApparelTweaker.HarmonyPatchA5"), "GetApparel_1");
                pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.OperandIs(m_GetApparel_1)) - 2;
            }
            else
            {
                var m_GetEnumerator = AccessTools.Method(typeof(List<Apparel>), "GetEnumerator");
                pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.OperandIs(m_GetEnumerator));
            }
            var windowOfTypeGeneric = AccessTools.Method(typeof(WindowStack), "WindowOfType").MakeGenericMethod(typeof(Dialog_ManageApparelPoliciesEx));

            var labelPop = ILGenerator.DefineLabel();
            var labelEnum = ILGenerator.DefineLabel();

            var g_WornApparel = AccessTools.PropertyGetter(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel));
            var pos2 = codes.FindLastIndex(pos, c => c.opcode == OpCodes.Callvirt && c.OperandIs(g_WornApparel)) - 3;
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

    /*public static class Patch_HeadApparelTweaker_PreProcessApparel
    {
        public static void Prefix(ref Apparel ap)
        {
            var window = Find.WindowStack.WindowOfType<Dialog_ManageApparelPoliciesEx>();
            if (window != null && window.inDialogPortraitRequest)
            {
                var newAp = window.preApparelsApparel.FirstOrDefault(a => a.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead) || a.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead));
                if (newAp != null)
                {
                    AccessTools.StaticFieldRefAccess<Apparel>(AccessTools.TypeByName("HeadApparelTweaker.HATweakerMod"), "apparel") = newAp;
                    ap = newAp;
                }
            }
        }

        public static void Postfix(ref bool __result)
        {
            var window = Find.WindowStack.WindowOfType<Dialog_ManageApparelPoliciesEx>();
            if (window != null && window.inDialogPortraitRequest)
            {
                __result = true;
            }
        }
    }*/
}