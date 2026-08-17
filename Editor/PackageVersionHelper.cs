using System.IO;
using System.Text.RegularExpressions;

namespace XO.Entityween.Editor
{
    [UnityEditor.InitializeOnLoad]
    public static class PackageVersionHelper
    {
        private static string _cachedVersion; 
        private const string DirectPath = "Packages/Entityween/package.json";

        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetCache()
        {
            _cachedVersion = null;
        }

        public static string Version
        {
            get
            {
                if (string.IsNullOrEmpty(_cachedVersion))
                {
                    _cachedVersion = ResolveVersion(DirectPath);
                }
                return _cachedVersion;
            }
        }

        private static string ResolveVersion(string directPath)
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PackageVersionHelper).Assembly);
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

            return "0.0.0";
        }
    }
}
