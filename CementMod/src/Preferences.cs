using MelonLoader;

namespace CementGB.Mod;

public static class CementPreferences
{
    public static bool VerboseMode => _verboseModeEntry?.Value ?? false;

    private static MelonPreferences_Category? _cmtPrefCateg = MelonPreferences.CreateCategory("CementGBPrefs", "CementGB Preferences");
    private static MelonPreferences_Entry<bool>? _verboseModeEntry;

    internal static void Initialize()
    {
        _cmtPrefCateg = MelonPreferences.CreateCategory("CementGBPrefs", "CementGB Preferences");
        _cmtPrefCateg.SetFilePath(Mod.userDataPath);
        _verboseModeEntry = _cmtPrefCateg.CreateEntry("verbose_mode", false, "Verbose Mode", "Enables extra log messages for developers.");
    }
}