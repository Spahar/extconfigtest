using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Unity.EditorCoroutines.Editor;
using System.Linq;
using UnityEditor.Compilation;

// Check Documentation: https://www.youtube.com/watch?v=x566N_aEBeY

public class MultiBuildEditorTool : EditorWindow
{
    [MenuItem("Tools/Multi-Build Tool")]
    public static void OnShowTools() => GetWindow<MultiBuildEditorTool>();

    List<BuildTarget> desiredBuildTargets = new List<BuildTarget>() {
        BuildTarget.StandaloneWindows64,
        BuildTarget.StandaloneOSX,
        BuildTarget.iOS,
        BuildTarget.Android
    };

    BuildTargetGroup GetTargetGroupForTarget(BuildTarget target)
    {
        switch (target)
        {
            //case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneOSX:
                return BuildTargetGroup.Standalone;
            case BuildTarget.iOS:
                return BuildTargetGroup.iOS;
            case BuildTarget.Android:
                return BuildTargetGroup.Android;
            default:
                return BuildTargetGroup.Unknown;
        }
    }

    Dictionary<BuildTarget, bool> targetsToBuild = new Dictionary<BuildTarget, bool>();
    Dictionary<ClientData, bool> clientsToBuild = new Dictionary<ClientData, bool>();

    List<BuildTarget> availableTargets = new List<BuildTarget>();
    List<ClientData> availableClients = new List<ClientData>();
    List<string> availableClientsScriptSymbols = new List<string>();

    void OnEnable()
    {
        #region Clients
        availableClients = ClientsManager.ClientsList.clients;
        availableClientsScriptSymbols.Clear();

        foreach (var client in availableClients)
        {
            availableClientsScriptSymbols.Add(client.scriptingSymbol);

            // add the client if not in the build list
            if (!clientsToBuild.ContainsKey(client))
                clientsToBuild[client] = false;
        }

        // check if any clients have gone away
        if (clientsToBuild.Count > availableClients.Count)
        {
            // build the list of removed clients
            List<ClientData> clientsToRemove = new List<ClientData>();
            foreach (var client in clientsToBuild.Keys)
            {
                if (!availableClients.Contains(client))
                    clientsToRemove.Add(client);
            }

            // cleanup the removed clients
            foreach (var client in clientsToRemove)
                clientsToBuild.Remove(client);
        }
        #endregion

        #region Build Targets
        availableTargets.Clear();

        foreach (var buildTarget in desiredBuildTargets)
        {
            // skip if unsupported
            if (!BuildPipeline.IsBuildTargetSupported(GetTargetGroupForTarget(buildTarget), buildTarget))
                continue;

            availableTargets.Add(buildTarget);

            // add the target if not in the build list
            if (!targetsToBuild.ContainsKey(buildTarget))
                targetsToBuild[buildTarget] = false;
        }

        // check if any targets have gone away
        if (targetsToBuild.Count > availableTargets.Count)
        {
            // build the list of removed targets
            List<BuildTarget> targetsToRemove = new List<BuildTarget>();
            foreach (var target in targetsToBuild.Keys)
            {
                if (!availableTargets.Contains(target))
                    targetsToRemove.Add(target);
            }

            // cleanup the removed targets
            foreach (var target in targetsToRemove)
                targetsToBuild.Remove(target);
        }
        #endregion
    }

