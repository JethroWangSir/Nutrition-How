using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GameplayFlowTests
{
    private const float SceneLoadTimeoutSeconds = 8f;

    [SetUp]
    public void SetUp()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResetProgress();
    }

    [TearDown]
    public void TearDown()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ResetProgress();
    }

    private static void ResetProgress()
    {
        Time.timeScale = 1f;
        Type progressType = FindRuntimeType("pass");
        if (progressType != null)
        {
            SetProgress(progressType, false);
        }
    }

    [UnityTest]
    [Order(1)]
    public IEnumerator StartAndOpeningFlowReachesMenu()
    {
        yield return LoadScene(0);
        MonoBehaviour startMenu = RequireBehaviour("menu", "start menu");
        SetTransitionTimeToZero();

        Invoke(startMenu, "StartGame");
        yield return WaitForScene(1, "opening");

        MonoBehaviour openingMenu = RequireBehaviour("menu", "opening menu");
        SetTransitionTimeToZero();

        Invoke(openingMenu, "Skip");
        yield return WaitForScene(2, "menu");
    }

    [UnityTest]
    [Order(2)]
    public IEnumerator MenuButtonsReachEveryLevelInstruction()
    {
        string[] routes = { "Level1", "Level2", "Level3", "Level4" };
        int[] expectedSceneIndexes = { 9, 11, 13, 18 };

        for (int index = 0; index < routes.Length; index++)
        {
            yield return LoadScene(2);
            MonoBehaviour menuController = RequireBehaviour("menu", "main menu");
            SetTransitionTimeToZero();

            Invoke(menuController, routes[index]);
            yield return WaitForScene(
                expectedSceneIndexes[index],
                $"level {index + 1} instruction");
        }
    }

    [UnityTest]
    [Order(3)]
    public IEnumerator CompletingAllLevelsLoadsEndingAndClearsProgress()
    {
        yield return LoadScene(2);
        foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            rootObject.SetActive(false);
        }

        GameObject loaderObject = new GameObject("levelLoader");
        MonoBehaviour loader = AddRuntimeComponent(loaderObject, "levelLoader");
        SetFloatField(loader, "transitionTime", 0f);

        GameObject progressObject = new GameObject("pass");
        MonoBehaviour progress = AddRuntimeComponent(progressObject, "pass");
        ReplaceProgressAnimations(progress);
        SetFloatField(progress, "endingDelay", 0f);

        SetProgress(progress.GetType(), true);
        Invoke(progress, "EvaluateCompletion");

        yield return WaitForScene(22, "ending");

        AssertProgressIsCleared(progress.GetType());
    }

    private static IEnumerator LoadScene(int buildIndex)
    {
        ResetProgress();
        AsyncOperation load = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        Assert.IsNotNull(load, $"Could not begin loading scene {buildIndex}.");

        while (!load.isDone)
        {
            yield return null;
        }

        DisableEventSystems();
        ResetProgress();

        yield return null;
        Assert.AreEqual(buildIndex, SceneManager.GetActiveScene().buildIndex);
    }

    private static IEnumerator WaitForScene(int buildIndex, string description)
    {
        float deadline = Time.realtimeSinceStartup + SceneLoadTimeoutSeconds;
        while (SceneManager.GetActiveScene().buildIndex != buildIndex &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.AreEqual(
            buildIndex,
            SceneManager.GetActiveScene().buildIndex,
            $"Timed out while loading {description}.");

        DisableEventSystems();
        // The level-loader prefab uses a 0.3 second transition. Allow any
        // already-started transition and scene-unload callbacks to finish
        // before the test starts another transition.
        yield return new WaitForSecondsRealtime(0.5f);
    }

    private static void DisableEventSystems()
    {
        foreach (EventSystem eventSystem in UnityEngine.Object.FindObjectsByType<EventSystem>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
        {
            eventSystem.enabled = false;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DisableEventSystems();
        ResetProgress();
    }

    private static MonoBehaviour RequireBehaviour(string typeName, string description)
    {
        MonoBehaviour behaviour = UnityEngine.Object
            .FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.GetType().Name == typeName);
        Assert.IsNotNull(behaviour, $"Could not find {description} in the active scene.");
        return behaviour;
    }

    private static void SetTransitionTimeToZero()
    {
        MonoBehaviour loader = RequireBehaviour("levelLoader", "level loader");
        SetFloatField(loader, "transitionTime", 0f);
    }

    private static void Invoke(MonoBehaviour target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName);
        Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} is missing.");
        method.Invoke(target, null);
    }

    private static Type FindRuntimeType(string typeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(typeName))
            .FirstOrDefault(type => type != null);
    }

    private static MonoBehaviour AddRuntimeComponent(GameObject target, string typeName)
    {
        Type type = FindRuntimeType(typeName);
        Assert.IsNotNull(type, $"Could not find runtime type {typeName}.");
        return (MonoBehaviour)target.AddComponent(type);
    }

    private static void SetFloatField(MonoBehaviour target, string fieldName, float value)
    {
        FieldInfo field = target.GetType().GetField(fieldName);
        Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} is missing.");
        field.SetValue(target, value);
    }

    private static void SetProgress(Type progressType, bool value)
    {
        foreach (string fieldName in new[]
                 { "level1Pass", "level2Pass", "level3Pass", "level4Pass" })
        {
            FieldInfo field = progressType.GetField(fieldName);
            Assert.IsNotNull(field, $"pass.{fieldName} is missing.");
            field.SetValue(null, value);
        }
    }

    private static void ReplaceProgressAnimations(MonoBehaviour progress)
    {
        FieldInfo field = progress.GetType().GetField("levelPassAnim");
        Assert.IsNotNull(field, "pass.levelPassAnim is missing.");

        GameObject[] inertIndicators =
        {
            new GameObject("TestLevel1Complete"),
            new GameObject("TestLevel2Complete"),
            new GameObject("TestLevel3Complete"),
            new GameObject("TestLevel4Complete")
        };
        field.SetValue(progress, inertIndicators);
    }

    private static void AssertProgressIsCleared(Type progressType)
    {
        foreach (string fieldName in new[]
                 { "level1Pass", "level2Pass", "level3Pass", "level4Pass" })
        {
            FieldInfo field = progressType.GetField(fieldName);
            Assert.IsNotNull(field, $"pass.{fieldName} is missing.");
            Assert.IsFalse((bool)field.GetValue(null), $"pass.{fieldName} was not cleared.");
        }
    }
}
