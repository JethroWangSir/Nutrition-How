using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayPlaytestMetrics
{
    [Serializable]
    private class PlaytestRecord
    {
        public string timestampUtc;
        public int level;
        public int attempt;
        public string outcome;
        public string reason;
        public float elapsedSeconds;
    }

    private static readonly Dictionary<int,float> StartTimes=new Dictionary<int,float>();
    private static readonly Dictionary<int,int> AttemptCounts=new Dictionary<int,int>();
    private static string outputPathOverride;

    public static string OutputPath => string.IsNullOrEmpty(outputPathOverride)
        ? Path.Combine(Application.persistentDataPath,"playtest-metrics.jsonl")
        : outputPathOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        StartTimes.Clear();
        AttemptCounts.Clear();
        SceneManager.sceneLoaded-=OnSceneLoaded;
        SceneManager.sceneLoaded+=OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        int level=GetStartingLevel(scene.buildIndex);
        if(level>0)
        {
            BeginLevel(level);
        }
    }

    private static int GetStartingLevel(int sceneIndex)
    {
        if(sceneIndex==NutritionHowScenes.Level1) return 1;
        if(sceneIndex==NutritionHowScenes.Level2) return 2;
        if(sceneIndex==NutritionHowScenes.Level3Part1) return 3;
        if(sceneIndex==NutritionHowScenes.Level4Part1) return 4;
        return 0;
    }

    private static void BeginLevel(int level)
    {
        AttemptCounts.TryGetValue(level,out int attempts);
        AttemptCounts[level]=attempts+1;
        StartTimes[level]=Time.realtimeSinceStartup;
    }

    public static void CompleteLevel(int level)
    {
        WriteOutcome(level,"completed",string.Empty);
    }

    public static void FailActiveLevel(string reason)
    {
        int level=GetLevelForScene(SceneManager.GetActiveScene().buildIndex);
        if(level>0)
        {
            WriteOutcome(level,"failed",reason);
        }
    }

    private static int GetLevelForScene(int sceneIndex)
    {
        if(sceneIndex==NutritionHowScenes.Level1) return 1;
        if(sceneIndex==NutritionHowScenes.Level2) return 2;
        if(sceneIndex==NutritionHowScenes.Level3Part1 || sceneIndex==NutritionHowScenes.Level3Part2) return 3;
        if(sceneIndex==NutritionHowScenes.Level4Part1 || sceneIndex==NutritionHowScenes.Level4Part2) return 4;
        return 0;
    }

    private static void WriteOutcome(int level,string outcome,string reason)
    {
        if(!StartTimes.TryGetValue(level,out float startTime))
        {
            return;
        }

        StartTimes.Remove(level);
        AttemptCounts.TryGetValue(level,out int attempt);
        PlaytestRecord record=new PlaytestRecord
        {
            timestampUtc=DateTime.UtcNow.ToString("o"),
            level=level,
            attempt=attempt,
            outcome=outcome,
            reason=reason,
            elapsedSeconds=Mathf.Max(0f,Time.realtimeSinceStartup-startTime)
        };

        try
        {
            string directory=Path.GetDirectoryName(OutputPath);
            if(!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.AppendAllText(OutputPath,JsonUtility.ToJson(record)+Environment.NewLine);
        }
        catch(Exception exception)
        {
            Debug.LogWarning($"Could not save local playtest metrics: {exception.Message}");
        }
    }

    public static void ConfigureOutputForTests(string path)
    {
        outputPathOverride=path;
        StartTimes.Clear();
        AttemptCounts.Clear();
    }

    public static void BeginLevelForTests(int level)
    {
        BeginLevel(level);
    }
}
