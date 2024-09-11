using System.Runtime.CompilerServices;
using MelonLoader;

namespace CementGB.Mod.Utilities;

public static class LoggingUtilities
{
    public static MelonLogger.Instance Logger => Melon<Mod>.Logger; // For if you're tired of the singleton pattern I guess

    public static void VerboseLog(string? message, [CallerMemberName] string? callerName = null, [CallerLineNumber] int lineNumber = 0)
    {
        if (!CementPreferences.VerboseMode) return;
        Logger.Msg(System.ConsoleColor.Gray, callerName == null ? $"{message}" : $"[{callerName.ToUpper()}] {message} : Ln {lineNumber}");
    }
}