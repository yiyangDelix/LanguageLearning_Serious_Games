using Coffee.UIEffects;
using DG.Tweening;
using Lean.Touch;
using MoreMountains.Feedbacks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ObjectCard : Card
{
    public ObjectCardData objectCardData = new ObjectCardData();
    public Texture2D texture = null;
    public bool canAttack = true;
    [SerializeField] private MMF_Player damageFeedback;
    [SerializeField] private UITransitionEffect[] dissolveEffects;
    [SerializeField] private GameObject[] deathParticles;
    [SerializeField] private GameObject[] columnCompletionParticles;
    [SerializeField] private Image[] removeMaskables;
    [SerializeField] private Gradient[] damageTextGradients;
    [SerializeField] private GameObject correctAnswerHighlightParticlePrefab;
    public event Action<float> onAttackUpdated = delegate { };
    public event Action<float> onHpUpdated = delegate { };
    public event Action<Grammars, string, Effect> onGrammarUpdated = delegate { };

    public void SetCard(int _id, string _cardName, int _attack, int _hpMax, CardOwner owner = CardOwner.Player)
    {
        cardData = objectCardData as CardData;
        base.SetCard(_id, _cardName, owner);
        objectCardData.attack = _attack;
        objectCardData.currentHp = _hpMax;
        objectCardData.hpMax = _hpMax;

        Destroy(gameObject.GetComponent<GrammarCard>());
    }

    public int GetFinalAttack()
    {
        return objectCardData.attack * objectCardData.attackMultiplier;
    }


    public Dictionary<Grammars, string> grammarAnswers = new();

    public Dictionary<Grammars, string> possibleGrammars = new(); // Just the grammar extensions

    public Dictionary<Grammars, Effect> effects = new();
    private List<Effect> otherEffects = new();
    public Dictionary<Effect.LocalEvent, List<Action<ObjectCard>>> onLocalEventActions = new();

    public void Awake()
    {
        foreach (Effect.LocalEvent value in Enum.GetValues(typeof(Effect.LocalEvent)))
        {
            onLocalEventActions[value] = new();
        }
    }

    public void SetTextureFromData()
    {
        if(objectCardData.texture == null)
        {
            return;
        }
        texture = new Texture2D(objectCardData.textureWidthHeight[0], objectCardData.textureWidthHeight[1], (TextureFormat)objectCardData.textureFormat, false);
        texture.LoadImage(objectCardData.texture, false);
        texture.Apply();
    }

    public void SetGrammarData(Dictionary<Grammars, string> grammarExtensions, Dictionary<Grammars, string> wordWithGrammar)
    {
        possibleGrammars = grammarExtensions;
        grammarAnswers = wordWithGrammar;
    }

    public string GrammarApplied(Grammars grammar, Effect effect)
    {
        Debug.Log("Applying effect of grammar " + grammar.ToString() + " to card \"" + cardData.cardName + "\"");
        bool alreadyFilled = effects.ContainsKey(grammar);
        effects[grammar] = effect;
        string answer = grammarAnswers[grammar];
        onGrammarUpdated(grammar, answer, effect);

        if (alreadyFilled) // Do not apply bonuses if the column/table was already filled
        {
            return answer;
        }
        
        bool isSingular = ((int) grammar) % 2 == 0;
        bool areSingularsFilled = effects.ContainsKey(Grammars.NominativeSingular) && effects.ContainsKey(Grammars.GenitiveSingular)
                                && effects.ContainsKey(Grammars.DativeSingular) && effects.ContainsKey(Grammars.AkkusativeSingular);
        bool arePluralsFilled = effects.ContainsKey(Grammars.NominativePlural) && effects.ContainsKey(Grammars.GenitivePlural)
                               && effects.ContainsKey(Grammars.DativePlural) && effects.ContainsKey(Grammars.AkkusativePlural);

        int normalAmount = EffectManager.STAT_BUFF_AMOUNT;
        EffectManager.STAT_BUFF_AMOUNT = 5;
        if (isSingular && areSingularsFilled)
        {
            AddEffect(EffectManager.instance.CreateEffect(GrammarCardEffect.HealthBuff, this));
            if (!arePluralsFilled) // if they are filled too, the table effect is activated instead
            {
                columnCompletionParticles[0].SetActive(true);
            }
        }
        else if (!isSingular && arePluralsFilled)
        {
            AddEffect(EffectManager.instance.CreateEffect(GrammarCardEffect.AttackBuff, this));
            if (!areSingularsFilled) // if they are filled too, the table effect is activated instead
            {
                columnCompletionParticles[1].SetActive(true);
            }
        }

        if (areSingularsFilled && arePluralsFilled)
        {
            AddEffect(EffectManager.instance.CreateEffect(GrammarCardEffect.Heal, this));
            AddEffect(EffectManager.instance.CreateEffect(GrammarCardEffect.AttackBuff, this));
            AddEffect(EffectManager.instance.CreateEffect(GrammarCardEffect.HealthBuff, this));
            columnCompletionParticles[2].SetActive(true);
        }
        EffectManager.STAT_BUFF_AMOUNT = normalAmount;

        return answer;
    }

    public void SetTexture(Texture2D tex)
    {
        texture = tex;
        objectCardData.texture = texture.EncodeToJPG(BattlefieldManager.instance.cardSettings.JpegImageQuality);
        objectCardData.textureFormat = (int)texture.format;
        objectCardData.textureWidthHeight = new int[] { texture.width, texture.height };
        cardDisplay.ShowCard();
    }

    public override void ApplyCard(CardSlot slot)
    {
        // simply place the card on the slot
        slot.SetCard(this);
        GetComponent<BoxCollider>().enabled = true;
        Destroy(gameObject.GetComponent<LeanSelectableByFinger>());
    }

    // Apply an effect independent of grammar
    public void AddEffect(Effect effect)
    {
        otherEffects.Add(effect);
    }

    public void NotifyEffects(Effect.LocalEvent context, ObjectCard target)
    {
        new List<Action<ObjectCard>>(onLocalEventActions[context]).ForEach(action => action.Invoke(target));
    }

    public Effect GetEffect(Grammars grammar)
    {
        return effects.GetValueOrDefault(grammar, null);
    }

    public void RegisterLocalEventAction(Effect.LocalEvent context, Action<ObjectCard> action)
    {
        onLocalEventActions[context].Add(action);
    }

    public void DeRegisterLocalEventAction(Effect.LocalEvent context, Action<ObjectCard> action)
    {
        onLocalEventActions[context].Remove(action);
    }

    public bool ContainsGrammar(Grammars grammar, string extension)
    {
        if (possibleGrammars.ContainsKey(grammar))
        {
            return possibleGrammars[grammar].Equals(extension);
        }
        return false;
    }

    public bool HasActiveEffect(Type type)
    {
        foreach (Effect effect in effects.Values)
        {
            if (effect.IsActive() && effect.GetType() == type)
            {
                return true;
            }
        }

        return otherEffects.Exists(effect => effect.IsActive() && effect.GetType() == type);
    }

    public void AddAttackStat(int amount)
    {
        objectCardData.attack += amount;
        onAttackUpdated(0f);
    }

    public void UpdateHealthStat()
    {
        onHpUpdated(0f);
    }

    /// <summary>
    /// Processes damage dealt to the card.
    /// </summary>
    /// <param name="damage">Amount of damage dealt</param>
    /// <returns> -1 if card is still alive, or a positive number holding the excess damage if the card is destroyed (can be 0)</returns>
    public int TakeDamage(int damage)
    {
        bool hit = NetworkManager.instance.currentRandomNumbers.randomNumbers[0] <= objectCardData.hitChance;
        NetworkManager.instance.currentRandomNumbers.randomNumbers.RemoveAt(0);
        if (hit)
        {
            objectCardData.currentHp -= damage;
            var floatingText = damageFeedback?.GetFeedbackOfType<MMF_FloatingText>();
            floatingText.ForceColor = true;
            floatingText.ForceLifetime = true;
            floatingText.Lifetime = 1f;
            if (damage > 0)
            {
                floatingText.Value = "-" + damage.ToString();
                floatingText.AnimateColorGradient = damageTextGradients[(int)objectCardData.owner];
                onHpUpdated(0.1f);
            }
            else
            {
                floatingText.Value = "Miss";
                floatingText.AnimateColorGradient = damageTextGradients[objectCardData.owner == CardOwner.Player ? 1 : 0];
            }
            damageFeedback.transform.localEulerAngles = new Vector3(0, 0, (int)objectCardData.owner * 180);

            damageFeedback?.PlayFeedbacks(transform.position);
            if (objectCardData.currentHp <= 0)
            {
                return -objectCardData.currentHp;
            }
        }
        else
        {
            var floatingText = damageFeedback?.GetFeedbackOfType<MMF_FloatingText>();
            floatingText.ForceColor = true;
            floatingText.ForceLifetime = true;
            floatingText.Lifetime = 1f;
            floatingText.Value = "Miss";
            floatingText.AnimateColorGradient = damageTextGradients[objectCardData.owner == CardOwner.Player ? 1 : 0];
            damageFeedback?.PlayFeedbacks(transform.position);
        }

        return -1;
    }

    public void CorrectAnswerHighlight()
    {
        var particle = Instantiate(correctAnswerHighlightParticlePrefab, transform);
        //StartCoroutine(CorrectAnswerHighlightCoroutine(2));
    }

    private IEnumerator CorrectAnswerHighlightCoroutine(int seconds)
    {
        var particle = Instantiate(correctAnswerHighlightParticlePrefab, transform.parent);
        yield return new WaitForSeconds(seconds);
        Destroy(particle);
    }

    public bool DestroyIfZeroHP()
    {
        if (objectCardData.currentHp <= 0)
        {
            // Animations
            foreach (var effect in dissolveEffects)
            {
                effect.enabled = true;
                effect.effectPlayer.duration = 1.5f;
                DOVirtual.Float(effect.effectFactor, 0, 1.5f, (float x) => { effect.effectFactor = x; });
            }
            cardDisplay.objectCardNameText.DOFade(0, 1.5f);
            foreach (var maskable in removeMaskables)
            {
                maskable.maskable = false;
            }
            cardDisplay.grammarTableObject.SetActive(false);
            cardDisplay.attackText.transform.parent.gameObject.SetActive(false);
            cardDisplay.hpText.transform.parent.gameObject.SetActive(false);
            deathParticles[(int)objectCardData.owner].SetActive(true);
            AudioManager.instance.Play(SoundType.CardDying);
            DestroyCard();

            return true;
        }
        return false;
    }

    private async void DestroyCard()
    {
        if (selectable != null)
        {
            selectable.OnSelected.RemoveAllListeners();
            selectable.OnDeselected.RemoveAllListeners();
        }
        await Task.Delay(1600);
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

}



