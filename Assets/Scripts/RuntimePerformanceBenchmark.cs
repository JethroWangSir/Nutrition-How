#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public sealed class RuntimePerformanceBenchmark : MonoBehaviour
{
    private const string EnableArgument = "--nutrition-how-benchmark";
    private const string OutputArgument = "--benchmark-output";
    private const string PassesArgument = "--benchmark-passes";
    private const float SettleSeconds = 2f;
    private const float SampleSeconds = 5f;

    [Serializable]
    private sealed class BenchmarkReport
    {
        public string generatedAtUtc;
        public string unityVersion;
        public string applicationVersion;
        public string operatingSystem;
        public string processorType;
        public string graphicsDeviceName;
        public int systemMemoryMb;
        public int graphicsMemoryMb;
        public int screenWidth;
        public int screenHeight;
        public int vSyncCount;
        public int targetFrameRate;
        public List<SceneResult> scenes = new List<SceneResult>();
    }

    [Serializable]
    private sealed class SceneResult
    {
        public int buildIndex;
        public int pass;
        public string sceneName;
        public float loadMilliseconds;
        public int sampledFrames;
        public float averageFrameMilliseconds;
        public float p95FrameMilliseconds;
        public float worstFrameMilliseconds;
        public float averageFps;
        public float minimumFps;
        public float averageMainThreadMilliseconds;
        public float peakMainThreadMilliseconds;
        public long averageGcAllocatedBytesPerFrame;
        public long peakGcAllocatedBytesPerFrame;
        public int framesWithGcAllocations;
        public long peakTotalAllocatedBytes;
        public long peakTotalReservedBytes;
        public long peakMonoUsedBytes;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        if(!arguments.Contains(EnableArgument) || FindObjectsByType<RuntimePerformanceBenchmark>(
               FindObjectsInactive.Include,
               FindObjectsSortMode.None).Length>0)
        {
            return;
        }

        GameObject benchmarkObject = new GameObject("RuntimePerformanceBenchmark");
        DontDestroyOnLoad(benchmarkObject);
        benchmarkObject.AddComponent<RuntimePerformanceBenchmark>();
    }

    private IEnumerator Start()
    {
        yield return RunBenchmark();
    }

    private IEnumerator RunBenchmark()
    {
        BenchmarkReport report = CreateReport();
        int[] sceneIndexes =
        {
            NutritionHowScenes.Menu,
            NutritionHowScenes.Level1,
            NutritionHowScenes.Level2,
            NutritionHowScenes.Level3Part1,
            NutritionHowScenes.Level3Part2,
            NutritionHowScenes.Level4Part1
        };

        int passes = GetPositiveIntArgument(PassesArgument,1);
        for(int passIndex=1;passIndex<=passes;passIndex++)
        {
            foreach(int sceneIndex in sceneIndexes)
            {
                SceneResult result = new SceneResult
                {
                    buildIndex = sceneIndex,
                    pass = passIndex
                };
                float loadStartedAt = Time.realtimeSinceStartup;
                Time.timeScale = 1f;
                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneIndex,LoadSceneMode.Single);
                while(!loadOperation.isDone)
                {
                    yield return null;
                }

                result.loadMilliseconds = (Time.realtimeSinceStartup-loadStartedAt)*1000f;
                result.sceneName = SceneManager.GetActiveScene().name;
                yield return new WaitForSecondsRealtime(SettleSeconds);

                List<float> frameMilliseconds = new List<float>(600);
                List<float> mainThreadMilliseconds = new List<float>(600);
                long totalGcAllocatedBytes = 0;
                ProfilerRecorder mainThreadRecorder = ProfilerRecorder.StartNew(
                    ProfilerCategory.Internal,
                    "Main Thread");
                ProfilerRecorder gcRecorder = ProfilerRecorder.StartNew(
                    ProfilerCategory.Memory,
                    "GC Allocated In Frame");
                float sampleDeadline = Time.realtimeSinceStartup+SampleSeconds;
                while(Time.realtimeSinceStartup<sampleDeadline)
                {
                    yield return null;
                    float frameMs = Time.unscaledDeltaTime*1000f;
                    if(frameMs>0f)
                    {
                        frameMilliseconds.Add(frameMs);
                    }
                    if(mainThreadRecorder.Valid && mainThreadRecorder.LastValue>0)
                    {
                        mainThreadMilliseconds.Add(mainThreadRecorder.LastValue/1000000f);
                    }
                    if(gcRecorder.Valid && gcRecorder.LastValue>0)
                    {
                        long gcBytes = gcRecorder.LastValue;
                        totalGcAllocatedBytes += gcBytes;
                        result.framesWithGcAllocations++;
                        result.peakGcAllocatedBytesPerFrame = Math.Max(
                            result.peakGcAllocatedBytesPerFrame,
                            gcBytes);
                    }
                    result.peakTotalAllocatedBytes = Math.Max(
                        result.peakTotalAllocatedBytes,
                        Profiler.GetTotalAllocatedMemoryLong());
                    result.peakTotalReservedBytes = Math.Max(
                        result.peakTotalReservedBytes,
                        Profiler.GetTotalReservedMemoryLong());
                    result.peakMonoUsedBytes = Math.Max(
                        result.peakMonoUsedBytes,
                        Profiler.GetMonoUsedSizeLong());
                }
                mainThreadRecorder.Dispose();
                gcRecorder.Dispose();

                PopulateFrameStatistics(result,frameMilliseconds);
                PopulateProfilerStatistics(
                    result,
                    mainThreadMilliseconds,
                    totalGcAllocatedBytes);
                report.scenes.Add(result);
                Debug.Log(
                    $"BENCHMARK pass {result.pass} {result.sceneName}: " +
                    $"{result.averageFps:F1} FPS average, " +
                    $"{result.p95FrameMilliseconds:F2} ms P95, " +
                    $"{result.averageMainThreadMilliseconds:F2} ms main thread, " +
                    $"{result.averageGcAllocatedBytesPerFrame} B/frame GC, " +
                    $"{result.peakTotalAllocatedBytes/1048576f:F1} MiB allocated.");
            }
        }

        string outputPath = GetOutputPath();
        string outputDirectory = Path.GetDirectoryName(outputPath);
        if(!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        File.WriteAllText(outputPath,JsonUtility.ToJson(report,true));
        Debug.Log($"BENCHMARK_COMPLETE {outputPath}");
        Application.Quit(0);
    }

    private static BenchmarkReport CreateReport()
    {
        return new BenchmarkReport
        {
            generatedAtUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            applicationVersion = Application.version,
            operatingSystem = SystemInfo.operatingSystem,
            processorType = SystemInfo.processorType,
            graphicsDeviceName = SystemInfo.graphicsDeviceName,
            systemMemoryMb = SystemInfo.systemMemorySize,
            graphicsMemoryMb = SystemInfo.graphicsMemorySize,
            screenWidth = Screen.width,
            screenHeight = Screen.height,
            vSyncCount = QualitySettings.vSyncCount,
            targetFrameRate = Application.targetFrameRate
        };
    }

    private static void PopulateFrameStatistics(SceneResult result,List<float> frameMilliseconds)
    {
        frameMilliseconds.Sort();
        result.sampledFrames = frameMilliseconds.Count;
        if(frameMilliseconds.Count==0)
        {
            return;
        }

        result.averageFrameMilliseconds = frameMilliseconds.Average();
        int p95Index = Mathf.Clamp(
            Mathf.CeilToInt(frameMilliseconds.Count*0.95f)-1,
            0,
            frameMilliseconds.Count-1);
        result.p95FrameMilliseconds = frameMilliseconds[p95Index];
        result.worstFrameMilliseconds = frameMilliseconds[frameMilliseconds.Count-1];
        result.averageFps = 1000f/result.averageFrameMilliseconds;
        result.minimumFps = 1000f/result.worstFrameMilliseconds;
    }

    private static void PopulateProfilerStatistics(
        SceneResult result,
        List<float> mainThreadMilliseconds,
        long totalGcAllocatedBytes)
    {
        if(mainThreadMilliseconds.Count>0)
        {
            result.averageMainThreadMilliseconds = mainThreadMilliseconds.Average();
            result.peakMainThreadMilliseconds = mainThreadMilliseconds.Max();
        }
        if(result.sampledFrames>0)
        {
            result.averageGcAllocatedBytesPerFrame =
                totalGcAllocatedBytes/result.sampledFrames;
        }
    }

    private static string GetOutputPath()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for(int index=0;index<arguments.Length-1;index++)
        {
            if(arguments[index]==OutputArgument)
            {
                return arguments[index+1];
            }
        }
        return Path.Combine(Application.persistentDataPath,"performance-benchmark.json");
    }

    private static int GetPositiveIntArgument(string argumentName,int fallback)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for(int index=0;index<arguments.Length-1;index++)
        {
            if(arguments[index]==argumentName &&
               int.TryParse(arguments[index+1],out int value) &&
               value>0)
            {
                return value;
            }
        }
        return fallback;
    }
}
#endif
