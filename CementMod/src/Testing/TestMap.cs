using CementGB.Mod.Modules;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace CementGB.Mod.Testing;
public static class TestMap
{
    public static void Register()
    {
        CustomScene.RegisterScene(new CustomScene(Melon<Mod>.Instance, "TestMap", Melon<Mod>.Instance.TestAssetBundleScenePaths[0]));
    }
}
