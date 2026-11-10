using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Menu: Week10 > Build Android APK
// One click: Android settings + APK (not .aab) into D:/unity/Builds/JumpHigh.apk
// Also works from the command line: -executeMethod Week10Build.BuildApk
public static class Week10Build
{
    const string OutDir = "D:/unity/Builds";
    const string OutFile = OutDir + "/JumpHigh.apk";

    [MenuItem("Week10/Build Android APK")]
    public static void BuildApk()
    {
        NamedBuildTarget android = NamedBuildTarget.Android;

        PlayerSettings.productName = "Jump High";
        PlayerSettings.SetApplicationIdentifier(android, "com.jumphigh.game");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;   // all modern phones
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        EditorUserBuildSettings.buildAppBundle = false;                            // .apk, not .aab
        AssetDatabase.SaveAssets();

        // scenes from the build list, MainMenu first
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.enabled && File.Exists(s.path)) scenes.Add(s.path);
        int menu = scenes.FindIndex(p => p.EndsWith("/MainMenu.unity"));
        if (menu > 0) { string m = scenes[menu]; scenes.RemoveAt(menu); scenes.Insert(0, m); }

        Directory.CreateDirectory(OutDir);
        BuildPlayerOptions o = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = OutFile,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(o);
        bool ok = report.summary.result == BuildResult.Succeeded;
        string msg = ok
            ? "APK is ready:\n" + OutFile + "\n\nSize: " + (report.summary.totalSize / (1024 * 1024)) + " MB\nSend it to the Android phone (Telegram) and install."
            : "Build failed: " + report.summary.result + " (" + report.summary.totalErrors + " errors).\nSee the Console.";
        Debug.Log("[Week10Build] " + msg);

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(ok ? 0 : 1);
            return;
        }
        EditorUtility.DisplayDialog("Build Android APK", msg, "OK");
        if (ok) EditorUtility.RevealInFinder(OutFile);
    }
}
