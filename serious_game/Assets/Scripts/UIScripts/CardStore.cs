using System.Collections.Generic;
using UnityEngine;

public class CardStore : MonoBehaviour
{

    public TextAsset cardData;
    public List<Card> cardList = new List<Card>();

    // Start is called before the first frame update
    void Start()
    {
        TestLoad();
    }

    // Update is called once per frame
    void Update()
    {

    }

    //public void LoadCardData()
    //{
    //    string[] dataRow = cardData.text.Split('\n');
    //    foreach (var row in dataRow)
    //    {
    //        string[] rowArray = row.Split(',');
    //        if (rowArray[0] == "#")
    //        {
    //            continue;
    //        }
    //        else if (rowArray[0] == "monster")
    //        {
    //            int id = int.Parse(rowArray[1]);
    //            string name = rowArray[2];
    //            int atk = int.Parse(rowArray[3]);
    //            int hp = int.Parse(rowArray[4]);
    //            ObjectCard monsterCard = new ObjectCard(id, name, atk, hp);
    //            cardList.Add(monsterCard);

    //            Debug.Log("Get the monsterCard: " + monsterCard.cardName);
    //        }
    //        else if (rowArray[0] == "spell")
    //        {
    //            int id = int.Parse(rowArray[1]);
    //            string name = rowArray[2];
    //            string effect = rowArray[3];
    //            GrammarCard spellCard = new GrammarCard(id, name, effect);
    //            cardList.Add(spellCard);

    //            Debug.Log("Get the SpellCard: " + spellCard.cardName);
    //        }
    //    }
    //}

    public void TestLoad()
    {
        foreach (var item in cardList)
        {
            //Debug.Log("Card: " + item.id.ToString() + item.cardName);
        }
    }

    public Card RandomCard()
    {
        Card card = cardList[Random.Range(0, cardList.Count)];
        return card;
    }
}