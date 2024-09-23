using System;
using MelonLoader;

namespace CementGB.Mod;

/// <summary>
/// Provides some useful hooks for certain in-game events.
/// </summary>
public static class CommonHooks
{
    /// <summary>
    /// Fired when the Menu scene loads for the first time in the app's lifespan. Will reset on application quit.
    /// </summary>
    public static event Action? OnMenuFirstBoot;

    private static bool _menuFirstBoot;

    internal static void Initialize()
    {
        MelonEvents.OnSceneWasLoaded.Subscribe(OnSceneWasLoaded);
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