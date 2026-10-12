#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RuntimeVisualRegressionCapture : MonoBehaviour
{
    private const string EnableArgument = "--nutrition-how-visual-capture";
    private const string OutputArgument = "--capture-output";

    [Serializable]
    private sealed class CaptureManifest
    {
        public string generatedAtUtc;
        public string unityVersion;
        public int screenWidth;
        public int screenHeight;
        public List<CaptureResult> captures = new List<CaptureResult>();
    }

    [Serializable]
    private sealed class CaptureResult
    {
        public int buildIndex;
        public string sceneName;
        public string fileName;
        public Rect safeAreaPixels;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        if(!arguments.Contains(EnableArgument) || FindObjectsByType<RuntimeVisualRegressionCapture>(
               FindObjectsInactive.Include,
               FindObjectsSortMode.None).Length>0)
        {
            return;
        }

        GameObject captureObject = new GameObject(nameof(RuntimeVisualRegressionCapture));
        DontDestroyOnLoad(captureObject);
        captureObject.AddComponent<RuntimeVisualRegressionCapture>();
    }

    private IEnumerator Start()
    {
        string outputDirectory = GetOutputDirectory();
        Directory.CreateDirectory(outputDirectory);
        CaptureManifest manifest = new CaptureManifest
        {
            generatedAtUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            screenWidth = Screen.width,
            screenHeight = Screen.height
        };

        for(int sceneIndex=0;sceneIndex<SceneManager.sceneCountInBuildSettings;sceneIndex++)
        {
            Time.timeScale = 1f;
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneIndex,LoadSceneMode.Single);
            while(!loadOperation.isDone)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.25f);
            yield return new WaitForEndOfFrame();

            string sceneName = SceneManager.GetActiveScene().name;
            string fileName = $"{sceneIndex:D2}-{sceneName}.png";
            string screenshotPath = Path.Combine(outputDirectory,fileName);
            ScreenCapture.CaptureScreenshot(screenshotPath);
            yield return new WaitForEndOfFrame();
            while(!File.Exists(screenshotPath))
            {
                yield return null;
            }
            manifest.captures.Add(new CaptureResult
            {
                buildIndex = sceneIndex,
                sceneName = sceneName,
                fileName = fileName,
                safeAreaPixels = LegacyDisplayAdapter.SafeAreaPixels
            });
        }

        File.WriteAllText(
            Path.Combine(outputDirectory,"capture-manifest.json"),
            JsonUtility.ToJson(manifest,true));
        Application.Quit(0);
    }

    private static string GetOutputDirectory()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for(int index=0;index<arguments.Length-1;index++)
        {
            if(arguments[index]==OutputArgument)
            {
                return arguments[index+1];
            }
        }
        return Path.Combine(Application.persistentDataPath,"visual-regression");
    }
}
#endif
