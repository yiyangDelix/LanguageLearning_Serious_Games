using DG.Tweening;
using UnityEngine;

public class ArrowMover : MonoBehaviour
{
    [SerializeField] private float startY = 1f;
    [SerializeField] private float endY = 1f;
    [SerializeField] private Vector3 rotationAxis = Vector3.zero;

    private void OnEnable()
    {
        transform.DOLocalMoveY(endY, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).From(startY);
        //transform.localRotation = Quaternion.identity;
        transform.DORotate(180f * rotationAxis, 1f, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Restart);
    }

    private void OnDisable()
    {
        transform.DOKill();
    }
}
