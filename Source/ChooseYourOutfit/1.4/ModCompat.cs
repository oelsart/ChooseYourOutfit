using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ChooseYourOutfit
{
    public static class ModCompat
    {
        public static class ProstheticNoMissingBodyParts
        {
            public static bool Active = ModsConfig.IsActive("Mlie.ProstheticNoMissingBodyParts");

            private static Mod Mod;

            private static ModSettings Settings;

            private static AccessTools.FieldRef<ModSettings, List<string>> ArmsWhitelist;

            private static AccessTools.FieldRef<ModSettings, List<string>> FeetWhitelist;

            private static AccessTools.FieldRef<ModSettings, List<string>> HandsWhitelist;

            private static AccessTools.FieldRef<ModSettings, List<string>> LegsWhitelist;

            static ProstheticNoMissingBodyParts()
            {
                if (Active)
                {
                    Mod = (Mod)AccessTools.Field("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsMod:_mod")?.GetValue(null);
                    if (Mod == null)
                    {
                        Log.Warning("[ChooseYourOutfit] Not found: ProstheticNoMissingBodyPartsMod");
                        return;
                    }
                    Settings = (ModSettings)AccessTools.Field("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsMod:_settings")?.GetValue(Mod);
                    if (Settings == null)
                    {
                        Log.Warning("[ChooseYourOutfit] Not found: ProstheticNoMissingBodyPartsSettings");
                        return;
                    }
                    var t_ProstheticNoMissingBodyPartsSettings = AccessTools.TypeByName("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsSettings");
                    ArmsWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "ArmsWhitelist");
                    FeetWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "FeetWhitelist");
                    HandsWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "HandsWhitelist");
                    LegsWhitelist = AccessTools.FieldRefAccess<List<string>>(t_ProstheticNoMissingBodyPartsSettings, "LegsWhitelist");
                }
            }

            public static IEnumerable<string> GetWhitelist
            {
                get
                {
                    return ArmsWhitelist(Settings)
                        .Concat(FeetWhitelist(Settings))
                        .Concat(HandsWhitelist(Settings))
                        .Concat(LegsWhitelist(Settings))
                        .Where(s => s != null);
                }
            }
        }
    }
}
