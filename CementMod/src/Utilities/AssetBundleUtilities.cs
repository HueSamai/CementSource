using MelonLoader;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CementGB.Mod.Utilities;

public static class AssetBundleUtilities
{
    public static T? LoadPersistentAsset<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object
    {
        var asset = bundle.LoadAsset(name);

        if (asset != null)
        {
            asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return asset.TryCast<T>();
        }

        return null;
    }

    public static void LoadPersistentAssetAsync<T>(this AssetBundle bundle, string name, Action<T>? onLoaded) where T : UnityEngine.Object
    {
        var request = bundle.LoadAssetAsync<T>(name);

        request.add_completed((Il2CppSystem.Action<AsyncOperation>)((a) =>
        {
            if (request.asset == null) return;
            var result = request.asset.TryCast<T>();
            if (result == null) return;
            result.hideFlags = HideFlags.DontUnloadUnusedAsset;
            onLoaded?.Invoke(result);
        }));
    }

    public static AssetBundle LoadEmbeddedAssetBundle(Assembly assembly, string name)
    {
        if (assembly.GetManifestResourceNames().Contains(name))
        {
            Melon<Mod>.Logger.Msg($"Loading stream for resource '{name}' embedded from assembly...");
            using var str = assembly.GetManifestResourceStream(name) ?? throw new Exception("Resource stream returned null. This could mean an inaccessible resource caller-side or an invalid argument was passed.");
            using var memoryStream = new MemoryStream();
            str.CopyTo(memoryStream);
            Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");
            byte[] resource = memoryStream.ToArray();

            Melon<Mod>.Logger.Msg($"Loading assetBundle from data '{name}', please be patient...");
            var bundle = AssetBundle.LoadFromMemory(resource);
            Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");
            return bundle;
        }
        throw new Exception($"No resources matching the name '{name}' were found in the assembly '{assembly.FullName}'. Please ensure you passed the correct name.");
    }
}
