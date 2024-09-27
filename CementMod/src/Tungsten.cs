using UnityEngine;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Reflection;
using CementGB.Mod.Utilities;
using MoonSharp.Interpreter.Interop;
using Il2CppInterop.Runtime;
using UnityEngine.Rendering.Universal;
using Il2CppGB.UI.Utils;

namespace CementGB.Mod;

/// <summary>
/// Handles the loading and processing of lua scripts.
/// </summary>
public static class Tungsten
{
    private static Dictionary<string, string> _nameToScriptText = new();
    private static List<Script> _scripts = new();
    private static List<Script> _newScripts = new();

    private static Dictionary<GameObject, List<Script>> _gameObjectToScripts = new();

    private static bool _inititalised = false;
    /// <summary>
    /// Registers all types in every loaded assembly, so that they can be used in lua scripts.
    /// </summary>
    public static void Init()
    {
        if (_inititalised) return;
        _inititalised = true;

        UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
    }

    /// <summary>
    /// Extracts all the scripts for a scene made in the level editor, and stores them in a dictionary to be used.
    /// </summary>
    /// <param name="path">Path to a JSON file containing the lua scripts needed</param>
    public static void LoadScriptsFrom(string path)
    {
        _nameToScriptText.Clear();

        // TODO: maybe throw an error here.
        if (!File.Exists(path))
            return;

        string rawText = File.ReadAllText(path);
        Dictionary<string, string>? scriptNameToContents = JsonSerializer.Deserialize<Dictionary<string, string>>(rawText);

        // TODO: maybe also throw an error here.
        if (scriptNameToContents == null) return;

        foreach (string scriptName in scriptNameToContents.Keys)
        {
            _nameToScriptText[scriptName] = scriptNameToContents[scriptName];
        }
    }

    public static void InjectFieldsFor(Script script, Dictionary<string, object> fieldInjections)
    {
        foreach (string fieldName in fieldInjections.Keys)
            script.Globals[fieldName] = fieldInjections[fieldName];
    }

    private static Script? GetScriptCopyFromName(string name)
    {
        if (!_nameToScriptText.ContainsKey(name))
            return null;
        Script script = new();
        script.DoString(_nameToScriptText[name]);
        return script;
    }

    public static void TryCallMethodOnScript(Script script, string methodName)
    {
        try
        {
            object method = script.Globals[methodName];
            if (method == null)
                return;

            script.Call(method);
        }
        catch (Exception ex)
        {
            LoggingUtilities.VerboseLog($"Error when trying to call '{methodName}' for script: " + ex.ToString());
        }
    }

    public static int Print(string msg)
    {
        MelonLoader.Melon<Mod>.Logger.Msg(msg);
        return 0;
    }

    private static Dictionary<string, Il2CppSystem.Type?> _nameToTypeCache = new();
    public static Il2CppSystem.Type? GetType(string name)
    {
        if (_nameToTypeCache.ContainsKey(name))
            return _nameToTypeCache[name];

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (Type type in assembly.GetTypes())
                if (type.Name == name || type.FullName == name)
                {
                    var il2cppType = Il2CppType.From(type);
                    _nameToTypeCache[name] = il2cppType;
                    return il2cppType;
                }

        return null;
    }

    public static void AttachScriptToObject(string scriptName, GameObject gameObject, Dictionary<string, object>? fields = null)
    {
        Script? script = GetScriptCopyFromName(scriptName);
        if (script == null) return;

        _scripts.Add(script);
        _newScripts.Add(script);

        if (fields != null)
            InjectFieldsFor(script, fields);

        script.Globals["gameObject"] = gameObject;
        script.Globals["print"] = (Func<string, int>)Print;
        script.Globals["type"] = (Func<string, Il2CppSystem.Type?>)GetType;
        script.Globals["FindObjectOfType"] = (Func<Il2CppSystem.Type, UnityEngine.Object>)GameObject.FindObjectOfType;

        if (!_gameObjectToScripts.ContainsKey(gameObject))
            _gameObjectToScripts[gameObject] = new();

        _gameObjectToScripts[gameObject].Add(script);

        TryCallMethodOnScript(script, "awake");
    }

    public static void Update()
    {
        foreach (Script script in _newScripts)
            TryCallMethodOnScript(script, "start");
        _newScripts.Clear();

        foreach (Script script in _scripts)
        {
            TryCallMethodOnScript(script, "update");
        }
    }

}
