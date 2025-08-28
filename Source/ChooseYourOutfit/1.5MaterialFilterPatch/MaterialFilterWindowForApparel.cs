using MaterialFilter;
using Verse;

namespace ChooseYourOutfit.MaterialFilterPatch
{
    public class MaterialFilterWindowForApparel : MaterialFilterWindow
    {
        public MaterialFilterWindowForApparel(ThingFilter __filter, float __top, float __left, WindowLayer __layer) : base(__filter, __top, __left, __layer)
        {
            dialog = Find.WindowStack.WindowOfType<Dialog_ManageApparelPoliciesEx>();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            DoWindowContents(windowRect); //DoWindowContents内でwindowRect.widthを変更しているようなので一回実行してからwidthを取得する
            offset = windowRect.width / 2;
            dialog.windowRect.x -= offset;
            windowRect.x -= offset;
        }

        public override void PostClose()
        {
            base.PostClose();
            dialog.windowRect.x += offset;
        }

        private Dialog_ManageApparelPoliciesEx dialog;

        private float offset;
    }
}
