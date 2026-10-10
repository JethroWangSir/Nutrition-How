using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class BuildSceneSmokeTests
{
    [UnityTest]
    public IEnumerator AllBuildScenesLoadWithoutRuntimeErrors()
    {
        Assert.Greater(SceneManager.sceneCountInBuildSettings, 0, "Build Settings contains no scenes.");
        List<string> unexpectedLogs = new List<string>();

        Application.LogCallback logHandler = (condition, stackTrace, type) =>
        {
            if (type != LogType.Log)
            {
                unexpectedLogs.Add($"[{type}] {condition}\n{stackTrace}");
            }
        };

        Application.logMessageReceived += logHandler;

        try
        {
            for (int buildIndex = 0; buildIndex < SceneManager.sceneCountInBuildSettings; buildIndex++)
            {
                unexpectedLogs.Clear();
                string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);

                Assert.IsTrue(
                    Application.CanStreamedLevelBeLoaded(scenePath),
                    $"Scene is missing from Build Settings: {scenePath}");

                AsyncOperation load = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                Assert.IsNotNull(load, $"Unity could not begin loading scene: {scenePath}");

                while (!load.isDone)
                {
                    yield return null;
                }

                yield return null;
                yield return new WaitForSecondsRealtime(0.25f);

                Assert.AreEqual(scenePath, SceneManager.GetActiveScene().path);
                Assert.IsEmpty(
                    unexpectedLogs,
                    $"Unexpected runtime logs while loading {scenePath}:\n{string.Join("\n", unexpectedLogs)}");
            }
        }
        finally
        {
            Application.logMessageReceived -= logHandler;
        }
    }
}