    private void OnGUI()
    {
        GUILayout.Label("Clients", EditorStyles.boldLabel);
        GUILayout.Space(5);

        //display clients
        int clientEnabled = 0;
        foreach (var client in availableClients)
        {
            clientsToBuild[client] = EditorGUILayout.Toggle(client.name, clientsToBuild[client]);

            if (clientsToBuild[client])
                clientEnabled++;

        }

        GUILayout.Space(20);
        GUILayout.Label("Platforms to Build", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // display the build targets
        int numEnabled = 0;
        foreach (var target in availableTargets)
        {
            targetsToBuild[target] = EditorGUILayout.Toggle(target.ToString(), targetsToBuild[target]);

            if (targetsToBuild[target])
                numEnabled++;
        }

        if (numEnabled > 0 && clientEnabled > 0)
        {
            // attempt to build?
            string prompt = numEnabled == 1 ? "Build 1 Platform" : $"Build {numEnabled} Platforms";
            prompt += " for " + (clientEnabled == 1 ? "1 Client" : $"{clientEnabled} Clients");

            if (GUILayout.Button(prompt))
            {
                List<BuildTarget> selectedTargets = targetsToBuild.Where(t => t.Value).Select(t => t.Key).ToList();
                List<ClientData> selectedClients = clientsToBuild.Where(c => c.Value).Select(c => c.Key).ToList();
                EditorCoroutineUtility.StartCoroutine(PerformBuild(selectedTargets, selectedClients), this);
            }
        }
    }

    IEnumerator PerformBuild(List<BuildTarget> buildTargets, List<ClientData> clients)
    {
        // show the progress display
        // int buildAllProgressID = Progress.Start("Build All", "Building all selected platforms", Progress.Options.Sticky);
        // Progress.ShowDetails();
        yield return new EditorWaitForSeconds(1f);

        BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
        // Get the current scripting define symbols for this build target (StandaloneWindows, Android, etc) in order to restore them later
        string originalScriptingDefineSymbols = GetCurrentScriptingDefineSymbols();

        foreach (BuildTarget buildTarget in buildTargets)
        {
            SetActiveBuildTarget(buildTarget);
            yield return new EditorWaitForSeconds(1f);

            foreach (ClientData client in clients)
            {
                // set the client script symbol
                SetClientScriptSymbol(client.scriptingSymbol);
                yield return new EditorWaitForSeconds(1f);

                // perform the build
                if (!BuildIndividualTarget(buildTarget, client.name))
                {
                    // Progress.Finish(buildTaskProgressID, Progress.Status.Failed);
                    // Progress.Finish(buildAllProgressID, Progress.Status.Failed);

                    if (EditorUserBuildSettings.activeBuildTarget != originalTarget)
                        EditorUserBuildSettings.SwitchActiveBuildTargetAsync(GetTargetGroupForTarget(originalTarget), originalTarget);
                    // Restore the scripting define symbols for this build target
                    RestoreCurrentScriptingDefineSymbols(originalScriptingDefineSymbols);

                    yield break;
                }

                // Progress.Finish(buildTaskProgressID, Progress.Status.Succeeded);
                yield return new EditorWaitForSeconds(1f);
            }
        }

        //Progress.Finish(buildAllProgressID, Progress.Status.Succeeded);

        if (EditorUserBuildSettings.activeBuildTarget != originalTarget)
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(GetTargetGroupForTarget(originalTarget), originalTarget);
        // Restore the scripting define symbols for this build target
        RestoreCurrentScriptingDefineSymbols(originalScriptingDefineSymbols);

        yield return null;
    }

    bool BuildIndividualTarget(BuildTarget target, string clientName)
    {
        BuildPlayerOptions options = new BuildPlayerOptions();

        // get the list of scenes
        List<string> scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        // configure the build
        options.scenes = scenes.ToArray();
        options.target = target;
        options.targetGroup = GetTargetGroupForTarget(target);

        // set the location path name
        if (target == BuildTarget.Android)
        {
            string apkName = PlayerSettings.productName + ".apk";
            options.locationPathName = System.IO.Path.Combine("Builds", target.ToString(), clientName, apkName);
        }
        else if (target == BuildTarget.StandaloneWindows64)
        {
            options.locationPathName = System.IO.Path.Combine("Builds", target.ToString(), clientName, PlayerSettings.productName + ".exe");
        }
        else if (target == BuildTarget.StandaloneLinux64)
        {
            options.locationPathName = System.IO.Path.Combine("Builds", target.ToString(), clientName, PlayerSettings.productName + ".x86_64");
        }
        else
            options.locationPathName = System.IO.Path.Combine("Builds", target.ToString(), clientName, PlayerSettings.productName);

        //if (BuildPipeline.BuildCanBeAppended(target, options.locationPathName) == CanAppendBuild.Yes)
        //    options.options = BuildOptions.AcceptExternalModificationsToPlayer;
        //else
        //    options.options = BuildOptions.None;

        // start the build
        BuildReport report = BuildPipeline.BuildPlayer(options);

        // was the build successful?
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build for client {clientName} for  {target.ToString()} completed in {report.summary.totalTime.Seconds} seconds");
            return true;
        }

        Debug.LogError($"Build for client {clientName} for {target.ToString()} failed");

        return false;
    }

    bool SetActiveBuildTarget(BuildTarget target)
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
    string GetCurrentScriptingDefineSymbols()
    {
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
    }

    void RestoreCurrentScriptingDefineSymbols(string currentScriptingDefineSymbols)
    {
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, currentScriptingDefineSymbols);
    }

    void SetClientScriptSymbol(string clientScriptSymbol)
    {
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

        // Get the current client script symbols
        List<string> currentScriptSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

        // Remove any client script symbols
        foreach (string scriptSymbol in availableClientsScriptSymbols)
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

