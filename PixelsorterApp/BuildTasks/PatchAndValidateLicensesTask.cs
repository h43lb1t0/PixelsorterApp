#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class PatchAndValidateLicensesTask : Task
{
	[Required]
	public ITaskItem[] JsonFiles { get; set; }

	public string OverridesFile { get; set; }

	public override bool Execute()
	{
		var overrides = new Dictionary<string, (string License, string LicenseUrl)>(StringComparer.OrdinalIgnoreCase);
		if (!string.IsNullOrEmpty(OverridesFile) && File.Exists(OverridesFile))
		{
			var overridesContent = File.ReadAllText(OverridesFile);
			var matches = Regex.Matches(overridesContent, @"\{[^{}]*\}");
			foreach (Match m in matches)
			{
				var idMatch = Regex.Match(m.Value, @"""(?:PackageId|PackageName)""\s*:\s*""([^""]+)""");
				if (idMatch.Success)
				{
					var id = idMatch.Groups[1].Value.Trim();
					var licMatch = Regex.Match(m.Value, @"""License(?:Type)?""\s*:\s*""((?:[^""\\]|\\.)*)""");
					var urlMatch = Regex.Match(m.Value, @"""LicenseUrl""\s*:\s*""([^""]*)""");
					overrides[id] = (
						licMatch.Success ? licMatch.Groups[1].Value : null,
						urlMatch.Success ? urlMatch.Groups[1].Value : null
					);
				}
			}
		}

		var deprecateRegex = new Regex(@"""LicenseUrl""\s*:\s*""https://aka\.ms/deprecateLicenseUrl/?""", RegexOptions.IgnoreCase);

		foreach (var item in JsonFiles)
		{
			var path = item.ItemSpec;
			if (!File.Exists(path)) continue;

			var fileName = Path.GetFileName(path);
			var text = File.ReadAllText(path);

			var objects = new List<string>();
			bool inString = false;
			bool escape = false;
			int depth = 0;
			int start = -1;

			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (escape) { escape = false; continue; }
				if (c == '\\' && inString) { escape = true; continue; }
				if (c == '"') { inString = !inString; continue; }
				if (!inString)
				{
					if (c == '{')
					{
						if (depth == 0) start = i;
						depth++;
					}
					else if (c == '}')
					{
						depth--;
						if (depth == 0 && start >= 0)
						{
							objects.Add(text.Substring(start, i - start + 1));
							start = -1;
						}
					}
				}
			}

			var updatedObjects = new List<string>();
			bool fileModified = false;

			foreach (var obj in objects)
			{
				var currentObj = obj;
				var pkgMatch = Regex.Match(currentObj, @"""PackageId""\s*:\s*""([^""]+)""");
				var pkgId = pkgMatch.Success ? pkgMatch.Groups[1].Value : "Unknown";

				if (pkgId.StartsWith("Microsoft.WindowsAppSDK", StringComparison.OrdinalIgnoreCase) ||
				    pkgId.StartsWith("Microsoft.Windows.SDK.", StringComparison.OrdinalIgnoreCase))
				{
					fileModified = true;
					continue;
				}

				var verMatch = Regex.Match(currentObj, @"""PackageVersion""\s*:\s*""([^""]*)""");
				var pkgVer = verMatch.Success ? verMatch.Groups[1].Value : "";

				var licMatch = Regex.Match(currentObj, @"""License""\s*:\s*""((?:[^""\\]|\\.)*)""");
				var licVal = licMatch.Success ? licMatch.Groups[1].Value : null;

				var urlMatch = Regex.Match(currentObj, @"""LicenseUrl""\s*:\s*""([^""]*)""");
				var urlVal = urlMatch.Success ? urlMatch.Groups[1].Value : null;

				// Fill in missing parts from licenses.json if present
				if (overrides.TryGetValue(pkgId, out var ov))
				{
					bool patchedFromOverrides = false;
					if (string.IsNullOrWhiteSpace(licVal) && !string.IsNullOrWhiteSpace(ov.License))
					{
						licVal = ov.License;
						currentObj = currentObj.TrimEnd('}') + ",\"License\":\"" + licVal + "\"}";
						patchedFromOverrides = true;
					}
					if ((string.IsNullOrWhiteSpace(urlVal) || urlVal.TrimEnd('/').Equals("https://aka.ms/deprecateLicenseUrl", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(ov.LicenseUrl))
					{
						urlVal = ov.LicenseUrl;
						currentObj = urlMatch.Success
							? Regex.Replace(currentObj, @"""LicenseUrl""\s*:\s*""[^""]*""", "\"LicenseUrl\":\"" + urlVal + "\"")
							: currentObj.TrimEnd('}') + ",\"LicenseUrl\":\"" + urlVal + "\"}";
						patchedFromOverrides = true;
					}

					if (patchedFromOverrides)
					{
						if (!string.IsNullOrWhiteSpace(licVal) && !string.IsNullOrWhiteSpace(urlVal) && currentObj.Contains("\"ValidationErrors\""))
						{
							currentObj = Regex.Replace(currentObj, @"""ValidationErrors""\s*:\s*\[[^\]]*\],?", "");
							currentObj = currentObj.Replace("{,", "{").Replace(",}", "}").Replace(",,", ",");
						}
						fileModified = true;
						Log.LogMessage(MessageImportance.High,
							string.Format("[Licenses] Filled missing license information for '{0}' from licenses.json.", pkgId));
					}
				}

				// Auto-generate license URL for deprecateLicenseUrl if valid license found
				if (!string.IsNullOrEmpty(urlVal) &&
					urlVal.TrimEnd('/').Equals("https://aka.ms/deprecateLicenseUrl", StringComparison.OrdinalIgnoreCase))
				{
					if (!string.IsNullOrWhiteSpace(licVal) &&
						!licVal.Contains("\\n") &&
						!licVal.Contains("\\r") &&
						licVal.Length <= 100)
					{
						var newUrl = "https://licenses.nuget.org/" + licVal.Trim();
						currentObj = deprecateRegex.Replace(currentObj, "\"LicenseUrl\":\"" + newUrl + "\"", 1);
						urlVal = newUrl;
						fileModified = true;
						Log.LogMessage(MessageImportance.High,
							string.Format("[Licenses] Auto-generated license URL for '{0}' ({1}): {2}", pkgId, pkgVer, newUrl));
					}
				}

				// Check for remaining problems
				var problems = new List<string>();
				if (currentObj.Contains("\"ValidationErrors\""))
				{
					problems.Add("validation errors found");
				}
				if (string.IsNullOrWhiteSpace(licVal))
				{
					problems.Add("missing license");
				}
				if (string.IsNullOrWhiteSpace(urlVal))
				{
					problems.Add("missing license URL");
				}
				else if (urlVal.TrimEnd('/').Equals("https://aka.ms/deprecateLicenseUrl", StringComparison.OrdinalIgnoreCase))
				{
					problems.Add("unresolved deprecateLicenseUrl");
				}

				if (problems.Count > 0)
				{
					var reason = string.Join(", ", problems);
					Log.LogWarning(string.Format("[Licenses] Problematic package: '{0}' ({1}) in {2} has issues: {3}. Please add missing info to licenses.json!", pkgId, pkgVer, fileName, reason));
				}

				updatedObjects.Add(currentObj);
			}

			if (fileModified)
			{
				var newJson = "[" + string.Join(",", updatedObjects) + "]";
				File.WriteAllText(path, newJson, System.Text.Encoding.UTF8);
				Log.LogMessage(MessageImportance.High,
					string.Format("[Licenses] Saved updated license file: {0}", fileName));
			}
		}

		return !Log.HasLoggedErrors;
	}
}
