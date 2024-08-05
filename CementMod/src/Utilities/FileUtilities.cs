using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CementGB.Mod.Utilities;

public static class FileUtilities
{
    public static string? ReadEmbeddedText(Assembly assembly, string resourceName)
    {
        assembly ??= Assembly.GetCallingAssembly();

        if (assembly.GetManifestResourceNames().Contains(resourceName))
        {
            using var str = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Resource stream returned null. This could mean an inaccessible resource caller-side or an invalid argument was passed.");
            using var reader = new StreamReader(str);

            return reader.ReadToEnd();
        }
        throw new Exception($"No resources matching the name '{resourceName}' were found in the assembly '{assembly.FullName}'. Please ensure you passed the correct name.");
    }
}