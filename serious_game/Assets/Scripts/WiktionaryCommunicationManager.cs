using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class WiktionaryCommunicationManager : MonoBehaviour
{
    public static WiktionaryCommunicationManager instance;
    private readonly string templateUrl = "https://de.wiktionary.org/w/api.php?action=query&titles={0}&prop=revisions&rvslots=main&rvprop=content&format=json";

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

    public void FetchGrammars(string objectName, Action<string> onSuccess, Action onFailure)
    {
        string url = String.Format(templateUrl, objectName);
        StartCoroutine(PerformRequest(url, onSuccess, onFailure));
    }

    private IEnumerator PerformRequest(string url, Action<string> onSuccess, Action onFailure)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = Regex.Unescape(request.downloadHandler.text);
            string parsed = ParseResponse(result);
            if (parsed == null)
            {
                Debug.Log("Failed to parse response, was: " + result);
                onFailure.Invoke();
            }
            else
            {
                onSuccess.Invoke(parsed);
            }
        }
        else
        {
            Debug.Log("WebRequest failed: " + request.result.ToString());
            onFailure.Invoke();
        }
    }

    private string ParseResponse(string response)
    {
        StringBuilder result = new();

        if (!ExtractFirstValue(response, createPossibilities("Genus"), out string genus)
        || (genus != "m" && genus != "f" && genus != "n"))
        {
            return null;
        }

        bool success = true;

        success |= ExtractFirstValue(response, createPossibilities("Nominativ Singular"), out string value);
        result.Append(SelectArticle(genus, "Der", "Die", "Das") + " " + value + "\n");

        success |= ExtractFirstValue(response, createPossibilities("Nominativ Plural"), out value);
        result.Append("Die " + value + "\n");

        success |= ExtractFirstValue(response, createPossibilities("Genitiv Singular"), out value);
        result.Append(SelectArticle(genus, "Des", "Der", "Des") + " " + value + "\n");

        success |= ExtractFirstValue(response, createPossibilities("Genitiv Plural"), out value);
        result.Append("Der " + value + "\n");

        success |= ExtractFirstValue(response, createPossibilities("Dativ Singular"), out value);
        result.Append(SelectArticle(genus, "Dem", "Der", "Dem") + " " + value + "\n");

        success |= ExtractFirstValue(response, createPossibilities("Dativ Plural"), out value);
        result.Append("Den " + value + "\n");

        success |= ExtractFirstValue(response, createPossibilities("Akkusativ Singular"), out value);
        result.Append(SelectArticle(genus, "Den", "Die", "Das") + " " + value + "\n");

        success |= ExtractFirstValue(response, createPossibilities("Akkusativ Plural"), out value);
        result.Append("Die " + value + "\n");

        return success ? result.ToString() : null;
    }

    private bool ExtractFirstValue(string response, string[] keys, out string result)
    {
        foreach (string key in keys)
        {
            Match m = new Regex($@"\|{key}=(.+?)\n").Match(response);

            if (m.Success && !string.IsNullOrEmpty(m.Groups[1].Value))
            {
                result = m.Groups[1].Value;
                return true;
            }
        }
        result = "";
        return false;
    }

    private string SelectArticle(string genus, string masc, string fem, string neu)
    {
        return genus == "m" ? masc : (genus == "f" ? fem : neu);
    }

    private string[] createPossibilities(string normal)
    {
        return new string[]
        {
            normal, normal + "\\*", normal + " 1", normal + " 2", normal + " 3", normal + " 4",
            normal + " stark", normal + " schwach", normal + " gemischt",
        };
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
