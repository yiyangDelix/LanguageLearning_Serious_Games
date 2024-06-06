using System.Collections.Generic;
using UnityEngine;

public class OpenPackage : MonoBehaviour
{
    public GameObject cardPrefab;
    public GameObject cardPool;

    CardStore CardStore;

    private List<GameObject> cards = new List<GameObject>();

    public PlayerData PlayerData;

    // Start is called before the first frame update
    void Start()
    {
        CardStore = GetComponent<CardStore>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnClickOpen()
    {
        if (PlayerData.playerCoins < 2)
        {
            return;
        }
        else
        {
            PlayerData.playerCoins -= 2;
        }

        ClearPool();
        for (int i = 0; i < 5; i++)
        {
            GameObject newCard = GameObject.Instantiate(cardPrefab, cardPool.transform);

            //newCard.GetComponent<CardDisplay>().card = CardStore.RandomCard();

            cards.Add(newCard);
        }

        SaveCardData();
        PlayerData.SavePlayerData();
    }

    public void ClearPool()
    {
        foreach (var card in cards)
        {
            Destroy(card);
        }

        cards.Clear();
    }

    public void SaveCardData()
    {
        foreach (var card in cards)
        {
            ////int id = card.GetComponent<CardDisplay>().card.id;
            //Debug.Log("card id = " + id);
            //PlayerData.playerCards[id] += 1;
        }
    }
}