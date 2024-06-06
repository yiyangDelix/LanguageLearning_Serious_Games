using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARSessionSingleton : MonoBehaviour
{
    public static ARSessionSingleton instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    
        
    }
}
