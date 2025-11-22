using MaterialFilter;
using Verse;

namespace ChooseYourOutfit.MaterialFilterPatch;

public class MaterialFilterWindowForApparel(ThingFilter filter, float top, float left, WindowLayer layer)
    : MaterialFilterWindow(filter, top, left, layer)
{
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

    private readonly Dialog_ManageApparelPoliciesEx dialog = Find.WindowStack.WindowOfType<Dialog_ManageApparelPoliciesEx>();

    private float offset;
}