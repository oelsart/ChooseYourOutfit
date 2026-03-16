using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ChooseYourOutfit;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("com.harmony.rimworld.chooseyouroutfit");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

[HarmonyPatch(typeof(PawnColumnWorker_Outfit), nameof(PawnColumnWorker_Outfit.DoHeader))]
//new Dialog_ManageOutfitsをnew Dialog_ManageOutfitsExに置き換える
internal static class Patch_PawnColumnWorker_Outfit_DoHeader
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        //置き換え後のoperandとしてDialog_ManageOutfitsExのコンストラクタを取得
        var operand = AccessTools.Constructor(typeof(Dialog_ManageOutfitsEx), [typeof(Pawn)]);
        //Dialog_ManageOutfitsのコンストラクタをoperandに持つNewobjの場所を検索
        var pos = codes.FindIndex(c =>
            c.opcode.Equals(OpCodes.Newobj) &&
            ((ConstructorInfo)c.operand).DeclaringType == typeof(Dialog_ManageOutfits));
        //新しいoperandに置き換え
        codes[pos].operand = operand;
        return codes;
    }
}

[HarmonyPatch(typeof(PawnColumnWorker_Outfit), nameof(PawnColumnWorker_Outfit.DoCell))]
//new Dialog_ManageOutfitsをnew Dialog_ManageOutfitsExに置き換え、渡すのをpawnにする
internal static class Patch_PawnColumnWorker_Outfit_DoCell
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        //置き換え後のoperandとしてDialog_ManageOutfitsExのコンストラクタを取得
        var operand = AccessTools.Constructor(typeof(Dialog_ManageOutfitsEx), [typeof(Pawn)]);
        //Dialog_ManageOutfitsのコンストラクタをoperandに持つNewobjの場所を検索
        var pos = codes.FindIndex(c =>
            c.opcode.Equals(OpCodes.Newobj) &&
            ((ConstructorInfo)c.operand).DeclaringType == typeof(Dialog_ManageOutfits));
        //新しいoperandに置き換え
        codes[pos].operand = operand;
        //(pawn以下の).outfits.CurrentOutfitを削除
        codes.RemoveAt(pos - 1);
        codes.RemoveAt(pos - 2);
        return codes;
    }
}

[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
internal static class Patch_FloatMenuMakerMap_AddHumanlikeOrders
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var pos = codes.FindIndex(c => c.opcode == OpCodes.Ldloc_S && ((LocalBuilder)c.operand).LocalIndex.Equals(124)) - 1;

        List<CodeInstruction> addCodes =
        [
            new(OpCodes.Ldarg_1),
            new(OpCodes.Ldloc_S, Convert.ToByte(120)),
            new(OpCodes.Ldarg_2),
            CodeInstruction.Call(typeof(Patch_FloatMenuMakerMap_AddHumanlikeOrders), "AddFilterDesignationOption",
                [typeof(Pawn), typeof(Thing), typeof(List<FloatMenuOption>)])
        ];

        codes.InsertRange(pos, addCodes);
        return codes;
    }

    public static void AddFilterDesignationOption(Pawn pawn, Thing apparel, List<FloatMenuOption> opts)
    {
        if (ChooseYourOutfit.settings.addFroatMenu)
        {
            var allows = pawn.outfits.CurrentOutfit.filter.Allows(apparel);
            var key = string.Format(allows ? "CYO.RemoveApparelFromFilter".Translate() : "CYO.AddApparelToFilter".Translate(), apparel.def.label, pawn.outfits.CurrentOutfit.label);
            opts.Add(new FloatMenuOption(key, delegate
            {
                pawn.outfits.CurrentOutfit.filter.SetAllow(apparel.def, !allows);
            }));
        }
    }
}

[HarmonyPatch(typeof(PawnGraphicSet), "ResolveApparelGraphics")]
internal static class Patch_PawnGraphicSet_ResolveApparelGraphics
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILGenerator)
    {
        var codes = instructions.ToList();
        var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.operand.Equals(AccessTools.Method(typeof(List<Apparel>), "GetEnumerator")));
        var windowOfTypeGeneric = AccessTools.Method(typeof(WindowStack), "WindowOfType").MakeGenericMethod(typeof(Dialog_ManageOutfitsEx));

        var labelPop = ILGenerator.DefineLabel();
        var labelEnum = ILGenerator.DefineLabel();

        codes[pos].WithLabels(labelEnum);
        codes.InsertRange(pos - 4, new List<CodeInstruction>
        {
            CodeInstruction.Call(typeof(Find), "get_WindowStack"),
            new(OpCodes.Callvirt, windowOfTypeGeneric),
            new(OpCodes.Dup),
            new(OpCodes.Brfalse_S, labelPop),
            new(OpCodes.Dup),
            CodeInstruction.LoadField(typeof(Dialog_ManageOutfitsEx), nameof(Dialog_ManageOutfitsEx.inDialogPortraitRequest)),
            new(OpCodes.Brfalse_S, labelPop),
            CodeInstruction.LoadField(typeof(Dialog_ManageOutfitsEx), nameof(Dialog_ManageOutfitsEx.preApparelsApparel)),
            new(OpCodes.Br_S, labelEnum),
            new CodeInstruction(OpCodes.Pop).WithLabels(labelPop)
        });

        return codes;
    }
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
internal static class Patch_Pawn_GetGizmos
{
    private static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
    {
        if (__instance.IsFreeColonist)
        {
            __result = __result.AddItem(new Command_OpenCYODialog());
        }
    }
}