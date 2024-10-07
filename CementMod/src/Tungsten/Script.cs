using MelonLoader;
using System.Collections.Generic;

namespace Tungsten;
public class Script
{
    public bool faultyScript { get; private set; }
    public string name { get; private set; }
    private ProgramInfo programInfo;

    private static Dictionary<string, ProgramInfo> scriptNameToProgramInfo = new();
    
    public Script(string name, string code)
    {
        this.name = name;

        if (scriptNameToProgramInfo.ContainsKey(name))
        {
            programInfo = scriptNameToProgramInfo[name] != null ? scriptNameToProgramInfo[name].Clone() : null;
        } 
        else
        {
            Parser parser = new(code);
            programInfo = parser.Parse();
            scriptNameToProgramInfo[name] = programInfo;

            this.name = name;
            faultyScript = programInfo == null ? true : programInfo.errorManager.HadError;
        }

        if (programInfo != null)
            RunFunction("__regglobal");
    }

    public static Script TryGetExisting(string name)
    {
        if (scriptNameToProgramInfo.ContainsKey(name))
        {
            return new Script(name, "");
        }
        return null;
    }

    public bool HasFunction(string functionName)
    {
        return programInfo.functions.ContainsKey(functionName);
    }

    public int GetFunctionArity(string functionName)
    {
        if (!HasFunction(functionName)) return -1;
        return programInfo.functions[functionName].arity;
    }

    public object? RunFunction(string functionName, params object?[] args)
    {
        if (programInfo == null)
        {
            MelonLogger.Error("Script doesn't have a program loaded.");
            return null;
        }

        if (faultyScript)
        {
            ((ProgramInfo)programInfo).errorManager.RaiseRuntimeError(-1, "Cannot run faulty script.");
            return null;
        }

        return VM.RunFunction((ProgramInfo)programInfo, functionName, args);
    }
}
