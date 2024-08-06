using Il2CppGB.Gamemodes;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CementGB.Mod.Modules;

public class CustomScene
{
    public static event Action<CustomScene>? OnRegister;

    public static CustomScene[] CustomScenes => _customScenes.ToArray();
    private static readonly List<CustomScene> _customScenes = new();

    public static bool IsCustomSceneName(string name) => CustomScenes.Any(scene => scene.name == name);
    public static CustomScene[] GetCustomScenesByName(string name) => CustomScenes.Where(scene => scene.name == name).ToArray();

    public static void RegisterScene(CustomScene scene)
    {
        _customScenes.Add(scene);
        OnRegister?.Invoke(scene);
    }

    public event Action? OnLoad;

    public readonly string name;
    public readonly GameModeEnum gameMode;
    public readonly string scenePath;
    public readonly MelonMod ownerMod;

    public CustomScene(MelonMod ownerMod, string name, string scenePath, GameModeEnum gameMode = GameModeEnum.Melee)
    {
        this.ownerMod = ownerMod;
        this.name = name;
        this.scenePath = scenePath;
        this.gameMode = gameMode;
    }
}