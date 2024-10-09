using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppCoreNet;
using Il2CppCS.CorePlatform;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Platform.Lobby;
using MelonLoader;
using System;
using System.Linq;
using UnityEngine;

namespace CementGB.Mod.Modules.NetBeard;

[RegisterTypeInIl2Cpp]
public class ServerManager : MonoBehaviour
{
    public const string DEFAULT_IP = "127.0.0.1";
    public const int DEFAULT_PORT = 5999;

    public static bool DontAutoStart => Environment.GetCommandLineArgs().Contains("-DONT-AUTOSTART");
    public static bool IsServer => Environment.GetCommandLineArgs().Contains("-Server");
    public static bool IsAutoJoiner => !IsServer && (!string.IsNullOrWhiteSpace(_ip) || !string.IsNullOrWhiteSpace(_port));
    public static string IP => string.IsNullOrWhiteSpace(_ip) ? DEFAULT_IP : _ip;
    public static int Port => string.IsNullOrWhiteSpace(_port) ? DEFAULT_PORT : int.Parse(_port);

    // This isn't necessary but I'll keep it because I'll probably need it at some point
    private static readonly string _ip = CommandLineParser.Instance.GetValueForKey("-ip", false);
    private static readonly string _port = CommandLineParser.Instance.GetValueForKey("-port", false);

    private void Awake()
    {
        PlatformEvents.add_OnPlatformInitializedEvent(new Action(() => ServerBoot()));

        Melon<Mod>.Logger.Msg("Setting up dedicated server overrides. . .");
        AudioListener.pause = IsServer;
        NetworkBootstrapper.IsDedicatedServer = IsServer;
        Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");
    }

    private void ServerBoot()
    {
        if (IsServer) UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer, null, "", "DGS");

        LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();
        FindObjectOfType<NetworkBootstrapper>().AutoRunServer = IsServer && !DontAutoStart;
        Melon<Mod>.Logger.Msg("Added DevelopmentTestServer.");

        if (IsServer)
        {
            GameObject.Find("Global(Clone)/LevelLoadSystem").SetActive(false);

            Melon<Mod>.Logger.Msg("Subscribing to server events. . .");
            NetworkManager.add_OnServerStarted(new Action(OnServerStarted));
            NetworkManager.add_OnClientConnected(new Action(OnClientConnected));
            NetworkManager.add_OnClientStopped(new Action(OnClientStopped));
            Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");
        }
    }

    private void OnClientStopped()
    {
        
    }

    private void OnClientConnected()
    {
        
    }

    private void OnServerStarted()
    {
        Melon<Mod>.Logger.Msg(ConsoleColor.Green, $"Server is ready on port {Port}!");
    }
}