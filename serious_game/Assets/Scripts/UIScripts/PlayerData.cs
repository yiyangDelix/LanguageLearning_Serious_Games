using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public CardStore CardStore;
    public int playerCoins;
    public int[] playerCards;

    public TextAsset playerData;

    // Start is called before the first frame update
    void Start()
    {
       // CardStore.LoadCardData();
        LoadPlayerData();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void LoadPlayerData()
    {
        playerCards = new int[CardStore.cardList.Count];
        Debug.Log(playerCards.Length);
        string[] dataRow = playerData.text.Split('\n');
        foreach (var row in dataRow)
        {
            string[] rowArray = row.Split(',');
            if (rowArray[0] == "#")
            {
                continue;
            }
            else if (rowArray[0] == "coins")
            {
                playerCoins = int.Parse(rowArray[1]);
            }
            else if (rowArray[0] == "card")
            {
                int id = int.Parse(rowArray[1]);
                int num = int.Parse(rowArray[2]);

                playerCards[id] = num;
            }
        }
    }

    public void SavePlayerData()
    {
        string path = Application.dataPath + "/playerdata.csv";

        List<string> datas = new List<string>();
        datas.Add("coins," + playerCoins.ToString());
        for (int i = 0; i < playerCards.Length; i++)
        {
            if (playerCards[i] != 0)
            {
                datas.Add("card," + i.ToString() + "," + playerCards[i].ToString());
            }
        }

        File.WriteAllLines(path, datas);
        Debug.Log(datas);
    }
}