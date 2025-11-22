using HarmonyLib;
using MaterialFilter;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ChooseYourOutfit.MaterialFilterPatch;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("com.harmony.rimworld.chooseyouroutfit.materialfilterpatch");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(Dialog_ManageApparelPolicies_DoWindowContents_Patch), nameof(Dialog_ManageApparelPolicies_DoWindowContents_Patch.drawFilterButton))]
internal static class Patch_MaterialFilter_drawFilterButton
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = [.. instructions];
        var pos = codes.FindIndex(c => c.opcode == OpCodes.Newobj && ((ConstructorInfo)c.operand).DeclaringType == typeof(MaterialFilterWindow));
        codes[pos].operand = AccessTools.Constructor(typeof(MaterialFilterWindowForApparel),
            [typeof(ThingFilter), typeof(float), typeof(float), typeof(WindowLayer)]);
        return codes;
    }
}