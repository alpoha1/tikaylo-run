#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TilkayoRun.EditorTools
{
    /// <summary>
    /// Меню «Тилкайо» и сборка из командной строки:
    /// Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod TilkayoRun.EditorTools.BuildScript.BuildApk
    /// </summary>
    public static class BuildScript
    {
        const string OutDir = "Builds";

        [MenuItem("Тилкайо/Собрать APK")]
        public static void BuildApk() { Build(false); }

        [MenuItem("Тилкайо/Собрать AAB (для Google Play)")]
        public static void BuildAab() { Build(true); }

        static void Build(bool aab)
        {
            ProjectSetup.Apply();
            Directory.CreateDirectory(OutDir);
            EditorUserBuildSettings.buildAppBundle = aab;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            string path = Path.Combine(OutDir, aab ? "TilkayoRun.aab" : "TilkayoRun.apk");

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            var sum = report.summary;
            Debug.Log("Build " + sum.result + ": " + path + " (" + (sum.totalSize / 1024 / 1024) + " MB)");
            if (sum.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
#endif
