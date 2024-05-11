using System;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit
{
    public class Dialog_AddBillsConfirm : Window
    {
        public Dialog_AddBillsConfirm(string title, Action onConfirm) : this(title, "Confirm".Translate(), onConfirm)
        {
        }

        public Dialog_AddBillsConfirm(string title, string confirm, Action onConfirm) : base()
        {
            this.title = title;
            this.confirm = confirm;
            this.onConfirm = onConfirm;
            this.forcePause = true;
            this.closeOnAccept = false;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, 200f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            bool flag = false;
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                flag = true;
                Event.current.Use();
            }
            Rect rect = inRect;
            rect.width = inRect.width / 2f - 5f;
            rect.yMin = inRect.yMax - Dialog_AddBillsConfirm.ButtonSize.y - 10f;
            Rect rect2 = inRect;
            rect2.xMin = rect.xMax + 10f;
            rect2.yMin = inRect.yMax - Dialog_AddBillsConfirm.ButtonSize.y - 10f;
            Rect rect3 = inRect;
            rect3.y += 4f;
            rect3.yMax = rect2.y - 10f - Text.LineHeight * 2;
            Rect rect4 = inRect;
            rect4.y = rect3.yMax;
            rect4.height = Text.LineHeight;
            Rect rect5 = rect4;
            rect5.y = rect4.yMax;
            Rect rect6 = rect5;
            rect6.y = rect5.yMax;

            using (new TextBlock(TextAnchor.UpperCenter))
            {
                Widgets.Label(rect3, this.title);
            }
            Widgets.CheckboxLabeled(rect4, "CYO.AddBillsConfirm.RestrictToPreviewedApparels".Translate(), ref restrictToPreviewedApparels);
            Widgets.CheckboxLabeled(rect5, "CYO.AddBillsConfirm.RestrictToPreviewedStuffs".Translate(), ref restrictToPreviewedStuffs);
            Widgets.CheckboxLabeled(rect6, "CYO.AddBillsConfirm.ForceRegister".Translate(), ref forceRegister);
            if (Widgets.ButtonText(rect, "Cancel".Translate(), true, true, true, null))
            {
                Find.WindowStack.TryRemove(this, true);
            }
            if (Widgets.ButtonText(rect2, this.confirm, true, true, true, null) || flag)
            {
                Action action = this.onConfirm;
                if (action != null)
                {
                    action();
                }
                Find.WindowStack.TryRemove(this, true);
            }
        }

        private string title;

        private string confirm;

        private Action onConfirm;

        public static bool restrictToPreviewedApparels = false;

        public static bool restrictToPreviewedStuffs = false;

        public static bool forceRegister = false;

        private static readonly Vector2 ButtonSize = new Vector2(120f, 32f);
    }
}
