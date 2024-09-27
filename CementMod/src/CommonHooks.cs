using Il2CppCoreNet.Components;
using MelonLoader;
using System;

namespace CementGB.Mod;

/// <summary>
/// Provides some useful shorthand hooks for certain in-game events.
/// </summary>
public static class CommonHooks
{
    /// <summary>
    /// Fired when the Menu scene loads for the first time in the app's lifespan. Will reset on application quit.
    /// </summary>
    public static event Action OnMenuFirstBoot;
    public static event Action OnGameSetup;
    public static event Action OnGameStart;
    public static event Action OnGameEnd;

    private static bool _menuFirstBoot;

    internal static void Initialize()
    {
        MelonEvents.OnSceneWasLoaded.Subscribe(OnSceneWasLoaded);
        OnMenuFirstBoot += Internal_MenuFirstBoot;
    }

    private static void Internal_MenuFirstBoot()
    {
        var netRoundOrganizer = NetRoundOrganiser.Instance;

        netRoundOrganizer.OnGameSetup.CombineImpl((Il2CppSystem.Action)OnGameSetup);
        netRoundOrganizer.OnGameStart.CombineImpl((Il2CppSystem.Action)OnGameStart);
        netRoundOrganizer.OnGameEnded.CombineImpl((Il2CppSystem.Action)OnGameEnd);
    }

    private static void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Menu" && !_menuFirstBoot)
        {
            _menuFirstBoot = true;
            OnMenuFirstBoot?.Invoke();
        }
    }
}