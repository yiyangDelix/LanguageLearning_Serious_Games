using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

public enum SoundType
{
    AcceptCard,
    ButtonClick,
    Phase1BackgroundMusic,
    Phase2BackgroundMusic,
    Swipe,
    WrongAnswer,
    CardPlaced,
    CardThrow,
    CardToCardAttack,
    CardToPlayerAttack,
    BottleMove,
    CardDying,
    TurnStart,
    TurnEnd,
    CardDraw,
    Win,
    Lose,
    AttackBuff,
    FreezeBuff,
    Frozen,
    Heal,
    HealthBuff,
    Swap,
    MutualSuicide,
    Fireworks,
    ClockTick,
}

public class AudioManager : MonoBehaviour
{
    // Singleton pattern to ensure only one instance of AudioManager exists

    #region public and private fields
    public static AudioManager instance;

    private bool mute = false;
    private bool audioPaused = false;

    [SerializeField] private MMF_Player[] soundFeedbacks;
    [SerializeField] private MMSoundManager soundManager;


    public bool Mute
    {
        get
        {
            return mute;
        }
        set
        {
            mute = value;
            if (mute)
            {
                soundManager.MuteAllSounds();
            }
            else
            {
                soundManager.UnmuteMaster();
                soundManager.UnmuteMusic();
                soundManager.UnmuteSfx();
                soundManager.UnmuteUI();
            }
        }
    }

    public bool AudioPaused
    {
        get
        {
            return audioPaused;
        }
        set
        {
            audioPaused = value;
            if (audioPaused)
            {
                soundManager.PauseAllSounds();
            }
            else
            {
                soundManager.PlayAllSounds();
            }
        }
    }

    #endregion
    #region single pattern in awake and set instance, create audio source, start background music

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }

    private void Start()
    {
        VolumeSldier(0.5f);
    }


    #endregion

    #region play, stop, pause, continue sound
    public void StartBackgroundMusicPhase1(float delay = 0f)
    {
        soundFeedbacks[(int)SoundType.Phase1BackgroundMusic].GetFeedbackOfType<MMF_Sound>().SetInitialDelay(delay);
        soundFeedbacks[(int)SoundType.Phase1BackgroundMusic].PlayFeedbacks();
    }

    public void VolumeSldier(float volume)
    {
        soundManager.SetVolumeMusic(volume);
        soundManager.SetVolumeSfx(volume);
        soundManager.SetVolumeUI(volume);
    }

    public void StartBackgroundMusicPhase2(float delay = 0f)
    {
        soundFeedbacks[(int)SoundType.Phase1BackgroundMusic].enabled = false;
        soundFeedbacks[(int)SoundType.Phase2BackgroundMusic].GetFeedbackOfType<MMF_Sound>().SetInitialDelay(delay);
        soundFeedbacks[(int)SoundType.Phase2BackgroundMusic].PlayFeedbacks();
    }
    public void PlayButtonClickSounds()
    {
        Play(SoundType.ButtonClick);
    }

    public void PlaySwipeSounds()
    {
        Play(SoundType.Swipe);
    }

    public void PlayCardThrowSound()
    {
        Play(SoundType.CardThrow);
    }


    public void Play(SoundType soundType, float delay = 0f, float pitch = 1f)
    {
        if (AudioPaused)
        {
            return;
        }

        var sound = soundFeedbacks[(int)soundType].GetFeedbackOfType<MMF_Sound>();
        sound.SetInitialDelay(delay);
        sound.MaxPitch = pitch;
        sound.MinPitch = pitch;
        soundFeedbacks[(int)soundType]?.PlayFeedbacks();
    }


    // Stop playing the current audio clip
    public void StopAudio(SoundType soundType)
    {
        soundFeedbacks[(int)soundType].StopFeedbacks();
        soundFeedbacks[(int)soundType].RestoreInitialValues();
    }
}
#endregion