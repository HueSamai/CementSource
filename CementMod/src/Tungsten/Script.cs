namespace Tungsten;
public class Script
{
    public bool faultyScript { get; private set; }
    private ProgramInfo? programInfo;

    public Script(string code)
    {
        Parser parser = new(code);
        programInfo = parser.Parse();
        faultyScript = parser.hadError;

        RunFunction("__regglobal");
    }

    public object? RunFunction(string functionName, params object?[] args)
    {
        if (faultyScript || programInfo == null) return null;
        return VM.RunFunction((ProgramInfo)programInfo, functionName, args);
    }

    public object? RunFunction(string functionName, int arg)
    {
        return RunFunction(functionName, new object? [] { arg });
    }
}
