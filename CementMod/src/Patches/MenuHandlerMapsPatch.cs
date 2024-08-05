using CementGB.Mod.Modules;
using HarmonyLib;
using Il2CppGB.UI;

namespace CementGB.Mod.Patches;

public static class MenuHandlerMapsPatch
{
    [HarmonyPatch(typeof(MenuHandlerMaps), nameof(MenuHandlerMaps.UpdateMapList))]
    private static class UpdateMapList
    {
        private static void Postfix(MenuHandlerMaps __instance)
        {
            Il2CppSystem.Collections.Generic.List<string> customSceneNames = new();

            foreach (var scene in CustomScene.CustomScenes)
            {
                customSceneNames.Add(scene.name);
            }

            __instance.UpdateMapList(customSceneNames);
        }
    }

    // TODO: Implement localization of map names.
    [HarmonyPatch(typeof(MenuHandlerMaps), nameof(MenuHandlerMaps.UpdateText))]
    private static class UpdateText
    {
        private static void Postfix(MenuHandlerMaps __instance)
        {
            __instance.mapValueText.text = __instance.mapList[__instance.currentMapIndex];
            return; // Completely removes this functionality.
        }
    }
}