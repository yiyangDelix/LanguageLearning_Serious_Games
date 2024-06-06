using Lean.Common;
using Lean.Touch;
using UnityEngine;

[System.Serializable]
public class CardData
{
    public int id;
    public CardOwner owner;
    public string cardName;
}

[System.Serializable]
public class ObjectCardData : CardData
{
    public int attack;
    public int hpMax;
    public int currentHp;
    public int attackMultiplier = 1; // only temporary buff, 1 round
    public float hitChance = 1f; // chance to get hit, 0.5f = 50% chance to get hit, 1f = 100% chance to get hit, 0f = 0% chance to get hit
    public byte[] texture;
    public int textureFormat;
    public int[] textureWidthHeight;
}

[System.Serializable]
public class GrammarCardData : CardData
{
    public GrammarCardEffect effect;
    public Grammars grammar;
    public string[] correctAnswerObjectCardNames;
}

public abstract class Card : MonoBehaviour
{
    public CardData cardData = new CardData();
    public RectTransform rect;
    public CardDisplay cardDisplay = null;
    public LeanSelectableByFinger selectable;
    protected void SetCard(int _id, string _cardName, CardOwner owner = CardOwner.Player)
    {
        cardData.id = _id;
        cardData.cardName = _cardName;
        cardData.owner = owner;

        rect = GetComponent<RectTransform>();
        cardDisplay = GetComponent<CardDisplay>();
        cardDisplay.SetCard(this);

        selectable = transform.GetChild(0).gameObject.AddComponent<LeanSelectableByFinger>();
        selectable.OnSelected.AddListener(CardSelected);
        selectable.OnDeselected.AddListener(CardDeselected);
    }

    public virtual void ApplyCard(CardSlot slot)
    {
        // This function will be overridden by the child classes
    }
    protected void CardSelected(LeanSelect finger)
    {
        PlayerManager.instance.CardSelected(this);
    }

    protected void CardDeselected(LeanSelect finger)
    {
        PlayerManager.instance.CardDeselected();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

}
