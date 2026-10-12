using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class StabilityRegressionTests
{
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        PlayerPrefs.DeleteKey("settings.masterVolumeDb");
    }

    [Test]
    public void StartTimerCompletesAfterCrossingBelowZero()
    {
        GameObject textObject = new GameObject(
            "TimerText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        Text timerText = textObject.GetComponent<Text>();

        GameObject timerObject = new GameObject("StartTimer");
        MonoBehaviour timer = AddRuntimeComponent(timerObject, "startTimer");
        SetField(timer, "timeText", timerText);
        SetField(timer, "timeLeft", -0.001f);

        Invoke(timer, "Update");

        Assert.AreEqual(0f, (float)GetField(timer, "timeLeft"));
        Assert.IsFalse(timerText.enabled);
        Assert.AreEqual(1f, Time.timeScale);

        UnityEngine.Object.DestroyImmediate(timerObject);
        UnityEngine.Object.DestroyImmediate(textObject);
    }

    [Test]
    public void LevelLoaderKeepsFirstRequestWhenButtonsArePressedRapidly()
    {
        GameObject loaderObject = new GameObject("levelLoader");
        MonoBehaviour loader = AddRuntimeComponent(loaderObject, "levelLoader");
        SetField(loader, "transitionTime", 10f);

        Invoke(loader, "LoadSomeLevel", NutritionHowScenes.Start);
        Invoke(loader, "LoadSomeLevel", NutritionHowScenes.Opening);

        Assert.IsTrue((bool)GetField(loader, "isLoading"));
        Assert.AreEqual(NutritionHowScenes.Start, (int)GetField(loader, "pendingSceneIndex"));

        UnityEngine.Object.DestroyImmediate(loaderObject);
    }

    [Test]
    public void LegacyDisplayAdapterCalculatesCenteredFourByThreeViewport()
    {
        Type adapterType = FindRuntimeType("LegacyDisplayAdapter");
        Assert.IsNotNull(adapterType);
        MethodInfo calculateViewport = adapterType.GetMethod(
            "CalculateViewport",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { typeof(int),typeof(int) },
            null);
        Assert.IsNotNull(calculateViewport);

        Rect wideViewport = (Rect)calculateViewport.Invoke(null,new object[] { 1280,800 });
        Assert.That(wideViewport.x,Is.EqualTo(1f/12f).Within(0.0001f));
        Assert.That(wideViewport.y,Is.EqualTo(0f).Within(0.0001f));
        Assert.That(wideViewport.width,Is.EqualTo(5f/6f).Within(0.0001f));
        Assert.That(wideViewport.height,Is.EqualTo(1f).Within(0.0001f));

        Rect tallViewport = (Rect)calculateViewport.Invoke(null,new object[] { 800,800 });
        Assert.That(tallViewport.x,Is.EqualTo(0f).Within(0.0001f));
        Assert.That(tallViewport.y,Is.EqualTo(0.125f).Within(0.0001f));
        Assert.That(tallViewport.width,Is.EqualTo(1f).Within(0.0001f));
        Assert.That(tallViewport.height,Is.EqualTo(0.75f).Within(0.0001f));
    }

    [Test]
    public void LegacyDisplayAdapterPreservesAuthoredSceneAspect()
    {
        Type adapterType = FindRuntimeType("LegacyDisplayAdapter");
        MethodInfo getTargetAspect = adapterType.GetMethod(
            "GetTargetAspect",
            BindingFlags.Static | BindingFlags.Public);
        MethodInfo calculateViewport = adapterType.GetMethod(
            "CalculateViewport",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { typeof(int),typeof(int),typeof(float) },
            null);

        Assert.That((float)getTargetAspect.Invoke(null,new object[] { "level1" }),
            Is.EqualTo(4f/3f).Within(0.0001f));
        Assert.That((float)getTargetAspect.Invoke(null,new object[] { "level4-2" }),
            Is.EqualTo(4f/3f).Within(0.0001f));
        Assert.That((float)getTargetAspect.Invoke(null,new object[] { "menu" }),
            Is.EqualTo(16f/9f).Within(0.0001f));
        Assert.That((float)getTargetAspect.Invoke(null,new object[] { "instruction1" }),
            Is.EqualTo(16f/9f).Within(0.0001f));

        Rect viewport = (Rect)calculateViewport.Invoke(
            null,
            new object[] { 1280,800,16f/9f });
        Assert.That(viewport.x,Is.EqualTo(0f).Within(0.0001f));
        Assert.That(viewport.y,Is.EqualTo(0.05f).Within(0.0001f));
        Assert.That(viewport.width,Is.EqualTo(1f).Within(0.0001f));
        Assert.That(viewport.height,Is.EqualTo(0.9f).Within(0.0001f));
    }

    [Test]
    public void VolumeManagerClampsAndPersistsMasterVolume()
    {
        Type managerType = FindRuntimeType("volumeManager");
        GameObject managerObject = new GameObject("VolumeManager");
        MonoBehaviour manager = (MonoBehaviour)managerObject.AddComponent(managerType);

        Invoke(manager,"SetVolume",-24f);
        Assert.That(PlayerPrefs.GetFloat("settings.masterVolumeDb"),
            Is.EqualTo(-24f).Within(0.001f));

        Invoke(manager,"SetVolume",20f);
        Assert.That(PlayerPrefs.GetFloat("settings.masterVolumeDb"),
            Is.EqualTo(0f).Within(0.001f));

        Invoke(manager,"SetVolume",-100f);
        Assert.That(PlayerPrefs.GetFloat("settings.masterVolumeDb"),
            Is.EqualTo(-80f).Within(0.001f));

        UnityEngine.Object.DestroyImmediate(managerObject);
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

    private static void Invoke(MonoBehaviour target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} is missing.");
        method.Invoke(target, arguments);
    }

    private static object GetField(MonoBehaviour target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} is missing.");
        return field.GetValue(target);
    }

    private static void SetField(MonoBehaviour target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} is missing.");
        field.SetValue(target, value);
    }
}
