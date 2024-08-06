using MelonLoader;
using MelonLoader.Utils;
using Mono.Cecil;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CementGB.Plugin;

public static class BuildInfo
{
    public const string Name = "CementPlugin";
    public const string Author = "HueSamai // dotpy";
    public const string Description = null;
    public const string Company = "CementGB";
    public const string Version = "4.0.0";
    public const string DownloadLink = "https://api.github.com/repos/HueSamai/CementSource/releases/latest";
}

public class Plugin : MelonPlugin
{
    public static readonly string userDataPath = Path.Combine(MelonEnvironment.UserDataDirectory, "CementGB");
    public static readonly string cementBinPath = Path.Combine(userDataPath, "bin");

    public static bool OfflineMode => _offlineModePref.GetValueAsString() == "true";
    public static bool DevMode => _devModePref.GetValueAsString() == "true";

    private static readonly MelonPreferences_Category _melonCat = MelonPreferences.CreateCategory("cement_prefs", "CementGB");
    private static readonly MelonPreferences_Entry _offlineModePref = _melonCat.CreateEntry(nameof(_offlineModePref), false);
    private static readonly MelonPreferences_Entry _devModePref = _melonCat.CreateEntry("DevMode", false, "Developer Mode");

    public override async void OnApplicationEarlyStart()
    {
        base.OnPreModsLoaded();

        if (!OfflineMode) await RunAutoUpdater();
    }

    private async Task<bool> RunAutoUpdater()
    {
        var updaterClient = new HttpClient();
        updaterClient.DefaultRequestHeaders.Add("User-Agent", "CMT_MelonUpdater");

        var filesInMods = Directory.GetFiles(MelonEnvironment.ModsDirectory);
        foreach (var file in filesInMods)
        {
            if (file is null) continue;

            LoggerInstance.Msg($"Scanning {file} for updates...");

            try
            {
                // Analyze MelonInfoAttribute constructor for local version and download link
                var module = ModuleDefinition.ReadModule(file);
                if (module is null) continue;

                var infoAttr = module.GetCustomAttributes().First(x => x.AttributeType.FullName == typeof(MelonInfoAttribute).FullName) ?? throw new Exception($"{file} does not contain a MelonInfoAttribute.");

                var downloadLink = (string?)infoAttr.ConstructorArguments[4].Value;
                var localVersion = (string?)infoAttr.ConstructorArguments[2].Value;

                if (downloadLink is null) throw new Exception($"{file} does not contain a DownloadLink in its MelonInfoAttribute.");
                if (localVersion is null) throw new Exception($"{file} does not contain a Version in its MelonInfoAttribute.");

                module.Dispose();

                if (!Uri.TryCreate(downloadLink, UriKind.Absolute, out var downloadUri) || downloadUri is null || downloadUri.Host != "api.github.com") continue;
                if (!Version.TryParse(localVersion, out var localVersionObj) || localVersionObj is null) continue;

                LoggerInstance.Msg($"Now fetching latest release from {file}'s MelonInfo's DownloadLink...");

                var response = await updaterClient.GetAsync(downloadUri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var contentJson = JsonNode.Parse(content) ?? throw new Exception("DownloadLink API response could not be parsed into JsonNode! Ensure you have a valid URL.");

                var latestReleaseTagNameJson = contentJson["tag_name"]?.AsValue() ?? throw new Exception("tag_name of API response could not be found. Ensure the response came back successful.");
                var latestReleaseTagName = latestReleaseTagNameJson?.GetValue<string>() ?? throw new Exception("tag_name of API response has no string value!");
                var remoteVersion = ParseVersionFromTagName(latestReleaseTagName);

                if (remoteVersion <= localVersionObj) continue;

                // TODO: Ask user if they want to update mod

                var assetsObj = contentJson["assets"]?.AsArray() ?? throw new Exception("Latest release has no assets!");
                foreach (var asset in assetsObj)
                {
                    if (asset is null) continue;
                    var assetName = asset["name"]?.AsValue()?.GetValue<string>();

                    if (string.IsNullOrWhiteSpace(assetName)) continue;

                    var assetDownloadUrl = asset["browser_download_url"]?.AsValue()?.GetValue<string>();
                    if (assetDownloadUrl is null) continue;

                    var assetDownloadResponse = await updaterClient.GetAsync(assetDownloadUrl);
                    assetDownloadResponse.EnsureSuccessStatusCode();

                    var assetLocalPath = Path.Combine(cementBinPath, assetName);

                    using var stream = await assetDownloadResponse.Content.ReadAsStreamAsync();
                    using var fs = File.Create(assetLocalPath);
                    await stream.CopyToAsync(fs);
                    fs.Flush();

                    if (assetName.EndsWith(".dll"))
                    {
                        File.Move(assetLocalPath, MelonEnvironment.ModsDirectory);
                    }
                    else if (assetName.EndsWith(".zip"))
                    {
                        ZipFile.ExtractToDirectory(assetLocalPath, MelonEnvironment.GameRootDirectory);
                    }
                    else
                    {
                        File.Delete(assetLocalPath);
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Failed to update {file}: ", e);
            }
        }

        return false;
    }

    private static Version? ParseVersionFromTagName(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) throw new ArgumentNullException(nameof(tagName));

        var splitVersion = tagName.Split('-', '_')[1];
        _ = Version.TryParse(splitVersion, out var parsedVersion);
        return parsedVersion;
    }
}
