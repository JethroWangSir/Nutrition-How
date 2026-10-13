using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pass : MonoBehaviour
{
    private static readonly string[] ProgressKeys =
    {
        "progress.level1Complete",
        "progress.level2Complete",
        "progress.level3Complete",
        "progress.level4Complete"
    };
    static public bool level1Pass,level2Pass,level3Pass,level4Pass;
    public GameObject[] levelPassAnim;
    public float timeLeft,time;
    public float endingDelay=2.3f;
    private PlayerInputActions controls;
    private bool endingScheduled;
    private void Awake()
    {
        LoadProgress();
        controls = new PlayerInputActions();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        controls.Player.NoLevelPass.started += ctx => NoLevelPass();
#endif
    }
    void OnEnable()
    {
        controls.Player.Enable();
    }
    void OnDisable()
    {
        controls.Player.Disable();
    }
    void OnDestroy()
    {
        if(controls != null)
        {
            controls.Dispose();
            controls = null;
        }
    }
    void Update()
    {
        SetIndicator(0,level1Pass);
        SetIndicator(1,level2Pass);
        SetIndicator(2,level3Pass);
        SetIndicator(3,level4Pass);
        EvaluateCompletion();
    }
    void SetIndicator(int index,bool active)
    {
        if(levelPassAnim==null || index>=levelPassAnim.Length || levelPassAnim[index]==null)
        {
            return;
        }
        if(levelPassAnim[index].activeSelf!=active)
        {
            levelPassAnim[index].SetActive(active);
        }
    }
    public void EvaluateCompletion()
    {
        if(level1Pass && level2Pass && level3Pass && level4Pass && !endingScheduled)
        {
            endingScheduled=true;
            if(endingDelay<=0f)
            {
                GotoEnding();
            }else
            {
                Invoke(nameof(GotoEnding),endingDelay);
            }
        }
    }
    void GotoEnding()
    {
        NoLevelPass();
        GameObject.Find("levelLoader").GetComponent<levelLoader>().LoadSomeLevel(NutritionHowScenes.Ending);
    }

    public static void MarkLevelComplete(int levelNumber)
    {
        if(levelNumber<1 || levelNumber>ProgressKeys.Length)
        {
            Debug.LogError($"Cannot complete level {levelNumber}; expected 1-{ProgressKeys.Length}.");
            return;
        }

        SetLevelComplete(levelNumber,true);
        PlayerPrefs.SetInt(ProgressKeys[levelNumber-1],1);
        PlayerPrefs.Save();
    }

    public static void LoadProgress()
    {
        for(int index=0;index<ProgressKeys.Length;index++)
        {
            SetLevelComplete(index+1,PlayerPrefs.GetInt(ProgressKeys[index],0)==1);
        }
    }

    public static void ClearProgress()
    {
        for(int index=0;index<ProgressKeys.Length;index++)
        {
            PlayerPrefs.DeleteKey(ProgressKeys[index]);
            SetLevelComplete(index+1,false);
        }
        PlayerPrefs.Save();
    }

    private static void SetLevelComplete(int levelNumber,bool complete)
    {
        switch(levelNumber)
        {
            case 1:
                level1Pass=complete;
                break;
            case 2:
                level2Pass=complete;
                break;
            case 3:
                level3Pass=complete;
                break;
            case 4:
                level4Pass=complete;
                break;
        }
    }

    void NoLevelPass()
    {
        CancelInvoke(nameof(GotoEnding));
        endingScheduled=false;
        ClearProgress();
    }
}
