using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanelParent;
    [SerializeField] private GameObject[] tutorialPanels;
    [SerializeField] private Button[] tutorialButtons; // 0 = next, 1 = skip
    [SerializeField] private TMPro.TextMeshProUGUI otherPlayerReadyTexts;
    [SerializeField] private GameObject raycastIcon;

    private int currentPanel = -1;
    bool isTutorialOngoing = false;
    public void StartTutorial()
    {
        raycastIcon.gameObject.SetActive(false);
        tutorialPanelParent.gameObject.SetActive(true);
        currentPanel = 0;
        tutorialPanels[0].SetActive(true);
        foreach (Button button in tutorialButtons)
        {
            button.interactable = true;
        }
        isTutorialOngoing = true;
    }

    public void OtherPlayerReady()
    {
        otherPlayerReadyTexts.text = "Other player is ready!";
    }

    public void NextTutorialPanel()
    {
        tutorialPanels[currentPanel].SetActive(false);
        AnimateButtons(true);
        if (currentPanel < tutorialPanels.Length - 2)
        {
            currentPanel++;
            tutorialPanels[currentPanel].SetActive(true);
        }
        else
        {
            LastTutorialPanel();
        }
    }

    private void AnimateButtons(bool isNext)
    {
        AudioManager.instance.PlayButtonClickSounds();
        tutorialButtons[0].interactable = false;
        tutorialButtons[1].interactable = false;
        if (isNext)
        {
            tutorialButtons[0].transform.DOPunchScale(1.3f * Vector3.one, 0.35f, 1, 1f).OnComplete(() =>
            {
                tutorialButtons[0].interactable = isTutorialOngoing;
                tutorialButtons[1].interactable = isTutorialOngoing;
            });
        }
        else
        {
            tutorialButtons[1].transform.DOPunchScale(1.3f * Vector3.one, 0.35f, 1, 1f).OnComplete(() =>
            {
                tutorialButtons[0].interactable = isTutorialOngoing;
                tutorialButtons[1].interactable = isTutorialOngoing;
            });
        }
    }

    public void SkipTutorial()
    {
        tutorialPanels[currentPanel].SetActive(false);
        AnimateButtons(false);
        LastTutorialPanel();
    }
    private void LastTutorialPanel()
    {
        isTutorialOngoing = false;
        currentPanel = tutorialPanels.Length - 1;
        tutorialPanels[currentPanel].SetActive(true);
        NetworkManager.instance.SendLocalPlayerTutorialOver();
    }
    public void EndTutorial()
    {
        foreach (Button button in tutorialButtons)
        {
            button.transform.DOKill();
        }
        raycastIcon.gameObject.SetActive(true);
        Destroy(gameObject);
    }

}
