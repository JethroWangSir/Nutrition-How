using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pass1 : MonoBehaviour
{
    private player target;
    void Start()
    {
        GameObject targetObject=GameObject.Find("player");
        if(targetObject!=null)
        {
            target=targetObject.GetComponent<player>();
        }
    }
    void Update()
    {
        if(target!=null && target.Pass())
        {
            pass.MarkLevelComplete(1);
            enabled=false;
        }
    }
}
