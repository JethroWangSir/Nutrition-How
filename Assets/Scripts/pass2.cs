using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pass2 : MonoBehaviour
{
    private player target;
    void Start()
    {
        GameObject targetObject=GameObject.Find("player2");
        if(targetObject!=null)
        {
            target=targetObject.GetComponent<player>();
        }
    }
    void Update()
    {
        if(target!=null && target.Pass())
        {
            pass.MarkLevelComplete(2);
            enabled=false;
        }
    }
}
