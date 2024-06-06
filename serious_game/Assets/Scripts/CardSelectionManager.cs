using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using WebSocketSharp;

public static class NativeArrayExtension
{
    public static byte[] ToRawBytes<T>(this NativeArray<T> arr) where T : struct
    {
        var slice = new NativeSlice<T>(arr).SliceConvert<byte>();
        var bytes = new byte[slice.Length];
        slice.CopyTo(bytes);
        return bytes;
    }

    public static void CopyFromRawBytes<T>(this NativeArray<T> arr, byte[] bytes) where T : struct
    {
        var byteArr = new NativeArray<byte>(bytes, Allocator.Temp);
        var slice = new NativeSlice<byte>(byteArr).SliceConvert<T>();

        UnityEngine.Debug.Assert(arr.Length == slice.Length);
        slice.CopyTo(arr);
    }
}
public class CardSelectionManager : MonoBehaviour
{
    public static CardSelectionManager instance;

    public GameObject phase1UI;
    public GameObject cardScreen;
    public GameObject waitingScreen;
    public CardDisplay cardDisplay;
    public GameObject cardPrefab;
    public GameObject cardContainer;
    public Image loadingImage;
    public TextMeshProUGUI cardAmount;
    public CardSettings cardSettings;
    public ARCameraManager cameraManager;
    public Image testimage;

    [SerializeField] private Button takePictureButton;
    [SerializeField] private GameObject alwaysEnabledUI;
    [SerializeField] private GameObject takePictureUI;
    [SerializeField] private GameObject cardSelectionButtonsHolder;
    [SerializeField] private string objectCardImageSamplePath;

    private Card currentCard;
    private int MAX_RETRIES = 3;
    private bool rotateLoadingImage = false;
    private GameObject currentCardGameObject;
    private CancellationTokenSource cancellationTokenSource;

    private void Awake()
    {
        instance = this;
    }

    public void EnableCardSelectionUI()
    {
        // Enable card selection tutorial and guide the player through the process
        //LeanTouch.Instance.enabled = false;
        Texture.allowThreadedTextureCreation = true;
        Destroy(ConjureKitManager.instance.gameObject);
        BattlefieldManager.instance.battlefieldGameObject.AddComponent<ARAnchor>();
        AlwaysActiveUIManager.Instance.SetInfoPanelText("Aim at an object and press the white button to take a picture. The picture taken will then be converted into a card.");
        AlwaysActiveUIManager.Instance.CancelShowInfo();
        AlwaysActiveUIManager.Instance.onInfoScreenOpened += SetButtonInteraction;
        alwaysEnabledUI.gameObject.SetActive(true);
        cardAmount.text = "0/" + cardSettings.NumCardsPerPlayer;
        takePictureUI.SetActive(true);
        takePictureButton.interactable = false;
        phase1UI.SetActive(false);
        AlwaysActiveUIManager.Instance.ShowInfo(5);
    }

    private void SetButtonInteraction(bool disabled)
    {
        takePictureButton.interactable = !disabled;
    }

    public void GetImageAsync()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var filePath = Application.streamingAssetsPath + objectCardImageSamplePath + UnityEngine.Random.Range(1, 10) + ".jpg";
        var loadingRequest = UnityWebRequest.Get(filePath);
        loadingRequest.SendWebRequest();
        while (!loadingRequest.isDone)
        {
            if (loadingRequest.isNetworkError || loadingRequest.isHttpError)
            {
                break;
            }
        }
        if (loadingRequest.isNetworkError || loadingRequest.isHttpError)
        {

        }
        else
        {
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "your.bytes"), loadingRequest.downloadHandler.data);
        }
        var imageArray = loadingRequest.downloadHandler.data;
        NativeArray<byte> data = new NativeArray<byte>(imageArray.Length, Allocator.Temp);
        data.CopyFrom(imageArray);
        cancellationTokenSource = new CancellationTokenSource();
        TryAskingGPT(data, new Vector2Int(1024, 414), TextureFormat.RGB24);
#else
        // Get information about the device camera image.
        if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            // If successful, launch a coroutine that waits for the image
            // to be ready, then apply it to a texture.
            ProcessImage(image);

            // It's safe to dispose the image before the async operation completes.
            image.Dispose();
        }
        else
        {

            Debug.Log("Image failed");
        }
