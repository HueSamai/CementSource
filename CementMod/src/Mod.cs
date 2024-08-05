using CementGB.Mod.Utilities;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using UnityEngine;

namespace CementGB.Mod;

public static class BuildInfo
{
    public const string Name = "Cement";
    public const string Author = "HueSamai // dotpy";
    public const string Description = null;
    public const string Company = "CementGB";
    public const string Version = "4.0.0";
    public const string DownloadLink = "https://api.github.com/repos/HueSamai/CementSource/releases/latest";
}

public class Mod : MelonMod
{
    public static readonly string userDataPath = Path.Combine(MelonEnvironment.UserDataDirectory, "CementGB");

    public static bool DevMode 
    {
        get
        {
            return _devModeEntry.GetValueAsString() == "true";
        }
    }

    internal static GameObject CementCompContainer
    {
        get
        {
            if (_cementCompContainer == null)
            {
                _cementCompContainer = new GameObject("CMTSingletons");
            }
            return _cementCompContainer;
        }
        set
        {
            Object.Destroy(_cementCompContainer);
            _cementCompContainer = value;
        }
    }
    private static GameObject? _cementCompContainer;

    private static readonly MelonPreferences_Category _melonCat = MelonPreferences.CreateCategory("cement_prefs", "CementGB");
    private static readonly MelonPreferences_Entry _devModeEntry = _melonCat.CreateEntry("DevMode", false, "Developer Mode");

    internal AssetBundle CementAssetBundle
    {
        get
        {
            if (_cementAssetBundle == null)
            {
                _cementAssetBundle = AssetBundleUtilities.LoadEmbeddedAssetBundle(MelonAssembly.Assembly, "CementGB.Mod.Assets.cement.bundle");
            }
            return _cementAssetBundle;
        }
        set
        {
            if (_cementAssetBundle != value)
                Object.Destroy(_cementAssetBundle);
            _cementAssetBundle = value;
        }
    }
    private AssetBundle? _cementAssetBundle;

    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();

        FileStructure();
    }

    public override void OnLateInitializeMelon()
    {
        base.OnLateInitializeMelon();

        CreateCementComponents();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        base.OnSceneWasLoaded(buildIndex, sceneName);

        LoggerInstance.Msg($"Scene {sceneName} loaded.");
    }

    private static void FileStructure()
    {
        Directory.CreateDirectory(userDataPath);
        _melonCat.SetFilePath(Path.Combine(userDataPath, "CementPrefs.cfg"));
    }

    private static void CreateCementComponents()
    {
        CementCompContainer = new("CMTSingletons");
        Object.DontDestroyOnLoad(CementCompContainer);
        CementCompContainer.hideFlags = HideFlags.DontUnloadUnusedAsset;
    }
}