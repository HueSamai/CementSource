using CementGB.Mod.Utilities;
using Il2CppGB.Data;
using System;

namespace CementGB.Mod.Patches;


[HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.LoadString))]
public class LoadStringPatch
{
    public static void Postfix(string key, ref string __result)
    {
        LoggingUtilities.VerboseLog("LoadString Postfix called");

        if (__result == null)
        {
            if (!ExtendedStringLoader.items.ContainsKey(key))
            {
                __result = key;
                return;
            }
            __result = ExtendedStringLoader.items[key];
            return;
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.LoadRawString))]
public class LoadRawStringPatch
{
    public static void Postfix(string key, ref string __result)
    {
        LoggingUtilities.VerboseLog("LoadRawString Postfix called");

        if (__result == null)
        {
            if (!ExtendedStringLoader.items.ContainsKey(key))
            {
                return;
            }
            __result = ExtendedStringLoader.items[key];
            return;
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.TryLoadStringByPlatform))]
public class TryLoadStringPatch
{
    public static void Postfix(ref string pulledString, string key, ref bool __result)
    {
        LoggingUtilities.VerboseLog("TryLoadString Postfix called");

        if (!__result)
        {
            if (!ExtendedStringLoader.items.ContainsKey(key))
            {
                return;
            }
            pulledString = ExtendedStringLoader.items[key];
            __result = true;
        }
    }
}
