using System;
using UnityEngine;

public class CardSlot : MonoBehaviour
{
    [Serializable]
    public struct Data
    {
        public int id;
        public ObjectCard card;
        public CardOwner owner;
    }

    [SerializeField] private Data slotData;
    [SerializeField] private SpriteRenderer slotSprite;

    public void SetCard(ObjectCard card)
    {
        slotData.card = card;

        //Show card on slot
        transform.GetChild(2).gameObject.SetActive(false);
    }

    public void RemoveCard()
    {
        slotData.card = null;
        //Hide card on slot
        if (slotData.owner == CardOwner.Player)
        {
            transform.GetChild(2).gameObject.SetActive(true);
        }
    }
    public void SetSlot(int id, CardOwner owner)
    {
        slotData.id = id;
        slotData.owner = owner;
        transform.GetChild((int)owner).gameObject.SetActive(true);
    }

    public void CorrectSlot()
    {
        slotSprite.color = Color.green;
    }
    public void WrongSlot()
    {
        slotSprite.color = Color.red;

    }
    public void ResetSlotColor()
    {
        slotSprite.color = Color.black;
    }

    public CardOwner GetOwner()
    {
        return slotData.owner;
    }
    public void EnableTextAndDisableEmoji()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).gameObject.SetActive(false);
        if (slotData.owner == CardOwner.Player)
        {
            transform.GetChild(2).gameObject.SetActive(true);
        }
        else
        {
            transform.GetChild(2).gameObject.SetActive(false);
        }
    }

    public int GetId()
    {
        return slotData.id;
    }

    public ObjectCard GetCard()
    {
        return slotData.card;
    }

    public bool HasCard()
    {
        return slotData.card != null;
    }
}
