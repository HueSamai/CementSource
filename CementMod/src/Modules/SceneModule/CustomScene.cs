using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Il2CppGB.Gamemodes;
using MelonLoader;

namespace CementGB.Mod.Modules;

public class CustomScene
{
    public static event Action<CustomScene>? OnRegister;

    public static ImmutableArray<CustomScene> CustomScenes => _customScenes.ToImmutableArray();
    private static readonly List<CustomScene> _customScenes = new();

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