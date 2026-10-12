using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class NutritionHowBuild
{
    private const string DefaultOutputPath = "/tmp/營養 How 解-macOS-arm64.app";

    [MenuItem("Build/Nutrition HOW/macOS Apple Silicon")]
    public static void BuildMacArm64()
    {
        BuildMacArm64(BuildOptions.CompressWithLz4HC);
    }

    [MenuItem("Build/Nutrition HOW/macOS Apple Silicon Development")]
    public static void BuildMacArm64Development()
    {
        BuildMacArm64(
            BuildOptions.CompressWithLz4HC |
            BuildOptions.Development |
            BuildOptions.AllowDebugging);
    }

    [MenuItem("Build/Nutrition HOW/macOS Apple Silicon Visual Validation")]
    public static void BuildMacArm64VisualValidation()
    {
        BuildMacArm64(
            BuildOptions.CompressWithLz4 |
            BuildOptions.Development |
            BuildOptions.AllowDebugging);
    }

    private static void BuildMacArm64(BuildOptions options)
    {
        ConfigureArm64Architecture();

        string outputPath = Environment.GetEnvironmentVariable("NUTRITION_HOW_BUILD_PATH");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = DefaultOutputPath;
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneOSX,
            options = options
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Apple Silicon build failed: {report.summary.result}");
        }
    }

    private static void ConfigureArm64Architecture()
    {
        Assembly editorAssembly = typeof(PlayerSettings).Assembly;
        Type buildProfileType = editorAssembly.GetType(
            "UnityEditor.Build.Profile.BuildProfile");
        MethodInfo setActiveProfile = buildProfileType?
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(method =>
                method.Name == "SetActiveBuildProfile" &&
                method.GetParameters().Length == 1);
        Type buildProfileContextType = editorAssembly.GetType(
            "UnityEditor.Build.Profile.BuildProfileContext");
        MethodInfo getClassicProfile = buildProfileContextType?.GetMethod(
            "GetForClassicPlatform",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(BuildTarget), typeof(StandaloneBuildSubtarget) },
            null);
        PropertyInfo contextInstanceProperty = buildProfileContextType?.GetProperty(
            "instance",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        object buildProfileContext = contextInstanceProperty?.GetValue(null);
        if (setActiveProfile == null || getClassicProfile == null || buildProfileContext == null)
        {
            throw new InvalidOperationException(
                "Unable to select Unity's classic macOS Build Profile.");
        }

        // Unity 6 Build Profiles can override the global PlayerSettings value.
        // Selecting the classic profile makes the architecture below authoritative.
        setActiveProfile.Invoke(null, new object[] { null });
        PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 1);

        object classicProfile = getClassicProfile.Invoke(
            buildProfileContext,
            new object[] { BuildTarget.StandaloneOSX, StandaloneBuildSubtarget.Player });
        PropertyInfo platformProfileProperty = buildProfileType.GetProperty(
            "platformBuildProfile",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        object platformProfile = platformProfileProperty?.GetValue(classicProfile);
        PropertyInfo architectureProperty = platformProfile?.GetType().GetProperty(
            "architecture",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (classicProfile == null || platformProfile == null || architectureProperty == null)
        {
            throw new InvalidOperationException(
                "Unable to configure Unity's classic macOS Build Profile for ARM64.");
        }

        architectureProperty.SetValue(platformProfile, OSArchitecture.ARM64);
        EditorUtility.SetDirty((UnityEngine.Object)classicProfile);
        AssetDatabase.SaveAssets();
    }
}
