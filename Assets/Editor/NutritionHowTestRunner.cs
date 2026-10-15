using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class NutritionHowTestRunner
{
    private static TestRunnerApi runner;
    private static TestCallbacks callbacks;

    [MenuItem("Tools/Nutrition HOW/Run All PlayMode Tests")]
    public static void RunAllPlayModeTests()
    {
        runner=ScriptableObject.CreateInstance<TestRunnerApi>();
        callbacks=new TestCallbacks();
        runner.RegisterCallbacks(callbacks);
        runner.Execute(new ExecutionSettings(new Filter
        {
            testMode=TestMode.PlayMode
        }));
    }

    private sealed class TestCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            // Scene callbacks can finish after the NUnit teardown. Keep their
            // writes isolated, then restore the production path two editor
            // updates after the whole run has completed.
            EditorApplication.delayCall+=() =>
                EditorApplication.delayCall+=RestoreProductionMetricsPath;
        }
    }

    private static void RestoreProductionMetricsPath()
    {
        Type metricsType=AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("GameplayPlaytestMetrics"))
            .FirstOrDefault(type => type!=null);
        MethodInfo configure=metricsType?.GetMethod(
            "ConfigureOutputForTests",
            BindingFlags.Static | BindingFlags.Public);
        configure?.Invoke(null,new object[] { null });
        callbacks=null;
        runner=null;
    }
}
