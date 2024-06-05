using System;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using HarmonyLib;
using MaterialFilter;
using System.Collections.Generic;

namespace ChooseYourOutfit.MaterialFilterPatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.chooseyouroutfit.materialfilterpatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Dialog_ManageOutfits_DoWindowContents_Patch), nameof(Dialog_ManageOutfits_DoWindowContents_Patch.drawFilterButton))]
    static class Patch_MaterialFilter_drawFilterButton
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Newobj) && (c.operand as ConstructorInfo).DeclaringType.Equals(typeof(MaterialFilterWindow)));
            codes.RemoveAt(pos);
            codes.Insert(pos, new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(MaterialFilterWindowForApparel)
                , new Type[] { typeof(ThingFilter), typeof(float), typeof(float), typeof(WindowLayer) })));
            return codes;
        }
    }
}
