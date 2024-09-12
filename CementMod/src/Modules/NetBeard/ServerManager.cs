using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppCoreNet.Config;
using Il2CppGB.Config;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CementGB.Mod.Modules.NetBeard;

[MelonLoader.RegisterTypeInIl2Cpp]
public class ServerManager : MonoBehaviour
{
    private static bool menuHasLoadedPreviously;
    private static bool isServer;

    void Awake()
    {
        if (Environment.GetCommandLineArgs().Contains("-SERVER"))
            isServer = true;

        SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>(ServerBoot));
    }

    void ServerBoot(Scene scene, LoadSceneMode mode)
    {
        if (menuHasLoadedPreviously) return;
        if (scene.name == "Menu")
        {
            menuHasLoadedPreviously = true;

            gameObject.AddComponent<DevelopmentTestServer>().ui = GameObject.Find("Global(Clone)/UI/PlatformUI/Development Server Menu").GetComponent<DevelopmentTestServerUI>();

            // Launching from my server terminal gives the server arg which just mutes the game, hides the load level UI (it bugs out) and attempts to
            // initialize integral coatsink wrappers
            if (Environment.GetCommandLineArgs().Contains("-SERVER"))
                if (isServer)
                {
                    UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer, null, "", "DGS");
                    AudioListener.pause = true;
                    GameObject.Find("Global(Clone)/LevelLoadSystem").SetActive(false);
                }
        }
    }

    void OnGUI()
    {
        /*            
        if (GUILayout.Button("Refresh configs"))
        {
            UpdateConfigs();
        }

        if (GUILayout.Button("Host"))
        {
            MonoSingleton<Global>.Instance.UNetManager.LaunchServer(serverConfig);
        }
        */
    }
}

[HarmonyLib.HarmonyPatch(typeof(DevelopmentTestServer), nameof(DevelopmentTestServer.SetupLocalServer))]
public static class CLAFix
{
    public static void Postfix(DevelopmentTestServer __instance, RotationConfig gameConfig, ServerConfig serverConfig)
    {
        string valueForKey = CommandLineParser.Instance.GetValueForKey("-DDC_IP", true);
        string valueForKey2 = CommandLineParser.Instance.GetValueForKey("-DDC_PORT", true);
        if (!string.IsNullOrEmpty(valueForKey2))
        {
            DevelopmentTestServer.DirectConnectPort = int.Parse(valueForKey2);
        }
        if (!string.IsNullOrEmpty(valueForKey))
        {
            DevelopmentTestServer.DirectConnectIP = valueForKey;
        }
    }
}