using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public int testRandomEffect = 0;
    public static int STAT_BUFF_AMOUNT = 2;

    [Serializable]
    public class SerializableTriple<T, U, V>
    {
        public T effectName;
        public U iconTextures;
        public V effectPrefabs;
    }

    public List<string> effectNames = new List<string>()
    {
        "ATTACK BUFF",
        "HEALTH BUFF",
        "HEAL",
        "MUTUAL SUICIDE",
        "FREEZE",
        "SWAP",
        "FROZEN"
    };

    public static EffectManager instance;

    // List of textures/prefabs as some effects need multiple textures/prefabs, depending on the situation. First is default.
    [SerializeField]
    private List<SerializableTriple<GrammarCardEffect, List<Texture2D>, List<GameObject>>> prefabs;

    // Because I don't wanna mess with the serializable class
    [SerializeField] private List<Color> effectNameColorList;

    public Dictionary<Effect.GlobalEvent, List<Action>> onGlobalEventActions = new();

    public Effect CreateEffect(GrammarCardEffect grammarCardEffect, ObjectCard owner)
    {
        return grammarCardEffect switch
        {
            GrammarCardEffect.AttackBuff => new AttackBuffEffect(owner),
            GrammarCardEffect.HealthBuff => new HealthBuffEffect(owner),
            GrammarCardEffect.Heal => new HealEffect(owner),
            GrammarCardEffect.MutualSuicide => new MutualSuicideEffect(owner),
            GrammarCardEffect.FreezeTurn => new FreezeEffect(owner),
            GrammarCardEffect.Swap => new SwapEffect(owner),
            GrammarCardEffect.Frozen => new FrozenEffect(owner),
            _ => null,
        };
    }

    public string GetEffectNameAndDescription(GrammarCardEffect grammarCardEffect)
    {
        return grammarCardEffect switch
        {
            GrammarCardEffect.AttackBuff => "Increases the attack stat of the target by " + STAT_BUFF_AMOUNT.ToString(),
            GrammarCardEffect.HealthBuff => "Increases the HP stat of the target by " + STAT_BUFF_AMOUNT.ToString(),
            GrammarCardEffect.Heal => "Restores the target to full HP",
            GrammarCardEffect.MutualSuicide => "Upon attacking, both the target and the attacked card will be destroyed, independent of their stats",
            GrammarCardEffect.FreezeTurn => "Upon attacking, the target inflicts a frozen effect on the attacked card",
            GrammarCardEffect.Swap => "Allows the target card to be switched with any other card",
            GrammarCardEffect.Frozen => "The target is prevented from attacking",
            _ => "Invalid",
        };
    }

    public void InstantiateAnimationAtParent(GrammarCardEffect grammarCardEffect, int index, GameObject parent)
    {
        GameObject prefab =  prefabs.Find(pair => pair.effectName == grammarCardEffect).effectPrefabs[index];
        GameObject animation = Instantiate(prefab, parent.transform);
        //animation.transform.position = parent.transform.position;
        animation.transform.localPosition = Vector3.forward * -10f;
        // animation.transform.localScale = 20 * Vector3.one;
        // animation.transform.localRotation = Quaternion.Euler(-90, 0, 0);
    }

    public List<Texture2D> GetEffectIconTextures(GrammarCardEffect effect)
    {
        return prefabs.Find(pair => pair.effectName == effect).iconTextures;
    }

    public Color GetEffectNameColor(GrammarCardEffect effect)
    {
        return effectNameColorList[(int)effect];
    }
    public void NotifyEffects(Effect.GlobalEvent context)
    {
        new List<Action>(onGlobalEventActions[context]).ForEach(action => action.Invoke());
    }

    public void RegisterGlobalEventAction(Effect.GlobalEvent context, Action action)
    {
        onGlobalEventActions[context].Add(action);
    }

    public void DeRegisterGlobalEventAction(Effect.GlobalEvent context, Action action)
    {
        onGlobalEventActions[context].Remove(action);
    }

    public GrammarCardEffect GetRandomEffect()
    {
        int random = UnityEngine.Random.Range(0, 100);
        if (random < 20) return GrammarCardEffect.AttackBuff;
        if (random < 40) return GrammarCardEffect.HealthBuff;
        if (random < 60) return GrammarCardEffect.Heal;
        if (random < 80) return GrammarCardEffect.FreezeTurn;
        if (random < 90) return GrammarCardEffect.MutualSuicide;
        return GrammarCardEffect.Swap;
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
            Init();
        }
    }

    private void Init()
    {
        onGlobalEventActions = new();
        foreach (Effect.GlobalEvent value in Enum.GetValues(typeof(Effect.GlobalEvent)))
        {
            onGlobalEventActions[value] = new();
        }
    }
}
