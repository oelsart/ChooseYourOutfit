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
    //new Dialog_ManageOutfitsをnew Dialog_ManageOutfitsExに置き換える
    static class Patch_PawnColumnWorker_Outfit_DoHeader
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            //置き換え後のoperandとしてDialog_ManageOutfitsExのコンストラクタを取得
            var operand = AccessTools.GetDeclaredConstructors(typeof(Dialog_ManageOutfitsEx)).First();
            //Dialog_ManageOutfitsのコンストラクタをoperandに持つNewobjの場所を検索
            int pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Newobj) && (c.operand as ConstructorInfo).DeclaringType.Equals(typeof(Dialog_ManageOutfits)));
            //新しいoperandに置き換え
            codes[pos].operand = operand;
            return codes;
        }
    }

    [HarmonyPatch(typeof(PawnColumnWorker_Outfit), nameof(PawnColumnWorker_Outfit.DoCell))]
    //new Dialog_ManageOutfitsをnew Dialog_ManageOutfitsExに置き換え、渡すのをpawnにする
    static class Patch_PawnColumnWorker_Outfit_DoCell
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            //置き換え後のoperandとしてDialog_ManageOutfitsExのコンストラクタを取得
            var operand = AccessTools.GetDeclaredConstructors(typeof(Dialog_ManageOutfitsEx)).First();
            //Dialog_ManageOutfitsのコンストラクタをoperandに持つNewobjの場所を検索
            int pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Newobj) && (c.operand as ConstructorInfo).DeclaringType.Equals(typeof(Dialog_ManageOutfits)));
            //新しいoperandに置き換え
            codes[pos].operand = operand;
            //(pawn以下の).outfits.CurrentOutfitを削除
            codes.RemoveAt(pos - 1);
            codes.RemoveAt(pos - 2);
            return codes;
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    static class Patch_FloatMenuMakerMap_AddHumanlikeOrders
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Ldloc_S) && (c.operand as LocalBuilder).LocalIndex.Equals(124)) - 1;

            List<CodeInstruction> addCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(120)),
                new CodeInstruction(OpCodes.Ldarg_2),
                CodeInstruction.Call(typeof(Patch_FloatMenuMakerMap_AddHumanlikeOrders), "AddFilterDesignationOption", new Type[] { typeof(Pawn), typeof(Thing), typeof(List<FloatMenuOption>) })
            };

            codes.InsertRange(pos, addCodes);
            return codes;
        }

        public static void AddFilterDesignationOption(Pawn pawn, Thing apparel, List<FloatMenuOption> opts)
        {
            if (ChooseYourOutfit.settings.addFroatMenu)
            {
                var allows = pawn.outfits.CurrentOutfit.filter.Allows(apparel);
                var key = string.Format(allows ? "CYO.RemoveApparelFromFilter".Translate() : "CYO.AddApparelToFilter".Translate(), apparel.def.label, pawn.outfits.CurrentOutfit.label);
                opts.Add(new FloatMenuOption(key, delegate ()
                {
                    pawn.outfits.CurrentOutfit.filter.SetAllow(apparel.def, !allows);
                }));
            }
        }
    }
}