using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

public class Builder
{
    static string[] clientScriptSymbols = new string[] { "CLIENT_0", "CLIENT_1" };

    [MenuItem("Build/All")]
    static void BuildAll()
    {
        // Save the current build target so we can restore it later
        BuildTarget targetBeforeBuildAll = EditorUserBuildSettings.activeBuildTarget;

        // Execute the build for each platform
        BuildWindows();
        BuildAndroid();
        BuildMac();
        
        // Restore the build target that was active before BuildAll was called
        SetActiveBuildTarget(targetBeforeBuildAll);
    }

    [MenuItem("Build/Windows")]
    static void BuildWindows()
    {
        BuildPlatform(BuildTarget.StandaloneWindows, Path.Combine(Application.dataPath, "..", "Builds/Windows"), "Client.exe");
    }

    [MenuItem("Build/Mac")]
    static void BuildMac()
    {
        BuildPlatform(BuildTarget.StandaloneOSX, Path.Combine(Application.dataPath, "..", "Builds/Mac"), "Client.x64");
    }

    [MenuItem("Build/Android")]
    static void BuildAndroid()
    {
        BuildPlatform(BuildTarget.Android, Path.Combine(Application.dataPath, "..", "Builds/Android"), "Client.apk");
    }

    static void BuildPlatform(BuildTarget target, string buildPath, string name)
    {
        SetActiveBuildTarget(target);

        Debug.Log($"Building for {target}...");

        var scenes = EditorBuildSettings.scenes;

        // Get the current scripting define symbols for this build target (StandaloneWindows, Android, etc) in order to restore them later
        string currentScriptingDefineSymbols = GetCurrentScriptingDefineSymbols();

        // Build the client for each client script symbol for this build target
        foreach (string clientScriptSymbol in clientScriptSymbols)
        {
            Debug.Log($"Building {clientScriptSymbol}");

            // Set the client script symbol
            SetClientScriptSymbol(clientScriptSymbol);

            // Build the client
            BuildPipeline.BuildPlayer(scenes, Path.Combine(buildPath, clientScriptSymbol, name), target, BuildOptions.None);
        }

        // Restore the scripting define symbols for this build target
        RestoreCurrentScriptingDefineSymbols(currentScriptingDefineSymbols);
    }

    static bool SetActiveBuildTarget(BuildTarget target)
    {
        // Only set the active build target if it's not already set
        if (EditorUserBuildSettings.activeBuildTarget == target) return true;

        // Set the active build target
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(target);
        bool switchSuccess = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);

        if (!switchSuccess)
        {
            Debug.LogError($"Failed to set active build target to {target}");
            return false;
        }
        return true;
    }

    #region Modifying Current Build Target Scripting Define Symbols
    static string GetCurrentScriptingDefineSymbols()
    {
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
    }

    static void RestoreCurrentScriptingDefineSymbols(string currentScriptingDefineSymbols)
    {
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, currentScriptingDefineSymbols);
    }

    static void SetClientScriptSymbol(string clientScriptSymbol)
    {
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

        // Get the current client script symbols
        List<string> currentScriptSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

        // Remove any client script symbols
        foreach (string scriptSymbol in clientScriptSymbols)
        {
            if (currentScriptSymbols.Contains(scriptSymbol))
            {
                currentScriptSymbols.Remove(scriptSymbol);
            }
        }

        // Add the required client script symbol
        currentScriptSymbols.Add(clientScriptSymbol);

        // Set the updated define symbols with required client symbol in it
        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", currentScriptSymbols));

        // Refresh the asset database
        CompilationPipeline.RequestScriptCompilation();
    }
    #endregion
}
