using UnityEngine;



public enum CardType
{
    Object,
    Grammar
}


public enum CardOwner
{
    Player,
    Opponent
}

public enum Grammars
{
    NominativeSingular,
    NominativePlural,
    GenitiveSingular,
    GenitivePlural,
    DativeSingular,
    DativePlural,
    AkkusativeSingular,
    AkkusativePlural,
}
// Each effect will have its own script which would be a child of an abstract class called Effect
public enum GrammarCardEffect
{
    AttackBuff,
    HealthBuff,
    Heal,
    MutualSuicide,
    FreezeTurn,
    Swap,
    Frozen,
    // Add more effects here, whatever comes to mind, we will figure out the code later
    // I think we should divide the effects into further categories like effects that affect the player and effects that affect the enemy and so on
}

public enum SpecialCardEffect
{
    // These are for the german exception cards only
    StatBuff,
    Poison,
    Bleed,
    // Add more effects here, whatever comes to mind, we will figure out the code later
}


[CreateAssetMenu(fileName = "CardSettings", menuName = "ScriptableObjects/CardSettings", order = 0)]



public class CardSettings : ScriptableObject
{

    [System.Serializable]
    private struct CardSettingsStruct
    {
        public int numCardsPerPlayer;
        public int numCardsOnDeck;
        public int numColumns;
        public Vector2 distanceBetweenCards;
        public Vector2 offsetFromBattlefieldBorders;
        public Vector2Int cardSize;
        public int jpegImageQuality;
    }

    [SerializeField] private CardSettingsStruct cardSettingsStruct;

    public int NumCardsPerPlayer { get => cardSettingsStruct.numCardsPerPlayer; }
    public int NumCardsOnDeck { get => cardSettingsStruct.numCardsOnDeck; }

    public int NumColumns { get => cardSettingsStruct.numColumns; }
    public int NumRows { get => cardSettingsStruct.numCardsOnDeck / cardSettingsStruct.numColumns; }
    public Vector2 DistanceBetweenCards { get => cardSettingsStruct.distanceBetweenCards; set => cardSettingsStruct.distanceBetweenCards = value; }

    public Vector2 OffsetFromBattlefieldBorders { get => cardSettingsStruct.offsetFromBattlefieldBorders; }
    public Vector2Int CardSize { get => cardSettingsStruct.cardSize; }

    public int JpegImageQuality { get => cardSettingsStruct.jpegImageQuality; }
}
