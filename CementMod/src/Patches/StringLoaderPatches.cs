using Il2CppGB.Data;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CementGB.Mod.Patches;

[HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.LoadString))]
public class LoadStringPatch
{
    public static void Postfix(string key, ref string __result)
    {
        MelonLogger.Msg(ConsoleColor.White, "LoadString Postfix called");

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

        if (__result.StartsWith("No translation"))
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
public class LoadRawStringPatch {
    public static void Postfix(string key, ref string __result) {
        MelonLogger.Msg(ConsoleColor.White, "LoadRawString Postfix called");

        if (__result == null) { 
            if (!ExtendedStringLoader.items.ContainsKey(key)) {
                return;
            }
            __result = ExtendedStringLoader.items[key];
            return;
        }

        if (__result.StartsWith("No translation")) {
            if (!ExtendedStringLoader.items.ContainsKey(key)) {
                return;
            }
            __result = ExtendedStringLoader.items[key];
            return;
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.TryLoadStringByPlatform))]
public class TryLoadStringPatch {
    public static void Postfix(ref string pulledString, string key, bool __result) {
        MelonLogger.Msg(ConsoleColor.White, "TryLoadString Postfix called");

        if (!__result) { 
            if (!ExtendedStringLoader.items.ContainsKey(key)) {
                return;
            }
            pulledString = ExtendedStringLoader.items[key];
            __result = true;
        }
    }
}
