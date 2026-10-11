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

        Invoke(loader, "LoadSomeLevel", 0);
        Invoke(loader, "LoadSomeLevel", 1);

        Assert.IsTrue((bool)GetField(loader, "isLoading"));
        Assert.AreEqual(0, (int)GetField(loader, "pendingSceneIndex"));

        UnityEngine.Object.DestroyImmediate(loaderObject);
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
