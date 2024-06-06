using MoreMountains.Feedbacks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CardStatText
{
    Attack,
    HP,
}

public class CardDisplay : MonoBehaviour
{
    [Serializable]
    public struct GrammarTableTextColumn
    {
        public TextMeshProUGUI[] grammarAnswers;
    }

    [Serializable]
    public struct GrammarTableIconColumn
    {
        public Image[] icons;
    }

    public TMPro.TextMeshProUGUI objectCardNameText;
    public TMPro.TextMeshProUGUI attackText;
    public TMPro.TextMeshProUGUI hpText;
    public TMPro.TextMeshProUGUI effectDescriptionText;
    public TMPro.TextMeshProUGUI effectNameText;
    public TMPro.TextMeshProUGUI grammarTypeText;
    public TMPro.TextMeshProUGUI grammarExtensionText;

    public Image backgroundImage;
    public Image objectImage;
    public Image grammarEffectImage;
    public GameObject grammarTableObject;

    public Card card = null;
    public GrammarTableTextColumn[] grammarTable;
    public GrammarTableIconColumn[] iconTable;
    public GameObject objectCardElements;
    public GameObject grammarCardElements;

    [SerializeField] private MMF_Player textWiggleUpdateFeedback;
    [SerializeField] private MMWiggle[] wigglers;
    [SerializeField] private MMF_Player textRevealFeedback;

    // Start is called before the first frame update
    void Start()
    {
        ClearTableContents();
    }

    public void SetCard(Card card)
    {
        if (card.GetType() == typeof(ObjectCard))
        {
            ObjectCard objectCard;
            if (this.card != null)
            {
                objectCard = this.card as ObjectCard;
                objectCard.onAttackUpdated -= UpdateAttack;
                objectCard.onHpUpdated -= UpdateHP;
                objectCard.onGrammarUpdated -= UpdateGrammar;
            }
            objectCard = card as ObjectCard;
            objectCard.onAttackUpdated += UpdateAttack;
            objectCard.onHpUpdated += UpdateHP;
            objectCard.onGrammarUpdated += UpdateGrammar;
        }
        this.card = card;
    }

    public void ResetCard()
    {
        if (this.card != null)
        {
            if (card.GetType() == typeof(ObjectCard))
            {
                var objectCard = this.card as ObjectCard;
                objectCard.onAttackUpdated -= UpdateAttack;
                objectCard.onHpUpdated -= UpdateHP;
                objectCard.onGrammarUpdated -= UpdateGrammar;
            }
            this.card = null;
        }
    }

    private void UpdateAttack(float delay)
    {
        var objectCard = card as ObjectCard;
        UpdateAndWiggleText(attackText, objectCard.objectCardData.attack.ToString(), CardStatText.Attack, delay);

    }
    private void UpdateHP(float delay)
    {
        var objectCard = card as ObjectCard;
        UpdateAndWiggleText(hpText, objectCard.objectCardData.currentHp.ToString(), CardStatText.HP, delay);
    }

    private void UpdateAndWiggleText(TextMeshProUGUI text, string newText, CardStatText cardStatText, float initialDelay = 0f)
    {
        var textDisplay = textWiggleUpdateFeedback?.GetFeedbackOfType<MMF_TMPText>();
        textDisplay.TargetTMPText = text;
        textDisplay.NewText = newText;
        var wiggle = textWiggleUpdateFeedback?.GetFeedbackOfType<MMF_Wiggle>();
        wiggle.TargetWiggle = wigglers[(int)cardStatText];
        textWiggleUpdateFeedback.InitialDelay = initialDelay;
        textWiggleUpdateFeedback.PlayFeedbacks();
    }

    private void UpdateGrammar(Grammars grammar, string answer, Effect effect)
    {
        var (i, j) = getIndicesByGrammar(grammar);
        var textReveal = textRevealFeedback.GetFeedbackOfType<MMF_TMPTextReveal>();
        textReveal.TargetTMPText = grammarTable[i].grammarAnswers[j];
        textReveal.NewText = answer;
        textRevealFeedback.PlayFeedbacks();

        UpdateEffectIcon(effect, i, j);
    }

