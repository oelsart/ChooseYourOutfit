using HarmonyLib;
using MaterialFilter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ChooseYourOutfit.MaterialFilterPatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.chooseyouroutfit.materialfilterpatch");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Dialog_ManageApparelPolicies_DoWindowContents_Patch), nameof(Dialog_ManageApparelPolicies_DoWindowContents_Patch.drawFilterButton))]
    static class Patch_MaterialFilter_drawFilterButton
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int pos = codes.FindIndex(c => c.opcode == OpCodes.Newobj && ((ConstructorInfo)c.operand).DeclaringType == typeof(MaterialFilterWindow));
            codes.RemoveAt(pos);
            codes.Insert(pos, new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(MaterialFilterWindowForApparel)
                , new Type[] { typeof(ThingFilter), typeof(float), typeof(float), typeof(WindowLayer) })));
            return codes;
        }
    }
}
