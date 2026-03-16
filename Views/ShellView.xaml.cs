using MahApps.Metro.Controls;
using Octokit;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace SPP_LegionV2_Management
{
	/// <summary>
	/// Interaction logic for ShellView.xaml
	/// </summary>
	public partial class ShellView : MetroWindow
	{
		public ShellView()
		{
			InitializeComponent();
			ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(System.Int32.MaxValue));
		}

		private void HelpAbout(object sender, RoutedEventArgs e)
		{
			// for .NET Core you need to add UseShellExecute = true
			// see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
			Process.Start(new ProcessStartInfo("https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/README.md"));
			e.Handled = true;
		}

		private async void UpdateApp(object sender, RoutedEventArgs e)
		{
			string workspaceName = "skeezerbean";
			string repositoryName = "SPP-LegionV2-Management";
			string filename = "temp.zip";
			string extractedFolderName = "SPP LegionV2 Management";
			string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			string file = @"DoUpdate.bat";

			// clean up existing items
			if (File.Exists(Path.Combine(path, filename)))
				File.Delete(Path.Combine(path, filename));
			if (File.Exists(Path.Combine(path, file)))
				File.Delete(Path.Combine(path, file));
			if (Directory.Exists(Path.Combine(path, extractedFolderName)))
				Directory.Delete(Path.Combine(path, extractedFolderName), recursive: true);

			GitHubClient client = new GitHubClient(new ProductHeaderValue(repositoryName));

			//Setup the versions
			var releases = await client.Repository.Release.GetAll(workspaceName, repositoryName);
			var latest = releases[0];

			// download latest
			string downloadFile = $"https://github.com/skeezerbean/SPP-LegionV2-Management/releases/download/{latest.Name}/SPP.LegionV2.Management.{latest.Name}.zip";
			using (var httpclient = new HttpClient())
			{
				var response = await httpclient.GetAsync(downloadFile);
				var fileStream = await response.Content.ReadAsStreamAsync();
				using (var tempFile = File.Create(path + "\\" + filename))
				{
					await fileStream.CopyToAsync(tempFile);
				}
			}

			// extract
			string zipPath = path + "\\" + filename;
			ZipFile.ExtractToDirectory(zipPath, path);

			// create our update bat file and execute, then exit
			using (StreamWriter writer = File.CreateText(file))
			{
				// write commands to the file to wait for close, extract, move to current folder, restart this app
				writer.WriteLine("@echo off");
				writer.WriteLine("echo Updating app...");
				writer.WriteLine("powershell Start-Sleep 2");
				writer.WriteLine($"del \"{path}\\Default Templates\\bnetserver.conf\"");
				writer.WriteLine($"del \"{path}\\Default Templates\\worldserver.conf\"");
				writer.WriteLine($"powershell.exe -Command \"Move-Item -Path '{path}\\{extractedFolderName}\\*' -Destination '{path}' -ErrorAction SilentlyContinue -Force");
				writer.WriteLine($"powershell.exe -Command \"Move-Item -Path '{path}\\{extractedFolderName}\\Default Templates\\*' -Destination '{path}\\Default Templates' -ErrorAction SilentlyContinue -Force");
				writer.WriteLine($"cmd /c start \"\" \"{path}\\SPP-LegionV2-Management.exe\"");
				writer.WriteLine($"rmdir /q /s \"{extractedFolderName}\"");
				writer.WriteLine("del temp.zip");
				writer.WriteLine("del DoUpdate.bat");
			}

			Process ps = new Process();
			ps.StartInfo.FileName = file;
			ps.Start();
			System.Windows.Application.Current.Shutdown();

			e.Handled = true;
		}

		// Save the window size/location when exiting
		private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			GeneralSettingsManager.SaveSettings(GeneralSettingsManager.SettingsPath, GeneralSettingsManager.GeneralSettings);
		}
	}
}