using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class NutritionHowSceneDependencyAudit
{
    private const string OutputEnvironmentVariable = "NUTRITION_HOW_AUDIT_PATH";
    private const string TongueAnimationPath = "Assets/Animation/tongue/tongue.anim";

    [Serializable]
    private sealed class AuditReport
    {
        public string generatedAtUtc;
        public string unityVersion;
        public List<SceneAudit> scenes = new List<SceneAudit>();
        public List<DependencyAudit> level1UniqueDependencies = new List<DependencyAudit>();
    }

    [Serializable]
    private sealed class SceneAudit
    {
        public string scenePath;
        public int dependencyCount;
        public long sourceBytes;
        public List<DependencyAudit> largestDependencies = new List<DependencyAudit>();
    }

    [Serializable]
    private sealed class DependencyAudit
    {
        public string path;
        public string assetType;
        public long sourceBytes;
    }

    [MenuItem("Tools/Nutrition HOW/Audit Scene Dependencies")]
    public static void AuditSceneDependencies()
    {
        string[] scenePaths =
        {
            "Assets/Scenes/menu.unity",
            "Assets/Scenes/level1.unity",
            "Assets/Scenes/level2.unity",
            "Assets/Scenes/level3-1.unity",
            "Assets/Scenes/level3-2.unity",
            "Assets/Scenes/level4-1.unity"
        };

        Dictionary<string,List<DependencyAudit>> dependenciesByScene = scenePaths
            .ToDictionary(path => path,path => GetDependencies(path));
        AuditReport report = new AuditReport
        {
            generatedAtUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion
        };

        foreach(string scenePath in scenePaths)
        {
            List<DependencyAudit> dependencies = dependenciesByScene[scenePath];
            report.scenes.Add(new SceneAudit
            {
                scenePath = scenePath,
                dependencyCount = dependencies.Count,
                sourceBytes = dependencies.Sum(dependency => dependency.sourceBytes),
                largestDependencies = dependencies
                    .OrderByDescending(dependency => dependency.sourceBytes)
                    .Take(30)
                    .ToList()
            });
        }

        HashSet<string> otherSceneDependencies = new HashSet<string>(
            dependenciesByScene
                .Where(pair => pair.Key != "Assets/Scenes/level1.unity")
                .SelectMany(pair => pair.Value)
                .Select(dependency => dependency.path));
        report.level1UniqueDependencies = dependenciesByScene["Assets/Scenes/level1.unity"]
            .Where(dependency => !otherSceneDependencies.Contains(dependency.path))
            .OrderByDescending(dependency => dependency.sourceBytes)
            .Take(50)
            .ToList();

        string outputPath = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
        if(string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "Docs/scene-dependency-audit.json");
        }
        string outputDirectory = Path.GetDirectoryName(outputPath);
        if(!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        File.WriteAllText(outputPath,JsonUtility.ToJson(report,true));
        Debug.Log($"Scene dependency audit written to {outputPath}");
    }

    [MenuItem("Tools/Nutrition HOW/Optimize Tongue Animation To 30 FPS")]
    public static void OptimizeTongueAnimationTo30Fps()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(TongueAnimationPath);
        if(clip == null)
        {
            throw new FileNotFoundException("Tongue animation was not found.",TongueAnimationPath);
        }

        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        if(bindings.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected one sprite curve in the tongue animation, found {bindings.Length}.");
        }

        ObjectReferenceKeyframe[] originalFrames = AnimationUtility.GetObjectReferenceCurve(
            clip,
            bindings[0]);
        if(originalFrames.Length <= 114)
        {
            Debug.Log($"Tongue animation is already optimized ({originalFrames.Length} frames).");
            return;
        }
        if(originalFrames.Length != 226)
        {
            throw new InvalidOperationException(
                $"Expected 226 tongue frames before optimization, found {originalFrames.Length}.");
        }

        ObjectReferenceKeyframe[] optimizedFrames = originalFrames
            .Where((frame,index) => index % 2 == 0)
            .ToArray();
        Undo.RecordObject(clip,"Optimize tongue animation to 30 FPS");
        AnimationUtility.SetObjectReferenceCurve(clip,bindings[0],optimizedFrames);
        clip.frameRate = 30f;
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"Tongue animation optimized from {originalFrames.Length} to " +
            $"{optimizedFrames.Length} frames while preserving its duration.");
    }

    private static List<DependencyAudit> GetDependencies(string scenePath)
    {
        return AssetDatabase.GetDependencies(scenePath,true)
            .Where(path => !path.EndsWith(".cs",StringComparison.OrdinalIgnoreCase))
            .Select(path => new DependencyAudit
            {
                path = path,
                assetType = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name ?? "Unknown",
                sourceBytes = GetSourceBytes(path)
            })
            .ToList();
    }

    private static long GetSourceBytes(string assetPath)
    {
        string absolutePath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            assetPath);
        return File.Exists(absolutePath) ? new FileInfo(absolutePath).Length : 0L;
    }
}
