using System;
using MelonLoader;

namespace CementGB.Mod;

public static class CommonHooks
{
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