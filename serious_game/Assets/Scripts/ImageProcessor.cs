using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ImageProcessor : MonoBehaviour
{
    public Text outputText;
    public Image imageDisplay;

    private readonly HttpClient client = new HttpClient();
    private string apiKey = "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";
    private string apiEndpoint = "https://api.openai.com/v1/chat/completions";
    //Can we have the prompts in English?
    private string prompt = "Was ist das zentrale Objekt in diesem Bild? Antworte lediglich mit dem Namen des Objektes und dessen Artikel, ohne weiteren Text oder Satzzeichen. "
                          + "Wenn es kein zentrales Objekt gibt, antworte mit \"Bitte nimm ein Foto von einem Objekt auf\""; // Add prompt for attack and hp (exception card later on)
    private string imagePath = "C:\\Users\\maxim\\Downloads\\objects.jpg";

    void Start()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void TakePicture()
    {
        Debug.Log("Taking a picture");
        //NativeCamera.CameraCallback callback = new NativeCamera.CameraCallback((path) =>
        //{
        //    imagePath = path;
        //    WWW www = new WWW("file://" + path);
        //    while (!www.isDone) { }
        //    imageDisplay.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f), 100);
        //});
        //NativeCamera.TakePicture(callback);
        outputText.text = "Image loaded, now go ask ChatGPT!";
        Debug.Log("Took a picture");
    }

    public async void ProcessImage()
    {
        Debug.Log("Asking ChatGPT\n");
        outputText.text = "Processing request...";

        if (imagePath == null || imagePath.Equals(""))
        {
            Debug.Log("Cannot load image: Empty image path\n");
            return;
        }

        string base64Image = EncodeImage(imagePath);

        var payload = new
        {
            model = "gpt-4-vision-preview",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                    }
                }
            },
            max_tokens = 300
        };
        string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

        var response = await client.PostAsync(apiEndpoint, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        string result = await response.Content.ReadAsStringAsync();
        Debug.Log(result);
        if (JObject.Parse(result).ContainsKey("error"))
        {
            return;
        }
        string content = (string)JObject.Parse(result)["choices"][0]["message"]["content"];
        content = content.Replace("\n\n", "\n");
        outputText.text = content;
        ParseGPTAnswerIntoCard(content);
        Debug.Log(content.Replace("\n", "\\n"));
    }

    string EncodeImage(string path)
    {
        byte[] imageArray = File.ReadAllBytes(path);
        return Convert.ToBase64String(imageArray);
    }

    private void ParseGPTAnswerIntoCard(string content)
    {
        if (content.Contains("Bitte nimm ein Foto von einem Objekt auf"))
        {
            //TODO: Show a message that the user should take a picture of an object/ Reset the process
            return;
        }
        string[] splitContent = content.Split('\n');
        string cardName = splitContent[0];
        int attack = int.Parse(splitContent[1]);
        int hp = int.Parse(splitContent[2]);
        //Card card = new ObjectCard(PlayerManager.instance.GetObjectCardCountInDeck(), cardName, attack, hp);
        //Add card to the player's deck
        //Display the card on the screen
    }
}
