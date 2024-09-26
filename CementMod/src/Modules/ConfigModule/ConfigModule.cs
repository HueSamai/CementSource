using CementGB.Mod.Utilities;
using Il2CppGB.Platform.Utils;
using Il2CppGB.UI.Menu;
using Il2CppGB.UI.Utils.Settings;
using Il2CppTMPro;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.UI;

namespace CementGB.Mod.Modules.ConfigModule;

[RegisterTypeInIl2Cpp]
public class ConfigModule : MonoBehaviour
{
    public static ConfigModule? Instance
    {
        get;
        private set;
    }

    /// <summary>
    /// Directory we store the configs
    /// </summary>
    public static readonly string ConfigDirectory = Path.Combine(Mod.userDataPath, "CMTConfigs");

    /// <summary>
    /// The extension for configs
    /// </summary>
    public const string CONFIG_EXTENSION = ".ccg";

    public static ReadOnlyCollection<string> ModConfigs => modConfigs.AsReadOnly();
    private static readonly List<string> modConfigs = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
        if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
    }

    /// <summary>
    /// Gets or creates a config instance that allows you to interact with a config
    /// </summary>
    /// <param name="configID">The ID to associate with your config. This should always be the same throughout your mod</param>
    /// <param name="delimiter">The delimiter is how we split each config setting. It's placed after a key and value to signify the next line will be a new config setting</param>
    /// <returns></returns>
    public static ModConfig CreateConfigInstance(string configID, string delimiter = "<c>")
    {
        if (!File.Exists(Path.Combine(ConfigDirectory, configID + CONFIG_EXTENSION)))
        {
            using FileStream createFile = File.Create(Path.Combine(ConfigDirectory, configID + CONFIG_EXTENSION)); createFile.Dispose();
        }

        ModConfig newConfig = new(configID, delimiter);
        return newConfig;
    }

    private static void ConstructConfigButton()
    {
        GameObject audioButton = GameObject.Find("Managers/Menu/Settings Menu/Canvas/Root Settings/Audio");
        GameObject graphicsButton = GameObject.Find("Managers/Menu/Settings Menu/Canvas/Root Settings/Graphics");
        GameObject inputButton = GameObject.Find("Managers/Menu/Settings Menu/Canvas/Root Settings/Input");
        GameObject configObject = Instantiate(audioButton, audioButton.transform.parent, true);

        for (int i = 2; i < audioButton.transform.parent.childCount - 1; i++)
        {
            audioButton.transform.parent.GetChild(i).localPosition -= Vector3.up * 65.0148f;
        }

        configObject.name = "Mod Configs";
        configObject.transform.SetSiblingIndex(inputButton.transform.GetSiblingIndex());
        configObject.transform.localPosition -= Vector3.up * 65.0148f * 2f;

        graphicsButton.GetComponent<Button>().ReconstructNavigationByChildren();
        inputButton.GetComponent<Button>().ReconstructNavigationByChildren();
        configObject.GetComponent<Button>().ReconstructNavigationByChildren();

        Destroy(configObject.GetComponent<LocalizeStringEvent>());
        configObject.GetComponent<TextMeshProUGUI>().text = configObject.name;

        var configButton = configObject.GetComponent<Button>();
        configButton.onClick.m_PersistentCalls.Clear();

        BaseMenuScreen configMenu = ConstructConfigMenu();

        configButton.onClick.AddListener(new Action(() =>
        {
            GameObject.Find("Managers/Menu").GetComponent<MenuController>().PushScreen(configMenu);
        }));
    }

    internal static BaseMenuScreen ConstructConfigMenu()
    {
        GameObject inputRoot = GameObject.Find("Managers/Menu/Settings Menu/Canvas/Input Root");
        GameObject configMenu = Instantiate(inputRoot, inputRoot.transform.parent, true);
        Button emptyButton = configMenu.transform.FindChild("Reset All").GetComponent<Button>();

        for (int i = 0; i < configMenu.transform.childCount; i++)
            if (configMenu.transform.GetChild(i).name != "Reset All") Destroy(configMenu.transform.GetChild(i).gameObject);

        Destroy(emptyButton.GetComponent<DisabledIfPlatform>());
        Destroy(emptyButton.GetComponent<LocalizeStringEvent>());
        Destroy(emptyButton.GetComponent<GameObjectLocalizer>());

        emptyButton.name = "Empty Button";
        emptyButton.GetComponent<TextMeshProUGUI>().text = emptyButton.name;
        emptyButton.onClick.RemoveAllListeners();

        Destroy(configMenu.GetComponent<InputOptions>());

        BaseMenuScreen menuScreen = configMenu.GetComponent<BaseMenuScreen>();
        menuScreen.defaultSelection = emptyButton;
        menuScreen.defaultSelectionFallback = emptyButton;
        menuScreen.cancelEvent.AddListener(new Action(() =>
        {
            GameObject.Find("Managers/Menu").GetComponent<MenuController>().PopScreen();
        }));

        return menuScreen;
    }

    private static void OnGUI()
    {
        if (GUILayout.Button("Construct config button"))
        {
            ConstructConfigButton();
        }
    }
}

/// <summary>
/// The base class for interacting with your mod config
/// </summary>
public class ModConfig
{
    public ModConfig(string configID, string delimiter)
    {
        ConfigID = configID;
        Delimiter = delimiter;

        using StreamReader reader = File.OpenText(ToWriteTo);
        string text = reader.ReadToEnd();
        string[] keysAndVals = text.Split(Delimiter);

        for (int i = 0; i < keysAndVals.Length; i++)
        {
            if (keysAndVals[i] == "") continue;

            string[] splittered = keysAndVals[i].Split(ValIdentifier);
            configVals.Add(splittered[0], splittered[1]);
        }
    }

    internal string? ConfigID { get; set; }
    internal string? Delimiter { get; set; }
    internal static readonly string ValIdentifier = "}val{";
    internal string ToWriteTo => Path.Combine(ConfigModule.ConfigDirectory, ConfigID + ConfigModule.CONFIG_EXTENSION);

    internal Dictionary<string, string> configVals = new();

    /// <summary>
    /// Set or create a value
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetValue(string key, string value)
    {
        if (configVals.ContainsKey(key))
        {
            string allText = File.ReadAllText(ToWriteTo);

            allText = allText.Replace(key + ValIdentifier + configVals[key], key + ValIdentifier + value);
            configVals[key] = value;

            using StreamWriter writer = File.CreateText(ToWriteTo);
            writer.Write(allText);

            return;
        }

        using (StreamWriter writer = File.AppendText(ToWriteTo))
        {
            configVals.Add(key, value);
            writer.Write(key + ValIdentifier + value + Delimiter);
            writer.Close();
        }
    }

    /// <summary>
    /// Returns the value from the config. Please ONLY pass an int, bool or float
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public T? GetValue<T>(string key)
    {
        if (configVals.ContainsKey(key))
        {
            if (typeof(T) == typeof(bool)) return (T)(object)bool.Parse(configVals[key]);
            if (typeof(T) == typeof(float)) return (T)(object)float.Parse(configVals[key]);
            if (typeof(T) == typeof(int)) return (T)(object)int.Parse(configVals[key]);

            return default;
        }

        return default;
    }
}
