using MaterialFilter;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Verse;

namespace ChooseYourOutfit.MaterialFilterPatch
{
    public class MaterialFilterWindowForApparel : MaterialFilterWindow
    {
        public MaterialFilterWindowForApparel(ThingFilter __filter, float __top, float __left, WindowLayer __layer) : base(__filter, __top, __left, __layer)
        {
            this.dialog = Find.WindowStack.WindowOfType<Dialog_ManageOutfitsEx>();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            this.DoWindowContents(this.windowRect); //DoWindowContents内でwindowRect.widthを変更しているようなので一回実行してからwidthを取得する
            offset = this.windowRect.width / 2;
            dialog.windowRect.x -= offset;
            this.windowRect.x -= offset;
        }

        public override void PostClose()
        {
            base.PostClose();
            dialog.windowRect.x += offset;
        }

        private Dialog_ManageOutfitsEx dialog;

        private float offset;
    }
}
