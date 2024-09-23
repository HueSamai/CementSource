using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppCoreNet.Config;
using Il2CppGB.Config;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.UnityServices.Matchmaking;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CementGB.Mod.Utilities;

[MelonLoader.RegisterTypeInIl2Cpp]
internal class ServerManager : MonoBehaviour
{
    bool menuHasLoadedPreviously;
    bool isServer;

    void Awake()
    {
        if (Environment.GetCommandLineArgs().Contains("-SERVER"))
        {
            isServer = true;
        }

        SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>(WrapperFix));
    }

    void WrapperFix(Scene scene, LoadSceneMode mode)
    {
        if (menuHasLoadedPreviously) return;
        if (scene.name == "Menu")
        {
            menuHasLoadedPreviously = true;

            gameObject.AddComponent<DevelopmentTestServer>().ui = GameObject.Find("Global(Clone)/UI/PlatformUI/Development Server Menu").GetComponent<DevelopmentTestServerUI>();

            // Launching from my server terminal gives the server arg which just mutes the game, hides the load level UI (it bugs out) and attempts to
            // initialize integral coatsink wrappers
            if (isServer)
            {
                NetworkBootstrapper.IsDedicatedServer = true;
                FindObjectOfType<NetworkBootstrapper>().AutoRunServer = true;

                UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer, null, "", "DGS");
                // AudioListener.pause = true;
                GameObject.Find("Global(Clone)/LevelLoadSystem").SetActive(false);
            }
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(DevelopmentTestServer), nameof(DevelopmentTestServer.Awake))]
public static class CLAFix
{
    public static void Postfix()
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