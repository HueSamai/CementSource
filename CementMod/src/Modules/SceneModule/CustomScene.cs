using Il2CppGB.Gamemodes;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CementGB.Mod.Modules;

// TODO: Reimplement functionality with less game-breaking patches lol

/// <summary>
/// SceneModule's master class. There may be more in the future. Entry point is <see cref="CustomScene(MelonMod, string, GameObject, GameModeEnum)"/>
/// </summary>
/// <remarks>CURRENTLY UNFINISHED.</remarks>
public class CustomScene
{
    /// <summary>
    /// Fired when <see cref="RegisterScene(CustomScene)"/> has fully completed.
    /// </summary>
    public static event Action<CustomScene>? OnRegister;
    /// <summary>
    /// Fired when the <see cref="CustomScene"/> has fully loaded. Useful for executing code after all objects from the original stage is destroyed and custom objects from the <c>sceneRoot</c> of the <see cref="CustomScene"/> are added.
    /// </summary>
    public static event Action<CustomScene>? OnLoad;

    /// <summary>
    /// A copied array of all the <see cref="CustomScene"/>s currently registered. Again, this produces a COPY of the original array used. This cannot be used to modify the original. Use <see cref="RegisterScene(CustomScene)"/> to add to this list!
    /// </summary>
    public static CustomScene[] CustomScenes => _customScenes.ToArray();
    private static readonly List<CustomScene> _customScenes = new();

    public static bool IsCustomSceneName(string name) => CustomScenes.Any(scene => scene.name == name);
    /// <summary>
    /// Tries to find an array of CustomScenes with a given name using <see cref="System.Linq"/>. Even though in most cases there will never be duplicate <see cref="CustomScene"/>s, We keep this an array to keep the one-liner concise. (SUBJECT TO CHANGE)
    /// </summary>
    /// <param name="name">The name to look for.</param>
    /// <returns>An array of CustomScenes named <paramref name="name"/></returns>
    public static CustomScene[] GetCustomScenesOfName(string name) => CustomScenes.Where(scene => scene.name == name).ToArray();

    /// <summary>
    /// Registers a scene to be loaded into <see cref="CustomScenes"/> the next time Gang Beasts loads the map select menu.
    /// </summary>
    /// <remarks>This will throw an <see cref="Exception"/> if there is already a CustomScene that exists with the same name.</remarks>
    /// <param name="scene">The scene to register.</param>
    /// <exception cref="Exception"></exception>
    public static void RegisterScene(CustomScene scene)
    {
        if (GetCustomScenesOfName(scene.name).Length > 0)
            throw new Exception($"Scene of name {scene.name} already exists.");

        _customScenes.Add(scene);
        OnRegister?.Invoke(scene);
    }

    public readonly string name;
    public readonly GameModeEnum gameMode;
    public readonly GameObject sceneRoot;
    public readonly MelonMod ownerMod;

    /// <summary>
    /// The entry point to the SceneModule. 
    /// This creates a <see cref="CustomScene"/> instance which can then be registered into the game using <see cref="RegisterScene(CustomScene)"/>.
    /// </summary>
    /// <param name="ownerMod">The mod that owns this CustomScene.</param>
    /// <param name="name">The name of the scene. This must be UNIQUE.</param>
    /// <param name="sceneRoot">The parent GameObject of everything in the <see cref="CustomScene"/>.</param>
    /// <param name="gameMode">The gamemodes this <see cref="CustomScene"/> should work for.</param>
    public CustomScene(MelonMod ownerMod, string name, GameObject sceneRoot, GameModeEnum gameMode = GameModeEnum.Melee)
    {
        this.ownerMod = ownerMod;
        this.name = name;
        this.sceneRoot = sceneRoot;
        this.gameMode = gameMode;
    }
}