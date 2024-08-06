using CementGB.Mod.Modules;
using HarmonyLib;
using Il2CppGB.Gamemodes;

namespace CementGB.Mod.Patches;
public static class GameModeMapTrackerPatch
{
    private static bool SceneNameAlreadyExists(GameModeMapTracker __instance, string sceneName)
    {
        foreach (var map in __instance.AvailableMaps)
        {
            if (map.MapName == sceneName) return true;
        }
        return false;
    }

    [HarmonyPatch(typeof(GameModeMapTracker), nameof(GameModeMapTracker.GetMapsFor))]
    private static class GetMapsFor
    {
        private static void Prefix(GameModeMapTracker __instance)
        {
            foreach (var scene in CustomScene.CustomScenes)
            {
                if (SceneNameAlreadyExists(__instance, scene.name)) continue;

                __instance.AvailableMaps.Add(new ModeMapStatus(scene.name, true)
                {
                    AllowedModesLocal = scene.gameMode
                });
            }
        }
    }
}
