using UnityEngine;

public enum GameEndState
{
    Ongoing,
    PlayerWon,
    OpponentWon,
    Draw
}
public struct CardState
{
    public int index;
    public Vector2 position;
    public Vector3 rotation;
    public Vector3 scale;
}

[CreateAssetMenu(fileName = "PlayerSettingsObject", menuName = "ScriptableObjects/PlayerSettingsObject", order = 1)]
public class PlayerSettingsSO : ScriptableObject
{
    [System.Serializable]
    private struct PlayerSettingsStruct
    {
        public int initialPlayerHealth; // always multiple of 5
        public int initialNumOfActions;
        public int timeToPlay; // in seconds
        public int initalTimeToPlaceCards; // in seconds
        public int numActionsPerTurn;
        public float distanceToStartShowingCloseUpCardView;
        public bool showCorrectAnswers;
        public bool closeUpCardView;
    }

    [SerializeField] private PlayerSettingsStruct playerSettings;

    public int InitialPlayerHealth { get => playerSettings.initialPlayerHealth; }
    public int InitialNumOfActions { get => playerSettings.initialNumOfActions; }
    public int TimeToPlay { get => playerSettings.timeToPlay; }
    public int InitalTimeToPlaceCards { get => playerSettings.initalTimeToPlaceCards; }
    public int NumActionsPerTurn { get => playerSettings.numActionsPerTurn; }
    public float DistanceToStartShowingCloseUpCardView { get => playerSettings.distanceToStartShowingCloseUpCardView; }
    public bool ShowCorrectAnswers
    {
        get => playerSettings.showCorrectAnswers;
        set => playerSettings.showCorrectAnswers = value;
    }
    public bool CloseUpCardView
    {
        get => playerSettings.closeUpCardView;
        set => playerSettings.closeUpCardView = value;
    }
}
