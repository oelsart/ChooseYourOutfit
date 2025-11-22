using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ChooseYourOutfit;

public class Dialog_WornApparelList : Window
{
    public Dialog_WornApparelList(Dialog_ManageOutfitsEx dialog, Pawn pawn, Outfit outfit)
    {
        this.dialog = dialog;
        this.pawn = pawn;
        this.outfit = outfit;

        layer = WindowLayer.Dialog;
        closeOnClickedOutside = true;
        drawShadow = false;
        preventCameraMotion = false;
        SoundDefOf.FloatMenu_Open.PlayOneShotOnCamera();
    }
    public override Vector2 InitialSize => new(300f, Widgets.ListSeparatorHeight + pawn.apparel.WornApparelCount * itemHeight + 29f + Margin * 2);

    public override void DoWindowContents(Rect inRect)
    {
        var num = 0f;

        if (!Find.WindowStack.IsOpen<Dialog_ManageOutfitsEx>()) Find.WindowStack.TryRemove(this);

        Widgets.ListSeparator(ref num, inRect.width, "Apparel".Translate());
        foreach (var apparel in from x in pawn.apparel.WornApparel
                 //where !x.def.apparel.layers.Contains(ApparelLayerDefOf.Belt)
                 select x into ap
                 orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                 select ap)
        {
            Rect rect = new(0f, num, Text.LineHeight, Text.LineHeight);
            Widgets.ThingIcon(rect, apparel);
            rect.x = rect.xMax + 6f;
            rect.xMax = inRect.xMax - Widgets.InfoCardButtonSize - 6f;
            if (dialog.SelectedApparels.Contains(apparel.def)) Widgets.DrawHighlightSelected(rect);
            Widgets.Label(rect, apparel.Label.Truncate(rect.width));
            Widgets.InfoCardButton(rect.xMax + 6f, num, apparel);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                if (Input.GetMouseButtonUp(0))
                {
                    Input.ResetInputAxes();
                    dialog.SelectApparel(apparel.def);
                }
                var text2 = apparel.LabelNoParenthesisCap.AsTipTitle() + GenLabel.LabelExtras(apparel, apparel.def.stackLimit, true, true) + "\n\n" + apparel.DescriptionDetailed;
                if (apparel.def.useHitPoints)
                {
                    text2 = string.Concat(text2, "\n", apparel.HitPoints, " / ", apparel.MaxHitPoints);
                }
                TooltipHandler.TipRegion(rect, text2);
            }
            num += itemHeight;
        }

        Rect buttonRect = new(0f, num, inRect.width, 24f);
        if (Widgets.ButtonText(buttonRect, "CYO.WornApparels.ApplyToFilter".Translate()))
        {
            outfit.filter.SetDisallowAll();
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                outfit.filter.SetAllow(apparel.def, true);
            }
        }
        TooltipHandler.TipRegion(buttonRect, "CYO.Tip.ApplyToFilter".Translate());
    }

    protected override void SetInitialSizeAndPosition()
    {
        var initialSize = InitialSize;
        windowRect = new Rect(UI.MousePositionOnUIInverted, initialSize);
        windowRect = windowRect.Rounded();
    }

    private readonly Dialog_ManageOutfitsEx dialog;

    private readonly Pawn pawn;

    private readonly Outfit outfit;

    private readonly float itemHeight = 28f;
}