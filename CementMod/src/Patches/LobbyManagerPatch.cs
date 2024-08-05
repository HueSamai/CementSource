using HarmonyLib;
using Il2Cpp;
using Il2CppGB.Platform.Lobby;
using MelonLoader;

namespace CementGB.Mod.Patches;
public static class LobbyManagerPatch
{
    [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.OnPlatformInitializedEvent))]
    private static class OnPlatformInitializedEvent
    {
        private static void Postfix(LobbyManager __instance)
        {
            if (Mod.DevMode)
                __instance.LobbyObject.AddComponent<DevelopmentTestServer>();
        }
    }
}
