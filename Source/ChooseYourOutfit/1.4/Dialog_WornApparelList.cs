using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace ChooseYourOutfit
{
    public class Dialog_WornApparelList : Window
    {
        public Dialog_WornApparelList(Pawn pawn, Outfit outfit)
        {
            this.pawn = pawn;
            this.outfit = outfit;

            this.layer = WindowLayer.Dialog;
            this.closeOnClickedOutside = true;
            this.drawShadow = false;
            this.preventCameraMotion = false;
            SoundDefOf.FloatMenu_Open.PlayOneShotOnCamera(null);
        }
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(300f, Widgets.ListSeparatorHeight + pawn.apparel.WornApparelCount * itemHeight + 29f + Margin * 2);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var num = 0f;

            if (!Find.WindowStack.IsOpen<Dialog_ManageOutfitsEx>()) Find.WindowStack.TryRemove(this, true);

            Widgets.ListSeparator(ref num, inRect.width, "Apparel".Translate());
            foreach (Apparel apparel in from x in this.pawn.apparel.WornApparel
                                            //where !x.def.apparel.layers.Contains(ApparelLayerDefOf.Belt)
                                        select x into ap
                                        orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                        select ap)
            {
                Rect rect = new Rect(0f, num, Text.LineHeight, Text.LineHeight);
                Widgets.ThingIcon(rect, apparel);
                rect.x = rect.xMax + 6f;
                rect.xMax = inRect.xMax - Widgets.InfoCardButtonSize - 6f;
                Widgets.Label(rect, apparel.Label.Truncate(rect.width));
                Widgets.InfoCardButton(rect.xMax + 6f, num, apparel);
                if (Mouse.IsOver(rect))
                {
                    string text2 = apparel.LabelNoParenthesisCap.AsTipTitle() + GenLabel.LabelExtras(apparel, apparel.def.stackLimit, true, true) + "\n\n" + apparel.DescriptionDetailed;
                    if (apparel.def.useHitPoints)
                    {
                        text2 = string.Concat(new object[]
                        {
                        text2,
                        "\n",
                        apparel.HitPoints,
                        " / ",
                        apparel.MaxHitPoints
                        });
                    }
                    TooltipHandler.TipRegion(rect, text2);
                }
                num += itemHeight;
            }

            Rect buttonRect = new Rect(0f, num, inRect.width, 24f);
            if (Widgets.ButtonText(buttonRect, "CYO.WornApparels.ApplyToFilter".Translate()))
            {
                outfit.filter.SetDisallowAll();
                foreach (var apparel in this.pawn.apparel.WornApparel)
                {
                    outfit.filter.SetAllow(apparel.def, true);
                }
            }
            TooltipHandler.TipRegion(buttonRect, "CYO.Tip.ApplyToFilter".Translate());
        }

        protected override void SetInitialSizeAndPosition()
        {
            Vector2 initialSize = this.InitialSize;
            this.windowRect = new Rect(UI.MousePositionOnUIInverted, initialSize);
            this.windowRect = this.windowRect.Rounded();
        }

        private Pawn pawn;

        private Outfit outfit;

        private readonly float itemHeight = 28f;
    }
}
