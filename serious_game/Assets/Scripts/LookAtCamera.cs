using DG.Tweening;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCam;
    private void Start()
    {
        mainCam = Camera.main;
    }
    private void Update()
    {
        if (gameObject.activeInHierarchy)
        {
            transform.DOLookAt(mainCam.transform.position, 0.1f, AxisConstraint.Y);
        }
    }
}