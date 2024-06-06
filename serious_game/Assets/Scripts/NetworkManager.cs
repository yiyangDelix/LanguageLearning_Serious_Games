using ExitGames.Client.Photon;
using Newtonsoft.Json;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    #region fields and properties
    public static NetworkManager instance;
    [SerializeField] private bool showConnectionStatus = true;
    [SerializeField] private GameObject startMenuUI;
    [SerializeField] private GameObject connectionMenuUI;
    [SerializeField] private Button[] startButtons;
    [SerializeField] private CardOwner whoseTurn = CardOwner.Player;
    [SerializeField] private GameObject errorMessageObject;
    [SerializeField] private TMPro.TextMeshProUGUI errorMessageText;
    [SerializeField] private TutorialManager tutorialManager;

    //Global call on room created on host player
    public event System.Action onReadyToStartBattlefieldPlacement = delegate { };
    public event System.Action onLocalPlayerEnteredRoom = delegate { };
    //public PhotonView photonView;
    public RandomNumbersSerializable currentRandomNumbers = new RandomNumbersSerializable();
    public Coroutine attackingCoroutine = null;

    private string playerName = null;
    private bool[] playersReadyForPhase2 = new bool[] { false, false };
    private bool[] playersTutorialOver = new bool[] { false, false };
    private CancellationTokenSource turnCancellationTokenSource;
    [SerializeField] private bool[] playersPlayedTurns = new bool[] { false, false };
    private string roomName = null;
    public bool useConjureKit = false;
    public ByteArraySlice byteArraySlice = new ByteArraySlice(new byte[1024 * 5]);
    private bool playerJoinedLobby = false;
    [SerializeField] private CardOwner lastTurn = CardOwner.Player;
    TypedLobby lobby = new TypedLobby(null, LobbyType.Default);
    private bool playerWantsToQuit = false;
    private bool playerWantsToRestart = false;
    private bool isPlayerHost = false;
    private bool isPlayerReconnecting = false;
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    private int swapTargetID = -1;
    private CardOwner swapTargetOwner;
    private TaskCompletionSource<(int, CardOwner)> swapTargetCompletionSource = new();
    private readonly object swapTargetLock = new();
    private int currentTurnPlayer=0;

    public CardOwner WhoseTurn
    {
        get
        {
            return whoseTurn;
        }
        set
        {
            whoseTurn = value;
        }
    }

    #endregion

    //--------------------------------------------------------------------------------------------------------------

    #region UNITY Methods

    public void SetInitialCurrentTurn(int id)
    {
        currentTurnPlayer = id;
    }

    public void SetInitialLastTurn(CardOwner owner)
    {
        lastTurn = owner;
    }
    public void SetNextTurnFromServer(CardOwner owner)
    {
        Debug.Log("Setting next turn from server = " + owner.ToString());
        WhoseTurn = owner;
        lastTurn = whoseTurn;
        if (WhoseTurn == CardOwner.Player)
        {
            StartTurn();
        }
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

    public void SetPlayerName(string name)
    {
        playerName = name;
    }

    public void SetRoomName(string name)
    {
        bool roomExists = false;
        bool roomFull = false;
        foreach (var room in cachedRoomList.Values)
        {
            if (room.Name == name)
            {
                roomExists = true;
                if (room.PlayerCount == room.MaxPlayers)
                {
                    roomFull = true;
                }
                break;
            }
        }
        if (roomExists && roomFull)
        {
            errorMessageObject.SetActive(true);
            errorMessageText.text = "Room is full!";
            return;
        }
        errorMessageObject?.SetActive(false);
        roomName = name;
    }

    public void SetUseConjureKit(bool enabled)
    {
        useConjureKit = enabled;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private async void Start()
    {
        PhotonNetwork.NetworkingClient.LoadBalancingPeer.CrcEnabled = true;
        PhotonNetwork.NetworkingClient.LoadBalancingPeer.MaximumTransferUnit = 520;
        PhotonNetwork.NetworkingClient.LoadBalancingPeer.QuickResendAttempts = 3;
        PhotonNetwork.NetworkingClient.LoadBalancingPeer.SentCountAllowance = 7;

        ToggleStartButtons(false);
        bool connected = false;
        while (!connected)
        {
            connected = PhotonNetwork.ConnectUsingSettings();
            await Task.Yield();
        }
        AudioManager.instance?.StartBackgroundMusicPhase1();
    }

    private void ToggleStartButtons(bool enable)
    {
        if (startButtons.Length > 1)
        {
            if (startButtons[0])
            {
                startButtons[0].gameObject.SetActive(enable);
                startButtons[0].interactable = enable;
            }
            if (startButtons[1])
            {
                startButtons[1].gameObject.SetActive(enable);
                startButtons[1].interactable = enable;
            }
        }
    }

    public void ConnectAsHost()
    {
        if (playerName.IsNullOrEmpty() || roomName.IsNullOrEmpty())
        {
            return;
        }
        AudioManager.instance.PlayButtonClickSounds();
        isPlayerHost = true;
        PhotonNetwork.LocalPlayer.NickName = playerName;
        bool roomExists = false;
        foreach (var room in cachedRoomList.Values)
        {
            if (room.Name == roomName)
            {
                roomExists = true;
                break;
            }
        }
        if (roomExists)
        {
            errorMessageObject.SetActive(true);
            errorMessageText.text = "Room already exists!";
            return;
        }
        errorMessageObject?.SetActive(false);
        StartCoroutine(ConnectAsHostCoroutine());


    }

    private IEnumerator ConnectAsHostCoroutine()
    {
        // Create a room with max players 2
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.PlayerTtl = 60000;
        bool roomCreated = false;
        ToggleStartButtons(false);
        while (!roomCreated)
        {
            roomCreated = PhotonNetwork.CreateRoom(roomName, roomOptions, lobby);
            yield return null;
        }
        Debug.Log("Room Created = " + roomCreated.ToString());
        onReadyToStartBattlefieldPlacement();
    }

    private void ReadyToPlaceBattlefield()
    {
        AlwaysActiveUIManager.Instance.gameObject.SetActive(true);
        AlwaysActiveUIManager.Instance.ShowInfo(5);
        PhotonNetwork.LocalPlayer.NickName = playerName;
        startMenuUI.SetActive(false);
        connectionMenuUI.SetActive(true);
        if (useConjureKit)
        {
            ConjureKitManager.instance.gameObject.SetActive(true);

            PlaneDetectionController.onPlacedBattlefield += BattlefieldManager.instance.SendLocation;
        }
        else
        {
            BattlefieldManager.instance.SpawnBattlefield(Vector3.forward * 5f, Quaternion.identity, 1f);
            BattlefieldManager.instance.ChangeBattlefieldSize(0.1f);
            PlaneDetectionController.onPlacedBattlefield += () =>
            {
                BattlefieldManager.instance.TurnOffInteraction();
                CardSelectionManager.instance.EnableCardSelectionUI();
            };
            // allow both players to place battlefield
            onReadyToStartBattlefieldPlacement();
        }

    }

    public void ConnectAsClient()
    {
        if (playerName.IsNullOrEmpty() || roomName.IsNullOrEmpty())
        {
            return;
        }
        AudioManager.instance.PlayButtonClickSounds();
        bool roomExists = false;
        foreach (var room in cachedRoomList.Values)
        {
            if (room.Name == roomName)
            {
                roomExists = true;
                break;
            }
        }
        if (!roomExists)
        {
            errorMessageObject.SetActive(true);
            errorMessageText.text = "Room does not exist!";
            return;
        }
        errorMessageObject?.SetActive(false);
        PhotonNetwork.LocalPlayer.NickName = playerName;
        StartCoroutine(ConnectAsClientCoroutine());

    }

    private IEnumerator ConnectAsClientCoroutine()
    {
        ToggleStartButtons(false);
        bool roomJoined = false;
        while (!roomJoined)
        {
            roomJoined = PhotonNetwork.JoinRoom(roomName);
            yield return null;
        }

    }


    // Update is called once per frame
    void Update()
    {
        if (showConnectionStatus)
        {
            Debug.Log("Connection Status: " + PhotonNetwork.NetworkClientState);

        }

    }

    public async void StartTurn()
    {
        turnCancellationTokenSource = new CancellationTokenSource();
        //Let player play
        PlayerManager.instance.ResetDataForEachTurn();
        BattlefieldManager.instance.PlayerStartedTurn(PlayerManager.instance.playerSettings.TimeToPlay);
        PlayerManager.instance.PlayTurn(PlayerManager.instance.playerSettings.TimeToPlay);
        //Start timer
        try
        {
            await Task.Delay(PlayerManager.instance.playerSettings.TimeToPlay * 1000, turnCancellationTokenSource.Token);
        }
        catch
        {
            Debug.Log("Turn was cancelled!");
            return;
        }
        finally
        {
            turnCancellationTokenSource.Dispose();
            turnCancellationTokenSource = null;
        }
        Debug.Log("Turn is over");
        PlayerManager.instance.EndTurn();
        PhotonNetwork.SendAllOutgoingCommands();
        if (photonView.IsMine)
        {
            PhotonNetwork.CleanRpcBufferIfMine(photonView);
        }
        photonView.RPC(nameof(RPC_NextTurn), RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        PhotonNetwork.SendAllOutgoingCommands();
    }

    private void CancelTurn()
    {
        turnCancellationTokenSource?.Cancel();
        BattlefieldManager.instance.PlayerCancelledTurn();
        PlayerManager.instance.EndTurn();
        PhotonNetwork.SendAllOutgoingCommands();
        if (photonView.IsMine)
        {
            PhotonNetwork.CleanRpcBufferIfMine(photonView);
        }
        photonView.RPC(nameof(RPC_NextTurn), RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        PhotonNetwork.SendAllOutgoingCommands();
    }

    private void SetSwapTargetID(int swapTargetID, CardOwner swapTargetOwner)
    {
        lock (swapTargetLock)
        {
            this.swapTargetID = swapTargetID;
            swapTargetCompletionSource.TrySetResult((swapTargetID, swapTargetOwner));
        }
    }

    public Task<(int, CardOwner)> WaitForSwapTarget()
    {
        lock (swapTargetLock)
        {
            if (swapTargetID != -1) // Already received target
            {
                int value = swapTargetID;
                swapTargetID = -1;
                return Task.FromResult((value, swapTargetOwner));
            }
            else
            {
                if (swapTargetCompletionSource.Task.IsCompleted)
                {
                    swapTargetCompletionSource = new TaskCompletionSource<(int, CardOwner)>();
                }
                return swapTargetCompletionSource.Task;
            }
        }
    }

    private void StopBattlefieldSyncAndStartPhase2(Vector3 position, Quaternion rotation, float scale)
    {
        BattlefieldManager.instance.ReceiveLocation(position, rotation, scale);

        photonView.RPC("RPC_StartCardSelection", RpcTarget.AllBuffered);
    }

    #endregion

    //--------------------------------------------------------------------------------------------------------------



    #region PHOTON Callback Methods

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (!isPlayerReconnecting)
        {
            ToggleStartButtons(true);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        cachedRoomList.Clear();
        if (cause != DisconnectCause.DisconnectByClientLogic && cause != DisconnectCause.DisconnectByServerLogic && cause != DisconnectCause.ApplicationQuit)
        {
            StartCoroutine(ReconnectCoroutine());
        }
    }

    private IEnumerator ReconnectCoroutine()
    {
        isPlayerReconnecting = true;
        bool roomJoined = false;
        while (!roomJoined)
        {
            roomJoined = PhotonNetwork.ReconnectAndRejoin();
            yield return null;
        }

    }
    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo info = roomList[i];
            if (info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
    }
    public override void OnLeftLobby()
    {
        cachedRoomList.Clear();
    }

    public override void OnConnected()
    {
        Debug.Log("We connected to Internet");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " is connected to Master Server");
        StartCoroutine(JoinLobbyCoroutine());
    }

    private IEnumerator JoinLobbyCoroutine()
    {
        bool lobbyJoined = false;

        while (!lobbyJoined)
        {
            lobbyJoined = PhotonNetwork.JoinLobby(lobby);
            yield return null;
        }

    }

    public override void OnJoinedLobby()
    {
        if (!isPlayerReconnecting)
        {
            if (errorMessageObject != null)
            {
                errorMessageObject.SetActive(false);
            }
            if (startMenuUI != null)
            {
                startMenuUI?.SetActive(true);
            }
            ToggleStartButtons(true);
        }

    }
    public override void OnJoinedRoom()
    {
        if (isPlayerReconnecting)
        {
            isPlayerReconnecting = false;
            if (isPlayerHost)
            {
                PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
            }
            return;
        }
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " joined to " + PhotonNetwork.CurrentRoom.Name);
        ReadyToPlaceBattlefield();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //continuous data stream
    }

    public void SendBattlefieldLocation(Vector3 position, Quaternion rotation, float scale)
    {
        photonView.RPC("RPC_BattlefieldLocation", RpcTarget.OthersBuffered, position, rotation, scale);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        //PlayerManager.instance.GoToMainMenu();
    }

    public static byte[] ObjectToByteArray(object obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }
    public void SendCardToOtherPlayer(ObjectCardData cardData)
    {
        byte[] bytes = ObjectToByteArray(cardData);
        photonView.RPC("RPC_ReceiveObjectCardFromOtherPlayer", RpcTarget.OthersBuffered, bytes);
    }

    public void SendObjectCardIDToPlaceOnBoard(int cardID, int slotID, bool validCard)
    {

        photonView.RPC("RPC_ReceiveObjectCardIDToPlaceOnBoard", RpcTarget.OthersBuffered, cardID, slotID, validCard);
    }

    public void SendSwapCardToOtherPlayer(int source, CardOwner sourceOwner, int target, CardOwner targetOwner)
    {
        photonView.RPC(nameof(RPC_GetCardSlotIDsToSwap), RpcTarget.OthersBuffered, source, (int)sourceOwner, target, (int)targetOwner);
    }
    public void SendGrammarCardToPlaceOnBoard(GrammarCardData grammarCardData, int slotID, bool validCard)
    {
        string json = JsonUtility.ToJson(grammarCardData);
        photonView.RPC("RPC_ReceiveGrammarCardToPlaceOnBoard", RpcTarget.OthersBuffered, json, slotID, validCard);
    }

    public void SendObjectCardGrammarAnswersToOtherPlayer(Dictionary<Grammars, string> grammarAnswers, int cardID)
    {
        //Send grammar answers to other player
        //Send grammar buffs to other player

        string json = JsonConvert.SerializeObject(grammarAnswers);
        photonView.RPC(nameof(RPC_ReceiveGrammarAnswersFromOtherPlayer), RpcTarget.OthersBuffered, json, cardID);
    }


    public void SendLocalPlayerReadyForPhase2()
    {
        //send message to server that local player is ready
        //switch the flag locally if local player is the host

        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            for (int i = 0; i < 10; i++)
            {
                currentRandomNumbers.randomNumbers.Add(UnityEngine.Random.value);
            }

            // convert array to json
            var randomNumbersJson = JsonUtility.ToJson(currentRandomNumbers);
            photonView.RPC(nameof(RPC_SetFirstRandomNumbers), RpcTarget.AllBuffered, randomNumbersJson);
            photonView.RPC("RPC_PlayerIsReadyForPhase2", RpcTarget.AllBuffered, 0);
        }
        else
        {
            photonView.RPC("RPC_PlayerIsReadyForPhase2", RpcTarget.AllBuffered, 1);
        }
    }

    public void SendAttackPhaseOver()
    {

    }

    public void SendLocalPlayerCancelTurn()
    {
        if (WhoseTurn == CardOwner.Player)
        {
            //photonView.RPC(nameof(RPC_CancelTurn), RpcTarget.Others);
            CancelTurn();
        }
    }

    public void SendLocalPlayerTutorialOver()
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_PlayerTutorialOver), RpcTarget.AllBuffered, 0);
        }
        else
        {
            photonView.RPC(nameof(RPC_PlayerTutorialOver), RpcTarget.AllBuffered, 1);
        }

    }

    public void SendSwapTarget(int swapTargetID, CardOwner swapTargetOwner)
    {
        // Need to switch side to preserve meaning
        swapTargetOwner = swapTargetOwner == CardOwner.Player ? CardOwner.Opponent : CardOwner.Player;
        photonView.RPC("RPC_ReceiveSwapTarget", RpcTarget.OthersBuffered, swapTargetID, swapTargetOwner);
    }

    // Convert a byte array to an Object
    public static object ByteArrayToObject(byte[] arrBytes)
    {
        using (var memStream = new MemoryStream())
        {
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream);
            return obj;
        }
    }


    [PunRPC]
    void RPC_ReceiveObjectCardFromOtherPlayer(byte[] cardDataJson)
    {
        ObjectCardData cardData = (ObjectCardData)ByteArrayToObject(cardDataJson);
        PlayerManager.instance.AddOpponentObjectCard(cardData);
    }

    [PunRPC]
    void RPC_SetFirstRandomNumbers(string randomNumbersJson)
    {
        currentRandomNumbers = JsonUtility.FromJson<RandomNumbersSerializable>(randomNumbersJson);
    }

    [PunRPC]
    void RPC_ReceiveObjectCardIDToPlaceOnBoard(int cardID, int slotID, bool validCard)
    {
        PlayerManager.instance.GotOpponentObjectCardID(cardID, slotID, validCard);
    }

    [PunRPC]
    void RPC_ReceiveGrammarCardToPlaceOnBoard(string grammarCardDataJson, int slotID, bool validCard)
    {
        var grammarCardData = JsonUtility.FromJson<GrammarCardData>(grammarCardDataJson);
        Debug.Log("Received grammar card with effect " + grammarCardData.effect.ToString() + " to place at slot " + slotID);
        PlayerManager.instance.GotOpponentGrammarCardData(grammarCardData, slotID, validCard);
    }


    [PunRPC]
    void RPC_ReceiveGrammarAnswersFromOtherPlayer(string grammarAnswersJson, int cardID)
    {
        var grammarAnswers = JsonConvert.DeserializeObject<Dictionary<Grammars, string>>(grammarAnswersJson);
        PlayerManager.instance.AddOpponentGrammarAnswers(grammarAnswers, cardID);
    }

    [PunRPC]
    float RPC_GetRandomNumber01()
    {
        return UnityEngine.Random.Range(0f, 1f);
    }


    [PunRPC]
    void RPC_GetCardSlotIDsToSwap(int sourceCardSlotID, int sourceCardSlotOwner, int targetCardSlotID, int targetCardSlotOwner)
    {
        // swap ownership here
        CardOwner sourceOwner = (CardOwner)sourceCardSlotOwner == CardOwner.Player ? CardOwner.Opponent : CardOwner.Player;
        CardOwner targetOwner = (CardOwner)targetCardSlotOwner == CardOwner.Opponent ? CardOwner.Player : CardOwner.Opponent;

        AudioManager.instance.Play(SoundType.CardPlaced);
        PlayerManager.instance.SwapCards(sourceCardSlotID, sourceOwner, targetCardSlotID, targetOwner);
    }


    [PunRPC]
    void RPC_NextTurn(int playerID)
    {
        if (!photonView.IsMine)
        {
            return;
        }

        PhotonNetwork.SendAllOutgoingCommands();
        PhotonNetwork.CleanRpcBufferIfMine(photonView);
        playersPlayedTurns[playerID - 1] = true;
        for (int i = 0; i < 10; i++)
        {
            currentRandomNumbers.randomNumbers.Add(UnityEngine.Random.value);
        }

        // convert array to json
        var randomNumbersJson = JsonUtility.ToJson(currentRandomNumbers);

        if (playersPlayedTurns[0] && playersPlayedTurns[1])
        {
            playersPlayedTurns[0] = false;
            playersPlayedTurns[1] = false;
            var nextTurnPlayer = (currentTurnPlayer == 1) ? 2 : 1;
            photonView.RPC("RPC_AttackAndNextTurn", RpcTarget.AllBuffered, randomNumbersJson, nextTurnPlayer);
        }
        else
        {
            var nextTurnPlayer = (currentTurnPlayer == 1) ? 2 : 1;
            photonView.RPC(nameof(RPC_SetNextTurn), RpcTarget.AllBuffered, randomNumbersJson, nextTurnPlayer);
        }
    }
    [PunRPC]
    void RPC_AttackAndNextTurn(string randomNumbersJson, int nextTurnPlayer)
    {
        if(attackingCoroutine != null)
        {
            return;
        }
        currentRandomNumbers = JsonUtility.FromJson<RandomNumbersSerializable>(randomNumbersJson);
        // Wrap this in a task because Attacking will take time

        currentTurnPlayer = nextTurnPlayer;
        Debug.Log("Attacking and next turn "+ nextTurnPlayer);
        CardOwner nextTurn = currentTurnPlayer == PhotonNetwork.LocalPlayer.ActorNumber ? CardOwner.Player : CardOwner.Opponent;
        //Start attack phase
        attackingCoroutine = StartCoroutine(BattlefieldManager.instance.Attack(true, nextTurn));
    }

    [PunRPC]
    void RPC_SetNextTurn(string randomNumbersJson, int nextTurnPlayer)
    {
        CardOwner nextTurn = nextTurnPlayer == PhotonNetwork.LocalPlayer.ActorNumber ? CardOwner.Player : CardOwner.Opponent;
        Debug.Log("Got next turn from server = " + nextTurn.ToString());
        SetNextTurnFromServer(nextTurn);
    }

    [PunRPC]
    void RPC_CancelTurn()
    {
        //Cancel turn from other player
    }

    [PunRPC]
    void RPC_BattlefieldLocation(Vector3 position, Quaternion rotation, float scale)
    {
        StopBattlefieldSyncAndStartPhase2(position, rotation, scale);
    }


    [PunRPC]
    void RPC_StartCardSelection()
    {
        //Start card selection
        Debug.Log("Start card selection");
        CardSelectionManager.instance.EnableCardSelectionUI();
    }

    [PunRPC]
    void RPC_PlayerIsReadyForPhase2(int player)
    {
        playersReadyForPhase2[player] = true;
        CheckForPhase2();
    }

    [PunRPC]
    void RPC_PlayerTutorialOver(int player)
    {
        playersTutorialOver[player] = true;

        //Update tutorial manager text if other player is ready
        if ((player == 0 && !PhotonNetwork.LocalPlayer.IsMasterClient) || (player == 1 && PhotonNetwork.LocalPlayer.IsMasterClient))
        {
            tutorialManager.OtherPlayerReady();
        }


        //Check if both players are ready
        CheckForTutorialOver();
    }

    [PunRPC]
    void RPC_ReceiveSwapTarget(int swapTargetID, CardOwner swapTargetOwner)
    {
        SetSwapTargetID(swapTargetID, swapTargetOwner);
    }

    #endregion

    private void CheckForPhase2()
    {
        foreach (bool ready in playersReadyForPhase2)
        {
            if (!ready)
            {
                return;
            }
        }
        AlwaysActiveUIManager.Instance.CancelShowInfo();
        AlwaysActiveUIManager.Instance.InfoPanelActivate(false);
        PlayerManager.instance.StartPhase2();
        tutorialManager.StartTutorial();
    }

    private void CheckForTutorialOver()
    {
        foreach (bool ready in playersTutorialOver)
        {
            if (!ready)
            {
                return;
            }
        }
        tutorialManager.EndTutorial();
        StartCoroutine(PlayerManager.instance.StartGame());
    }

}

[Serializable]
public class RandomNumbersSerializable
{
    public List<float> randomNumbers = new List<float>();
}
