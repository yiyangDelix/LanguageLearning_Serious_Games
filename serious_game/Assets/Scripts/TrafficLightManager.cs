using DG.Tweening;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum LightState
{
    Red,
    Yellow,
    Green
}
public class TrafficLightManager : MonoBehaviour
{

    [SerializeField] private Light[] lights; //0: red, 1: yellow, 2: green

    private CancellationTokenSource cancellationTokenSource;
    private Light mainLight;
    private SpriteRenderer battlefieldColourPartsRenderer;
    public void Setup()
    {
        mainLight = GameObject.Find("Directional Light").GetComponent<Light>();
        battlefieldColourPartsRenderer = transform.parent.GetChild(1).GetComponent<SpriteRenderer>();
        battlefieldColourPartsRenderer.DOColor(Color.white, 0f);
        //TurnOnLight(LightState.Red);
    }
    public void TurnOnLight(LightState light)
    {
        for (int i = 0; i < lights.Length; i++)
        {
            if (i == (int)light)
            {
                lights[i].enabled = true;
            }
            else
            {
                lights[i].enabled = false;
            }
        }
        if (gameObject.activeInHierarchy)
        {
            AudioManager.instance.PlayButtonClickSounds();
        }
    }

    public async void PlayerTurnLights(int seconds)
    {
        cancellationTokenSource = new CancellationTokenSource();
        //Green light
        TurnOnLight(LightState.Green);

        battlefieldColourPartsRenderer.DOColor(Color.green, 0.5f);
        //Start timer
        try
        {
            await Task.Delay((seconds - 4) * 1000, cancellationTokenSource.Token);
        }
        catch
        {
            Debug.Log("Lights green Task was cancelled!");
            return;
        }
        finally
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
        // Yellow light flash
        TurnOnLight(LightState.Yellow);
        battlefieldColourPartsRenderer.color = Color.yellow;
        var timeLeft = 4000;
        Tween flashing = null;
        Tween colouredPartsFlashing = null;
        // make flashing faster with less time left
        cancellationTokenSource = new CancellationTokenSource();
        flashing = lights[1].DOColor(Color.yellow, 0.5f).SetEase(Ease.OutFlash).SetLoops(-1, LoopType.Yoyo).From(Color.black);
        AudioManager.instance.Play(SoundType.ClockTick);
        colouredPartsFlashing = battlefieldColourPartsRenderer.DOColor(Color.yellow, 0.5f).SetEase(Ease.OutFlash).SetLoops(-1, LoopType.Yoyo).From(Color.white);
        flashing.DOTimeScale(2.5f, timeLeft / 1000f);
        colouredPartsFlashing.DOTimeScale(2.5f, timeLeft / 1000f);
        try
        {
            await Task.Delay(timeLeft, cancellationTokenSource.Token);
        }
        catch
        {
            Debug.Log("Lights yellow Task was cancelled!");
            return;
        }
        finally
        {
            lights[1].DOKill();
            AudioManager.instance.StopAudio(SoundType.ClockTick);
            battlefieldColourPartsRenderer.DOKill();
            battlefieldColourPartsRenderer.color = Color.red;
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }
        flashing.Kill();
        lights[1].DOKill();
        colouredPartsFlashing?.Kill();
        AudioManager.instance.StopAudio(SoundType.ClockTick);
        battlefieldColourPartsRenderer.DOKill();
        TurnOnLight(LightState.Red);
        battlefieldColourPartsRenderer.color = Color.red;
        Debug.Log("Lights red! Turn over");
    }

    public void StopPlayerTurnLights()
    {
        cancellationTokenSource?.Cancel();
        AudioManager.instance.StopAudio(SoundType.ClockTick);
        lights[0].DOKill();
        lights[1].DOKill();
        lights[2].DOKill();
        battlefieldColourPartsRenderer.DOKill();
        TurnOnLight(LightState.Red);
        battlefieldColourPartsRenderer.color = Color.red;
    }

    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
    }
}
