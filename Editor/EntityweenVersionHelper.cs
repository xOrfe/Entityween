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
                if (!string.IsNullOrEmpty(packageInfo.version))
                {
                    return packageInfo.version;
                }

                string packageJsonPath = Path.Combine(packageInfo.resolvedPath, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    try
                    {
                        string content = File.ReadAllText(packageJsonPath);
                        var match = Regex.Match(content, @"""version""\s*:\s*""([^""]+)""");
                        if (match.Success)
                        {
                            return match.Groups[1].Value.Trim();
                        }
                    }
                    catch
                    {
                    }
                }
            }

            string directPath = "Packages/Entityween/package.json";
            if (File.Exists(directPath))
            {
                try
                {
                    string content = File.ReadAllText(directPath);
                    var match = Regex.Match(content, @"""version""\s*:\s*""([^""]+)""");
                    if (match.Success)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
                catch
                {
                }
            }

            return "1.1.3";
        }
    }
}
