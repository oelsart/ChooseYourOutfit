using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
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

    [HarmonyPatch(typeof(Dialog_ManageApparelPolicies_DoWindowContents_Patch), nameof(Dialog_ManageApparelPolicies_DoWindowContents_Patch.drawFilterButton))]
    static class Patch_MaterialFilter_drawFilterButton
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Callvirt) && (c.operand as MethodInfo).Equals(AccessTools.Method(typeof(Window), "Close"))) + 1;
            codes.Insert(pos, new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Patch_MaterialFilter_drawFilterButton), "PostClose")));

            pos = codes.FindIndex(c => c.opcode.Equals(OpCodes.Callvirt) && (c.operand as MethodInfo).Equals(AccessTools.Method(typeof(WindowStack), "Add"))) + 1;
            codes.Insert(pos, new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Patch_MaterialFilter_drawFilterButton), "PostOpen")));

            return codes;
        }

        public static void PostClose()
        {
            Find.WindowStack.TryGetWindow<Dialog_ManageApparelPolicies>(out var dialog);
            dialog.windowRect.x += offset;
        }

        public static void PostOpen()
        {
            Find.WindowStack.TryGetWindow<MaterialFilterWindow>(out var tmpFilterWindow);
            tmpFilterWindow.DoWindowContents(tmpFilterWindow.windowRect); //DoWindowContents内でwindowRect.widthを変更しているようなので一回実行してからwidthを取得する
            offset = tmpFilterWindow.windowRect.width / 2;
            Find.WindowStack.TryGetWindow<Dialog_ManageApparelPolicies>(out var dialog);
            dialog.windowRect.x -= offset;
            tmpFilterWindow.windowRect.x -= offset;
        }

        private static float offset;
    }
}