#endif
    }
    Texture2D m_Texture;
    private async void ProcessImage(XRCpuImage image)
    {
        Debug.Log("Got image");
        // Create the async conversion request.
        var request = image.ConvertAsync(new XRCpuImage.ConversionParams
        {
            // Use the full image.
            inputRect = new RectInt(0, 0, image.width, image.height),

            // Downsample by 2.
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),

            // Color image format.
            outputFormat = TextureFormat.RGB24,

            // Flip across the Y axis.
            transformation = XRCpuImage.Transformation.MirrorY

        });

        // Wait for the conversion to complete.
        while (!request.status.IsDone())
            await Task.Yield();

        // Check status to see if the conversion completed successfully.
        if (request.status != XRCpuImage.AsyncConversionStatus.Ready)
        {
            // Something went wrong.
            Debug.LogErrorFormat("Request failed with status {0}", request.status);

            // Dispose even if there is an error.
            request.Dispose();
            return;
        }

        // Image data is ready. Let's apply it to a Texture2D.
        var rawData = request.GetData<byte>();
        TryAskingGPT(rawData, request.conversionParams.outputDimensions, request.conversionParams.outputFormat);


        // Need to dispose the request to delete resources associated
        // with the request, including the raw data.
        request.Dispose();
    }
    private async void TryAskingGPT(NativeArray<byte> data, Vector2Int outputDims, TextureFormat format)
    {
        m_Texture = new Texture2D(outputDims.x, outputDims.y, format, false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        m_Texture.LoadImage(data.ToArray());
#else
        m_Texture.LoadRawTextureData(data);
        m_Texture.Apply();
#endif
        //texture.LoadImage(data.ToArray());
        byte[] bytedata = m_Texture.EncodeToJPG();
        string encodedImage = Convert.ToBase64String(bytedata);
        int tries = 0;
        ObjectCard card = null;
        //testimage.sprite = Sprite.Create(m_Texture, new Rect(0, 0, m_Texture.width, m_Texture.height), new Vector2(0.5f, 0.5f), 100);
        do
        {
            ++tries;
            string response = await GPTCommunicationManager.instance.AskChatGPTForObject(encodedImage, cancellationTokenSource);
            Debug.Log("Response: " + response);
            if (response.IsNullOrEmpty())
            {
                Debug.Log("Response is empty");
                continue;
            }
            card = ParseGPTAnswerIntoCard(response, cancellationTokenSource);
        } while (card == null && tries < MAX_RETRIES);

        if (card == null)
        {
            //TODO Show a message that the user should take a picture of an object/ Reset the process
            Debug.Log("Card creation failed");
            DiscardCard();
            return;
        }

        DisplayCardOnScreen(card);

        AudioManager.instance.Play(SoundType.AcceptCard);
    }

    private void DisplayCardOnScreen(ObjectCard card)
    {
        // Create a Texture2D to store the converted image
        currentCard = card;
        cardDisplay.SetCard(card);
        card.SetTexture(m_Texture);
        loadingImage.gameObject.SetActive(false);
        currentCardGameObject = card.gameObject;
        cardDisplay.ShowCard();
        //currentCard.rect.SetParent(cardDisplay.transform);
        //currentCard.rect.localPosition = Vector3.zero;
        rotateLoadingImage = false;
        AlwaysActiveUIManager.Instance.raycastCenterImage.gameObject.SetActive(false);
        cardDisplay.gameObject.SetActive(true);
        cardSelectionButtonsHolder.SetActive(true);
    }

    public void TakePicture()
    {
        AudioManager.instance.PlayButtonClickSounds();
        //NativeCamera.TakePicture(ProcessImage);
        GetImageAsync();
        takePictureUI.SetActive(false);
        cardDisplay.gameObject.SetActive(false);
        loadingImage.gameObject.SetActive(true);
        cardScreen.SetActive(true);
        cardSelectionButtonsHolder.SetActive(false);
        rotateLoadingImage = true;
    }

    public void Update()
    {
        if (rotateLoadingImage)
        {
            loadingImage.rectTransform.Rotate(Vector3.forward, Time.deltaTime * 200);
        }
    }

    public void SaveCard()
    {
        AudioManager.instance.Play(SoundType.AcceptCard);
        PlayerManager.instance.AddCardToDeck(currentCard);
        AlwaysActiveUIManager.Instance.raycastCenterImage.gameObject.SetActive(true);
        cardScreen.SetActive(false);

        takePictureUI.SetActive(true);
        currentCardGameObject.transform.localPosition = Vector3.zero;
        currentCardGameObject.transform.SetParent(cardContainer.transform, false);
        int currentAmount = PlayerManager.instance.GetObjectCardCountInDeck();
        int maxAmount = cardSettings.NumCardsPerPlayer;
        var objectCard = currentCard as ObjectCard;
        NetworkManager.instance.SendCardToOtherPlayer(objectCard.objectCardData);
        currentCard = null;
        cardAmount.text = currentAmount + "/" + maxAmount;
        currentCardGameObject = null;
        if (currentAmount == maxAmount)
        {
            ShowWaitingScreen();
            return;
        }
        takePictureButton.interactable = true;
    }


    private void ShowWaitingScreen()
    {
        AlwaysActiveUIManager.Instance.raycastCenterImage.gameObject.SetActive(false);
        takePictureUI.SetActive(false);
        waitingScreen.SetActive(true);
        AlwaysActiveUIManager.Instance.onInfoScreenOpened -= SetButtonInteraction;
        AlwaysActiveUIManager.Instance.SetInfoPanelText("Waiting for the other player to finish selecting cards....");
        AlwaysActiveUIManager.Instance.ShowInfo(2);
        NetworkManager.instance.SendLocalPlayerReadyForPhase2();
        //Skip to scene 2 (just for testing)
        //PlayerManager.instance.StartPhase2();
    }

    public void DiscardCard()
    {
        AlwaysActiveUIManager.Instance.raycastCenterImage.gameObject.SetActive(true);
        if (cardScreen == null)
        {
            return;
        }
        cardScreen.SetActive(false);
        if (currentCardGameObject != null)
        {
            Destroy(currentCardGameObject);
        }
        takePictureUI.SetActive(true);

        AudioManager.instance.Play(SoundType.WrongAnswer);
    }

    private ObjectCard ParseGPTAnswerIntoCard(string content, CancellationTokenSource cancellationTokenSource)
    {

        if (content.Contains("No object detected"))
        {
            return null;
        }

        try
        {
            string[] lines = content.Split('\n');

            string cardName = lines[0];

            if (!int.TryParse(lines[1], out int attack) || attack < 1 || attack > 5
            || !int.TryParse(lines[2], out int hp) || hp < 10 || hp > 20)
            {
                return null;
            }

            ObjectCard newCard = Instantiate(cardPrefab, cardContainer.transform).GetComponent<ObjectCard>();
            newCard.SetCard(PlayerManager.instance.GetObjectCardCountInDeck(), cardName, attack, hp);

            WiktionaryCommunicationManager.instance.FetchGrammars(cardName,
                (grammars) => { ComputeGrammarData(newCard, grammars); },
                async () =>
                {
                    string response = await GPTCommunicationManager.instance.AskChatGPTForGrammar(cardName, cancellationTokenSource);
                    ComputeGrammarData(newCard, response);
                }
            );

            return newCard;
        }
        catch (Exception ex)
        {
            Debug.Log("Error trying to parse gpt answer: " + ex);
            return null;
        }
    }

    private static void ComputeGrammarData(ObjectCard card, string grammars)
    {
        if (card == null)
        {
            return;
        }
        string[] lines = grammars.Split('\n');

        Dictionary<Grammars, string> wordWithGrammar = new();
        for (int i = 0; i < Enum.GetValues(typeof(Grammars)).Length; i++)
        {
            wordWithGrammar[(Grammars)i] = lines[i];
        }

        SetGrammarDataForCard(card, wordWithGrammar);
        NetworkManager.instance.SendObjectCardGrammarAnswersToOtherPlayer(wordWithGrammar, card.objectCardData.id);
    }

    public static void SetGrammarDataForCard(ObjectCard card, Dictionary<Grammars, string> wordWithGrammar)
    {
        Dictionary<Grammars, string> grammarExtensions = new();
        foreach (Grammars value in Enum.GetValues(typeof(Grammars)))
        {
            grammarExtensions[value] = ComputeDifference(wordWithGrammar[Grammars.NominativeSingular], wordWithGrammar[value]);
        }
        // Just use article for nominative singular
        grammarExtensions[Grammars.NominativeSingular] = wordWithGrammar[Grammars.NominativeSingular].Split(" ")[0];

        card.SetGrammarData(grammarExtensions, wordWithGrammar);
        StringBuilder logOutput = new StringBuilder();
        logOutput.Append("Successfully computed grammar for " + card.cardData.cardName + ":\n");
        foreach (Grammars value in Enum.GetValues(typeof(Grammars)))
        {
            logOutput.Append(value.ToString() + ": " + wordWithGrammar[value] + " (" + grammarExtensions[value] + ")\n");
        }
        Debug.Log(logOutput.ToString());
    }

    static string ComputeDifference(string normal, string special)
    {
        string normalArticle = normal.Split(" ")[0];
        string normalWord = normal.Split(" ")[1];
        string specialArticle = special.Split(" ")[0];
        string specialWord = special.Split(" ")[1];

        string result = specialArticle + " ";

        bool needsDash = false;
        if (specialWord != normalWord)
        {
            for (int i = 0; i < specialWord.Length; i++)
            {
                if (i >= normalWord.Length)
                {
                    if (needsDash)
                    {
                        result += "-";
                    }
                    result += specialWord[i..];
                    break;
                }


                if (specialWord[i] == normalWord[i])
                {
                    if (result[^1] != '-')
                    {
                        needsDash = true;
                    }
                }
                else
                {
                    if (needsDash)
                    {
                        result += "-";
                        needsDash = false;
                    }

                    result += specialWord[i];
                }
            }
        }

        return result;
    }

    private string EncodeImage(string path)
    {
        byte[] imageArray = File.ReadAllBytes(path);
        return Convert.ToBase64String(imageArray);
    }

    private void OnDestroy()
    {
        AlwaysActiveUIManager.Instance.onInfoScreenOpened -= SetButtonInteraction;
        StopAllCoroutines();
        cancellationTokenSource?.Cancel();
    }
}
