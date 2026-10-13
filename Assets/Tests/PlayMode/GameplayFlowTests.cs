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
            MethodInfo clearProgress = progressType.GetMethod(
                "ClearProgress",
                BindingFlags.Static | BindingFlags.Public);
            if(clearProgress != null)
            {
                clearProgress.Invoke(null,null);
            }else
            {
                SetProgress(progressType, false);
            }
        }
    }

    [UnityTest]
    [Order(1)]
    public IEnumerator StartAndOpeningFlowReachesMenu()
    {
        yield return LoadScene(NutritionHowScenes.Start);
        MonoBehaviour startMenu = RequireBehaviour("menu", "start menu");
        SetTransitionTimeToZero();

        Invoke(startMenu, "StartGame");
        yield return WaitForScene(NutritionHowScenes.Opening, "opening");

        MonoBehaviour openingMenu = RequireBehaviour("menu", "opening menu");
        SetTransitionTimeToZero();

        Invoke(openingMenu, "Skip");
        yield return WaitForScene(NutritionHowScenes.Menu, "menu");
    }

    [UnityTest]
    [Order(2)]
    public IEnumerator MenuButtonsReachEveryLevelInstruction()
    {
        string[] routes = { "Level1", "Level2", "Level3", "Level4" };
        int[] expectedSceneIndexes =
        {
            NutritionHowScenes.Instruction1,
            NutritionHowScenes.Instruction2,
            NutritionHowScenes.Instruction3Part1,
            NutritionHowScenes.Instruction4
        };

        for (int index = 0; index < routes.Length; index++)
        {
            yield return LoadScene(NutritionHowScenes.Menu);
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
        yield return LoadScene(NutritionHowScenes.Menu);
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

        yield return WaitForScene(NutritionHowScenes.Ending, "ending");

        AssertProgressIsCleared(progress.GetType());
    }

    [UnityTest]
    [Order(4)]
    public IEnumerator PauseResumeAndHomeRestoreTimeAndReachMenu()
    {
        yield return LoadScene(NutritionHowScenes.Level1);
        MonoBehaviour menuController = RequireBehaviour("menu", "level menu");

        Invoke(menuController, "Pause");
        Assert.AreEqual(0f, Time.timeScale);

        Invoke(menuController, "Resume");
        Assert.AreEqual(1f, Time.timeScale);

        SetTransitionTimeToZero();
        Invoke(menuController, "Pause");
        Invoke(menuController, "Home");

        yield return WaitForScene(NutritionHowScenes.Menu, "menu after pause");
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    [Order(5)]
    public IEnumerator DirectSceneLoadWhilePausedRestoresTimeAndCompletesTransition()
    {
        yield return LoadScene(NutritionHowScenes.Level1);
        MonoBehaviour loader = RequireBehaviour("levelLoader", "level loader");
        SetFloatField(loader, "transitionTime", 0.05f);

        Time.timeScale = 0f;
        Invoke(loader, "LoadSomeLevel", NutritionHowScenes.Menu);

        yield return WaitForScene(NutritionHowScenes.Menu, "menu requested while paused");
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    [Order(6)]
    public IEnumerator DeathDialogAppearsAndRestartReloadsTheLevel()
    {
        yield return LoadScene(NutritionHowScenes.Level1);
        MonoBehaviour activePlayer = RequireBehaviour("player", "level 1 player");
        GameObject loseDialog = (GameObject)GetField(activePlayer, "loseDialog");

        SetField(activePlayer, "hp", 0);
        Invoke(activePlayer, "Hpfunction");
        yield return new WaitForSecondsRealtime(1.6f);

        Assert.IsTrue(loseDialog.activeSelf, "The death dialog did not appear.");
        Assert.AreEqual(0f, Time.timeScale, "Gameplay continued behind the failure dialog.");

        MonoBehaviour menuController = RequireBehaviour("menu", "level menu");
        SetTransitionTimeToZero();
        int previousSceneHandle = SceneManager.GetActiveScene().handle;
        Invoke(menuController, "Restart");

        yield return WaitForSceneReload(previousSceneHandle, NutritionHowScenes.Level1);

        MonoBehaviour restartedPlayer = RequireBehaviour("player", "restarted level 1 player");
        GameObject restartedLoseDialog = (GameObject)GetField(restartedPlayer, "loseDialog");
        Assert.IsFalse(restartedLoseDialog.activeSelf, "The death dialog remained open after restart.");
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    [Order(7)]
    public IEnumerator BossDefeatOpensTheExitOnce()
    {
        yield return LoadScene(NutritionHowScenes.Level1);
        MonoBehaviour boss = RequireBehaviour("caker", "level 1 boss");
        GameObject exitDoor = (GameObject)GetField(boss, "door");

        SetField(boss, "hp", 0);
        Invoke(boss, "Hpfunction");
        yield return new WaitForSecondsRealtime(1.1f);

        Assert.IsTrue(exitDoor.activeSelf, "The exit did not open after the boss was defeated.");
        Assert.IsTrue((bool)GetField(boss, "deathScheduled"));
    }

    [UnityTest]
    [Order(8)]
    public IEnumerator ThirdLevelShipRetainsUsableFlightAndDamageValues()
    {
        yield return LoadScene(NutritionHowScenes.Level3Part2);
        MonoBehaviour levelShip = RequireBehaviour("ship", "level 3 ship");

        Assert.Greater((float)GetField(levelShip, "jumpForce"), 0f);
        Assert.Greater((float)GetField(levelShip, "hurtForce"), 0f);
        Assert.Greater((float)GetField(levelShip, "speed"), 0f);
        Assert.Greater((int)GetField(levelShip, "max_hp"), 0);
    }

    [UnityTest]
    [Order(9)]
    public IEnumerator FourthLevelTimeoutShowsLoseDialogAndStopsWaterTrigger()
    {
        yield return LoadScene(NutritionHowScenes.Level4Part1);
        MonoBehaviour levelShip = RequireBehaviour("ship4", "level 4 ship");
        GameObject loseDialog = (GameObject)GetField(levelShip, "loseDialog");
        GameObject water = (GameObject)GetField(levelShip, "water");

        SetField(levelShip, "timeLeft", 5.1f);
        Invoke(levelShip, "Timer");
        Assert.AreEqual(4, (int)GetField(levelShip, "countdownImageIndex"));

        SetField(levelShip, "timeLeft", 4.1f);
        Invoke(levelShip, "Timer");
        Assert.AreEqual(3, (int)GetField(levelShip, "countdownImageIndex"));

        SetField(levelShip, "timeLeft", 0f);
        Invoke(levelShip, "Timer");

        Assert.IsTrue(loseDialog.activeSelf, "The timeout dialog did not appear.");
        Assert.IsTrue((bool)GetField(levelShip, "timerExpired"));
        Assert.IsFalse(water.GetComponent<BoxCollider2D>().enabled);
        Assert.AreEqual(0f, Time.timeScale, "Gameplay continued behind the timeout dialog.");
    }

    [UnityTest]
    [Order(10)]
    public IEnumerator PlatformJumpBuffersInputUntilLanding()
    {
        yield return LoadScene(NutritionHowScenes.Level1);
        MonoBehaviour activePlayer = RequireBehaviour("player", "level 1 player");
        activePlayer.enabled = false;
        Rigidbody2D body = activePlayer.GetComponent<Rigidbody2D>();
        body.linearVelocity = Vector2.zero;

        SetField(activePlayer, "isGround", false);
        Invoke(activePlayer, "QueueJump");
        Invoke(activePlayer, "UpdateJumpGraceTimers", 0.02f);
        Assert.Greater((float)GetField(activePlayer, "jumpBufferTimer"), 0f);

        SetField(activePlayer, "isGround", true);
        Invoke(activePlayer, "UpdateJumpGraceTimers", 0.02f);
        Invoke(activePlayer, "TryBufferedJump");

        Assert.Greater(body.linearVelocity.y, 0f,
            "A jump pressed just before landing was not performed on landing.");
        Assert.AreEqual(0f, (float)GetField(activePlayer, "jumpBufferTimer"));
    }

    [UnityTest]
    [Order(11)]
    public IEnumerator PlatformJumpAllowsBriefCoyoteTimeAfterLeavingGround()
    {
        yield return LoadScene(NutritionHowScenes.Level1);
        MonoBehaviour activePlayer = RequireBehaviour("player", "level 1 player");
        activePlayer.enabled = false;
        Rigidbody2D body = activePlayer.GetComponent<Rigidbody2D>();
        body.linearVelocity = Vector2.zero;

        SetField(activePlayer, "isGround", true);
        Invoke(activePlayer, "UpdateJumpGraceTimers", 0.02f);
        SetField(activePlayer, "isGround", false);
        Invoke(activePlayer, "UpdateJumpGraceTimers", 0.04f);
        Invoke(activePlayer, "QueueJump");
        Invoke(activePlayer, "TryBufferedJump");

        Assert.Greater(body.linearVelocity.y, 0f,
            "A jump pressed just after leaving a platform was not accepted.");
        Assert.AreEqual(0f, (float)GetField(activePlayer, "coyoteTimer"));
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

    private static IEnumerator WaitForSceneReload(int previousHandle, int expectedBuildIndex)
    {
        float deadline = Time.realtimeSinceStartup + SceneLoadTimeoutSeconds;
        while (SceneManager.GetActiveScene().handle == previousHandle &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.AreNotEqual(
            previousHandle,
            SceneManager.GetActiveScene().handle,
            "Timed out while reloading the active scene.");
        Assert.AreEqual(expectedBuildIndex, SceneManager.GetActiveScene().buildIndex);
        DisableEventSystems();
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

    private static void Invoke(MonoBehaviour target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} is missing.");
        method.Invoke(target, arguments);
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

    private static object GetField(MonoBehaviour target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} is missing.");
        return field.GetValue(target);
    }

    private static void SetField(MonoBehaviour target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic);
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
