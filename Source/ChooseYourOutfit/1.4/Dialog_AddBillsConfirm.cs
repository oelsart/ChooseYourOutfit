using System;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit;

public class Dialog_AddBillsConfirm : Window
{
    public Dialog_AddBillsConfirm(string title, Action onConfirm) : this(title, "Confirm".Translate(), onConfirm)
    {
    }

    public Dialog_AddBillsConfirm(string title, string confirm, Action onConfirm)
    {
        this.title = title;
        this.confirm = confirm;
        this.onConfirm = onConfirm;
        forcePause = true;
        closeOnAccept = false;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(500f, 200f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        var flag = false;
        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
        {
            flag = true;
            Event.current.Use();
        }
        var rect = inRect;
        rect.width = inRect.width / 2f - 5f;
        rect.yMin = inRect.yMax - ButtonSize.y - 10f;
        var rect2 = inRect;
        rect2.xMin = rect.xMax + 10f;
        rect2.yMin = inRect.yMax - ButtonSize.y - 10f;
        var rect3 = inRect;
        rect3.y += 4f;
        rect3.yMax = rect2.y - 10f - Text.LineHeight * 3;
        var rect4 = inRect;
        rect4.y = rect3.yMax;
        rect4.height = Text.LineHeight;
        var rect5 = rect4;
        rect5.y = rect4.yMax;
        var rect6 = rect5;
        rect6.y = rect5.yMax;

        using (new TextBlock(TextAnchor.UpperCenter))
        {
            Widgets.Label(rect3, title);
        }
        Widgets.CheckboxLabeled(rect4, "CYO.AddBillsConfirm.RestrictToPreviewedApparels".Translate(), ref restrictToPreviewedApparels);
        Widgets.CheckboxLabeled(rect5, "CYO.AddBillsConfirm.RestrictToPreviewedStuffs".Translate(), ref restrictToPreviewedStuffs);
        Widgets.CheckboxLabeled(rect6, "CYO.AddBillsConfirm.ForceRegister".Translate(), ref forceRegister);
        if (Widgets.ButtonText(rect, "Cancel".Translate()))
        {
            Find.WindowStack.TryRemove(this);
        }
        if (Widgets.ButtonText(rect2, confirm) || flag)
        {
            var action = onConfirm;
            if (action != null)
            {
                action();
            }
            Find.WindowStack.TryRemove(this);
        }
    }

    private readonly string title;

    private readonly string confirm;

    private readonly Action onConfirm;

    public static bool restrictToPreviewedApparels;

    public static bool restrictToPreviewedStuffs;

    public static bool forceRegister;

    private static readonly Vector2 ButtonSize = new(120f, 32f);
}