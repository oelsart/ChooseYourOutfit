using HarmonyLib;
using RimWorld;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ChooseYourOutfit;

[StaticConstructorOnStartup]
internal static class HarmonyPatches
{
	public static Harmony Instance { get; }

	static HarmonyPatches()
	{
		Instance = new Harmony("com.harmony.rimworld.chooseyouroutfit");
		Instance.PatchAll();
	}
}

//new Dialog_ManageApparelPoliciesをnew Dialog_ManageApparelPoliciesExに置き換える
[HarmonyPatch(typeof(PawnColumnWorker_Outfit), nameof(PawnColumnWorker_Outfit.DoHeader))]
internal static class Patch_PawnColumnWorker_Outfit_DoHeader
{
	private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> codes = [.. instructions];
		//置き換え後のoperandとしてDialog_ManageApparelPoliciesExのコンストラクタを取得
		var operand = AccessTools.Constructor(typeof(Dialog_ManageApparelPoliciesEx), [typeof(Pawn)]);
		//Dialog_ManageApparelPoliciesのコンストラクタをoperandに持つNewobjの場所を検索
		var code = codes.Find(c =>
			c.opcode == OpCodes.Newobj && ((ConstructorInfo)c.operand).DeclaringType == typeof(Dialog_ManageApparelPolicies));
		//新しいoperandに置き換え
		code.operand = operand;
		return codes;
	}
}

//new Dialog_ManageApparelPoliciesをnew Dialog_ManageApparelPoliciesExに置き換え、渡すのをpawnにする
[HarmonyPatch]
internal static class Patch_PawnColumnWorker_Outfit_Button_GenerateMenu
{
	private static MethodInfo TargetMethod()
	{
		var m_WindowStack_Add = AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add));
		return AccessTools.FindIncludingInnerTypes(typeof(PawnColumnWorker_Outfit),
			t => t.GetMethods(AccessTools.all).FirstOrDefault(m =>
			{
				return m.Name.Contains("<Button_GenerateMenu>") &&
				       PatchProcessor.ReadMethodBody(m).Any(c => m_WindowStack_Add.Equals(c.Value));
			}));
	}

	private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> codes = [.. instructions];
		//置き換え後のoperandとしてDialog_ManageApparelPoliciesExのコンストラクタを取得
		var operand = AccessTools.Constructor(typeof(Dialog_ManageApparelPoliciesEx), [typeof(Pawn)]);
		//Dialog_ManageApparelPoliciesのコンストラクタをoperandに持つNewobjの場所を検索
		var pos = codes.FindIndex(c =>
			c.opcode == OpCodes.Newobj && ((ConstructorInfo)c.operand).DeclaringType == typeof(Dialog_ManageApparelPolicies));
		//新しいoperandに置き換え
		codes[pos].operand = operand;
		//(pawn以下の).outfits.CurrentApparelPolicyを削除
		codes.RemoveAt(pos - 1);
		codes.RemoveAt(pos - 2);
		return codes;
	}
}

[HarmonyPatch]
internal static class Patch_DynamicPawnRenderNodeSetup_Apparel_GetDynamicNodes
{
	private static MethodInfo TargetMethod()
	{
		return AccessTools.FindIncludingInnerTypes(typeof(DynamicPawnRenderNodeSetup_Apparel), t =>
		{
			return !t.Name.Contains("<GetDynamicNodes>")
				? null
				: t.GetDeclaredMethods().FirstOrDefault(m => m.Name.Contains("MoveNext"));
		});
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
		ILGenerator generator)
	{
		return new CodeMatcher(instructions, generator)
			.MatchStartForward(CodeMatch.Calls(AccessTools.Method(typeof(List<Apparel>), nameof(List<>.GetEnumerator))))
			.CreateLabel(out var label)
			.DeclareLocal(typeof(Dialog_ManageApparelPoliciesEx), out var window)
			.Insert(
				new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Find), nameof(Find.WindowStack))),
				new CodeInstruction(OpCodes.Callvirt,
					AccessTools.Method(typeof(WindowStack), nameof(WindowStack.WindowOfType))
						.MakeGenericMethod(typeof(Dialog_ManageApparelPoliciesEx))),
				new CodeInstruction(OpCodes.Stloc_S, window),
				new CodeInstruction(OpCodes.Ldloc_S, window),
				new CodeInstruction(OpCodes.Brfalse_S, label),
				new CodeInstruction(OpCodes.Ldloc_S, window),
				CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx),
					nameof(Dialog_ManageApparelPoliciesEx.inDialogPortraitRequest)),
				new CodeInstruction(OpCodes.Brfalse_S, label),
				new CodeInstruction(OpCodes.Pop),
				new CodeInstruction(OpCodes.Ldloc_S, window),
				CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx),
					nameof(Dialog_ManageApparelPoliciesEx.preApparelsApparel)))
			.InstructionEnumeration();
	}
}

[HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
[HarmonyAfter("net.velc.rimworld.mod.hds")]
internal static class Patch_PawnRenderTree_AdjustParms
{
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
		ILGenerator generator)
	{
		return new CodeMatcher(instructions, generator)
			.MatchStartForward(CodeMatch.Calls(AccessTools.Method(typeof(List<Apparel>), nameof(List<>.GetEnumerator))))
			.CreateLabel(out var label)
			.DeclareLocal(typeof(Dialog_ManageApparelPoliciesEx), out var window)
			.Insert(
				new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Find), nameof(Find.WindowStack))),
				new CodeInstruction(OpCodes.Callvirt,
					AccessTools.Method(typeof(WindowStack), nameof(WindowStack.WindowOfType))
						.MakeGenericMethod(typeof(Dialog_ManageApparelPoliciesEx))),
				new CodeInstruction(OpCodes.Stloc_S, window),
				new CodeInstruction(OpCodes.Ldloc_S, window),
				new CodeInstruction(OpCodes.Brfalse_S, label),
				new CodeInstruction(OpCodes.Ldloc_S, window),
				CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx),
					nameof(Dialog_ManageApparelPoliciesEx.inDialogPortraitRequest)),
				new CodeInstruction(OpCodes.Brfalse_S, label),
				new CodeInstruction(OpCodes.Pop),
				new CodeInstruction(OpCodes.Ldloc_S, window),
				CodeInstruction.LoadField(typeof(Dialog_ManageApparelPoliciesEx),
					nameof(Dialog_ManageApparelPoliciesEx.preApparelsApparel)))
			.InstructionEnumeration();
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