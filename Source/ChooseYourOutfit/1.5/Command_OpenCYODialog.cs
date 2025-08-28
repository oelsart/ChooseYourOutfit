using RimWorld;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit
{
    public class Command_OpenCYODialog : Command_Action
    {
        public Command_OpenCYODialog()
        {
            hotKey = DefDatabase<KeyBindingDef>.GetNamed("OpenChooseYourOutfitDialog");
            action = () =>
            {
                var pawn = Find.Selector.SingleSelectedThing as Pawn;
                if (pawn != null)
                {
                    Find.WindowStack.Add(new Dialog_ManageApparelPoliciesEx(pawn));
                }
            };
            Order = float.MaxValue;
        }

        protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
        {
            KeyCode keyCode = (hotKey == null) ? KeyCode.None : hotKey.MainKey;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                if (hotKey.KeyDownEvent)
                {
                    GizmoResult result;
                    if (!TutorSystem.AllowAction(TutorTagSelect))
                    {
                        return new GizmoResult(GizmoState.Mouseover, null);
                    }
                    result = new GizmoResult(GizmoState.Interacted, Event.current);
                    TutorSystem.Notify_Event(TutorTagSelect);
                    Event.current.Use();
                    return result;
                }
            }
            return new GizmoResult(GizmoState.Clear, null);
        }
    }
}
