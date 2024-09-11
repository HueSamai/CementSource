/* // caused build errors, not sure what to do so im disabling it for now
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using MelonLoader;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using UnityEngine.ResourceManagement.AsyncOperations;
using Il2CppGB.Core;
using Il2CppGB.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using System.IO;
using Il2CppGB.Data;
using System.Collections.Generic;

namespace NetBeard
{
    [HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.LoadString))]
    public class LoadStringPatch
    {
        public static void Postfix(string key, ref string __result)
        {
            MelonLogger.Msg(ConsoleColor.White, "LoadString Postfix called");

            if (__result == null)
            {
                if (!ExtendedStringLoader.items.ContainsKey(key))
                {
                    __result = key;
                    return;
                }
                __result = ExtendedStringLoader.items[key];
                return;
            }

            if (__result.StartsWith("No translation"))
            {
                if (!ExtendedStringLoader.items.ContainsKey(key))
                {
                    __result = key;
                    return;
                }
                __result = ExtendedStringLoader.items[key];
                return;
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.LoadRawString))]
    public class LoadRawStringPatch
    {
        public static void Postfix(string key, ref string __result)
        {
            MelonLogger.Msg(ConsoleColor.White, "LoadRawString Postfix called");

            if (__result == null)
            {
                if (!ExtendedStringLoader.items.ContainsKey(key))
                {
                    return;
                }
                __result = ExtendedStringLoader.items[key];
                return;
            }

            if (__result.StartsWith("No translation"))
            {
                if (!ExtendedStringLoader.items.ContainsKey(key))
                {
                    return;
                }
                __result = ExtendedStringLoader.items[key];
                return;
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.TryLoadStringByPlatform))]
    public class TryLoadStringPatch
    {
        public static void Postfix(ref string pulledString, string key, bool __result)
        {
            MelonLogger.Msg(ConsoleColor.White, "TryLoadString Postfix called");

            if (!__result)
            {
                if (!ExtendedStringLoader.items.ContainsKey(key))
                {
                    return;
                }
                pulledString = ExtendedStringLoader.items[key];
                __result = true;
            }
        }
    }
    public static class ExtendedStringLoader
    {
        public static Dictionary<string, string> items = new();
        public static void RegisterItem(string key, string value)
        {
            items[key] = value;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.LoadScene))]
    public class LoadScenePatch
    {
        public static bool Prefix(SceneLoader __instance, string key, LoadSceneMode loadMode, bool activateOnLoad, int priority, ref SceneLoadTask __result)
        {
            try
            {
                Melon<NetBeardPlugin>.Logger.Error("PATCH IS BEING CALLED!");

                if (__instance._sceneList[key] != null)
                {
                    return true;
                }

                MelonLogger.Error("CUSTOM SCENE LOADING!");

                AssetReference assetReference = new AssetReference(key + "-Data");
                assetReference.m_AssetGUID = Guid.NewGuid().ToString() + assetReference.m_SubObjectName;

                MelonLogger.Error("GUID: " + assetReference.m_AssetGUID);

                NetBeardPlugin.Log.Msg(ConsoleColor.Magenta, assetReference.RuntimeKeyIsValid());

                if (assetReference == null)
                {
                    MelonLogger.Error("oopsie! something went wrong!");
                    __result = null;
                    return false;
                }
                if (!assetReference.RuntimeKeyIsValid())
                {
                    MelonLogger.Error("[SCENELOAD] LoadScene: Failed validation | Key: {0} | Ref: {1}", new object[]
                    {
                        key,
                        assetReference != null
                    });
                    __result = null;
                    return false;
                }
                __instance._currentKey = key;
                __instance._currentScene = key.ToLower();
                SceneLoadTask sceneLoadTask = new SceneLoadTask((SceneLoadTask.OnLoaded)__instance.OnSceneLoaded, loadMode, activateOnLoad, priority);
                MelonLogger.Msg("[SCENELOAD] LoadScene: Start loading | Key: {0}", new object[]
                {
                    __instance._currentKey
                });

                sceneLoadTask._sceneDataRef = assetReference;
                var ethan = Addressables.LoadAssetAsync<SceneData>(key + "-Data");
                ethan.WaitForCompletion();
                sceneLoadTask._sceneData = ethan.Result;
                sceneLoadTask._sceneLoading = Addressables.LoadSceneAsync(key);
                sceneLoadTask._sceneLoading.WaitForCompletion();
                sceneLoadTask._sceneInstance = sceneLoadTask._sceneLoading.Result;
                if (sceneLoadTask._activateOnLoad)
                {
                    sceneLoadTask._status = AsyncOperationStatus.Succeeded;
                }
                __instance.OnSceneLoaded(sceneLoadTask);
                sceneLoadTask._dataLoading = null;

                Global.Instance.LevelLoadSystem.LoadingScreen.LoadTasks.Add(new ICompleteTracker(sceneLoadTask.Pointer));

                __result = sceneLoadTask;
                return false;
            }
            catch (Exception a)
            {
                MelonLogger.Error(a);
                return false;
            }
        }
    }

    [RegisterTypeInIl2Cpp]
    public class CustomMapManager : MonoBehaviour
    {
        private List<string> _customMaps = new();
        private bool _injectedCustomMaps = false;
        public void Awake()
        {
            SceneManager.sceneLoaded += (Action<Scene, LoadSceneMode>)OnSceneLoaded;
            LoadAddressables();
        }

        public void Update()
        {
            if (!_injectedCustomMaps)
                InjectCustomMaps();
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            _injectedCustomMaps = false;
        }

        public void InjectCustomMaps()
        {
            var gamemodesHandler = FindObjectOfType<MenuHandlerGamemodes>();
            if (gamemodesHandler == null)
            {
                return;
            }
            MenuHandlerMaps handler = gamemodesHandler.mapSetup;
            foreach (var map in _customMaps)
            {
                handler.mapList.Add(map);
            }
            _injectedCustomMaps = true;
        }

        public void LoadAddressables()
        {
            string mapsPath = Path.Combine(Application.dataPath, "Maps");
            foreach (var dir in Directory.GetDirectories(mapsPath))
            {
                string catalogPath = Path.Combine(dir, "catalog.json");
                if (File.Exists(catalogPath))
                {
                    AsyncOperationHandle<IResourceLocator> resourceLocator = Addressables.LoadContentCatalogAsync(catalogPath).Acquire();
                    resourceLocator.WaitForCompletion();
                    if (resourceLocator.Status == AsyncOperationStatus.Failed)
                    {
                        MelonLogger.Error("FAILED TO LOAD ADDRESSABLE FOR MAP: " + resourceLocator.OperationException.ToString());
                        continue;
                    }

                    string[] parts = dir.Replace("/", "\\").Split("\\");
                    string name = parts[parts.Length - 1];
                    ExtendedStringLoader.RegisterItem($"STAGE_{name.ToUpper()}", name);
                    _customMaps.Add(name);

                    MelonLogger.Msg(ConsoleColor.Green, $"LOADED IN ADDRESSABLE {name}");
                }
                else
                {
                    MelonLogger.Error("NO CATALOG FOUND FOR ADDRESSABLE!");
                }
            }
        }
    }
}
*/
