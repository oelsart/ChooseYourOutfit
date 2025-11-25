using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ChooseYourOutfit;

public class FloatMenuOptionProvider_ApparelFilter : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool CanSelfTarget => false;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (ChooseYourOutfit.settings.addFroatMenu && clickedThing.def.IsApparel)
        {
            var pawn = context.FirstSelectedPawn;
            if (pawn?.outfits?.CurrentApparelPolicy?.filter is null) yield break;
            
            var apparel = clickedThing.def;
            var allows = pawn.outfits.CurrentApparelPolicy.filter.Allows(apparel);
            yield return new FloatMenuOption(
                allows
                    ? "CYO.RemoveApparelFromFilter".Translate(apparel.label, pawn.outfits.CurrentApparelPolicy.label)
                    : "CYO.AddApparelToFilter".Translate(apparel.label, pawn.outfits.CurrentApparelPolicy.label),
                () => pawn.outfits.CurrentApparelPolicy.filter.SetAllow(apparel, !allows));
        }
    }
}