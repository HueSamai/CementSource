using UnityEngine;
using MelonLoader;
using Il2CppSystem.Net;
using System.Reflection.Metadata.Ecma335;

namespace Tungsten;

[RegisterTypeInIl2Cpp]
/// A Unity component for controlling a Tungsten script
public class ScriptBehaviour : MonoBehaviour
{
    public string scriptName = "";
    private Script _script = null;

    private bool _hasUpdate;
    public void OnScriptAdded()
    {
        if (scriptName == "")
        {
            return;
        }

        _script = Script.TryGetExisting(scriptName);
        if (_script == null) return;

        if (_script.HasFunction("awake"))
            _script.RunFunction("awake");

        _hasUpdate = _script.HasFunction("update");
    }
    
    private void Awake()
    {
        OnScriptAdded();
    }

    private void Start()
    {
        if (_script == null) return;
        
        if (_script.HasFunction("start"))
            _script.RunFunction("start");
    }

    private void Update()
    {
        if (_script == null) return;
        if (_hasUpdate)
            _script.RunFunction("update");
    }
}