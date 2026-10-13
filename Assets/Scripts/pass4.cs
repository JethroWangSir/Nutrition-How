using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pass4 : MonoBehaviour
{
    private ship4 target;
    void Start()
    {
        GameObject targetObject=GameObject.Find("ship");
        if(targetObject!=null)
        {
            target=targetObject.GetComponent<ship4>();
        }
    }
    void Update()
    {
        if(target!=null && target.Pass())
        {
            pass.MarkLevelComplete(4);
            enabled=false;
        }
    }
}
