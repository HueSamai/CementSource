using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CementGB.Installer
{
    public partial class MainForm : Form
    {
        public const string REPO_API_LINK = "https://api.github.com/repos/HueSamai/CementSource/releases/latest";
        public const string MSG_DONE = "Done downloading Cement! When you want to install an update, " +
                    "just run this application again. You can now close me, and reopen Gang Beasts.";
        public const string MSG_ERROR = "Whoops! An error occurred while trying to download Cement: ";
        public const string MSG_GANG_BEASTS_NOT_OPEN = "Please open Gang Beasts, then click the retry button.\nThe installer will automatically close it.";
        public const string INSTALLER_CONTENTS_FILE_NAME = "CementInstallerContents.zip";

        private string pathToGangBeastsExe;

        public MainForm()
        {
            InitializeComponent();
        }

        private static Process GetGangBeastsProcess()
        {
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName == "Gang Beasts")
                {
                    return process;
                }
            }

            return null;
        }

        [SupportedOSPlatform("Windows")]
        private void SetFont()
        {
            //https://stackoverflow.com/questions/1297264/using-custom-fonts-on-a-label-on-winforms
            //Create your private font collection object.
            PrivateFontCollection pfc = new();

            //Select your font from the resources.
            //My font here is "Digireu.ttf"
            int fontLength = Properties.Resources.Quantico_Regular.Length;

            // create a buffer to read in to
            byte[] fontdata = Properties.Resources.Quantico_Regular;

            // create an unsafe memory block for the font data
            System.IntPtr data = Marshal.AllocCoTaskMem(fontLength);

            // copy the bytes to the unsafe memory block
            Marshal.Copy(fontdata, 0, data, fontLength);

            // pass the font to the font collection
            pfc.AddMemoryFont(data, fontLength);

            infoText.Font = new Font(pfc.Families[0], infoText.Font.Size);
            infoText.ForeColor = Color.FromArgb(203, 246, 255);
            retryButton.Font = new Font(pfc.Families[0], retryButton.Font.Size);
            retryButton.ForeColor = Color.FromArgb(203, 246, 255);
        }

        private void SetColors()
        {
            BackColor = Color.FromArgb(146, 187, 228);
            pictureBox1.BackColor = Color.Transparent;
            infoText.BackColor = Color.Transparent;
            retryButton.BackColor = Color.FromArgb(1, 66, 87);
        }

        private void DownloadFinished()
        {
            Process.Start(pathToGangBeastsExe);
            Close();
        }

        private async Task DownloadLatestCementRelease(string url, string gangBeastsPath)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "request");

            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Host != "api.github.com") return;

                var response = await client.GetStringAsync(uri);
                Console.WriteLine(response);

                var releaseResponseObj = JsonNode.Parse(response).AsObject();
                var assetsArray = (releaseResponseObj["assets"]?.AsArray()) ?? throw new NullReferenceException("assetsArray was not found.");
                foreach (var asset in assetsArray) 
                {
                    var assetObj = asset?.AsObject();
                    if (assetObj is null) continue;

                    var assetName = assetObj["name"]?.AsValue();
                    if (assetName is null || string.IsNullOrWhiteSpace(assetName.GetValue<string>())) continue;

                    if (assetName.GetValue<string>() == INSTALLER_CONTENTS_FILE_NAME)
                    {
                        var assetDownloadUrl = assetObj["browser_download_url"]?.AsValue()?.GetValue<string>();
                        if (assetDownloadUrl is null) continue;

                        var assetBytes = await client.GetByteArrayAsync(assetDownloadUrl);
                        var tempZipPath = Path.Combine(gangBeastsPath, INSTALLER_CONTENTS_FILE_NAME);
                        await File.WriteAllBytesAsync(tempZipPath, assetBytes);

                        ZipFile.ExtractToDirectory(tempZipPath, gangBeastsPath);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error downloading/installing Cement from latest release! {e}");
                downloadTimer.Stop();
                infoText.Text = $"{MSG_ERROR}: {e.Message}";
                infoText.ForeColor = Color.Red;
                await Task.Delay(1000);
            }
            finally
            {
                client.Dispose();
            }
        }

        private async void TryInstallLoader()
        {
            Process gangBeastsProcess = GetGangBeastsProcess();
            if (gangBeastsProcess != null)
            {
                retryButton.Visible = false;
                string pathToGangBeasts = Path.Combine(gangBeastsProcess.MainModule.FileName, "..");
                pathToGangBeastsExe = gangBeastsProcess.MainModule.FileName;
                gangBeastsProcess.Kill();
                gangBeastsProcess.Dispose();
                Console.WriteLine($"Found Gang Beasts at {pathToGangBeasts}");
                infoText.Text = "Downloading";
                downloadTimer.Enabled = true;
                await DownloadLatestCementRelease(REPO_API_LINK, pathToGangBeasts);
            }
            else
            {
                infoText.Text = MSG_GANG_BEASTS_NOT_OPEN;
            }

            retryButton.Visible = true;
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            switch (args)
            {
                case string[] arg1 when arg1.Contains("--no-install"):
                    Process gangBeastsProcess = GetGangBeastsProcess();
                    if (gangBeastsProcess != null)
                    {
                        gangBeastsProcess.Kill();
                        pathToGangBeastsExe = gangBeastsProcess.MainModule.FileName;
                        DownloadFinished();
                        return;
                    }
                    break;
            }

            SetColors();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) SetFont();
            TryInstallLoader();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            TryInstallLoader();
        }
    }
}