using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RuntimeStressTests
{
    private static readonly int[] StressSceneIndexes =
    {
        NutritionHowScenes.Level1,
        NutritionHowScenes.Level2,
        NutritionHowScenes.Level3Part1,
        NutritionHowScenes.Level4Part1
    };

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator GameplayScenesRunUnderAutomatedStressWithoutRuntimeErrors()
    {
        foreach(int sceneIndex in StressSceneIndexes)
        {
            Time.timeScale = 1f;
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneIndex,LoadSceneMode.Single);
            Assert.IsNotNull(load,$"Could not load stress scene {sceneIndex}.");
            while(!load.isDone)
            {
                yield return null;
            }

            foreach(EventSystem eventSystem in UnityEngine.Object.FindObjectsByType<EventSystem>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None))
            {
                eventSystem.enabled = false;
            }

            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.IsTrue(
                behaviours.Any(IsGameplayBehaviour),
                $"Scene {SceneManager.GetActiveScene().name} has no active gameplay behaviour.");

            // Accelerate hostile projectile creation in scenes that have a
            // saliva spawner, then play every already-active particle system.
            // Unexpected exceptions and error logs fail Unity tests by default.
            foreach(MonoBehaviour behaviour in behaviours.Where(
                        candidate => candidate.GetType().Name == "salivacreator"))
            {
                SetFloatField(behaviour,"cd_time",0.05f);
            }
            foreach(ParticleSystem particles in UnityEngine.Object.FindObjectsByType<ParticleSystem>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None))
            {
                particles.Play(true);
            }

            Time.timeScale = 2f;
            for(int frame=0;frame<120;frame++)
            {
                yield return null;
            }

            Assert.AreEqual(sceneIndex,SceneManager.GetActiveScene().buildIndex);
        }
    }

    private static bool IsGameplayBehaviour(MonoBehaviour behaviour)
    {
        string typeName = behaviour.GetType().Name;
        return typeName == "player" ||
               typeName == "ship" ||
               typeName == "ship4" ||
               typeName == "enemy" ||
               typeName == "virus" ||
               typeName == "bacterial" ||
               typeName == "caker";
    }

    private static void SetFloatField(MonoBehaviour target,string fieldName,float value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field,$"{target.GetType().Name}.{fieldName} is missing.");
        field.SetValue(target,value);
    }
}
