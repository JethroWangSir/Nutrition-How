using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class startTimer : MonoBehaviour
{
    public float timeLeft;
    public Text timeText;
    private bool completed;
    private int displayedSeconds=int.MinValue;

    void Update()
    {
        if(completed)
        {
            return;
        }

        timeLeft=Mathf.Max(0f,timeLeft-Time.deltaTime);
        int roundedSeconds=Mathf.RoundToInt(timeLeft);
        if(displayedSeconds!=roundedSeconds)
        {
            displayedSeconds=roundedSeconds;
            timeText.text=roundedSeconds.ToString();
        }
        if(timeLeft<=0f)
        {
            completed=true;
            Time.timeScale=1f;
            timeText.enabled=false;
        }
    }
}
