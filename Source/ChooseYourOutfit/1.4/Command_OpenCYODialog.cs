using RimWorld;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit;

public sealed class Command_OpenCYODialog : Command_Action
{
    public Command_OpenCYODialog()
    {
        hotKey = DefDatabase<KeyBindingDef>.GetNamed("OpenChooseYourOutfitDialog"!);
        action = () =>
        {
            if (Find.Selector.SingleSelectedThing is Pawn pawn)
            {
                Find.WindowStack.Add(new Dialog_ManageOutfitsEx(pawn));
            }
        };
        Order = float.MaxValue;
    }

    protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
    {
        if (hotKey is null)
            return new GizmoResult(GizmoState.Clear, null);
        var keyCode = hotKey.MainKey;
        if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
        {
            if (hotKey.KeyDownEvent)
            {
                if (!TutorSystem.AllowAction(TutorTagSelect))
                {
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                GizmoResult result = new(GizmoState.Interacted, Event.current);
                TutorSystem.Notify_Event(TutorTagSelect);
                Event.current.Use();
                return result;
            }
        }
        return new GizmoResult(GizmoState.Clear, null);
    }
}