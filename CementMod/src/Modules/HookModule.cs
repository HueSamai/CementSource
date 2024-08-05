using System.Reflection;
using MelonLoader;
using HarmonyLib;
using System;

namespace CementGB.Mod.Modules;

/// <summary>
/// This is a really simple hooking library that uses Harmony, basically modular UltiLib.
/// It should be used to create custom functionality before or after vanilla methods.
/// </summary>
public static class HookModule
{
    /// <summary>
    /// A struct containing information required to create toggleable Harmony "hooks", or patches, with Cement.
    /// </summary>
    public struct CementHook
    {
        public MethodInfo original;
        public MethodInfo hook;
        public MelonMod callingMod;
        public bool isPrefix;

        public CementHook(MethodInfo original, MelonMod callingMod, MethodInfo hook, bool isPrefix) : this()
        {
            this.original = original;
            this.callingMod = callingMod;
            this.hook = hook;
            this.isPrefix = isPrefix;
        }
    }

    /// <summary>
    /// Create a hook (before or after depending on CementHook's <c>isPrefix</c> boolean) on a method that will toggle on and off with the passed MelonMod.
    /// </summary>
    /// <param name="hook">The <see cref="CementHook"/> info to patch with.</param>
    public static void CreateHook(CementHook hook, bool canToggle = true)
    {
        Func<bool> doBeforeHook = () =>
        {
            // TODO: if mod is disabled, disable hook as well
            return true;
        };

        HarmonyMethod? prefix = hook.isPrefix ? new HarmonyMethod(hook.hook) : null;
        HarmonyMethod? postfix = hook.isPrefix ? null : new HarmonyMethod(hook.hook);

        HarmonyMethod beforeEitherFix = new(doBeforeHook.Method);

        hook.callingMod.HarmonyInstance.Patch(hook.original, prefix, postfix);

        if (canToggle)
            hook.callingMod.HarmonyInstance.Patch(hook.hook, beforeEitherFix);

        Melon<Mod>.Logger.Msg($"New {(hook.isPrefix ? "PREFIX" : "POSTFIX")} hook on {hook.original.DeclaringType?.Name}.{hook.original.Name} to {hook.hook.DeclaringType?.Name}.{hook.hook.Name}");
    }
}