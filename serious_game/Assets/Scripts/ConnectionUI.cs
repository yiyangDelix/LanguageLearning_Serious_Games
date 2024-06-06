using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{

    [SerializeField] private RectTransform infoPanelRect;
    [SerializeField] private Toggle infoPanelIconToggle;
    [SerializeField] private TMPro.TextMeshProUGUI infoPanelText;
    private Image infoPanelImage;

    private async void Start()
    {
        /*infoPanelImage = infoPanelRect.GetComponent<Image>();
        infoPanelIconToggle.interactable = false;
        await Task.Delay(2000);
        if (infoPanelRect == null) { return; }
        infoPanelIconToggle.isOn = false;
        ToggleInfoPanel(false);*/

    }

    public void ToggleInfoPanel(bool active)
    {
        if (infoPanelRect.gameObject.activeSelf == active) { return; }

        infoPanelIconToggle.interactable = false;
        if (active)
        {
            infoPanelRect.gameObject.SetActive(active);
            infoPanelRect.DOAnchorPosY(infoPanelRect.anchoredPosition.y, 0.5f).From(Vector2.zero).SetEase(Ease.OutSine).OnComplete(() =>
            {

                infoPanelIconToggle.interactable = true;
                infoPanelText.gameObject.SetActive(true);
            });
            infoPanelImage.DOFillAmount(1, 0.5f).From(0.17f).SetEase(Ease.InCirc);
        }
        else
        {
            infoPanelText.gameObject.SetActive(false);
            infoPanelImage.DOFillAmount(0.17f, 0.3f).From(1f).SetEase(Ease.OutCirc);
            float yPos = infoPanelRect.anchoredPosition.y;
            infoPanelRect.DOAnchorPosY(0f, 0.5f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                infoPanelRect.anchoredPosition = new Vector2(0, yPos);
                infoPanelRect.gameObject.SetActive(active);
                infoPanelIconToggle.interactable = true;
            });
        }
    }

}
