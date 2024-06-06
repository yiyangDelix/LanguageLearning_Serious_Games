using UnityEngine;

public class MutualSuicideEffect : Effect
{
    public MutualSuicideEffect(ObjectCard _owner) : base(_owner)
    {
        
        EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.MutualSuicide, 0, owner.gameObject);
       // AudioManager.instance.Play(SoundType.MutualSuicide);

        OnGlobalEvent(GlobalEvent.AttackPhaseBegin, () =>
        {
            ObjectCard target = BattlefieldManager.instance.GetOppositeCard(owner);
            if (target != null)
            {
                target.canAttack = false;
            }
            owner.canAttack = false;
        });

        OnLocalEvent(LocalEvent.UnableToAttack, target =>
        {
            Debug.Log(owner.cardData.cardName + " is unable to attack, opponent is " + (target == null ? "null" : target.cardData.cardName));
            if (target != null && !owner.HasActiveEffect(typeof(FrozenEffect)))
            {
                target.objectCardData.currentHp = 0;
                target.UpdateHealthStat();
                EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.MutualSuicide, 1, target.gameObject);
            }
            EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.MutualSuicide, 1, owner.gameObject);
            AudioManager.instance.Play(SoundType.MutualSuicide);
            owner.objectCardData.currentHp = 0;
            owner.UpdateHealthStat();
            Deactivate(false);
        });
    }

    override public Texture2D getIconTexture()
    {
        return EffectManager.instance.GetEffectIconTextures(GrammarCardEffect.MutualSuicide)[0];
    }
}
