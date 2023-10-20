using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System;
using CementTools;

public struct ProcessedModData
{
    public bool succeeded;
    public string name;
    public string directoryName;
    public string message;
    public string pathToMod;
}

// a class which handles installing all the required dependencies for a specific mod
// each mod file gets its own ModDownloadHandler, managed by the Cement class.
public class ModDownloadHandler
{
    private string _pathToMod;
    public event Action<float> OnProgress;

    private Dictionary<string, float> _percentages = new Dictionary<string, float>();
    private int _numberOfLinks;

    public ModDownloadHandler(string pathToMod)
    {
        _pathToMod = pathToMod;
    }

    private string GetLatestVersion(string url)
    {
        if (url == null)
        {
            return "Latest";
        }
        
        try
        {   
            WebClient client = new WebClient();
            client.Proxy = null;
            return client.DownloadString(url);
        }
        catch
        {
            return null;
        }
    }

    private string ReadMessage(string message)
    {
        if (message == null)
        {
            return null;
        }
        if (LinkHelper.IsLink(message))
        {   
            WebClient client = new WebClient();
            string downloadedMessage = client.DownloadString(message);
            CementTools.Cement.Log($"DOWNLOADED MESSAGE {downloadedMessage}");
            client.Dispose();
            return downloadedMessage;
        }
        return message;
    }

    private string GetUpdatedCementFile(string linkToFile)
    {
        WebClient client = new WebClient();
        string downloadContents = client.DownloadString(linkToFile);
        client.Dispose();
        return downloadContents;
    }

    private void UpdateCementFile(ModFile file)
    {
        string link = file.GetString("CementFile");
        if (link == null)
        {
            return;
        }

        string updatedFile = GetUpdatedCementFile(link);
        File.WriteAllText(file.path, updatedFile);
        file.Reload(false);
    }

    public async void Download(Action<ProcessedModData> callback)
    {
        ProcessedModData data = new ProcessedModData();
        data.succeeded = false;

        ModFile modFile = ModFile.Get(_pathToMod);
        CementTools.Cement.Log($"FINISHED PROCESSING MOD FILE FOR {_pathToMod}");
        data.pathToMod = _pathToMod;

        string name = modFile.GetString("Name");
        string author = modFile.GetString("Author");

        if (name == null || author == null)
        {
            data.name = "Unknown";
            data.message = null;
            callback.Invoke(data);
            return;
        }

        string currentVersion = modFile.GetString("CurrentVersion");
        string latestVersion;
        string modMessage = ReadMessage(modFile.GetString("Message"));

        data.name = name;
        CementTools.Cement.Log($"MESSAGE {modMessage}");
        data.message = modMessage;

        string directoryName = $"{LinkHelper.ToUsableName(author)}.{LinkHelper.ToUsableName(name)}";
        data.directoryName = directoryName;


        CementTools.Cement.Log($"GETTING LATEST VERSION FOR {_pathToMod}...");
        if (CementTools.Cement.HasInternet)
        {
            latestVersion = GetLatestVersion(modFile.GetString("LatestVersion"));
        }
        else
        {
            latestVersion = "";
        }

        if (latestVersion == null)
        {
            CementTools.Cement.Log("FAILED!");
            OnProgress(100f);
            callback.Invoke(data);
            return;
        }
        latestVersion = latestVersion.Replace("\n", "");
        CementTools.Cement.Log($"SUCCEEDED {latestVersion}!");

        if (latestVersion != currentVersion)
        {
            CementTools.Cement.Log($"DOWNLOADING LINKS FOR MOD {_pathToMod}!");
            if (CementTools.Cement.HasInternet)
            {
                bool succeeded = await DownloadLinks(modFile.GetString("Links"), directoryName);
                if (succeeded)
                {
                    CementTools.Cement.Log($"SUCCEEDED!");

                    modFile.SetString("CurrentVersion", latestVersion);
                    try
                    {
                        UpdateCementFile(modFile);
                        modFile.UpdateFile();
                    }
                    catch (Exception e)
                    {
                            CementTools.Cement.Log($"FAILED TO UPDATE CEMENT FILE BECAUSE {e}");
                    }

                    CementTools.Cement.Log("FINISHED UPDATING CEMENT FILE");
                    data.succeeded = true;

                    OnProgress(100f);
                    callback.Invoke(data);
                }
                else
                {
                    CementTools.Cement.Log($"FAILED!");
                    OnProgress(100f);
                    callback.Invoke(data);
                }
            }
            else 
            {
                if (currentVersion != null)
                {
                    OnProgress(100f);
                    data.succeeded = true;
                    callback.Invoke(data);
                }
                else
                {
                    OnProgress(100f);
                    callback.Invoke(data);
                }
            }
        }
        else
        {
            OnProgress(100f);
            data.succeeded = true;
            callback.Invoke(data);
        }
    }

    private void ProgressChanged(string link, float percentage)
    {
        _percentages[link] = percentage;

        float totalPercentages = 0;
        foreach (float p in _percentages.Values)
        {
            totalPercentages += p;
        }

        OnProgress.Invoke(totalPercentages / _numberOfLinks);
    }

    private async Task<bool> DownloadLinks(string links, string directoryName)
    {
        string directoryPath = Path.Combine(CementTools.Cement.CACHE_PATH, directoryName);

        if (Directory.Exists(directoryPath))
        {
            IOExtender.DeleteFilesInDirectory(directoryPath);
        }
        Directory.CreateDirectory(directoryPath);

        string[] splitLinks = links.Split(',');
        _numberOfLinks = splitLinks.Length;
        foreach (string link in splitLinks)
        {
            if (LinkHelper.IsLinkToMod(link)) // already downloaded mods so can ignore
            {
                _percentages[link] = 100f;
                continue;
            }

            Cement.Log($"DOWNLOAD LINK: {link}");
            bool succeeded = await DownloadHelper.DownloadFile(link, Path.Combine(directoryPath, LinkHelper.GetNameFromLink(link)),
            delegate (object sender, DownloadProgressChangedEventArgs eventArgs)
            {
                CementTools.Cement.Log($"PROGRESS CHANGED: {eventArgs.ProgressPercentage}");
                ProgressChanged(link, eventArgs.ProgressPercentage);
            });

            if (!succeeded)
            {
                return false;
            }
        }

        return true;
    }
}