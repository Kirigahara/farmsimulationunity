using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameTemplate.Editor.Infrastructure
{
    /// <summary>
    /// Build Pipeline - one-click build Android/iOS.
    ///
    /// Tính năng:
    ///   - Auto increment Bundle Version Code (Android) / Build Number (iOS)
    ///   - Tạo folder build theo format: Builds/<Platform>/<YYYYMMDD-HHmm>_<Version>/
    ///   - Tự gắn timestamp vào tên file
    ///   - Mở folder sau khi build xong
    ///   - Tự switch Development/Release mode
    /// </summary>
    public static class BuildPipeline
    {
        private const string BuildRoot = "Builds";

        [MenuItem("GameTemplate/Build/Android Development", priority = 20)]
        public static void BuildAndroidDev()
        {
            BuildAndroid(development: true);
        }

        [MenuItem("GameTemplate/Build/Android Release", priority = 21)]
        public static void BuildAndroidRelease()
        {
            if (!EditorUtility.DisplayDialog(
                "Build Release",
                "Build production AAB? (sẽ tắt ENABLE_GAME_LOG và set IL2CPP)",
                "Build", "Cancel"))
                return;

            BuildAndroid(development: false);
        }

        [MenuItem("GameTemplate/Build/iOS Development", priority = 22)]
        public static void BuildIosDev()
        {
            BuildIos(development: true);
        }

        [MenuItem("GameTemplate/Build/iOS Release", priority = 23)]
        public static void BuildIosRelease()
        {
            if (!EditorUtility.DisplayDialog(
                "Build Release",
                "Build production iOS Xcode project?",
                "Build", "Cancel"))
                return;

            BuildIos(development: false);
        }

        private static void BuildAndroid(bool development)
        {
            // Auto-increment Bundle Version Code
            PlayerSettings.Android.bundleVersionCode++;

            // AAB cho release, APK cho dev
            EditorUserBuildSettings.buildAppBundle = !development;

            // IL2CPP cho release (bắt buộc cho 64-bit Google Play)
            PlayerSettings.SetScriptingBackend(
                NamedBuildTargetFromGroup(BuildTargetGroup.Android),
                development ? ScriptingImplementation.Mono2x : ScriptingImplementation.IL2CPP);

            // Architectures
            PlayerSettings.Android.targetArchitectures = development
                ? AndroidArchitecture.ARMv7
                : (AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64);

            var ext = development ? "apk" : "aab";
            var path = MakeBuildPath("Android", ext);

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = path,
                target = BuildTarget.Android,
                options = development ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None,
            };

            ExecuteBuild(options);
        }

        private static void BuildIos(bool development)
        {
            PlayerSettings.iOS.buildNumber = (int.Parse(PlayerSettings.iOS.buildNumber) + 1).ToString();

            var path = MakeBuildPath("iOS", "");

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = path,
                target = BuildTarget.iOS,
                options = development ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None,
            };

            ExecuteBuild(options);
        }

        private static void ExecuteBuild(BuildPlayerOptions options)
        {
            var report = UnityEditor.BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                var sizeMb = summary.totalSize / 1024f / 1024f;
                Debug.Log($"[Build] OK: {options.locationPathName} ({sizeMb:F1} MB, {summary.totalTime})");
                EditorUtility.RevealInFinder(options.locationPathName);
            }
            else
            {
                Debug.LogError($"[Build] FAILED: {summary.result} ({summary.totalErrors} errors)");
            }
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) scenes.Add(s.path);
            return scenes.ToArray();
        }

        private static string MakeBuildPath(string platform, string extension)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            var version = Application.version;
            var productName = SanitizeName(PlayerSettings.productName);
            var folder = Path.Combine(BuildRoot, platform, $"{timestamp}_{version}");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return string.IsNullOrEmpty(extension)
                ? folder // iOS Xcode project = folder
                : Path.Combine(folder, $"{productName}_v{version}.{extension}");
        }

        private static string SanitizeName(string name)
            => System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9]", "");

        // Helper cho Unity 2022+ NamedBuildTarget API
        private static UnityEditor.Build.NamedBuildTarget NamedBuildTargetFromGroup(BuildTargetGroup group)
            => UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
    }
}
