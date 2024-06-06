using Lean.Touch;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;

public class TestGameplayScript : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private RectTransform cardHolderDeck;
    [SerializeField] private TrafficLightManager trafficLight;
    [SerializeField] private Transform card;
    [SerializeField] private int cardSlotID;
    [SerializeField] private Canvas overlay;
    [SerializeField] private GameObject[] testObjectCards;
    [SerializeField] private GameObject damageFeedback;
    // Start is called before the first frame update
    async void Start()
    {
        await Task.Delay(1000);
        //GetComponent<LeanFingerSwipe>().OnFinger.AddListener(OnFinger);
        CreateRandomBattlefield();
        BattlefieldManager.instance.battlefieldGameObject.SetActive(true);
        PlaceRandomCardOnSlots();
        var colouredParts = BattlefieldManager.instance.battlefieldGameObject.transform.GetChild(1);
        colouredParts.GetChild(0).gameObject.SetActive(true);
        colouredParts.GetChild(1).gameObject.SetActive(true);
        colouredParts.GetChild(2).gameObject.SetActive(true);
        BattlefieldManager.instance.SetupGameplayObjects(true);
        NetworkManager.instance.currentRandomNumbers.randomNumbers = new List<float>();
        for (int i = 0; i < 100; i++)
        {
            NetworkManager.instance.currentRandomNumbers.randomNumbers.Add(UnityEngine.Random.Range(0f, 1f));
        }
        PlayerManager.instance.PlayerHealth = 10;
        PlayerManager.instance.OpponentHealth = 10;
        await Task.Delay(1000);

        
        for (int i = 0; i < BattlefieldManager.instance.cardSettings.NumColumns; i++)
        {
            var cardSlot = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(i, CardOwner.Player);
            var card = cardSlot.GetCard();
            var effect = EffectManager.instance.GetRandomEffect();
            var mainEffect = EffectManager.instance.CreateEffect(effect, card);
            card.AddEffect(mainEffect);
        }

        await Task.Delay(2000);
        StartCoroutine(BattlefieldManager.instance.Attack());

        //CreateRandomCards();
        //CreateRandomGrammar();
        /*PlayerManager.instance.SetAllPossibleGrammars();
        PlayerManager.instance.OrganizeDeck();*//*
        PlayerManager.instance.StartPhase2();*/
        //trafficLight.PlayerTurnLights(10);
    }

    private void PlaceRandomCardOnSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            //0.003 scale
            var cardSlot = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(i, CardOwner.Player);
            var card = Instantiate(testObjectCards[UnityEngine.Random.Range(0, testObjectCards.Count())], cardSlot.transform.parent, instantiateInWorldSpace: true).GameObject().GetComponent<ObjectCard>();
            card.transform.localScale = Vector3.one * 0.018f;
            card.transform.localPosition = cardSlot.transform.localPosition;
            card.transform.localRotation = Quaternion.Euler(90, 0, 0);
            card.objectCardData.owner = CardOwner.Player;
            card.objectCardData.attack = UnityEngine.Random.Range(6, 8);
            card.objectCardData.currentHp = UnityEngine.Random.Range(1, 6);
            card.cardDisplay.SetCard(card);
            cardSlot.SetCard(card);

            Instantiate(damageFeedback, card.transform.GetChild(0));
        }
        for (int i = 0; i < 3; i++)
        {
            //0.003 scale
            var cardSlot = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(i, CardOwner.Opponent);
            var card = Instantiate(testObjectCards[UnityEngine.Random.Range(0, testObjectCards.Count())], cardSlot.transform.parent, instantiateInWorldSpace: true).GameObject().GetComponent<ObjectCard>();
            card.objectCardData.attack = UnityEngine.Random.Range(1, 3);
            card.objectCardData.currentHp = UnityEngine.Random.Range(1, 6);
            card.transform.localScale = Vector3.one * 0.018f;
            card.transform.localRotation = Quaternion.Euler(90, 0, 0);
            card.transform.localPosition = cardSlot.transform.localPosition;
            card.objectCardData.owner = CardOwner.Opponent;
            card.cardDisplay.SetCard(card);
            cardSlot.SetCard(card);
            Instantiate(damageFeedback, card.transform.GetChild(0));
        }
    }

    public async void KillCard(ObjectCard card)
    {
        card.TakeDamage(1000);
        await Task.Delay(2000);
        card.DestroyIfZeroHP();
    }
    private void OnFinger(LeanFinger finger)
    {
        if ((finger.SwipeScreenDelta.x > 0 || finger.SwipeScreenDelta.x < 0) && card != null)
        {
            var cardRect = card.GetComponent<RectTransform>();
            var slotScale = BattlefieldManager.instance.GetCardSlotFromSiblingIndex(cardSlotID).transform.localScale;
            var targetScale = new Vector2(1 / cardRect.rect.width * slotScale.x, 1 / cardRect.rect.height * slotScale.z);
            overlay.renderMode = RenderMode.ScreenSpaceCamera;
            overlay.planeDistance = 1f;
            overlay.worldCamera = Camera.main;
            card.SetParent(BattlefieldManager.instance.battlefieldGameObject.transform, true);
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            //PlayerManager.ThrowCardAnimation(card, BattlefieldManager.instance.GetCardSlotFromSiblingIndex(cardSlotID).transform, targetScale, finger.SwipeScaledDelta.normalized, cardRect);
            card = null;

        }
    }

    private IEnumerator testCoroutine()
    {
        overlay.renderMode = RenderMode.ScreenSpaceCamera;
        overlay.worldCamera = Camera.main;
        yield return null;
        card.SetParent(BattlefieldManager.instance.battlefieldGameObject.transform.parent, true);
    }

    public void GenCard()
    {
        BattlefieldManager.instance.battlefieldGameObject.SetActive(true);
        card = Instantiate(cardPrefab, cardHolderDeck).transform;
        card.localScale = Vector3.one * 2f;
    }

    private static void CreateRandomBattlefield()
    {
        BattlefieldManager.instance.SpawnBattlefield(Vector3.forward * 4f, Quaternion.Euler(0f, 0f, 0f), 1f);
        //BattlefieldManager.instance.ChangeBattlefieldSize(0.2f);
    }

    private static void CreateRandomGrammar()
    {
        // Set some random grammar for object cards just for testing
        // SetGrammarsForObjects(List<Dictionary<Grammars,string>> grammarExtensions, List<List<List<string>>> wordWithGrammar)
        // Create a dictionary of grammarExtensions

        var grammarExtensions = new List<Dictionary<Grammars, string>>();
        var wordWithGrammar = new List<Dictionary<Grammars, string>>();

        // Fill them up with random values

        for (int i = 0; i < PlayerManager.instance.cardsInDeck.Count; i++)
        {
            Dictionary<Grammars, string> words = new();
            foreach (Grammars value in Enum.GetValues(typeof(Grammars)))
            {
                words[value] = "word" + UnityEngine.Random.Range(0, 10);
            }
            wordWithGrammar.Add(words);

            grammarExtensions.Add(new()
            {
                { Grammars.DativeSingular, "Test -en" },
                { Grammars.AkkusativeSingular, "Test -es" },
                { Grammars.GenitiveSingular, "Test -i" }
            });
        }
        PlayerManager.instance.SetGrammarsForObjects(grammarExtensions, wordWithGrammar);

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestRay();
        }
    }

    public void TestRay()
    {
        var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        pos.z = 0;
        var cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Instantiate(cubeObject, ray.GetPoint(0f), Quaternion.identity);
    }
}
