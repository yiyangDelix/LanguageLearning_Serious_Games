using DG.Tweening;
using Lean.Touch;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class BattlefieldManager : MonoBehaviour
{
    public static BattlefieldManager instance;

    public GameObject battlefieldPrefab;
    public GameObject cardSlotPrefab;
    public event System.Action<GameObject> onBattlefieldSpawned = delegate { };
    public XROrigin arSessionOrigin;
    public GameObject battlefieldGameObject;
    public CardSettings cardSettings;
    public GameObject TrafficLightPrefab;
    public HealthAnimation healthAnimation;
    public GameObject columnAttackParticlePrefab;
    public GameObject directAttackParticlePrefab;
    public Sprite columnAttackShapeSprite;


    private float battlefieldScale = 1f;
    private LeanPinchScale leanPinchScale;
    private LeanTwistRotateAxis leanTwistRotateAxis;
    private Vector3 battlefieldSize;
    private Dictionary<CardOwner, CardSlot[,]> cardSlots;
    private Dictionary<int, CardSlot> cardSlotsSiblingIDHashMap = new Dictionary<int, CardSlot>();
    private Dictionary<(int, CardOwner), CardSlot> cardSlotIDOwnerHashMap = new Dictionary<(int, CardOwner), CardSlot>();
    private GameObject columnAttackParticle;
    private bool syncBattleField = false;
    private TrafficLightManager trafficLight;
    private int playerCardsKillCount = 0;
    private int opponentCardsKillCount = 0;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            cardSlots = new Dictionary<CardOwner, CardSlot[,]>
        {
            { CardOwner.Player, new CardSlot[cardSettings.NumColumns, cardSettings.NumRows] },
            { CardOwner.Opponent, new CardSlot[cardSettings.NumColumns, cardSettings.NumRows] },
        };
            //DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

    }

    public void SpawnBattlefield(Vector3 position, Quaternion rotation, float scale)
    {
        battlefieldGameObject = Instantiate(battlefieldPrefab, position, rotation, transform);
        //var scaleInteractable = battlefieldGameObject.GetComponent<ARScaleInteractable>();
        //scaleInteractable.xrOrigin = arSessionOrigin;
        //var rotationInteractable = battlefieldGameObject.GetComponent<ARRotationInteractable>();
        //rotationInteractable.xrOrigin = arSessionOrigin;
        leanPinchScale = battlefieldGameObject.GetComponent<LeanPinchScale>();
        leanTwistRotateAxis = battlefieldGameObject.GetComponent<LeanTwistRotateAxis>();

        ChangeBattlefieldSize(scale);
        SetupCardSlots();
        battlefieldGameObject.SetActive(false);
        onBattlefieldSpawned(battlefieldGameObject);


        Instantiate(TrafficLightPrefab, battlefieldGameObject.transform);
        trafficLight = battlefieldGameObject.GetComponentInChildren<TrafficLightManager>();
        trafficLight.gameObject.SetActive(false);
        trafficLight.Setup();
        trafficLight.TurnOnLight(LightState.Red);
    }

    public void SetupGameplayObjects(bool enable)
    {
        trafficLight.gameObject.SetActive(enable);
        trafficLight.transform.DOScale(1f, 1f).From(0f).SetEase(Ease.OutBack);
        trafficLight.transform.DOLocalMoveY(trafficLight.transform.localPosition.y, 1f).From(-0.2f).SetEase(Ease.OutBack);
        AudioManager.instance?.PlaySwipeSounds();
        foreach (var cardSlot in cardSlotsSiblingIDHashMap.Values)
        {
            cardSlot.EnableTextAndDisableEmoji();
        }
        healthAnimation.ShowHealthObjects(enable);
    }

    public void PlayerStartedTurn(int seconds)
    {
        trafficLight.PlayerTurnLights(seconds);
    }

    public void PlayerCancelledTurn()
    {
        trafficLight.StopPlayerTurnLights();
    }

    public void SendLocation()
    {
        //Send location to other players from master client only
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            //ConjureKitManager.instance.CreateBattlefieldEntity(Pose, battlefieldScale);
            NetworkManager.instance.SendBattlefieldLocation(battlefieldGameObject.transform.position, battlefieldGameObject.transform.rotation, battlefieldScale);
            leanTwistRotateAxis.enabled = false;
            leanPinchScale.enabled = false;
        }
    }

    public void ReceiveLocation(Vector3 position, Quaternion rotation, float scale)
    {
        //Receive location from server
        Debug.Log("Receive battlefield location");
        //SpawnBattlefield(position, rotation, scale);
        battlefieldGameObject.SetActive(true);

        AudioManager.instance.Play(SoundType.CardPlaced);
        TurnOffInteraction();
    }

    public void TurnOffInteraction()
    {
        syncBattleField = false;
        if(leanTwistRotateAxis != null)
        {
            leanTwistRotateAxis.enabled = false;
        }
        if(leanPinchScale != null)
        {
            leanPinchScale.enabled = false;
        }
    }

    public void SyncBattlefield(bool enable)
    {
        leanTwistRotateAxis.enabled = !enable;
        //leanPinchScale.enabled = !enable;
        syncBattleField = enable;

        AudioManager.instance.Play(SoundType.CardPlaced);
        battlefieldGameObject.SetActive(true);
    }

    public void ChangeBattlefieldSize(float scale)
    {
        battlefieldScale = scale;
        battlefieldGameObject.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void SetupCardSlots()
    {
        battlefieldSize = battlefieldGameObject.transform.GetChild(0).GetComponent<SpriteRenderer>().bounds.size / 2f - new Vector3(cardSettings.OffsetFromBattlefieldBorders.x, 0f, cardSettings.OffsetFromBattlefieldBorders.y);
        var cardSlotSizeX = (battlefieldSize.x * 2 - cardSettings.DistanceBetweenCards.x * (cardSettings.NumColumns - 1)) / cardSettings.NumColumns;
        var cardSlotSizeY = (battlefieldSize.z - cardSettings.DistanceBetweenCards.y * Mathf.Max(cardSettings.NumRows - 1, 1)) / cardSettings.NumRows;
        var finalScaleMultiplier = 0f;
        var finalScaleMultiplierX = cardSlotSizeX / cardSettings.CardSize.x;
        var finalScaleMultiplierY = cardSlotSizeY / cardSettings.CardSize.y;
        if (finalScaleMultiplierX < finalScaleMultiplierY)
        {
            finalScaleMultiplier = finalScaleMultiplierX;
        }
        else
        {
            finalScaleMultiplier = finalScaleMultiplierY;
        }
        // Check for division by zero
        //cardSettings.DistanceBetweenCards = new Vector2((battlefieldSize.x * 2 / Mathf.Max(cardSettings.NumColumns - 1, 1)), (battlefieldSize.z / Mathf.Max(cardSettings.NumRows - 1, 1)));
        //Vector2 reducedScale = new Vector2((battlefieldSize.x * 2f - 0.2f) / cardSettings.NumColumns * cardSettings.CardSize.x, (battlefieldSize.z - 0.2f) / cardSettings.NumRows * cardSettings.CardSize.y);
        Vector2 reducedScale = new Vector2(cardSettings.CardSize.x * finalScaleMultiplier, cardSettings.CardSize.y * finalScaleMultiplier);
        Vector2 cardslotHalfSize = reducedScale / 2f;
        Vector2 newDistanceBetweenCards = new Vector2((battlefieldSize.x * 2f - (cardSettings.NumColumns * reducedScale.x)) / (cardSettings.NumColumns - 1), (battlefieldSize.z - (cardSettings.NumRows * reducedScale.y)) / Mathf.Max(cardSettings.NumRows - 1, 1));
        //Spawn card slots in a grid on the battlefield according to the card settings rows and distance between cards
        for (int j = 0; j < cardSettings.NumRows; j++)
        {
            for (int i = 0; i < cardSettings.NumColumns; i++)
            {

                //Place card slots on either side of the battlefield and centered

                Vector3 position = new Vector3(cardslotHalfSize.x - battlefieldSize.x + i * (reducedScale.x + newDistanceBetweenCards.x), 0f, (-newDistanceBetweenCards.y + cardslotHalfSize.y) - battlefieldSize.z + j * (reducedScale.y + newDistanceBetweenCards.y));
                GenerateCardSlot(i, j, position, CardOwner.Player, reducedScale);

                position = new Vector3(cardslotHalfSize.x - battlefieldSize.x + i * (reducedScale.x + newDistanceBetweenCards.x), 0f, (newDistanceBetweenCards.y - cardslotHalfSize.y) + battlefieldSize.z - j * (reducedScale.y + newDistanceBetweenCards.y));
                GenerateCardSlot(i, j, position, CardOwner.Opponent, reducedScale);
            }
        }
        var battlefieldSizeForHealthObjects = battlefieldGameObject.transform.GetChild(0).GetComponent<SpriteRenderer>().bounds.size / 2f;
        healthAnimation.GenerateHealthObjects(CardOwner.Player, PlayerManager.instance.playerSettings.InitialPlayerHealth, new Vector3(-battlefieldSizeForHealthObjects.x - 1f, 0f, -battlefieldSizeForHealthObjects.z - 1f), battlefieldGameObject.transform);
        healthAnimation.GenerateHealthObjects(CardOwner.Opponent, PlayerManager.instance.playerSettings.InitialPlayerHealth, new Vector3(-battlefieldSizeForHealthObjects.x - 1f, 0f, battlefieldSizeForHealthObjects.z + 1f), battlefieldGameObject.transform);

    }

    private void GenerateCardSlot(int i, int j, Vector3 position, CardOwner cardOwner, Vector2 reducedScale)
    {
        GameObject cardSlot = Instantiate(cardSlotPrefab, battlefieldGameObject.transform);
        cardSlot.transform.localPosition = position;

        cardSlot.transform.localRotation = Quaternion.identity;
        cardSlot.transform.localScale = new Vector3(reducedScale.x, 1f, reducedScale.y);
        CardSlot cardSlotScript = cardSlot.GetComponent<CardSlot>();
        var id = 0;
        if (NetworkManager.instance.photonView && !NetworkManager.instance.photonView.IsMine)
        {
            id = j * cardSettings.NumColumns + cardSettings.NumColumns - i - 1;
        }
        else
        {
            id = j * cardSettings.NumColumns + i;
        }
        cardSlotScript.SetSlot(id, cardOwner);
        cardSlotIDOwnerHashMap.Add((id, cardOwner), cardSlotScript);
        cardSlots[cardOwner][i, j] = cardSlotScript;
        
        cardSlotsSiblingIDHashMap.Add(cardSlotScript.transform.GetSiblingIndex(), cardSlotScript);
    }

    public IEnumerator Attack(bool continueGame = false, CardOwner lastTurn = CardOwner.Opponent)
    {
        NetworkManager.instance.WhoseTurn = CardOwner.Opponent;
        trafficLight.StopPlayerTurnLights();
        yield return CountdownManager.Instance.AttackPhaseTextAnimation();
        yield return new WaitForSeconds(2f);
        int frontRow = cardSettings.NumRows - 1;
        GameEndState gameEndState = GameEndState.Ongoing;
        EffectManager.instance.NotifyEffects(Effect.GlobalEvent.AttackPhaseBegin);
        Debug.Log("Attack phase begin " + lastTurn);
        int currentPlayerHP = PlayerManager.instance.PlayerHealth;
        int currentOpponentHP = PlayerManager.instance.OpponentHealth;
        var battlefieldSize = battlefieldGameObject.transform.GetChild(0).GetComponent<SpriteRenderer>().bounds.size;
        var columnShape = columnAttackShapeSprite.bounds.size;
        var columnShapeScale = new Vector3(battlefieldSize.x / (columnShape.x * cardSettings.NumColumns), battlefieldSize.z / columnShape.y, 1f);
        int columnStart = cardSettings.NumColumns - 1;
        int columnEnd = -1;
        if (NetworkManager.instance.photonView != null)
        {
            columnStart = NetworkManager.instance.photonView.IsMine ? 0 : cardSettings.NumColumns - 1;
            columnEnd = NetworkManager.instance.photonView.IsMine ? cardSettings.NumColumns : -1;
        }

        for (int i = columnStart; i != columnEnd;)
        {
            Debug.Log("Column number = " + i);

            //Create dotween animation sequence for each side, add the attacking animation, hp drop animation and player hp decrease animation to the sequence

            // The order of the following statements is deliberate, a card may programmatically already be destroyed before it should attack

            CardSlot playerCardSlot = cardSlots[CardOwner.Player][i, frontRow];
            ObjectCard playerCard = playerCardSlot.HasCard() ? playerCardSlot.GetCard() : null;

            CardSlot opponentCardSlot = cardSlots[CardOwner.Opponent][i, frontRow];
            ObjectCard opponentCard = opponentCardSlot.HasCard() ? opponentCardSlot.GetCard() : null;

            //if (columnAttackParticle)
            //{
            //    Destroy(columnAttackParticle);
            //}
            //columnAttackParticle = Instantiate(columnAttackParticlePrefab, battlefieldGameObject.transform);
            //columnAttackParticle.transform.localPosition = Vector3.zero;
            //columnAttackParticle.transform.position += Vector3.right * playerCardSlot.gameObject.transform.position.x;
            //columnAttackParticle.transform.localScale = columnShapeScale;

            yield return new WaitForSeconds(0.5f);

            bool playerCardDeath = false;
            bool opponentCardDeath = false;
            bool playerHasCard = playerCardSlot.HasCard();
            bool opponentHasCard = opponentCardSlot.HasCard();

            if (playerHasCard && playerCard.canAttack)
            {
                if (PerformAttack(playerCard, i, CardOwner.Opponent))
                {
                    // Attack performed on card
                    //playerSequence.Append();
                }
            }

            if (opponentHasCard && opponentCard.canAttack)
            {
                if (PerformAttack(opponentCard, i, CardOwner.Player))
                {
                    // Attack performed on card
                    //opponentSequence.Append();
                }
            }

            if (playerHasCard && !playerCard.canAttack)
            {
                playerCard.NotifyEffects(Effect.LocalEvent.UnableToAttack, opponentCard);
            }
            if (opponentHasCard && !opponentCard.canAttack)
            {
                opponentCard.NotifyEffects(Effect.LocalEvent.UnableToAttack, playerCard);
            }

            if (playerHasCard || opponentHasCard)
            {
                yield return new WaitForSeconds(2f);
                Debug.Log("Player damage = " + PlayerManager.instance.PlayerCurrentDamage);
                Debug.Log("Opponent damage = " + PlayerManager.instance.OpponentCurrentDamage);
                healthAnimation.UpdateDamageTexts(CardOwner.Player, PlayerManager.instance.PlayerCurrentDamage);
                healthAnimation.UpdateDamageTexts(CardOwner.Opponent, PlayerManager.instance.OpponentCurrentDamage);
            }
            else
            {
                if (NetworkManager.instance.photonView.IsMine)
                {
                    i++;
                }
                else
                {
                    i--;
                }
                continue;
            }

            //Destroy cards that have 0 hp
            if (playerCard) { playerCardDeath = playerCard.DestroyIfZeroHP(); }
            if (opponentCard) { opponentCardDeath = opponentCard.DestroyIfZeroHP(); }

            if (playerCardDeath)
            {
                playerCardsKillCount++;
                playerCardSlot.RemoveCard();
            }

            if (opponentCardDeath)
            {
                opponentCardsKillCount++;
                opponentCardSlot.RemoveCard();
            }


            if (NetworkManager.instance.photonView && NetworkManager.instance.photonView.IsMine)
            {
                i++;
            }
            else
            {
                i--;
            }
        }

        EffectManager.instance.NotifyEffects(Effect.GlobalEvent.AttackPhaseEnd);

        ForEachCardSlot(slot =>
        {
            if (slot.HasCard())
            {
                slot.GetCard().canAttack = true;
            }
        });

        // Get overall damage from both players and adjust hp accordingly
        var totalDamage = PlayerManager.instance.PlayerCurrentDamage - PlayerManager.instance.OpponentCurrentDamage;
        Debug.Log("Total damage = " + totalDamage);
        // Health negative check
        if (currentPlayerHP - totalDamage < 0)
        {
            totalDamage = currentPlayerHP;
        }
        else if (currentOpponentHP + totalDamage < 0)
        {
            totalDamage = -currentOpponentHP;
        }

        PlayerManager.instance.PlayerHealth -= totalDamage;
        PlayerManager.instance.OpponentHealth += totalDamage;
        if (totalDamage > 0)
        {
            healthAnimation.TransferHealthObject(CardOwner.Player, totalDamage);
            yield return new WaitForSeconds(0.5f * totalDamage);
        }
        else if (totalDamage < 0)
        {
            healthAnimation.TransferHealthObject(CardOwner.Opponent, -totalDamage);
            yield return new WaitForSeconds(0.5f * -totalDamage);
        }
        healthAnimation.ResetDamageTexts();

        //Game end check
        gameEndState = HasGameEnded();
        if (gameEndState != GameEndState.Ongoing)
        {
            PlayerManager.instance.GameHasEnded(gameEndState);
            yield break;
        }
        switch (gameEndState)
        {
            case GameEndState.PlayerWon:
                // Add game end animation to the sequence, bring up game end screen
                break;
            case GameEndState.OpponentWon:
                // Add game end animation to the sequence, bring up game end screen
                break;
            case GameEndState.Draw:
                // Add game end animation to the sequence, bring up game end screen
                break;
            default:
                break;
        }
        //if (columnAttackParticle)
        //{
        //    Destroy(columnAttackParticle);
        //}
        NetworkManager.instance.attackingCoroutine = null;
        if (gameEndState == GameEndState.Ongoing && continueGame)
        {
            NetworkManager.instance.SetNextTurnFromServer(lastTurn);
        }
    }


    private GameEndState HasGameEnded()
    {

        //Check if player and opponent both have lost all cards
        if (playerCardsKillCount == cardSettings.NumCardsPerPlayer && opponentCardsKillCount == playerCardsKillCount)
        {
            //End game according to whoever has more health
            if (PlayerManager.instance.PlayerHealth > PlayerManager.instance.OpponentHealth)
            {
                return GameEndState.PlayerWon;
            }
            else if (PlayerManager.instance.PlayerHealth < PlayerManager.instance.OpponentHealth)
            {
                return GameEndState.OpponentWon;
            }
            else
            {
                return GameEndState.Draw;
            }
        }

        // Check if player or opponent has lost all cards or have no health left

        if (PlayerManager.instance.PlayerHealth <= 0 || playerCardsKillCount == cardSettings.NumCardsPerPlayer)
        {
            return GameEndState.OpponentWon;
        }

        if (PlayerManager.instance.OpponentHealth <= 0 || opponentCardsKillCount == cardSettings.NumCardsPerPlayer)
        {
            return GameEndState.PlayerWon;
        }

        return GameEndState.Ongoing;
    }

    private bool PerformAttack(ObjectCard attacker, int col, CardOwner side)
    {
        var damageDone = 0;
        bool attackPerformedOnCard = false;
        int damage = attacker.GetFinalAttack();
        for (int currentRow = cardSettings.NumRows - 1; currentRow >= 0; currentRow--)
        {
            CardSlot attackedCardSlot = cardSlots[side][col, currentRow];
            attacker.NotifyEffects(Effect.LocalEvent.Attacking, attackedCardSlot.GetCard());
            if (attackedCardSlot.HasCard())
            {
                AudioManager.instance.Play(SoundType.CardToCardAttack);
                attackPerformedOnCard = true;
                ObjectCard attackedCard = attackedCardSlot.GetCard();
                attackedCard.NotifyEffects(Effect.LocalEvent.Attacked, attacker);
                damageDone = attackedCardSlot.GetCard().TakeDamage(damage);
                if (damageDone == -1)
                {
                    return true;
                }
            }
            else
            {
                AudioManager.instance.Play(SoundType.CardToPlayerAttack);
                var particle = Instantiate(directAttackParticlePrefab, battlefieldGameObject.transform);
                particle.transform.position = attacker.transform.position;
                particle.transform.forward = Vector3.Normalize(attackedCardSlot.transform.position - attacker.transform.position);
                break;
            }

        }
        if (!attackPerformedOnCard)
        {
            // damage player/opponent with card damage
            if (side == CardOwner.Player)
            {
                PlayerManager.instance.PlayerCurrentDamage += damage;
            }
            else
            {
                PlayerManager.instance.OpponentCurrentDamage += damage;
            }
        }
        else if (damageDone > 0 && attackPerformedOnCard)
        {
            // damage player/opponent with excess damage
            if (side == CardOwner.Player)
            {
                PlayerManager.instance.PlayerCurrentDamage += damageDone;
            }
            else
            {
                PlayerManager.instance.OpponentCurrentDamage += damageDone;
            }
            return true;
        }
        return attackPerformedOnCard;
    }

    public CardSlot GetCardSlotFromSiblingIndex(int siblingIndex)
    {
        return cardSlotsSiblingIDHashMap[siblingIndex];
    }

    public CardSlot GetCardSlotFromIDAndOwnerHashMap(int id, CardOwner cardOwner)
    {
        return cardSlotIDOwnerHashMap[(id, cardOwner)];
    }

    public CardSlot GetCardSlotFromID(int id, CardOwner cardOwner)
    {
        int row = id / cardSettings.NumColumns;
        int col = id % cardSettings.NumColumns;
        if (row >= cardSettings.NumRows || row < 0)
        {
            return null;
        }
        if (col >= cardSettings.NumColumns || col < 0)
        {
            return null;
        }
        return cardSlots[cardOwner][col, row];
    }

    public int FindCardSlotFromCard(Card toFind)
    {
        for (int i = 0; i < cardSettings.NumCardsOnDeck; i++)
        {
            if (cardSlotIDOwnerHashMap[(i, toFind.cardData.owner)].GetCard() == toFind)
            {
                return i;
            }
        }
        return -1;
    }

    public void ForEachCardSlot(Action<CardSlot> action)
    {
        for (int i = 0; i < cardSettings.NumCardsOnDeck; i++)
        {
            action.Invoke(cardSlotIDOwnerHashMap[(i, CardOwner.Player)]);
            action.Invoke(cardSlotIDOwnerHashMap[(i, CardOwner.Opponent)]);
        }
    }

    public ObjectCard GetOppositeCard(Card card)
    {
        int slotId = FindCardSlotFromCard(card);
        CardSlot targetSlot = GetCardSlotFromIDAndOwnerHashMap(slotId, card.cardData.owner == CardOwner.Player ? CardOwner.Opponent : CardOwner.Player);
        return targetSlot.GetCard();
    }

    private void Update()
    {
        if (syncBattleField)
        {
            ConjureKitManager.instance.UpdateEntity(3);
        }
    }
}
