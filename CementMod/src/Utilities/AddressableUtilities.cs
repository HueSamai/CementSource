using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Il2CppSystem.Linq;
using MelonLoader;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CementGB.Mod.Utilities;

public static class AddressableUtilities
{
    public static AssetReference CreateModdedAssetReference(string key)
    {
        var assetReference = new AssetReference(key);
        assetReference.m_AssetGUID = Guid.NewGuid().ToString() + assetReference.m_SubObjectName;
        return assetReference;
    }

    /*

    {
        "ContentType1": {
            "AddressableNames": [
                "addr_name",
                . . .
            ]
        },
        . . .
    }

    */
    public static ReadOnlyDictionary<string, Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>> PackAddressableKeys => new(_packAddressableKeys);
    private static readonly Dictionary<string, Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>> _packAddressableKeys = new();

    internal static void LoadCCCatalogs()
    {
        _packAddressableKeys.Clear();

        foreach (var dir in Directory.GetDirectories(Mod.customContentPath))
        {
            var catalogPath = Path.Combine(dir, "catalog.json");
            if (File.Exists(catalogPath))
            {
                var resourceLocatorHandle = Addressables.LoadContentCatalogAsync(catalogPath).Acquire();
                var addressablePackName = Path.GetDirectoryName(catalogPath);
                if (string.IsNullOrWhiteSpace(addressablePackName)) continue;

                resourceLocatorHandle.WaitForCompletion();
                if (resourceLocatorHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Melon<Mod>.Logger.Error($"Failed to load Addressable content catalog for \"{addressablePackName}\": ", resourceLocatorHandle.OperationException.ToString());
                    continue;
                }

                _packAddressableKeys.Add(addressablePackName, resourceLocatorHandle.Result.Keys.ToList());

                Melon<Mod>.Logger.Msg(ConsoleColor.Green, $"Content catalog for \"{addressablePackName}\" loaded OK");
                resourceLocatorHandle.Release();
            }
            else
            {
                Melon<Mod>.Logger.Warning($"No catalog found in directory \"{dir}\"");
            }
        }
    }
}