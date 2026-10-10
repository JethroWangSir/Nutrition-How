using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collection : MonoBehaviour
{
    public void Death()
    {
        FindFirstObjectByType<player>().Collectionscount1();
        Destroy(gameObject);
    }
}
