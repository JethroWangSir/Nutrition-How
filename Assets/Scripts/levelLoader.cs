using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class levelLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionTime;
    private bool isLoading;
    private int pendingSceneIndex = -1;

    public void LoadNextLevel()
    {
        RequestLoad(SceneManager.GetActiveScene().buildIndex+1);
    }

    public void LoadThisLevel()
    {
        RequestLoad(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadPreviousLevel()
    {
        RequestLoad(SceneManager.GetActiveScene().buildIndex-1);
    }

    public void LoadSomeLevel(int levelNum)
    {
        RequestLoad(levelNum);
    }

    void RequestLoad(int levelIndex)
    {
        if(isLoading)
        {
            return;
        }
        if(levelIndex<0 || levelIndex>=SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Cannot load scene index {levelIndex}; Build Settings contains {SceneManager.sceneCountInBuildSettings} scenes.");
            return;
        }
        isLoading=true;
        pendingSceneIndex=levelIndex;
        // A scene change can be requested from pause, dialog, or failure UI.
        // Always restore gameplay time here so individual callers cannot leave
        // the destination scene frozen.
        Time.timeScale=1f;
        StartCoroutine(LoadLevel(levelIndex));
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        // var loadingOperation = SceneManager.LoadSceneAsync(levelIndex);
        // loadingOperation.allowSceneActivation = false;
        if(transition != null)
        {
            transition.SetTrigger("start");
        }
        // Keep navigation independent from gameplay time. This also prevents a
        // transition from hanging if another system pauses during the fade.
        yield return new WaitForSecondsRealtime(transitionTime);
        SceneManager.LoadScene(levelIndex);
        // loadingOperation.allowSceneActivation = true;
    }
}
