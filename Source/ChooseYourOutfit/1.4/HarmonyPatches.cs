﻿using System;
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
            foreach (var code in codes)
            {
                yield return code;
            }
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
            foreach (var code in codes)
            {
                yield return code;
            }
        }
    }
}