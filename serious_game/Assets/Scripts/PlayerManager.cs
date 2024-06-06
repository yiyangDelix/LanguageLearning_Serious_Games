using CartoonFX;
using DG.Tweening;
using Lean.Touch;
using MoreMountains.Feedbacks;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using WebSocketSharp;

public class PlayerManager : MonoBehaviour
{
    #region fields and properties
    public static PlayerManager instance;

    public List<Card> cardsInDeck = new List<Card>();
    public PlayerSettingsSO playerSettings;

    // This should be here as it also affects the object cards (may be of the opponent as well)
    [SerializeField] private Sprite[] cardHolderButtonSprites;
    [SerializeField] private ARRaycastManager m_ARRaycastManager;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject phase1Deletables;
    [SerializeField] private RectTransform opponentCardContainer;
    [SerializeField] private int testHighlightCardSlotID;
    [SerializeField] private CardOwner testSelectedCardOwner;
    [SerializeField] private Card currentSelectedCard;
    [SerializeField] private GameObject gameplayInteractions;
    [SerializeField] private GameObject cardThrowParticlePrefab;
    [SerializeField] private GameObject cardPlaceParticlePrefab;
    [SerializeField] private GameObject wrongGrammarParticlePrefab;
    [SerializeField] private GameObject dynamicTextParticlePrefab;
    [SerializeField] private GameObject buffParticlePrefab;
    [SerializeField] private Color[] wrongGrammarColours;
    [SerializeField] private Color[] rightGrammarColours;
    [SerializeField] private MMF_Player floatingTextFeedback;
    [SerializeField] private LeanSelectByFinger leanSelect;
    [SerializeField] private CardDisplay closeUpCardView;
    [SerializeField] private Image closeUpCardViewBorder;
    [SerializeField] private bool testCardSlotSelection;
    [SerializeField] private MMF_Player winParticleFeedback;
    [SerializeField] private MMF_Player loseParticleFeedback;
    [SerializeField] private Image circularBar;


    private CardSlot highlightedCardSlot = null;
    private bool validSlot = false;
    private bool isCardSlotDetectionActive = false;
    private Vector3 centerOfScreen;
    private Vector3 topOfScreen;
    private int raycastLayerMask;
    private bool isObjectCardSelected = false;
    private CardState prevCardState;
    private Dictionary<(string, Grammars), string> allPossibleGrammars = new Dictionary<(string, Grammars), string>();
    private GameObject cardSlotHighlight;
    private GameObject cardSlotInvalid;
    private Vector3 cardSlotHighlightScale;
    private int playerHealth;
    private int opponentHealth;
    [SerializeField] private int actionsLeft = 0;
    private bool hasGameEnded = false;
    private Queue<Tween> deckCardTweens = new Queue<Tween>();
    private Dictionary<int, ObjectCardData> opponentCardDataDict = new Dictionary<int, ObjectCardData>();
    private Dictionary<int, Dictionary<Grammars, string>> opponentGrammarAnswersDict = new();
    private Card currentOpponentCard;
    private CardSlot opponentCardSlot;
    private Vector2 cardThrowDirection;
    private Vector2 opponentCardRectSize;
    private CFXR_ParticleText_Runtime dynamicTextParticle;
    private Vector2 lastScreenPosition;
    private int playerCurrentDamage = 0;
    private int opponentCurrentDamage = 0;
    private bool grammarCardsEnabled = false;
    private TMPro.TextMeshPro actionsLeftText;
    bool isAllGrammarForCardsOnBoardUnlocked = false;
    bool isAllGrammarForAllCardsUnlocked = false;
    private bool gameStarted = false;
    private bool needToSelectCardSlotForSwap = false;
    private System.Action<CardSlot,bool> cardSlotDetection = null;
    private CardSlot cardSlotToSwapWith = null;
    private CardSlot sourceCardSlotForSwap = null;
    public int ActionsLeft
    {
        get
        {
            return actionsLeft;
        }
        set
        {
            if (value < 0)
            {
                return;
            }
            
            int prevValue = actionsLeft;
            actionsLeft = value;
            if (actionsLeftText != null)
            {
                actionsLeftText.text = actionsLeft.ToString();
                actionsLeftText.transform.DOPunchScale(Vector3.one * 0.4f, 0.3f, 1, 0.5f);
            }
            if (prevValue != 0 && actionsLeft == 0)
            {
                //StartCoroutine(CountdownManager.Instance.ScaleUpTextAnimation("No Actions Left"));
                // TODO uncomment eventually (currently breaks swap effect)
                //if (!needToSelectCardSlotForSwap)
                //{
                //    Phase2RefHolder.instance.cardHolderButton.isOn = false;
                //}
            }
        }
    }
    public int PlayerHealth
    {
        get
        {
            return playerHealth;
        }
        set
        {
            playerHealth = value;
        }
    }

    public CardSlot HighlightedCardSlot
    {
        get { return highlightedCardSlot; }
        set 
        { 
            if(highlightedCardSlot != null)
            {
                highlightedCardSlot.ResetSlotColor();
            }
            highlightedCardSlot = value; 
        }
    }

    public int OpponentHealth
    {
        get
        {
            return opponentHealth;
        }
        set
        {
            opponentHealth = value;
        }
    }

    public int PlayerCurrentDamage
    {
        get
        {
            return playerCurrentDamage;
        }
        set
        {
            playerCurrentDamage = value;
        }
    }
    public int OpponentCurrentDamage
    {
        get
        {
            return opponentCurrentDamage;
        }
        set
        {
            opponentCurrentDamage = value;
        }
    }
    #endregion

    //-----------------------------------------------------------------------------------

    #region Opponent functions


    public void GotOpponentObjectCardID(int cardID, int slotID, bool validCard)
    {
        currentOpponentCard = SetupOpponentObjectCard(cardID);
        opponentCardSlot = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(slotID, CardOwner.Opponent);
        Debug.Log("Received object card to place on board: " + currentOpponentCard.cardData.cardName + " (" + cardID + ")");
        StartCoroutine(MoveCardToBoard(validCard, opponentCardSlot, opponentCardRectSize, currentOpponentCard, UnityEngine.Random.insideUnitCircle, false));
        currentOpponentCard = null;
        opponentCardSlot = null;
    }


    public void GotOpponentGrammarCardData(GrammarCardData grammarCardData, int slotID, bool validCard)
    {
        currentOpponentCard = SetupOpponentGrammarCard(grammarCardData);
        opponentCardSlot = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(slotID, CardOwner.Opponent);
        StartCoroutine(MoveCardToBoard(validCard, opponentCardSlot, opponentCardRectSize, currentOpponentCard, UnityEngine.Random.insideUnitCircle, false));
        currentOpponentCard = null;
        opponentCardSlot = null;
    }

