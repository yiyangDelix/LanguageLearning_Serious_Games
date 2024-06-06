using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GPTCommunicationManager : MonoBehaviour
{
    public static GPTCommunicationManager instance;

    private readonly HttpClient client = new();

    private readonly string apiKey = "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";
    private readonly string apiEndpoint = "https://api.openai.com/v1/chat/completions";
    // Add prompt for exceptions later
    private readonly string objectPrompt = "Du bist ein fortgeschrittener Bildanalysator mit Fokus auf Objekterkennung und Wertzuteilung. Sieh dir das angehängte Bild genau an und identifiziere das zentrale Objekt. Sobald du das Objekt identifiziert hast, bewerte es wie folgt, wobei jede Information in einer eigenen Zeile ausgegeben wird:\n"
    + "1. Den Namen des Objekts (im Singular, ohne Artikel). Wenn es mehrere Bezeichnungen für das Wort gibt, bevorzuge die kürzeste eindeutige Bezeichnung.\n"
    + "2. An attack value for the object: Choose a value between 1 and 5 (inclusive) that reflects it's average dimensions in comparison to other objects you will find on an average desk. Only output the number.\n"
    + "3. Einen Gesundheitswert für das Objekt: Bestimme diesen Wert im Bereich von 10 bis 20, indem du die scheinbare Robustheit und Widerstandsfähigkeit des Objekts im Vergleich zu anderen Objekten, die man auf einem Schreibtisch findet, betrachtest.Versuche, den vollen Bereich bis 20 zu nutzen. Gib lediglich den Wert aus.\n"
    + "Falls du das zentrale Objekt im Bild nicht bestimmen kannst, gib lediglich 'No object detected' aus. Stelle sicher, dass deine Ausgabe präzise und auf die oben genannten Kriterien beschränkt ist, ohne weitere Erklärungen oder Kommentare.";

    private readonly string grammarPrompt = "Gib für das Wort \"{0}\" die folgenden Formen aus, jeweils in einer eigenen Zeile:\n"
    + "Den Nominativ Singular des Wortes (mit Artikel)\n"
    + "Den Nominativ Plural des Wortes (mit Artikel)\n"
    + "Den Genitiv Singular des Wortes (mit Artikel)\n"
    + "Den Genitiv Plural des Wortes (mit Artikel)\n"
    + "Den Dativ Singular des Wortes (mit Artikel)\n"
    + "Den Dativ Plural des Wortes (mit Artikel)\n"
    + "Den Akkusativ Singular des Wortes (mit Artikel)\n"
    + "Den Akkusativ Plural des Wortes (mit Artikel)\n"
    + "Gib außer exakt dem was ich dir aufgetragen habe nichts aus. "
    + "Falls du dazu nicht in der Lage bist, gib lediglich \"Grammar could not be determined\" aus.";

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

    // private constructor
    private GPTCommunicationManager()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> AskChatGPTForObject(string base64Image, CancellationTokenSource cancellationTokenSource)
    {
        Debug.Log("Asking ChatGPT for object detection\n");
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
                        new { type = "text", text = objectPrompt },
                        new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}", detail = "low" } }
                    }
                }
            },
            max_tokens = 300
        };
        return await AskChatGPT(payload, cancellationTokenSource);
    }



    public async Task<string> AskChatGPTForGrammar(string word, CancellationTokenSource cancellationTokenSource)
    {
        Debug.Log("Asking ChatGPT for grammar for the word " + word + "\n");
        var payload = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "user", content = String.Format(grammarPrompt, word) }
            },
            max_tokens = 300
        };
        return await AskChatGPT(payload, cancellationTokenSource);
    }

    public async Task<string> AskChatGPT(object payload, CancellationTokenSource cancellationTokenSource)
    {
        string jsonPayload = JsonConvert.SerializeObject(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, apiEndpoint);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        //var response = await client.PostAsync(apiEndpoint, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        string result = await response.Content.ReadAsStringAsync();
        if (JObject.Parse(result).ContainsKey("error"))
        {
            return "";
        }
        string content = (string)JObject.Parse(result)["choices"][0]["message"]["content"];
        content = content.Replace("\n\n", "\n");
        Debug.Log(content.Replace("\n", "\\n"));
        return content;
    }

}
