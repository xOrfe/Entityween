using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace XO.Entityween.Editor
{
    public static class EntityweenVersionHelper
    {
        private static string _cachedVersion;

        public static string Version
        {
            get
            {
                if (string.IsNullOrEmpty(_cachedVersion))
                {
                    _cachedVersion = ResolveVersion();
                }
                return _cachedVersion;
            }
        }

        private static string ResolveVersion()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(EntityweenVersionHelper).Assembly);
            if (packageInfo != null)
            {
                string readmePath = Path.Combine(packageInfo.resolvedPath, "README.md");
                if (File.Exists(readmePath))
                {
                    try
                    {
                        string content = File.ReadAllText(readmePath);
                        var match = Regex.Match(content, @"<!--\s*Version:\s*([0-9]+\.[0-9]+\.[0-9]+)\s*-->");
                        if (match.Success)
                        {
                            return match.Groups[1].Value.Trim();
                        }
                    }
                    catch
                    {
                        // Ignore and fallback
                    }
                }

                // Fallback to PackageManager package version
                if (!string.IsNullOrEmpty(packageInfo.version))
                {
                    return packageInfo.version;
                }
            }

            // Ultimate fallback
            return "1.1.0";
        }
    }
}
