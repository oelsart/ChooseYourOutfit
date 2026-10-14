using HarmonyLib;
using RimWorld;
using System.Reflection;
using System.Reflection.Emit;
using LudeonTK;
using UnityEngine;
using Verse;
using static ChooseYourOutfit.ModCompat;

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

//Policy編集ボタン3つを右端からポリシー名の横に変える
[HarmonyPatch]
internal static class Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents
{
	private static MethodInfo TargetMethod()
	{
		var generic = typeof(Dialog_ManagePolicies<ApparelPolicy>);
		return AccessTools.Method(generic, nameof(Window.DoWindowContents));
	}

	private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		var codes = instructions.ToList();
		var m_LabelEllipses = ((Delegate)Widgets.LabelEllipses).Method;
		var pos = codes.FindIndex(c => c.Calls(m_LabelEllipses));
		const float WindowMargin = 18f;
		var root = generator.DeclareLocal(typeof(float));

		var label = generator.DefineLabel();
		codes[pos].labels.Add(label);
		codes.InsertRange(pos, [
			CodeInstruction.LoadArgument(0),
			CodeInstruction.Call(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), nameof(MoveButtons)),
			new CodeInstruction(OpCodes.Brfalse_S, label),
			new CodeInstruction(OpCodes.Dup),
			CodeInstruction.Call(typeof(Text), nameof(Text.CalcSize)),
			CodeInstruction.LoadField(typeof(Vector2), "x"),
			new CodeInstruction(OpCodes.Ldc_R4, WindowMargin),
			new CodeInstruction(OpCodes.Ldc_R4, 194f),
			new CodeInstruction(OpCodes.Add),
			new CodeInstruction(OpCodes.Add),
			new CodeInstruction(OpCodes.Ldc_R4, WindowMargin + 488f),
			CodeInstruction.Call(typeof(Math), "Min", [typeof(float), typeof(float)]),
			new CodeInstruction(OpCodes.Stloc_S, root),
			CodeInstruction.LoadLocal(4, true),
			new CodeInstruction(OpCodes.Ldc_R4, WindowMargin + 488f),
			new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(Rect), nameof(Rect.xMax)))
		]);

		var pos2 = codes.FindIndex(pos, c => c.LoadsConstant("DeletePolicyTip"));
		var label2 = generator.DefineLabel();
		var label3 = generator.DefineLabel();
		var offset = generator.DeclareLocal(typeof(float));
		var m_OffsetButton = AccessTools.Method(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents),
			nameof(OffsetButton));
		codes[pos2].labels.Add(label2);
		codes.InsertRange(pos2, [
			CodeInstruction.LoadArgument(0),
			CodeInstruction.Call(typeof(Patch_Dialog_ManagePolicies_ApparelPolicy_DoWindowContents), nameof(MoveButtons)),
			new CodeInstruction(OpCodes.Brfalse_S, label2),
			new CodeInstruction(OpCodes.Ldc_R4, 0f),
			new CodeInstruction(OpCodes.Stloc_S, offset),
			CodeInstruction.LoadLocal(13),
			new CodeInstruction(OpCodes.Brtrue_S, label3),
			CodeInstruction.LoadLocal(8, true),
			new CodeInstruction(OpCodes.Ldloc_S, root),
			new CodeInstruction(OpCodes.Ldloca_S, offset),
			new CodeInstruction(opcode: OpCodes.Call, m_OffsetButton),
			CodeInstruction.LoadLocal(7, true).WithLabels(label3),
			new CodeInstruction(OpCodes.Ldloc_S, root),
			new CodeInstruction(OpCodes.Ldloca_S, offset),
			new CodeInstruction(opcode: OpCodes.Call, m_OffsetButton),
			CodeInstruction.LoadLocal(6, true),
			new CodeInstruction(OpCodes.Ldloc_S, root),
			new CodeInstruction(OpCodes.Ldloca_S, offset),
			new CodeInstruction(opcode: OpCodes.Call, m_OffsetButton),
			CodeInstruction.LoadLocal(5, true),
			new CodeInstruction(OpCodes.Ldloc_S, root),
			new CodeInstruction(OpCodes.Ldloca_S, offset),
			new CodeInstruction(opcode: OpCodes.Call, m_OffsetButton)
		]);
		return codes;
	}

	private static bool MoveButtons(Window dialog)
	{
		return !ChooseYourOutfit.Settings.disableAddedUI && dialog is Dialog_ManageApparelPoliciesEx;
	}

	private static void OffsetButton(ref Rect rect, float root, ref float offset)
	{
		rect.x = root + offset;
		offset += 42f;
	}

	[DebugAction("ChooseYourOutfit")]
	private static void RepatchButtonOffset()
	{
		var target = TargetMethod();
		var patch = ((Delegate)Transpiler).Method;
		HarmonyPatches.Instance.Unpatch(target, patch);
		HarmonyPatches.Instance.Patch(target, transpiler: patch);
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