using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pass : MonoBehaviour
{
    static public bool level1Pass,level2Pass,level3Pass,level4Pass;
    public GameObject[] levelPassAnim;
    public float timeLeft,time;
    public float endingDelay=2.3f;
    private PlayerInputActions controls;
    private bool endingScheduled;
    private void Awake()
    {
        controls = new PlayerInputActions();
        controls.Player.NoLevelPass.started += ctx => NoLevelPass();
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
    void NoLevelPass()
    {
        CancelInvoke(nameof(GotoEnding));
        endingScheduled=false;
        level1Pass=false;
        level2Pass=false;
        level3Pass=false;
        level4Pass=false;
    }
}
