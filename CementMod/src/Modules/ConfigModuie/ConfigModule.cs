using CementGB.Mod.Modules.NetBeard;
using CementGB.Mod.src.Utilities;
using CementGB.Mod.Utilities;
using Il2CppInterop.Runtime.Injection;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace CementGB.Mod.src.Modules.ConfigModuie;

[MelonLoader.RegisterTypeInIl2Cpp]
public class ConfigModule : MonoBehaviour
{
    public static ConfigModule Instance
    {
        get;
        protected set;
    }

    /// <summary>
    /// Directory we store the configs
    /// </summary>
    public static string ConfigDirectory => Path.Combine(MelonEnvironment.GameRootDirectory, "Cement Configs");
    /// <summary>
    /// The extension for configs
    /// </summary>
    public const string CONFIG_EXTENSION = ".ccg";
    internal List<string> modConfigs = new();

    internal void Awake()
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
    public ModConfig CreateConfigInstance(string configID, string delimiter = "<c>")
    {
        if (!File.Exists(Path.Combine(ConfigDirectory, configID + CONFIG_EXTENSION)))
        {
            using (FileStream createFile = File.Create(Path.Combine(ConfigDirectory, configID + CONFIG_EXTENSION))) createFile.Dispose();
        }

        ModConfig newConfig = new ModConfig(configID, delimiter);
        return newConfig;
    }

    internal void ConstructConfigButton()
    {
        GameObject audioButton = GameObject.Find("Managers/Menu/Settings Menu/Canvas/Root Settings/Audio");
        GameObject configButton = Instantiate(audioButton, audioButton.transform.parent, true);
        configButton.name = "Mod Configurations";
        configButton.transform.SetSiblingIndex(audioButton.transform.GetSiblingIndex());
        configButton.transform.localPosition += Vector3.up * 65.0148f;

        audioButton.GetComponent<Button>().ReconstructNavigationByChildren();
        configButton.GetComponent<Button>().ReconstructNavigationByChildren();

        Destroy(configButton.GetComponent<LocalizeStringEvent>());
        configButton.GetComponent<TextMeshProUGUI>().text = "Mod Configurations";
    }
    
    internal void OnGUI()
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
    internal ModConfig(string configID, string delimiter)
    {
        ConfigID = configID;
        Delimiter = delimiter;

        using (StreamReader reader = File.OpenText(ToWriteTo))
        {
            string text = reader.ReadToEnd();
            string[] keysAndVals = text.Split(Delimiter);

            for (int i = 0; i < keysAndVals.Length; i++)
            {
                if (keysAndVals[i] == "") continue;

                string[] splittered = keysAndVals[i].Split(ValIdentifier);
                configVals.Add(splittered[0], splittered[1]);
            }
        }
    }

    internal string? ConfigID { get; set; }
    internal string? Delimiter { get; set; }
    internal string ValIdentifier => "}val{";
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
            int index = allText.IndexOf(key);

            allText.Replace(key + ValIdentifier + configVals[key], key + ValIdentifier + value);
            configVals[key] = value;

            using (StreamWriter writer = File.CreateText(ToWriteTo))
            {
                writer.Write(allText);
            }

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

            return default(T);
        }

        return default(T);
    }
}