    public Card SetupOpponentGrammarCard(GrammarCardData grammarCardData)
    {
        var opponentCard = Instantiate(cardPrefab, opponentCardContainer).GetComponent<GrammarCard>();
        opponentCard.grammarCardData = grammarCardData;
        opponentCard.SetCard(grammarCardData.id, grammarCardData.cardName, grammarCardData.effect.ToString(), CardOwner.Opponent, grammarCardData.grammar, grammarCardData.effect);

        SetOpponentCardPosition(opponentCard as Card);
        opponentCard.cardDisplay.ShowCard();
        return opponentCard as Card;
    }
    public Card SetupOpponentObjectCard(int id)
    {
        if (!opponentCardDataDict.ContainsKey(id))
        {
            Debug.LogError("Opponent card data not found for id " + id);
            return null;
        }

        var opponentCard = Instantiate(cardPrefab, opponentCardContainer).GetComponent<ObjectCard>();
       
        var cardData = opponentCardDataDict[id];
        opponentCardDataDict.Remove(id);
        opponentCard.objectCardData = cardData;
        opponentCard.SetCard(cardData.id, cardData.cardName, cardData.attack, cardData.hpMax, CardOwner.Opponent);

        // combine grammar list into a single string separated by 
        CardSelectionManager.SetGrammarDataForCard(opponentCard, opponentGrammarAnswersDict[id]);
        opponentGrammarAnswersDict.Remove(id);
        SetOpponentCardPosition(opponentCard as Card);
        opponentCard.SetTextureFromData();
        opponentCard.cardDisplay.ShowCard();
        return opponentCard as Card;
    }

