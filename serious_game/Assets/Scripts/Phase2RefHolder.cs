using Lean.Touch;
using UnityEngine;
using UnityEngine.UI;

public class Phase2RefHolder : MonoBehaviour
{
    public static Phase2RefHolder instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public RectTransform cardHolder;
    public Image cardHolderButtonImage;
    public Transform cardDeckCircleCenter;

    public GameObject cardSlotHighlightGameObject;
    public GameObject cardSlotInvalidGameObject;

    public Toggle cardHolderButton;
    public Button GrammarPileButton;
    public LeanFingerSwipe playerSwipe;
    public Canvas overlayCanvas;


}
