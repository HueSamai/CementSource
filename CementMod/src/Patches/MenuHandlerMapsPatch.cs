using CementGB.Mod.Modules;
using HarmonyLib;
using Il2CppGB.UI;
using MelonLoader;
using System.Security.Cryptography;

namespace CementGB.Mod.Patches;

public static class MenuHandlerMapsPatch
{
    // TODO: Implement localization of map names.
    [HarmonyPatch(typeof(MenuHandlerMaps), nameof(MenuHandlerMaps.UpdateText))]
    private static class UpdateText
    {
        private static void Postfix(MenuHandlerMaps __instance)
        {
            __instance.mapValueText.text = __instance.mapList[__instance.currentMapIndex];
        }
    }
}