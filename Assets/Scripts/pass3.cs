using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pass3 : MonoBehaviour
{
    private ship target;
    void Start()
    {
        GameObject targetObject=GameObject.Find("ship3-2");
        if(targetObject!=null)
        {
            target=targetObject.GetComponent<ship>();
        }
    }
    void Update()
    {
        if(target!=null && target.Pass())
        {
            pass.MarkLevelComplete(3);
            enabled=false;
        }
    }
}
