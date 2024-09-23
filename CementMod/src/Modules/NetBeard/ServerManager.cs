using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppCoreNet;
using Il2CppGB.Platform.Lobby;
using MelonLoader;
using System;
using System.Linq;
using UnityEngine;

namespace CementGB.Mod.Modules.NetBeard;

[RegisterTypeInIl2Cpp]
public class ServerManager : MonoBehaviour
{
    public ServerManager(IntPtr ptr) : base(ptr) { }

    public static bool DontAutoStart => Environment.GetCommandLineArgs().Contains("--DONT-AUTOSTART");
    public static bool IsDedicated => Environment.GetCommandLineArgs().Contains("--DEDICATED-SERVER");
    //public static bool IsP2P => Environment.GetCommandLineArgs().Contains("--P2P-SERVER"); // TODO: Implement a P2P solution for servers.
    public static string IP => CommandLineParser.Instance.GetValueForKey("--DDC_IP", true);
    public static string Port => CommandLineParser.Instance.GetValueForKey("--DDC_PORT", true);

    private void Awake()
    {
        CommonHooks.OnMenuFirstBoot += ServerBoot;
        AudioListener.pause = IsDedicated;

        /*
            if (IsDedicated && IsP2P)
            {
                Melon<Mod>.Logger.Error("Server cannot be both dedicated and P2P; quitting. . .");
                Application.Quit();
            }
        */
    }

    private static void ServerBoot()
    {
        LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();

        if (IsDedicated)
        {
            Melon<Mod>.Logger.Msg("Setting up dedicated server overrides and initializers. . .");
            UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer | UnityServicesManager.InitialiseFlags.GameClient, null, "", "DGS");
            GameObject.Find("Global(Clone)/LevelLoadSystem").SetActive(false);
            Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");

            Melon<Mod>.Logger.Msg("Subscribing to server events. . .");
            NetworkManager.add_OnServerStarted(new Action(OnServerStarted));
            NetworkManager.add_OnClientConnected(new Action(OnClientConnected));
            NetworkManager.add_OnClientStopped(new Action(OnClientStopped));
            Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");

            Melon<Mod>.Logger.Msg("Populating direct connect credentials. . ."); // TODO: make this set the port of the server
            DevelopmentTestServer.DirectConnectIP = IP;
            DevelopmentTestServer.DirectConnectPort = int.Parse(Port);
            Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");

            Melon<Mod>.Logger.Msg(ConsoleColor.Green, $"Starting dedicated server on {IP}:{Port}. . .");
            DevelopmentTestServer.SetupLocalServer();
        }
        /*
        else if (IsP2P)
        {
            throw new NotImplementedException("P2P servers are not currently available.");
        }
        */
    }

    private static void OnServerStarted()
    {
        Melon<Mod>.Logger.Msg(ConsoleColor.Green, $"Server started at {IP}:{Port}");
    }

    private static void OnClientConnected()
    {
        // TODO: display log messages detailing the client that connected
    }

    private static void OnClientStopped()
    {
        // TODO: display log messages detailing the client that left/stopped
    }
}