    public void UpdateEffectIcon(Effect effect)
    {
        ObjectCard objectCard = card as ObjectCard;
        foreach (Grammars grammar in Enum.GetValues(typeof(Grammars)))
        {
            if (objectCard.GetEffect(grammar) == effect)
            {
                var (i, j) = getIndicesByGrammar(grammar);
                UpdateEffectIcon(effect, i, j);
                return;
            }
        }
    }

    private void UpdateEffectIcon(Effect effect, int i, int j)
    {
        Image icon = iconTable[i].icons[j];
        Texture2D texture = effect.getIconTexture();
        if (texture == null)
        {
            Debug.Log("Texture of effect in UpdateGrammar in CardDisplay was null");
            return;
        }
        icon.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
        icon.enabled = true;
    }

    private (int, int) getIndicesByGrammar(Grammars grammar)
    {
        return grammar switch
        {
            Grammars.NominativeSingular => (0, 0),
            Grammars.NominativePlural => (1, 0),
            Grammars.GenitiveSingular => (0, 1),
            Grammars.GenitivePlural => (1, 1),
            Grammars.DativeSingular => (0, 2),
            Grammars.DativePlural => (1, 2),
            Grammars.AkkusativeSingular => (0, 3),
            Grammars.AkkusativePlural => (1, 3),
            _ => (-1, -1),
        };
    }

    public void ClearTableContents()
    {
        foreach (GrammarTableTextColumn col in grammarTable)
        {
            foreach (TextMeshProUGUI text in col.grammarAnswers)
            {
                text.text = "";
            }
        }

        foreach (GrammarTableIconColumn col in iconTable)
        {
            foreach (Image image in col.icons)
            {
                image.enabled = false;
            }
        }
    }

    public void ShowCard()
    {
        Debug.Log("Is nameText null " + objectCardNameText == null);
        if (card is ObjectCard)
        {
            objectCardNameText.text = card.cardData.cardName;
            var objectCard = card as ObjectCard;
            attackText.text = objectCard.objectCardData.attack.ToString();
            hpText.text = objectCard.objectCardData.currentHp.ToString();
            if (objectCard.texture != null)
            {
                objectImage.sprite = Sprite.Create(objectCard.texture, new Rect(0, 0, objectCard.texture.width, objectCard.texture.height), new Vector2(0.5f, 0.5f), 100);
            }

            // loop through all Grammars enum and store in columns
            foreach (Grammars value in Enum.GetValues(typeof(Grammars)))
            {
                var (i, j) = getIndicesByGrammar(value);
                if (objectCard.effects.ContainsKey(value))
                {
                    grammarTable[i].grammarAnswers[j].text = objectCard.grammarAnswers[value];
                    UpdateEffectIcon(objectCard.effects[value], i, j);
                }
                else
                {
                    grammarTable[i].grammarAnswers[j].text = "-";
                    iconTable[i].icons[j].enabled = false;
                }
            }
            objectCardElements.SetActive(true);
            grammarCardElements.SetActive(false);
        }
        else if (card is GrammarCard)
        {
            var grammarCard = card as GrammarCard;

            grammarTypeText.text = grammarCard.grammarCardData.grammar.ToString();
            grammarExtensionText.text = grammarCard.grammarCardData.cardName;
            effectDescriptionText.text = EffectManager.instance.GetEffectNameAndDescription(grammarCard.grammarCardData.effect);
            effectNameText.text = EffectManager.instance.effectNames[(int)grammarCard.grammarCardData.effect];
            effectNameText.color = EffectManager.instance.GetEffectNameColor(grammarCard.grammarCardData.effect);
            var effectTex = EffectManager.instance.GetEffectIconTextures(grammarCard.grammarCardData.effect)[0];
            grammarEffectImage.sprite = Sprite.Create(effectTex, new Rect(0, 0, effectTex.width, effectTex.height), Vector2.zero);
            grammarCardElements.SetActive(true);
            objectCardElements.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        ObjectCard objectCard;
        if (this.card != null && this.card.GetType() == typeof(ObjectCard))
        {
            objectCard = this.card as ObjectCard;
            objectCard.onAttackUpdated -= UpdateAttack;
            objectCard.onHpUpdated -= UpdateHP;
            objectCard.onGrammarUpdated -= UpdateGrammar;
        }
    }
}
