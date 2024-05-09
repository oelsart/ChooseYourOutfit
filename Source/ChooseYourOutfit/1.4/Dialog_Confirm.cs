using System;
using UnityEngine;

namespace Verse
{
    // Token: 0x020005B9 RID: 1465
    public class Dialog_Confirm : Window
    {
        // Token: 0x06002D21 RID: 11553 RVA: 0x0011D81A File Offset: 0x0011BA1A
        public Dialog_Confirm(string title, Action onConfirm) : this(title, "Confirm".Translate(), onConfirm)
        {
        }

        // Token: 0x06002D22 RID: 11554 RVA: 0x0011D833 File Offset: 0x0011BA33
        public Dialog_Confirm(string title, string confirm, Action onConfirm) : base()
        {
            this.title = title;
            this.confirm = confirm;
            this.onConfirm = onConfirm;
            this.forcePause = true;
            this.closeOnAccept = false;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        // Token: 0x17000896 RID: 2198
        // (get) Token: 0x06002D23 RID: 11555 RVA: 0x0011D86D File Offset: 0x0011BA6D
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(280f, 150f);
            }
        }

        // Token: 0x06002D24 RID: 11556 RVA: 0x0011D880 File Offset: 0x0011BA80
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
            rect.yMin = inRect.yMax - Dialog_Confirm.ButtonSize.y - 10f;
            Rect rect2 = inRect;
            rect2.xMin = rect.xMax + 10f;
            rect2.yMin = inRect.yMax - Dialog_Confirm.ButtonSize.y - 10f;
            Rect rect3 = inRect;
            rect3.y += 4f;
            rect3.yMax = rect2.y - 10f;
            using (new TextBlock(TextAnchor.UpperCenter))
            {
                Widgets.Label(rect3, this.title);
            }
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

        // Token: 0x04001D17 RID: 7447
        private string title;

        // Token: 0x04001D18 RID: 7448
        private string confirm;

        // Token: 0x04001D19 RID: 7449
        private Action onConfirm;

        // Token: 0x04001D1A RID: 7450
        private static readonly Vector2 ButtonSize = new Vector2(120f, 32f);
    }
}
