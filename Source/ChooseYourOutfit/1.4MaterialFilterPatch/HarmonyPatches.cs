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

    [HarmonyPatch(typeof(Dialog_ManageOutfits_DoWindowContents_Patch), nameof(Dialog_ManageOutfits_DoWindowContents_Patch.drawFilterButton))]
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
            var dialog = Find.WindowStack.WindowOfType<Dialog_ManageOutfits>();
            dialog.windowRect.x += offset;
        }

        public static void PostOpen()
        {
            var tmpFilterWindow = Find.WindowStack.WindowOfType<MaterialFilterWindow>();
            tmpFilterWindow.DoWindowContents(tmpFilterWindow.windowRect); //DoWindowContents内でwindowRect.widthを変更しているようなので一回実行してからwidthを取得する
            offset = tmpFilterWindow.windowRect.width / 2;
            var dialog = Find.WindowStack.WindowOfType<Dialog_ManageOutfits>();
            dialog.windowRect.x -= offset;
            tmpFilterWindow.windowRect.x -= offset;
        }

        private static float offset;
    }
}
