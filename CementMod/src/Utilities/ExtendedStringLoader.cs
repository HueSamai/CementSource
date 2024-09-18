using System;
using System.Collections.Generic;

namespace CementGB.Mod.Utilities;

public static class ExtendedStringLoader
{
    public static Dictionary<string, string> items = new();

    public static void Register(string key, string value)
    {
        if (items.ContainsKey(key))
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed, $"'{key}' has already been registered in ExtendedStringLoader");
            return;
        }

        items[key] = value;
    }
}