    public void SwapCards(int firstCardSlotID, CardOwner firstOwner, int secondCardSlotID, CardOwner secondOwner)
    {
        if ((firstCardSlotID == secondCardSlotID && firstOwner == secondOwner) || secondOwner == null)
        {
            // TODO still display effect?
            return;
        }

        Debug.Log("Swapping cards. First card slot ID: " + firstCardSlotID + " Second card slot ID: " + secondCardSlotID + " First owner: " + firstOwner + " Second owner: " + secondOwner);

        CardSlot firstSlot = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(firstCardSlotID, firstOwner);
        CardSlot secondSlot = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(secondCardSlotID, secondOwner);

        ObjectCard firstCard = firstSlot.GetCard();
        ObjectCard secondCard = secondSlot.GetCard();
        firstCard.cardData.owner = secondOwner;
        secondCard.cardData.owner = firstOwner;

        firstSlot.SetCard(secondCard);
        secondSlot.SetCard(firstCard);
        
        AudioManager.instance.Play(SoundType.CardPlaced);

        (firstCard.transform.localPosition, secondCard.transform.localPosition) = (secondCard.transform.localPosition, firstCard.transform.localPosition);


        // why generate grammar here?
        // NormalGrammarExtensionsMethod();

        // TODO play animation and apply effects to the cardslots
        // Display effect here instead of inside the swap effect script
        EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.Swap, 0, firstCard.gameObject);
        EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.Swap, 0, secondCard.gameObject);

        //Update possible grammars
        if(firstSlot.GetOwner() == CardOwner.Opponent && secondSlot.GetOwner() == CardOwner.Player)
        {
            //Remove possible grammars of previous card, because currently grammar cards are only applied to player cards
            RemoveAllPossibleGrammarsOfCard(secondCard);

            //Add possible grammars of new card
            AddAllPossibleGrammarsOfCard(firstCard);
        }
        else if(firstSlot.GetOwner() == CardOwner.Player && secondSlot.GetOwner() == CardOwner.Opponent)
        {
            //Remove possible grammars of previous card, because currently grammar cards are only applied to player cards
            RemoveAllPossibleGrammarsOfCard(firstCard);

            //Add possible grammars of new card
            AddAllPossibleGrammarsOfCard(secondCard);
        }
    }

   

    public void PrintSlotCardData()
    {
        CardSlot slot = BattlefieldManager.instance.GetCardSlotFromID(testHighlightCardSlotID, CardOwner.Player);
        string debugString = "SlotData. Card is " + slot.GetCard();
        if (slot.HasCard())
        {
            Card c = slot.GetCard();
            debugString += ". CardName is '" + c.cardData.cardName + "' (" + c.cardData.id + ")";
        }
        Debug.Log(debugString);
    }

    private void SetOpponentCardPosition(Card opponentCard)
    {
        Destroy(opponentCard.selectable);
        var sourcePos = Camera.main.transform.position;
        var battleFieldTransform = BattlefieldManager.instance.battlefieldGameObject.transform;
        opponentCard.transform.localScale = new Vector3(2f, 2f, 1f);
        opponentCardRectSize = opponentCard.rect.rect.size;
        MoveCardFromOverlayToWorld(opponentCard);

        // create a plane object representing the Plane
        var plane = new Plane(-battleFieldTransform.forward, battleFieldTransform.position);

        // get the closest point on the plane for the Source position
        var mirrorPoint = plane.ClosestPointOnPlane(sourcePos);

        // get the position of Source relative to the mirrorPoint
        var distance = sourcePos - mirrorPoint;
        opponentCard.transform.position = mirrorPoint - distance;
    }

    public void AddOpponentObjectCard(ObjectCardData cardData)
    {
        if (!opponentCardDataDict.ContainsKey(cardData.id))
        {
            opponentCardDataDict.Add(cardData.id, cardData);
        }
    }

    public void AddOpponentGrammarAnswers(Dictionary<Grammars, string> grammarTable, int cardID)
    {
        if (!opponentGrammarAnswersDict.ContainsKey(cardID))
        {
            opponentGrammarAnswersDict.Add(cardID, grammarTable);
        }
    }
    #endregion

    //-----------------------------------------------------------------------------------

    #region turn functions

    public void ResetDataForEachTurn()
    {
        ActionsLeft = playerSettings.NumActionsPerTurn;
    }

    public void ToggleCardVisibilityWithCancelTurn(bool visible)
    {
        ToggleCardVisibility(visible);
        if (!visible)
        {
            Debug.Log("Cancel turn");
            NetworkManager.instance.SendLocalPlayerCancelTurn();
        }
    }


    private void ToggleCardVisibility(bool enabled)
    {
        if ((enabled && Phase2RefHolder.instance.cardHolderButtonImage.sprite == cardHolderButtonSprites[1]) || (!enabled && Phase2RefHolder.instance.cardHolderButtonImage.sprite == cardHolderButtonSprites[0]))
        {
            return;
        }
        AudioManager.instance.PlayButtonClickSounds();
        Phase2RefHolder.instance.cardHolderButton.interactable = false;
        if (enabled)
        {
            Phase2RefHolder.instance.cardHolder.DOAnchorPosY(250f, 0.4f).SetEase(Ease.OutCirc).OnComplete(() =>
            {
                Phase2RefHolder.instance.cardHolderButtonImage.sprite = cardHolderButtonSprites[1];

                Phase2RefHolder.instance.cardHolderButton.interactable = true;
            });
        }
        else
        {
            currentSelectedCard?.selectable.Deselect();
            StopArrowFlash();
            Phase2RefHolder.instance.cardHolder.DOAnchorPosY(-200f, 0.4f).SetEase(Ease.OutCirc).OnComplete(() =>
            {
                Phase2RefHolder.instance.cardHolderButtonImage.sprite = cardHolderButtonSprites[0];

                Phase2RefHolder.instance.cardHolderButton.interactable = true;
            });
        }
    }

    public void PlayTurn(int time)
    {
        if (hasGameEnded)
        {
            return;
        }
        AudioManager.instance.Play(SoundType.TurnStart);
        Phase2RefHolder.instance.GrammarPileButton.interactable = ActionsLeft > 0 && grammarCardsEnabled;
        Phase2RefHolder.instance.cardHolderButton.onValueChanged.RemoveAllListeners();
        if (!Phase2RefHolder.instance.cardHolderButton.isOn)
        {
            Phase2RefHolder.instance.cardHolderButton.isOn = true;
            ToggleCardVisibility(true);
        }
        Phase2RefHolder.instance.cardHolderButton.onValueChanged.AddListener(ToggleCardVisibilityWithCancelTurn);
        gameplayInteractions.SetActive(true);
        LeanTouch.OnFingerSwipe += OnFinger;
        isCardSlotDetectionActive = true;
        StartCircularTimer(time);
    }

    public void EndTurn()
    {
        AudioManager.instance.Play(SoundType.TurnEnd);
        StopCircularTimer();
        LeanTouch.OnFingerSwipe -= OnFinger;
        ActionsLeft = 0;
        StopArrowFlash();
        Phase2RefHolder.instance.cardHolderButton.onValueChanged.RemoveAllListeners();
        NetworkManager.instance.WhoseTurn = CardOwner.Opponent;
        Phase2RefHolder.instance.GrammarPileButton.interactable = false;

        if (currentSelectedCard != null)
        {
            MoveCardFromTopToDeck();
            currentSelectedCard.rect.DOComplete(true);
        }
        currentSelectedCard = null;
        HighlightedCardSlot = null;
        if (needToSelectCardSlotForSwap)
        {
            AudioManager.instance.StopAudio(SoundType.Swap);
            AlwaysActiveUIManager.Instance.CancelProgressCircle();
            CardSlotSelectedForSwap();

            // Send cancel event for swap event
            cardSlotToSwapWith = sourceCardSlotForSwap;
        }


        cardSlotHighlight.SetActive(false);
        cardSlotInvalid.SetActive(false);


        if (Phase2RefHolder.instance.cardHolderButton.isOn)
        {
            Phase2RefHolder.instance.cardHolderButton.isOn = false;
            ToggleCardVisibility(false);
        }
        Phase2RefHolder.instance.cardHolderButton.onValueChanged.AddListener(ToggleCardVisibility);
        OrganizeDeck(true);
        isCardSlotDetectionActive = false;
    }

    #endregion

    //-----------------------------------------------------------------------------------


    #region grammar card functions


    // This function receives answers from GPT in this format
    /// <summary>
    /// grammarExtensions - List of all grammar extensions for the objects. Object order same as the one in Get ALL Object Names in a List. It should contain the extensions only (to be showed on the grammar card later)
    /// wordWithGrammar - List of all object words with their respective grammar attached. Outermost List is for object order. Second List index // 0 = Singular, 1 = Plural, other list index from enum
    /// </summary>
    public void SetGrammarsForObjects(List<Dictionary<Grammars, string>> grammarExtensions, List<Dictionary<Grammars, string>> wordWithGrammar)
    {
        for (int i = 0; i < cardsInDeck.Count; i++)
        {
            ObjectCard card = (ObjectCard)cardsInDeck[i];
            card.SetGrammarData(grammarExtensions[i], wordWithGrammar[i]);
        }
    }

    public void SetAllPossibleGrammars(IReadOnlyList<Card> cardList)
    {
        foreach (var card in cardList)
        {
            var objectCard = card as ObjectCard;
            AddAllPossibleGrammarsOfCard(objectCard);
        }
    }

    private void AddAllPossibleGrammarsOfCard(ObjectCard objectCard)
    {
        foreach (var key in objectCard.possibleGrammars?.Keys)
        {
            if (!allPossibleGrammars.ContainsKey((objectCard.objectCardData.cardName, key)))
            {
                allPossibleGrammars.Add((objectCard.objectCardData.cardName, key), objectCard.possibleGrammars[key]);
            }
        }
    }

    private void RemoveAllPossibleGrammarsOfCard(ObjectCard objectCard)
    {
        foreach (var key in objectCard.possibleGrammars.Keys)
        {
            allPossibleGrammars.Remove((objectCard.objectCardData.cardName, key));
        }
    }

    // Called when player uses a grammar card on an object
    public void UsedGrammarCard(GrammarCard grammarCard, ObjectCard objectCard)
    {
        // Fill up grammar table and apply buff
        Effect effect = EffectManager.instance.CreateEffect(grammarCard.grammarCardData.effect, objectCard);
        string answer = objectCard.GrammarApplied(grammarCard.grammarCardData.grammar, effect);

        // Grammar card may have more than 1 correct answer, player may have chosen any one of them (the one that was not removed from the dictionary)
        var correctAnswerObjectCards = grammarCard.grammarCardData.correctAnswerObjectCardNames;
        for (int i = 0; i < correctAnswerObjectCards.Length; i++)
        {
            var key = (correctAnswerObjectCards[i], grammarCard.grammarCardData.grammar);
            if (!allPossibleGrammars.ContainsKey(key))
            {
                allPossibleGrammars.Add(key, grammarCard.grammarCardData.cardName);
            }
        }

        // Finally remove the current object card grammar from the dictionary
        allPossibleGrammars.Remove((objectCard.objectCardData.cardName, grammarCard.grammarCardData.grammar));


        var buffParticle = Instantiate(buffParticlePrefab, grammarCard.transform.position, Quaternion.identity, grammarCard.transform.parent);
        buffParticle.transform.localEulerAngles = new Vector3(-90f, 0f, 0);
        var floatingText = floatingTextFeedback?.GetFeedbackOfType<MMF_FloatingText>();
        floatingText.Value = answer;
        floatingTextFeedback.FeedbacksIntensity = 1f;
        floatingText.AnimateColorGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[] { new GradientColorKey(rightGrammarColours[0], 0f), new GradientColorKey(rightGrammarColours[1], 1f) }
        };
        floatingTextFeedback.PlayFeedbacks(grammarCard.transform.position);

        grammarCard.transform.DOKill();
        grammarCard.transform.GetChild(0)?.DOKill();
        // Destroy grammar card
        Destroy(grammarCard.gameObject);
        ActionsLeft--;
        if(ActionsLeft == 0)
        {
            StartArrowFlash();
        }
    }

    private bool IsCorrectGrammar()
    {
        //Returns true if grammar is correct

        var grammarCard = currentSelectedCard as GrammarCard;
        return HighlightedCardSlot.GetCard().ContainsGrammar(grammarCard.grammarCardData.grammar, grammarCard.grammarCardData.cardName);
    }


    public void GenerateGrammarCard()
    {
        if (!isCardSlotDetectionActive || ActionsLeft < 1)
        {
            return;
        }
        NormalGrammarExtensionsMethod();
    }


    /// <summary>
    /// Also used when all grammar for all cards are unlocked and we need to repeat the grammar
    /// </summary>
    private void NormalGrammarExtensionsMethod()
    {
        // Get list of keys from allPossibleGrammars
        List<(string, Grammars)> keys = new List<(string, Grammars)>(allPossibleGrammars.Keys);
        List<ObjectCard> objectCardsOnBoard = new List<ObjectCard>();
        List<string> objectCardNames = new List<string>();
        GetObjectCardsOnBoard(objectCardsOnBoard, objectCardNames);

        if (keys.Count == 0)
        {
            // No more grammar cards can be generated, all cards have all grammars unlocked
            // Repeat the grammar 
            SetAllPossibleGrammars(objectCardsOnBoard);
            isAllGrammarForAllCardsUnlocked = true;
            // Repeated grammar from object cards on board will be counted so all grammar flag may not become true even when it should be
        }

        List<string> correctAnswers = new List<string>();
        var randomKey = GetRandomKeyFromDict(keys, objectCardNames, correctAnswers);
        if (randomKey.Item1.IsNullOrEmpty())
        {
            // No more grammar cards can be generated, all cards on board have all grammars unlocked
            // Generate grammar for cards in deck, maybe later
            // For now just repeat the grammar
            SetAllPossibleGrammars(objectCardsOnBoard);
            isAllGrammarForCardsOnBoardUnlocked = true;
            randomKey = GetRandomKeyFromDict(keys, objectCardNames, correctAnswers);
        }
        string randomValue = allPossibleGrammars[randomKey];

        correctAnswers.Add(randomKey.Item1);
        // Get all objects with same grammar as randomValue
        foreach (var key in keys)
        {
            if (key.Item1 != randomKey.Item1 && allPossibleGrammars[key] == randomValue)
            {
                if (!correctAnswers.Contains(key.Item1))
                {
                    correctAnswers.Add(key.Item1);
                }
            }
        }

        // Remove that value from the dictionary
        allPossibleGrammars.Remove(randomKey);

        SetupGrammarCard(correctAnswers, randomKey.Item2, randomValue);

        AudioManager.instance.Play(SoundType.CardDraw);
    }

    private void SetupGrammarCard(List<string> correctAnswers, Grammars grammar, string grammarExtension)
    {
        //Generate grammar card
        var deckContainer = Phase2RefHolder.instance.cardHolder.GetChild(0);
        var grammarCard = Instantiate(cardPrefab, deckContainer).GetComponent<GrammarCard>();

        // Get random effect from grammarCardEffect enums
        var randomEffect = EffectManager.instance.GetRandomEffect();

        grammarCard.SetCard(0, grammarExtension, "Some effect", CardOwner.Player, grammar, randomEffect);
        grammarCard.SetCorrectAnswerObjectCardNames(correctAnswers);
        var currentPosition = Phase2RefHolder.instance.GrammarPileButton.transform.position;
        AddCardToDeck(grammarCard);
        OrganizeDeck();

        grammarCard.rect.DOComplete(true);
        var newPosition = grammarCard.rect.localPosition;
        var newRotation = grammarCard.rect.localRotation;
        grammarCard.rect.rotation = Quaternion.identity;
        Phase2RefHolder.instance.GrammarPileButton.interactable = false;
        grammarCard.rect.DOMove(Vector3.right * centerOfScreen.x + Vector3.up * topOfScreen.y * 0.35f, 0.4f).SetEase(Ease.OutCirc).From(currentPosition).OnComplete(() =>
        {
            grammarCard.rect.DOLocalMove(newPosition, 0.2f).SetEase(Ease.InCirc).SetDelay(0.4f);
            grammarCard.rect.DOLocalRotateQuaternion(newRotation, 0.2f).SetEase(Ease.InCirc).SetDelay(0.4f);
            bool shouldButtonsBeInteractable = NetworkManager.instance.WhoseTurn == CardOwner.Player;
            Phase2RefHolder.instance.GrammarPileButton.interactable = shouldButtonsBeInteractable || ActionsLeft > 0;
            //gameplayInteractions.SetActive(shouldButtonsBeInteractable || ActionsLeft > 0);
            ActionsLeft--;
            if (ActionsLeft == 0)
            {
                StartArrowFlash();
            }
        });
    }

    private (string, Grammars) GetRandomKeyFromDict(List<(string, Grammars)> keys, List<string> objectCardNames, List<string> correctAnswers)
    {
        var keysCopy = new List<(string, Grammars)>(keys);
        foreach (var key in keysCopy)
        {
            if (!objectCardNames.Contains(key.Item1))
            {
                keys.Remove(key);
            }
        }

        // Get random key from keys
        if (keys.Count == 0)
        {
            return (null, Grammars.NominativeSingular);
        }

        var randomKey = keys[UnityEngine.Random.Range(0, keys.Count)];
        return randomKey;
    }

    private static void GetObjectCardsOnBoard(List<ObjectCard> objectCardsOnBoard, List<string> objectCardNames)
    {
        for (int i = 0; i < BattlefieldManager.instance.cardSettings.NumColumns * BattlefieldManager.instance.cardSettings.NumRows; i++)
        {
            var card = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(i, CardOwner.Player).GetCard();
            if (card != null)
            {
                objectCardsOnBoard.Add(card);
                objectCardNames.Add(card.objectCardData.cardName);
            }
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------


    #region slot functions and deck functions

    private bool IsValidSlot()
    {
        if(ActionsLeft < 1)
        {
            validSlot = false;
            HighlightedCardSlot?.WrongSlot();
            return false;
        }
        if (currentSelectedCard == null)
        {
            HighlightedCardSlot?.CorrectSlot();
            return true;
        }

        validSlot = false;
        // check card slot occupied and selected card is object card
        // check if card slot is empty and selected card is grammar card


        if (HighlightedCardSlot == null || HighlightedCardSlot.GetOwner() == CardOwner.Opponent)
        {
            HighlightedCardSlot?.WrongSlot();
            return false;
        }
        if (HighlightedCardSlot.GetCard() != null)
        {
            if (isObjectCardSelected)
            {
                HighlightedCardSlot?.WrongSlot();
                return false;
            }
        }
        else
        {
            if (!isObjectCardSelected)
            {
                HighlightedCardSlot?.WrongSlot();
                return false;
            }
        }

        HighlightedCardSlot?.CorrectSlot();
        validSlot = true;
        return true;
    }


    public void RemoveCardFromDeck(Card card)
    {
        cardsInDeck.Remove(card);
    }

    public void AddCardToDeck(Card card)
    {
        cardsInDeck.Add(card);
    }



    public void CardSelected(Card card)
    {
        Debug.Log("Card selected " + card.name + "Type " + card.GetType());
        if (currentSelectedCard != null)
        {
            currentSelectedCard.selectable.Deselect();
            //MoveCardFromCenterToDeck();
        }
        currentSelectedCard = card;
        prevCardState = new CardState()
        {
            index = cardsInDeck.IndexOf(currentSelectedCard),
            position = currentSelectedCard.rect.position,
            rotation = currentSelectedCard.rect.rotation.eulerAngles,
            scale = currentSelectedCard.rect.localScale
        };
        RemoveCardFromDeck(currentSelectedCard);
        OrganizeDeck();
        isObjectCardSelected = card.GetType() == typeof(ObjectCard);
        if (!isObjectCardSelected && playerSettings.ShowCorrectAnswers)
        {
            var grammarCard = card as GrammarCard;
            HighlightCorrectAnswers(grammarCard);
        }
        HighlightCardSlot();
        MoveCardFromDeckToTop();
    }


    public void CardDeselected()
    {
        if (currentSelectedCard == null)
        {
            return;
        }

        MoveCardFromTopToDeck();
        HighlightCardSlot();
        currentSelectedCard = null;
    }

    private void MoveCardFromTopToDeck()
    {
        //currentSelectedCard.rect.position = prevCardState.position;
        //currentSelectedCard.rect.rotation = Quaternion.Euler(prevCardState.rotation);
        currentSelectedCard.rect.DOComplete(true);
        var position = currentSelectedCard.rect.position;
        var rotation = currentSelectedCard.rect.rotation;
        var scale = currentSelectedCard.rect.localScale;
        cardsInDeck.Insert(prevCardState.index, currentSelectedCard);
        OrganizeDeck();
        currentSelectedCard.rect.DOComplete(false);
        currentSelectedCard.rect.DOScale(currentSelectedCard.rect.localScale, 0.3f).SetEase(Ease.OutCirc).From(scale);
        currentSelectedCard.rect.DOMove(currentSelectedCard.rect.position, 0.3f).SetEase(Ease.OutCirc).From(position);
        currentSelectedCard.rect.DORotateQuaternion(currentSelectedCard.rect.rotation, 0.3f).SetEase(Ease.OutCirc).From(rotation);
        AudioManager.instance.PlaySwipeSounds();
    }

    private void AddDeckCardTween(Tween tween)
    {
        deckCardTweens.Enqueue(tween);
    }

    private void MoveCardFromDeckToTop()
    {
        
        //Animate card from deck to center of screen
        currentSelectedCard.rect.DOComplete(true);
        currentSelectedCard.rect.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.3f).SetEase(Ease.OutCirc);
        currentSelectedCard.rect.DOMove(centerOfScreen + Vector3.up * 230f, 0.3f).SetEase(Ease.OutCirc);
        currentSelectedCard.rect.DORotateQuaternion(Quaternion.identity, 0.3f).SetEase(Ease.OutCirc);
        AudioManager.instance.PlaySwipeSounds();

    }

    public void OnFinger(LeanFinger finger)
    {
        lastScreenPosition = finger.LastScreenPosition;
        cardThrowDirection = finger.SwipeScaledDelta.normalized;
        if (cardThrowDirection.x > 0 && cardThrowDirection.x > 1.2f * Mathf.Abs(cardThrowDirection.y))
        {
            OnSwipeRight();
        }
        else if (cardThrowDirection.x < 0 && cardThrowDirection.x < -1.2f * Mathf.Abs(cardThrowDirection.y))
        {
            OnSwipeLeft();
        }
        else if(cardThrowDirection.y > 0 && cardThrowDirection.y > 1.2f * Mathf.Abs(cardThrowDirection.x))
        {
            OnSwipeUp();
        }
        else if(cardThrowDirection.y < 0 && cardThrowDirection.y < -1.2f * Mathf.Abs(cardThrowDirection.x))
        {
            OnSwipeDown();
        }
    }

    private void OnSwipeDown()
    {
        currentSelectedCard?.selectable.Deselect();
    }

    private void OnSwipeUp()
    {
        OnSwipeRight();
    }

    public void OnSwipeRight()
    {
        if (currentSelectedCard != null && ActionsLeft > 0)
        {
            PlaceCardOnBoard();
        }
    }

    public void OnSwipeLeft()
    {
        //Animate card from center of screen to deck
        //store a previous position of the card
        //currentSelectedCard = null;
        OnSwipeRight();
    }

    public void OrganizeDeck(bool complete = false)
    {
        // Show cards in an arc

        float angle = 5f * cardsInDeck.Count; // Initial angle.
        float increment = 10.0f;
        float initX = -20f * cardsInDeck.Count;
        float initY = 440f;
        var deckContainer = Phase2RefHolder.instance.cardDeckCircleCenter;
        for (int i = 0; i < cardsInDeck.Count; i++)
        {
            Card card = cardsInDeck[i];
            if (cardsInDeck != null)
            {
                card.rect.DOComplete(true);
                card.rect.SetParent(deckContainer);
                card.rect.DOLocalMove(new Vector3(initX + 40f * i, initY, 0f), 0.3f).SetEase(Ease.Linear);
                card.rect.localScale = new Vector3(1f, 1f, 1f);
                var rotation = cardsInDeck[i].rect.rotation;
                card.rect.rotation = Quaternion.identity;
                card.rect.RotateAround(deckContainer.position, Vector3.forward, angle);
                card.rect.DORotateQuaternion(cardsInDeck[i].rect.rotation, 0.3f).From(rotation).SetEase(Ease.Linear);
                angle -= increment;
                if (complete)
                {
                    card.rect.DOComplete(true);
                }
            }
        }
    }

    public async Task<(int, CardOwner)> ChooseSwapTarget()
    {
        // TODO replace with actual code
        // TODO return (and send) Int.MaxValue if swap target selection not successful (e.g. not selected on time)
        await Task.Delay(5000);
        NetworkManager.instance.SendSwapTarget(testHighlightCardSlotID, testSelectedCardOwner);
        return (testHighlightCardSlotID, testSelectedCardOwner);

        /*
        int swapTargetID = highlightedCardSlot.GetCard().cardData.id;
        NetworkManager.instance.SendSwapTarget(swapTargetID);
        return swapTargetID;
        */
    }


    private void HighlightCardSlot()
    {
        if (HighlightedCardSlot == null)
        {
            return;
        }
        // Highlight card slot
        cardSlotHighlight.SetActive(true);
        cardSlotHighlight.transform.localPosition = Vector3.up * 0.03f + HighlightedCardSlot.transform.localPosition;
        cardSlotInvalid.transform.localPosition = Vector3.up * 0.03f + HighlightedCardSlot.transform.localPosition;
        cardSlotInvalid.SetActive(false);
        if (!IsValidSlot())
        {
            cardSlotHighlight.SetActive(false);
            cardSlotInvalid.SetActive(true);
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------

    #region phase 2 start functions

    public void StartPhase2()
    {
        Phase2RefHolder.instance.GrammarPileButton.onClick.AddListener(GenerateGrammarCard);
        Phase2RefHolder.instance.playerSwipe.OnFinger.RemoveAllListeners();
        //Phase2RefHolder.instance.playerSwipe.OnFinger.AddListener(OnFinger);
        Phase2RefHolder.instance.overlayCanvas.gameObject.SetActive(true);
        Phase2RefHolder.instance.cardHolderButton.onValueChanged.RemoveAllListeners();
        LeanTouch.OnFingerSwipe -= OnFinger;
        //BattlefieldManager.instance.battlefieldGameObject.transform.localEulerAngles = new Vector3(0f, BattlefieldManager.instance.battlefieldGameObject.transform.localEulerAngles.y, 0f);
        Phase2RefHolder.instance.cardHolderButton.interactable = false;
        Phase2RefHolder.instance.cardHolderButton.isOn = true;
        OrganizeDeck();
        Destroy(phase1Deletables);
        cardSlotHighlight = Instantiate(Phase2RefHolder.instance.cardSlotHighlightGameObject, BattlefieldManager.instance.battlefieldGameObject.transform);
        cardSlotInvalid = Instantiate(Phase2RefHolder.instance.cardSlotInvalidGameObject, BattlefieldManager.instance.battlefieldGameObject.transform);
        cardSlotHighlight.SetActive(false);
        cardSlotInvalid.SetActive(false);
        //dynamicTextParticle = Instantiate(dynamicTextParticlePrefab, BattlefieldManager.instance.battlefieldGameObject.transform).GetComponent<CFXR_ParticleText_Runtime>();
        //dynamicTextParticle.transform.localScale = Vector3.one * 0.1f;
        cardSlotHighlightScale = cardSlotHighlight.transform.localScale;
        SetAllPossibleGrammars(cardsInDeck);
        UnityEngine.Random.InitState(40);

        OpponentHealth = playerSettings.InitialPlayerHealth;
        PlayerHealth = playerSettings.InitialPlayerHealth;
        Phase2RefHolder.instance.cardHolder.gameObject.SetActive(false);
        //Let player place all object cards on the battlefield
        //StartCoroutine(StartGame());
    }

    public IEnumerator StartGame()
    {
        gameStarted = true;
        AlwaysActiveUIManager.Instance.raycastCenterImage.gameObject.SetActive(true);
        AudioManager.instance.StartBackgroundMusicPhase2();
        AlwaysActiveUIManager.Instance.SetInfoPanelText("Place all your cards on the board.\nTime = " + playerSettings.InitalTimeToPlaceCards + " seconds.");

        AlwaysActiveUIManager.Instance.ShowInfo(3);
        yield return new WaitForSeconds(3f);
        LeanTouch.Instance.SwipeThreshold = 50f;
        var colouredParts = BattlefieldManager.instance.battlefieldGameObject.transform.GetChild(1);
        colouredParts.GetChild(0).gameObject.SetActive(true);
        colouredParts.GetChild(1).gameObject.SetActive(true);
        actionsLeftText = colouredParts.GetChild(2).GetComponent<TMPro.TextMeshPro>();
        actionsLeftText.gameObject.SetActive(true);
        ActionsLeft = playerSettings.InitialNumOfActions;
        BattlefieldManager.instance?.SetupGameplayObjects(true);
        AlwaysActiveUIManager.Instance.StartProgressCircle(3f, null);
        yield return new WaitForSeconds(3f);
        Phase2RefHolder.instance.cardHolder.gameObject.SetActive(true);
        BattlefieldManager.instance?.PlayerStartedTurn(playerSettings.InitalTimeToPlaceCards);
        cardSlotDetection = CardSlotDetectionForPlacingCard;
        isCardSlotDetectionActive = true;
        PlayTurn(playerSettings.InitalTimeToPlaceCards);
        Phase2RefHolder.instance.cardHolderButton.onValueChanged.RemoveAllListeners();
        Phase2RefHolder.instance.cardHolderButton.onValueChanged.AddListener(ToggleCardVisibility);
        yield return new WaitForSeconds(playerSettings.InitalTimeToPlaceCards);
        EndTurn();
        yield return BattlefieldManager.instance.Attack();
        AlwaysActiveUIManager.Instance.SetInfoPanelText("Each turn you can do " + playerSettings.NumActionsPerTurn + " actions. Playing a card and drawing a card each is 1 action.\nTap on the bottom arrow to end your turn. You have " + playerSettings.TimeToPlay + "seconds.\n Your time starts in....");
        AlwaysActiveUIManager.Instance.ShowInfo(5);

        yield return new WaitForSeconds(5f);
        grammarCardsEnabled = true;
        //Countdown animation
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            yield return CountdownManager.Instance.Countdown321("Your Turn");
            NetworkManager.instance.SetInitialCurrentTurn(1);
            NetworkManager.instance.SetInitialLastTurn(CardOwner.Player);
            yield return new WaitForSeconds(1f);
            //Phase2RefHolder.instance.cardHolderButton.interactable = true;
            NetworkManager.instance.WhoseTurn = CardOwner.Player;
            NetworkManager.instance.StartTurn();
        }
        else
        {
            yield return CountdownManager.Instance.Countdown321("Opponent's Turn"); 
            NetworkManager.instance.SetInitialCurrentTurn(1);
            NetworkManager.instance.SetInitialLastTurn(CardOwner.Opponent);
            yield return new WaitForSeconds(1f);
        }


    }
    #endregion

    //-----------------------------------------------------------------------------------

    #region card to board functions



    public void PlaceCardOnBoard()
    {
        if (IsValidSlot())
        {
            currentSelectedCard.selectable.OnSelected.RemoveAllListeners();
            currentSelectedCard.selectable.OnDeselected.RemoveAllListeners();
            leanSelect.Deselect(currentSelectedCard.selectable);
            currentSelectedCard.rect.DOComplete();
            var particle = Instantiate(cardThrowParticlePrefab, Camera.main.ScreenToWorldPoint(new Vector3(lastScreenPosition.x, lastScreenPosition.y, 4f)), Quaternion.identity, BattlefieldManager.instance.transform);
            particle.transform.localScale *= 0.5f;
            bool isCardValid = true;


            if (!isObjectCardSelected && !IsCorrectGrammar())
            {
                isCardValid = false;
            }



            if (!isObjectCardSelected)
            {
                var grammarCard = currentSelectedCard as GrammarCard;
                NetworkManager.instance.SendGrammarCardToPlaceOnBoard(grammarCard.grammarCardData, HighlightedCardSlot.GetId(), isCardValid);
            }
            else
            {
                var objectCard = currentSelectedCard as ObjectCard;
                isAllGrammarForCardsOnBoardUnlocked = false; // Need to change this if we want to apply grammar to opponent cards as well

                NetworkManager.instance.SendObjectCardIDToPlaceOnBoard(objectCard.objectCardData.id, HighlightedCardSlot.GetId(), isCardValid);
            }
            var currentCard = currentSelectedCard;
            StartCoroutine(MoveCardToBoard(isCardValid, HighlightedCardSlot, currentCard.rect.rect.size, currentCard, cardThrowDirection, true));
            currentSelectedCard = null;
            if(isObjectCardSelected)
            {
                ActionsLeft--;
                if (ActionsLeft == 0)
                {
                    StartArrowFlash();
                }
            }
        }
    }

    private IEnumerator MoveCardToBoard(bool validCard, CardSlot cardSlot, Vector2 rectSize, Card card, Vector2 cardThrowVec, bool spaceCoversion)
    {
        //Animate card from center of screen to card slot
        var slotScale = cardSlot.transform.localScale;
        var targetScale = new Vector3(1 / rectSize.x * slotScale.x, 1 / rectSize.y * slotScale.z, 1 / rectSize.x * slotScale.z);
        if (spaceCoversion)
        {
            MoveCardFromOverlayToWorld(card);
        }
        yield return ThrowCardAnimation(card.transform, cardSlot.transform, targetScale, cardThrowVec);

        Debug.Log("Card throw completed");
        AudioManager.instance.Play(SoundType.CardPlaced);
        if (validCard)
        {
            if (card.GetType() == typeof(ObjectCard))
            {
                var cardPlaceParticle = Instantiate(cardPlaceParticlePrefab, card.transform.parent);
                cardPlaceParticle.transform.localPosition = card.transform.localPosition;
            }

            card.ApplyCard(cardSlot);
        }
        else
        {
            // Wrong grammar
            AudioManager.instance.Play(SoundType.WrongAnswer);
            Instantiate(wrongGrammarParticlePrefab, card.transform.position, Quaternion.identity, card.transform.parent);
            var floatingText = floatingTextFeedback?.GetFeedbackOfType<MMF_FloatingText>();
            floatingTextFeedback.FeedbacksIntensity = 1f;
            floatingText.Value = "WRONG!!";
            floatingText.AnimateColorGradient = new Gradient()
            {
                colorKeys = new GradientColorKey[] { new GradientColorKey(wrongGrammarColours[0], 0f), new GradientColorKey(wrongGrammarColours[1], 1f) }
            };
            card.transform.DOKill();
            card.transform.GetChild(0)?.DOKill();
            floatingTextFeedback.PlayFeedbacks(card.transform.position);

            // Add the value back to the dictionary
            var grammarCard = card as GrammarCard;
            var objectCard = cardSlot.GetCard();
            var key = (objectCard.objectCardData.cardName, grammarCard.grammarCardData.grammar);
            if (!allPossibleGrammars.ContainsKey(key))
            {
                allPossibleGrammars.Add(key, grammarCard.grammarCardData.cardName);
            }

            HighlightCorrectAnswers(grammarCard);

            ActionsLeft--;
            if (ActionsLeft == 0)
            {
                StartArrowFlash();
            }
            Destroy(card.gameObject);
        }
    }

    private static void HighlightCorrectAnswers(GrammarCard grammarCard)
    {
        // Highlight the correct object cards
        List<string> correctAnswersList = new List<string>(grammarCard.grammarCardData.correctAnswerObjectCardNames);
        for (int i = 0; i < BattlefieldManager.instance.cardSettings.NumColumns * BattlefieldManager.instance.cardSettings.NumRows; i++)
        {
            var tempObjectCard = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(i, CardOwner.Player).GetCard();
            if (tempObjectCard != null && correctAnswersList.Contains(tempObjectCard.objectCardData.cardName))
            {
                tempObjectCard.CorrectAnswerHighlight();
            }
        }
    }

    public void MoveCardFromOverlayToWorld(Card card)
    {
        var overlayCanvasEnabled = Phase2RefHolder.instance.overlayCanvas.gameObject.activeSelf;
        if (!overlayCanvasEnabled)
        {
            Phase2RefHolder.instance.overlayCanvas.gameObject.SetActive(true);
        }
        Phase2RefHolder.instance.overlayCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        Phase2RefHolder.instance.overlayCanvas.planeDistance = 1f;
        Phase2RefHolder.instance.overlayCanvas.worldCamera = Camera.main;
        card.transform.SetParent(BattlefieldManager.instance.battlefieldGameObject.transform, true);
        Phase2RefHolder.instance.overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (!overlayCanvasEnabled)
        {
            Phase2RefHolder.instance.overlayCanvas.gameObject.SetActive(false);
        }
    }

    public static IEnumerator ThrowCardAnimation(Transform cardTransform, Transform targetTransform, Vector3 targetScale, Vector2 throwDir)
    {
        AudioManager.instance.PlayCardThrowSound();
        var zdist = targetTransform.localPosition.z - cardTransform.localPosition.z;
        var z = targetTransform.position.z - Mathf.Sign(zdist) * targetTransform.parent.transform.localScale.x;
        var midLoc = new Vector3(throwDir.x * 3f, targetTransform.localPosition.y + 2f, cardTransform.localPosition.z + zdist * 0.75f);
        var nextLoc = targetTransform.localPosition + Vector3.right * Mathf.Sign(throwDir.x) * 7f + targetTransform.up * 10f;

        cardTransform.DOLocalPath(new Vector3[] { midLoc }, 0.7f, PathType.CatmullRom, PathMode.Full3D, 30, Color.green).SetEase(Ease.OutSine).OnComplete(() =>
        {
            cardTransform.DOLocalPath(new Vector3[] { targetTransform.localPosition }, 0.3f, PathType.CatmullRom, PathMode.Full3D, 30, Color.green).SetEase(Ease.InSine);
        });
        var cardChild = cardTransform.GetChild(0);
        cardChild.DOLocalRotate(new Vector3(0f, 0f, 360f) * Mathf.Sign(throwDir.x) * 5, 1f, RotateMode.FastBeyond360).SetEase(Ease.InOutSine);
        //cardTransform.DOLocalRotate(new Vector3(90f, 0f, 0f), 0.7f, RotateMode.LocalAxisAdd);
        cardTransform.DOScale(targetScale, 0.98f).SetEase(Ease.OutCirc);
        yield return cardTransform.DOLocalRotate(Vector3.right * 90f, 1f, RotateMode.Fast).SetEase(Ease.Linear).WaitForCompletion();
    }



    #endregion

    //-----------------------------------------------------------------------------------


    public async Task<(int, CardOwner)> SelectCardSlotForSwap(int sourceSlotID)
    {
        Debug.Log("Select card slot for swap");
        var currentTextInfo = AlwaysActiveUIManager.Instance.GetInfoPanelText();
        AlwaysActiveUIManager.Instance.SetInfoPanelText("Select a card to swap with. Aim at the card and hold for 2 seconds.");
        AlwaysActiveUIManager.Instance.ShowInfo(2);
        Phase2RefHolder.instance.GrammarPileButton.interactable = false;
        HighlightedCardSlot = null;
        cardSlotDetection = CardSlotDetectionForSwappingCard;
        sourceCardSlotForSwap = BattlefieldManager.instance.GetCardSlotFromIDAndOwnerHashMap(sourceSlotID,CardOwner.Player); //assumes player is the source because grammar card always applied to player cards
        needToSelectCardSlotForSwap = true;
        while (cardSlotToSwapWith == null)
        {
            await Task.Yield();
        }
        AlwaysActiveUIManager.Instance.CancelShowInfo();
        AlwaysActiveUIManager.Instance.InfoPanelActivate(false);
        AlwaysActiveUIManager.Instance.SetInfoPanelText(currentTextInfo);
       (int, CardOwner) cardSlot = (cardSlotToSwapWith.GetId(), cardSlotToSwapWith.GetOwner());
        sourceCardSlotForSwap = null;
        cardSlotToSwapWith = null;
        return cardSlot;
    }


    public void CardSlotSelectedForSwap()
    {
        Debug.Log("Card slot selected for swap" + HighlightedCardSlot);
        cardSlotDetection = CardSlotDetectionForPlacingCard;
        cardSlotToSwapWith = HighlightedCardSlot;
        bool shouldButtonsBeInteractable = NetworkManager.instance.WhoseTurn == CardOwner.Player;
        gameplayInteractions.SetActive(shouldButtonsBeInteractable || ActionsLeft > 0);
        Phase2RefHolder.instance.GrammarPileButton.interactable = shouldButtonsBeInteractable || ActionsLeft > 0;
        needToSelectCardSlotForSwap = false;
        if(ActionsLeft == 0)
        {
            //Phase2RefHolder.instance.cardHolderButton.isOn = false;
        }
    }
    public int GetObjectCardCountInDeck()
    {
        int count = 0;
        foreach (var card in cardsInDeck)
        {
            if (card.GetType() == typeof(ObjectCard))
            {
                count++;
            }
        }
        return count;
    }

    public List<string> GetAllObjectNamesInAList()
    {
        var list = new List<string>();
        foreach (var card in cardsInDeck)
        {
            list.Add(card.cardData.cardName);
        }
        return list;
    }




    public void GameHasEnded(GameEndState gameEndState)
    {
        EndTurn();
        hasGameEnded = true;
        PhotonNetwork.Disconnect();
        switch (gameEndState)
        {
            case GameEndState.PlayerWon:
                AudioManager.instance.Play(SoundType.Win);
                AlwaysActiveUIManager.Instance.SetInfoPanelText("You Won!\nTap on the 'X' button on top left to go to main menu.");
                StartWinFireworksCoroutine();
                break;
            case GameEndState.OpponentWon:
                AudioManager.instance.Play(SoundType.Lose);
                AlwaysActiveUIManager.Instance.SetInfoPanelText("You Lost!\nTap on the 'X' button on top left to go to main menu.");
                StartLoseEffectsCoroutine();
                break;
            case GameEndState.Draw:
                AudioManager.instance.Play(SoundType.Lose);
                AlwaysActiveUIManager.Instance.SetInfoPanelText("Draw!\nTap on the 'X' button on top left to go to main menu.");
                StartLoseEffectsCoroutine();
                break;
            default:
                break;
        }
        AlwaysActiveUIManager.Instance.InfoPanelActivate(true);
    }

    public void StartWinFireworksCoroutine()
    {
        StartCoroutine(WinFireworks());
    }
    public void StartLoseEffectsCoroutine()
    {
        StartCoroutine(LoseEffects());
    }
    private IEnumerator WinFireworks()
    {
        var distanceFromCamera = 10f;
        while (true)
        {
            //get world point from random screen point
            var worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(UnityEngine.Random.Range(0, Screen.width), UnityEngine.Random.Range(0, Screen.height), distanceFromCamera));
            winParticleFeedback.PlayFeedbacks(worldPoint);
            AudioManager.instance.Play(SoundType.Fireworks);
            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator LoseEffects()
    {
        var distanceFromCamera = 10f;
        while (true)
        {
            //get world point from random screen point
            var worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(UnityEngine.Random.Range(0, Screen.width), UnityEngine.Random.Range(0, Screen.height), distanceFromCamera));
            loseParticleFeedback.PlayFeedbacks(worldPoint);
            yield return new WaitForSeconds(1f);
        }

    }


    private void StartArrowFlash()
    {
        Phase2RefHolder.instance.cardHolderButtonImage.DOColor(Color.red,0.5f).From(Color.white).SetLoops(-1,LoopType.Yoyo).SetEase(Ease.InOutSine);
        Phase2RefHolder.instance.cardHolderButtonImage.transform.DOScale(1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
    private void StopArrowFlash()
    {
        DOTween.Kill(Phase2RefHolder.instance.cardHolderButtonImage);
        DOTween.Kill(Phase2RefHolder.instance.cardHolderButtonImage.transform);
        Phase2RefHolder.instance.cardHolderButtonImage.color = Color.white;
        Phase2RefHolder.instance.cardHolderButtonImage.transform.localScale = 1f * Vector3.one;
    }

    private void StartCircularTimer(int time)
    {
        circularBar.DOFillAmount(0, time).From(1).SetEase(Ease.Linear);
        var gradient = new Gradient();
        gradient.colorSpace = ColorSpace.Gamma;
        gradient.colorKeys = new GradientColorKey[] { new GradientColorKey(Color.green, 0f), new GradientColorKey(Color.yellow, 0.6f), new GradientColorKey(Color.red, 1f) };
        gradient.alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
        gradient.SetKeys(gradient.colorKeys, gradient.alphaKeys);
        circularBar.DOGradientColor(gradient, time).SetEase(Ease.Linear);
    }

    private void StopCircularTimer()
    {
        circularBar.DOKill(false);
        circularBar.fillAmount = 0;
    }

    private void Start()
    {
        centerOfScreen = new Vector3(Screen.width / 2, Screen.height / 2);
        topOfScreen = new Vector3(Screen.width / 2, Screen.height);
        raycastLayerMask = LayerMask.GetMask("CardSlot", "Card");
        prevCardState = new CardState()
        {
            index = -1,
            position = Vector2.zero,
            rotation = Vector3.zero,
            scale = Vector3.zero
        };

        
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
    }

    private void Update()
    {
        if (hasGameEnded)
        {
            return;
        }
        if (isCardSlotDetectionActive || playerSettings.CloseUpCardView)
        {
            if (!testCardSlotSelection)
            {
                Ray ray = Camera.main.ScreenPointToRay(centerOfScreen);
                var playerRaycastHit = Physics.RaycastAll(ray.origin, ray.direction, 1000f, raycastLayerMask);
                if (playerRaycastHit.Length > 0)
                {
                    CardSlot cardSlot = null;
                    Card card = null;
                    foreach (var hit in playerRaycastHit)
                    {
                        if (hit.collider != null && cardSlot == null && hit.collider.CompareTag("CardSlot"))
                        {
                            cardSlot = BattlefieldManager.instance.GetCardSlotFromSiblingIndex(hit.collider.gameObject.transform.GetSiblingIndex());
                        }
                        if (hit.collider != null && card == null && hit.collider.CompareTag("Card"))
                        {
                            card = hit.collider.gameObject.GetComponent<Card>();
                        }
                    }

                    // select cardslot by raycast hit
                    if (isCardSlotDetectionActive)
                    {
                        cardSlotDetection(cardSlot, false);
                    }

                    

                    // select card for close up view by raycast hit, limit it to a certain distance
                    if (playerSettings.CloseUpCardView && card != null && currentSelectedCard == null) // && Vector3.Distance(card.transform.position, Camera.main.transform.position) <= playerSettings.DistanceToStartShowingCloseUpCardView
                    {
                        if (closeUpCardView.card != card)
                        {
                            closeUpCardView.gameObject.SetActive(true);
                            closeUpCardView.SetCard(card);
                            closeUpCardView.ShowCard();
                            closeUpCardViewBorder.gameObject.SetActive(true);
                            closeUpCardViewBorder.color = card.cardData.owner == CardOwner.Opponent ? Color.red : Color.green;
                        }
                    }
                    else if(playerSettings.CloseUpCardView && currentSelectedCard != null)
                    {
                        closeUpCardView.ResetCard();
                        closeUpCardView.gameObject.SetActive(false);
                        closeUpCardViewBorder.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if(isCardSlotDetectionActive)
                    {
                        cardSlotDetection(null, false);
                    }
                    if(playerSettings.CloseUpCardView)
                    {
                        //closeUpCardView.ResetCard();
                        //closeUpCardView.gameObject.SetActive(false);
                        //closeUpCardViewBorder.gameObject.SetActive(false);
                    }
                }

            }
            else
            {
                var newCardSlot = BattlefieldManager.instance.GetCardSlotFromID(testHighlightCardSlotID, CardOwner.Player);
                if (newCardSlot != null)
                {
                    if (newCardSlot != HighlightedCardSlot)
                    {
                        HighlightedCardSlot = newCardSlot;
                        HighlightCardSlot();
                    }
                }
                else
                {
                    HighlightedCardSlot = null;
                    cardSlotHighlight.SetActive(false);
                    cardSlotInvalid.SetActive(false);
                }
            }
        }
        if(!playerSettings.CloseUpCardView && closeUpCardView.gameObject.activeSelf)
        {
            closeUpCardView.ResetCard();
            closeUpCardView.gameObject.SetActive(false);
            closeUpCardViewBorder.gameObject.SetActive(false);
        }
        else if(playerSettings.CloseUpCardView && closeUpCardView.gameObject.activeSelf)
        {
            if(closeUpCardView.card == null)
            {
                closeUpCardView.ResetCard();
                closeUpCardView.gameObject.SetActive(false);
                closeUpCardViewBorder.gameObject.SetActive(false);
            }
        }
    }

    private void CardSlotDetectionForPlacingCard(CardSlot cardSlot, bool continuousAim = false)
    {
        if (cardSlot != null)
        {
            if(cardSlot != HighlightedCardSlot)
            {
                HighlightedCardSlot = cardSlot;
                HighlightCardSlot();
            }
        }
        else if(continuousAim)
        {
            HighlightedCardSlot = null;
            cardSlotHighlight.SetActive(false);
            cardSlotInvalid.SetActive(false);
        }
    }

    private void CardSlotDetectionForSwappingCard(CardSlot cardSlot, bool continuousAim = false)
    {
        if(cardSlot != null && cardSlot.GetCard() == null)
        {
            cardSlot = null;
        }
        if (cardSlot != HighlightedCardSlot)
        {
            AudioManager.instance.StopAudio(SoundType.Swap);
            AlwaysActiveUIManager.Instance.CancelProgressCircle(); 
            if (cardSlot != null)
            {
                AudioManager.instance.Play(SoundType.Swap);
                AlwaysActiveUIManager.Instance.StartProgressCircle(2f, CardSlotSelectedForSwap);
            }
        }
        
        CardSlotDetectionForPlacingCard(cardSlot, true);
    }
    public void GoToMainMenu()
    {
        AudioManager.instance.PlayButtonClickSounds();
        if (gameStarted)
        {
            NetworkManager.instance.SendLocalPlayerCancelTurn();
        }
        DOTween.KillAll();
        StopAllCoroutines();
        if(PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        Destroy(NetworkManager.instance);
        if (CardSelectionManager.instance != null)
        {
            CardSelectionManager.instance.StopAllCoroutines();
            Destroy(CardSelectionManager.instance.gameObject);
        }
        Destroy(AudioManager.instance.gameObject);
        SceneManager.LoadScene(0);
    }



}
