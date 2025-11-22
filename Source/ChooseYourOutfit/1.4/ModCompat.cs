using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ChooseYourOutfit;

public static class ModCompat
{
    public static class ProstheticNoMissingBodyParts
    {
        public static readonly bool Active = ModsConfig.IsActive("Mlie.ProstheticNoMissingBodyParts");

        private static readonly ModSettings Settings;

        private static readonly AccessTools.FieldRef<ModSettings, List<string>> ArmsWhitelist;

        private static readonly AccessTools.FieldRef<ModSettings, List<string>> FeetWhitelist;

        private static readonly AccessTools.FieldRef<ModSettings, List<string>> HandsWhitelist;

        private static readonly AccessTools.FieldRef<ModSettings, List<string>> LegsWhitelist;

        static ProstheticNoMissingBodyParts()
        {
            if (Active)
            {
                var mod = (Mod)AccessTools.Field("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsMod:_mod")?.GetValue(null);
                if (mod == null)
                {
                    Log.Warning("[ChooseYourOutfit] Not found: ProstheticNoMissingBodyPartsMod");
                    return;
                }
                Settings = (ModSettings)AccessTools.Field("ProstheticNoMissingBodyParts.ProstheticNoMissingBodyPartsMod:_settings")?.GetValue(mod);
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

        public static IEnumerable<string> GetWhitelist =>
            ArmsWhitelist(Settings)
                .Concat(FeetWhitelist(Settings))
                .Concat(HandsWhitelist(Settings))
                .Concat(LegsWhitelist(Settings))
                .Where(s => s != null);
    }
}