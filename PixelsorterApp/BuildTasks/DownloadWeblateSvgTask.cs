using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class DownloadWeblateSvgTask : Task
{
	[Required]
	public string SourceUrl { get; set; }

	[Required]
	public string DestinationFile { get; set; }

	public override bool Execute()
	{
		var dir = Path.GetDirectoryName(DestinationFile);
		if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}

		try
		{
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
			client.DefaultRequestHeaders.UserAgent.ParseAdd("PixelsorterApp-MSBuild/1.0");
			var svgContent = client.GetStringAsync(SourceUrl).GetAwaiter().GetResult();

			if (!string.IsNullOrWhiteSpace(svgContent) && svgContent.Contains("<svg"))
			{
				var existingContent = File.Exists(DestinationFile) ? File.ReadAllText(DestinationFile, Encoding.UTF8) : null;
				if (existingContent != svgContent)
				{
					File.WriteAllText(DestinationFile, svgContent, Encoding.UTF8);
					Log.LogMessage(MessageImportance.High, $"[Weblate] Updated {DestinationFile} from {SourceUrl}");
				}
			}
		}
		catch (Exception ex)
		{
			if (!File.Exists(DestinationFile))
			{
				var fallbackSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"75\"></svg>";
				File.WriteAllText(DestinationFile, fallbackSvg, Encoding.UTF8);
				Log.LogWarning($"[Weblate] Could not download widget from {SourceUrl}: {ex.Message}. Created minimal placeholder SVG.");
			}
			else
			{
				Log.LogWarning($"[Weblate] Could not download latest widget from {SourceUrl}: {ex.Message}. Using cached local file.");
			}
		}

		return !Log.HasLoggedErrors;
	}
}
