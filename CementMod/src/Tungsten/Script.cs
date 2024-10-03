using MelonLoader;

namespace Tungsten;
public class Script
{
    public bool faultyScript { get; private set; }
    private ProgramInfo? programInfo;

    public Script(string code)
    {
        Parser parser = new(code);
        programInfo = parser.Parse();

        faultyScript = parser.HadError;

        RunFunction("__regglobal");
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
