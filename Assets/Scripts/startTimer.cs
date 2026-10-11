using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class startTimer : MonoBehaviour
{
    public float timeLeft;
    public Text timeText;
    private bool completed;

    void Update()
    {
        if(completed)
        {
            return;
        }

        timeLeft=Mathf.Max(0f,timeLeft-Time.deltaTime);
        timeText.text=timeLeft.ToString("0");
        if(timeLeft<=0f)
        {
            completed=true;
            Time.timeScale=1f;
            timeText.enabled=false;
        }
    }
}
