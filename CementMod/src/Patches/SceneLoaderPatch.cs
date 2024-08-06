using CementGB.Mod.Modules;
using HarmonyLib;
using Il2CppGB.Core.Loading;
using Il2CppGB.Game;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

# nullable disable

namespace CementGB.Mod.Patches;
public static class SceneLoaderPatch
{
    private static string lastCustomSceneName = null;

    [HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.LoadScene))]
    private static class LoadScene
    {
        private static bool Prefix(SceneLoader __instance, object[] __args)
        {
            try
            {
                if (!CustomScene.IsCustomSceneName((string)__args[0]))
                {
                    return true;
                }
                lastCustomSceneName = (string)__args[0];

                __instance.LoadScene("Grind");
            }
            catch (Exception e)
            {
                Melon<Mod>.Logger.Error(e);
                return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.OnSceneLoaded))]
    private static class OnSceneLoaded
    {
        private static void Postfix()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(lastCustomSceneName))
                {
                    Melon<Mod>.Logger.Msg("GRIND LOADED, REPLACING MAP OBJECTS");
                    // Gets rid of objects from grind
                    List<string> objectsToDestroy = new()
                    {
                        "World", "Void", "KillVolumes", "AI", "AchievementTrackers", "Plane", "Plane (1)", "Sounds",
                        "Sphere", "RoomLeavers", "CCTV Camera", "PlayerSpawns"
                    };

                    foreach (string gameObjectName in objectsToDestroy)
                    {
                        GameObject objectToDestroy = GameObject.Find(gameObjectName);
                        if (objectToDestroy != null)
                        {
                            UnityEngine.Object.Destroy(objectToDestroy);
                        }
                    }

                    foreach (var child in GameObject.Find("Lighting & Effects").GetComponentsInChildren<Transform>().Where(child => child.name != "Postprocessing Global Volume"))
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }

                    SceneManager.LoadSceneAsync(CustomScene.GetCustomScenesByName(lastCustomSceneName)[0].scenePath, LoadSceneMode.Additive).add_completed(new Action<AsyncOperation>((operation) =>
                    {
                        MelonCoroutines.Start((IEnumerator)AccessTools.Method(typeof(GameManagerNew), nameof(GameManagerNew.SpawnRoutine)).Invoke(GameManagerNew.Instance, Array.Empty<object>()));
                    }));
                    lastCustomSceneName = null;
                }
            }
            catch (Exception e)
            {
                Melon<Mod>.Logger.Error(e);
            }
        }
    }
}
