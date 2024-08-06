using CementGB.Mod.Testing;
using CementGB.Mod.Utilities;
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

    internal AssetBundle? TestAssetBundle
    {
        get
        {
            if (_testAssetBundle == null)
            {
                _testAssetBundle = AssetBundleUtilities.LoadEmbeddedAssetBundle(MelonAssembly.Assembly, "CementGB.Mod.Assets.testmap.bundle");
            }
            return _testAssetBundle;
        }
    }
    private AssetBundle? _testAssetBundle;

    internal string[] TestAssetBundleScenePaths => TestAssetBundle?.GetAllScenePaths();

    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();

        FileStructure();
    }

    public override void OnLateInitializeMelon()
    {
        base.OnLateInitializeMelon();

        CreateCementComponents();
        TestMap.Register();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        base.OnSceneWasLoaded(buildIndex, sceneName);

        LoggerInstance.Msg($"Scene {sceneName} loaded.");
    }

    private static void FileStructure()
    {
        Directory.CreateDirectory(userDataPath);
        //_melonCat.SetFilePath(Path.Combine(userDataPath, "CementPrefs.cfg"));
    }

    private static void CreateCementComponents()
    {
        CementCompContainer = new("CMTSingletons");
        Object.DontDestroyOnLoad(CementCompContainer);
        CementCompContainer.hideFlags = HideFlags.DontUnloadUnusedAsset;
    }
}