using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace Fog_of_war
{
    [HarmonyPatch(typeof(NMapLegendItem), "OnFocus")]
    class LegendOnFocusPatch
    {
        
        static bool Prefix()
        {
            return false;
        }
    }
    [HarmonyPatch(typeof(NMapLegendItem), "OnUnfocus")]
    class LegendOnUnfocusPatch
    {

        static bool Prefix()
        {
            return false;
        }
    }
}
