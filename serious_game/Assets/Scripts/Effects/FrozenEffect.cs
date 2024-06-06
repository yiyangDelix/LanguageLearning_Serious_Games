using UnityEngine;

public class FrozenEffect : Effect
{
    private int timeLeft = 1;

    public FrozenEffect(ObjectCard _owner) : base(_owner)
    {
        //EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.Frozen, 0, owner.gameObject);
        //AudioManager.instance.Play(SoundType.Frozen);

        OnLocalEvent(LocalEvent.UnableToAttack, target =>
        {
            EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.Frozen, 0, owner.gameObject);
            AudioManager.instance.Play(SoundType.Frozen);
        });

        // not called the very first time because effect is instantiated in AttackPhaseBegin
        OnGlobalEvent(GlobalEvent.AttackPhaseBegin, () =>
        {
            owner.canAttack = false;
        });

        OnGlobalEvent(GlobalEvent.AttackPhaseEnd, () =>
        {
            if (--timeLeft == 0)
            {
                Deactivate(false);
            }
        });
    }

    override public Texture2D getIconTexture()
    {
        return EffectManager.instance.GetEffectIconTextures(GrammarCardEffect.Frozen)[0];
    }
}
