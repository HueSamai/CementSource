using HarmonyLib;
using Il2CppGB.UI;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CementGB.Mod.Patches;
public static class SelectablePatch
{
    [HarmonyPatch(typeof(Selectable), nameof(Selectable.Awake))]
    private static class Awake
    {
        private static void Postfix(Selectable __instance)
        {
            var button = __instance.TryCast<Button>();
            if (button == null) return;

            if (__instance.name == "Online")
            {
                UnityEngine.Object.Destroy(__instance.gameObject);
                //TODO: Fix navigation after destroying Online button
            }
        }
    }
}
