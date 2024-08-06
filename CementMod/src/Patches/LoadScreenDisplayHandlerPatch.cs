using AssetsTools.NET;
using CementGB.Mod.Modules;
using CementGB.Mod.Utilities;
using HarmonyLib;
using Il2CppGB.Core.Loading;
using Il2CppTMPro;
using System.Collections.Generic;

namespace CementGB.Mod.Patches;
public static class LoadScreenDisplayHandlerPatch
{
    [HarmonyPatch(typeof(LoadScreenDisplayHandler), nameof(LoadScreenDisplayHandler.SetSubTitle))]
    private static class SetSubTitle
    {
        private static bool Prefix(LoadScreenDisplayHandler __instance, object[] __args)
        {
            if (!CustomScene.IsCustomSceneName((string)__args[0])) return true;

            var text = __instance._subTitle.GetComponent<TextMeshProUGUI>();
            UnityEngine.Object.Destroy(__instance._subTitle);
            text.text = (string)__args[0];

            return false;
        }
    }
}
