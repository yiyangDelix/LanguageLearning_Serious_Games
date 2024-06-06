using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CountdownManager : MonoBehaviour
{
    public static CountdownManager Instance { get; private set; }
    [SerializeField] private TMPro.TextMeshProUGUI countdown;
    [SerializeField] private int countDownTexts = 2;
    [SerializeField] private AnimationCurve attackCurve;
    [SerializeField] private AnimationCurve phaseCurve;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else { Destroy(gameObject); }
    }
    private List<TextMeshProUGUI> textList = new List<TextMeshProUGUI>();
    private void Start()
    {
        countdown.gameObject.SetActive(false);
        for (int i = 0; i < countDownTexts; i++)
        {
            var text = Instantiate(countdown, transform);
            text.rectTransform.anchoredPosition = countdown.rectTransform.anchoredPosition - 150 * Vector2.up * (i+1);
            textList.Add(text);
            textList[i].gameObject.SetActive(false);
        }
    }
    public IEnumerator Countdown321(string finalText)
    {
        countdown.gameObject.SetActive(true);
        yield return Animate1SecScaleUp("3");
        yield return Animate1SecScaleUp("2");
        yield return Animate1SecScaleUp("1");
        yield return Animate1SecScaleUp(finalText);
        countdown.gameObject.SetActive(false);
    }

    public IEnumerator ScaleUpTextAnimation(string text)
    {
        countdown.gameObject.SetActive(true);
        yield return Animate1SecScaleUp(text);
        countdown.gameObject.SetActive(false);
    }
    public IEnumerator Animate1SecScaleUp(string text)
    {
        countdown.text = text;
        countdown.DOFade(0f, 0.5f).SetDelay(0.3f).From(1f);
        var tween = countdown.transform.DOScale(2f, 1f).From(1f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            countdown.alpha = 1f;
            countdown.transform.localScale = Vector3.one;
        });
        tween.easeOvershootOrAmplitude = 3f;
        yield return tween.WaitForCompletion();
    }

    public IEnumerator AttackPhaseTextAnimation()
    {
        var centerLeftScreen = -Screen.width / 2f - 500f;
        var centerRightScreen = Screen.width / 2f + 500f;
        AudioManager.instance.PlaySwipeSounds();
        AnimateTextSlide(0, centerLeftScreen, centerRightScreen, "Attack", 2f, attackCurve);
        yield return new WaitForSeconds(0.5f);

        AudioManager.instance.Play(SoundType.Swipe, 0, 1.5f);
        AudioManager.instance.Play(SoundType.Swipe, 0.9f, 0.8f);
        AnimateTextSlide(1, centerRightScreen, centerLeftScreen, "Phase", 1.5f, phaseCurve);

    }

    private void AnimateTextSlide(int textId, float initPos, float finalPos, string text, float time, AnimationCurve curve)
    {
        textList[textId].text = text;
        textList[textId].gameObject.SetActive(true);
        textList[textId].rectTransform.anchoredPosition = new Vector2(initPos, textList[textId].rectTransform.anchoredPosition.y);
        var tween = textList[textId].rectTransform.DOAnchorPosX(finalPos, time).SetEase(curve).OnComplete(() =>
        {
            textList[textId].gameObject.SetActive(false);
            textList[textId].rectTransform.anchoredPosition *= Vector2.up;
        });
    }
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
