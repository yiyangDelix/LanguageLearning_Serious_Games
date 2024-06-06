using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AlwaysActiveUIManager : MonoBehaviour
{
    public static AlwaysActiveUIManager Instance { get; private set; }
    public event System.Action<bool> onInfoScreenOpened = delegate { };
    public event System.Action<bool> onSettingsPanelOpened = delegate { };
    public GameObject infoScreen;
    public Image raycastCenterImage;
    [SerializeField] private RectTransform infoPanelRect;
    [SerializeField] private Toggle infoPanelIconToggle;
    [SerializeField] private TMPro.TextMeshProUGUI infoPanelText;
    [SerializeField] private RectTransform SettingsPanel;
    [SerializeField] private Toggle SettingsButton;
    [SerializeField] private Toggle cardCloseUpViewToggle;
    [SerializeField] private Toggle showCorrectAnswersToggle;
    [SerializeField] private GameObject cardCloseUpViewTick;
    [SerializeField] private GameObject showCorrectAnswersTick;
    [SerializeField] private GameObject[] settingsPanelObjects;
    [SerializeField] private Image settingsPanelBackground;
    [SerializeField] private Image progressCircle;
    [SerializeField] private Image magnifyingIcon;


    private Image infoPanelImage;
    private float infoPanelInitPosY;
    private bool inbuiltAnim = false;
    private Coroutine showInfoCoroutine = null;
    private Tween progressCircleTween = null;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else { Destroy(gameObject); }
    }

    public void StartProgressCircle(float time, System.Action callback)
    {
        if (progressCircleTween != null)
        {
            return;
        }
        progressCircleTween = progressCircle.DOFillAmount(1f, time).From(0f).SetEase(Ease.Linear).OnComplete(() =>
        {
            progressCircle.fillAmount = 0f;
            if (callback != null)
            {
                callback();
            }
        });
    }

    public void CancelProgressCircle()
    {
        progressCircleTween?.Kill(false);
        progressCircleTween = null;
        progressCircle.fillAmount = 0f;
    }
    public void TogglePanel(bool active, RectTransform panel, Toggle button, Action<bool> callback, GameObject[] gameObjects = null, Image image = null, bool buttonInteractionFollowsAnimation = true)
    {
        if (panel.gameObject.activeSelf == active) { return; }
        AudioManager.instance.PlaySwipeSounds();
        button.interactable = false;
        if (callback != null)
        {
            callback(true);
        }

        if (active)
        {
            panel.gameObject.SetActive(active);
            panel.DOAnchorPosY(panel.anchoredPosition.y, 0.5f).From(Vector2.zero).SetEase(Ease.OutSine).OnComplete(() =>
            {

                button.interactable = !inbuiltAnim || !buttonInteractionFollowsAnimation;
                foreach (var text in gameObjects)
                {
                    text.gameObject.SetActive(true);
                }
            });
            image.DOFillAmount(1, 0.5f).From(0.17f).SetEase(Ease.InCirc);
        }
        else
        {
            float yPos = panel.anchoredPosition.y;
            foreach (var text in gameObjects)
            {
                text.gameObject.SetActive(false);
            }
            image.DOFillAmount(0.17f, 0.3f).From(1f).SetEase(Ease.OutCirc);
            panel.DOAnchorPosY(0f, 0.5f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                panel.anchoredPosition = new Vector2(0, yPos);
                panel.gameObject.SetActive(active);
                button.interactable = true;
                if (callback != null)
                {
                    callback(false);
                }
            });
        }
    }

    public void CloseUpCardViewToggle(bool active)
    {
        AudioManager.instance.PlayButtonClickSounds();
        //cardCloseUpViewTick.SetActive(active);
        magnifyingIcon.color = active ? Color.white : Color.grey;
        PlayerManager.instance.playerSettings.CloseUpCardView = active;
    }

    public void ShowCorrectAnswersToggle(bool active)
    {
        AudioManager.instance.PlayButtonClickSounds();
        showCorrectAnswersTick.SetActive(active);
        PlayerManager.instance.playerSettings.ShowCorrectAnswers = active;
    }

    public void ToggleSettingsPanel(bool active)
    {
        var rotationDir = active ? -1 : 1;
        SettingsButton.transform.DORotate(Vector3.forward * 45f * rotationDir, 0.5f, RotateMode.Fast);
        TogglePanel(active, SettingsPanel, SettingsButton, onSettingsPanelOpened, settingsPanelObjects, settingsPanelBackground, false);
    }

    public void ResetInfoPanel()
    {
        infoPanelRect.DOComplete(true);
        infoPanelImage.DOComplete(true);
        infoPanelRect.anchoredPosition = new Vector2(0, infoPanelInitPosY);
        infoPanelRect.gameObject.SetActive(false);
        infoPanelIconToggle.interactable = true;
        infoPanelText.gameObject.SetActive(false);
        infoPanelImage.fillAmount = 0.17f;
        infoPanelIconToggle.isOn = false;
    }

    public void ShowInfo(int seconds)
    {
        if (showInfoCoroutine != null)
        {
            return;
        }
        showInfoCoroutine = StartCoroutine(ShowInfoCoroutine(seconds));
    }
    private IEnumerator ShowInfoCoroutine(int seconds)
    {
        inbuiltAnim = true;
        infoPanelIconToggle.interactable = false;
        ResetInfoPanel();
        InfoPanelActivate(true);
        yield return new WaitForSeconds(seconds);
        inbuiltAnim = false;
        InfoPanelActivate(false);
        showInfoCoroutine = null;
    }

    public void InfoPanelActivate(bool activate)
    {
        var panelObjects = new GameObject[] { infoPanelText.gameObject };
        TogglePanel(activate, infoPanelRect, infoPanelIconToggle, onInfoScreenOpened, panelObjects, infoPanelImage);
    }

    public void SetInfoPanelText(string text)
    {
        infoPanelText.text = text;
    }

    public string GetInfoPanelText()
    {
        return infoPanelText.text;
    }
    public void CancelShowInfo()
    {
        StopAllCoroutines();
        showInfoCoroutine = null;
        infoPanelRect.DOComplete(true);
        infoPanelImage.DOComplete(true);
    }
    private void Start()
    {
        infoPanelInitPosY = infoPanelRect.anchoredPosition.y;
        infoPanelImage = infoPanelRect.GetComponent<Image>();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        this.StopAllCoroutines();
    }
}
