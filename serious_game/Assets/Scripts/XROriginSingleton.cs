using UnityEngine;

public class XROriginSingleton : MonoBehaviour
{
    public static XROriginSingleton instance;

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
