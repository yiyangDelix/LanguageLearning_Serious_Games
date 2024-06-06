using UnityEngine;

public class HealEffect : Effect
{
    public HealEffect(ObjectCard _owner) : base(_owner)
    {
        owner.objectCardData.currentHp = owner.objectCardData.hpMax;
        owner.UpdateHealthStat();
        Deactivate(true);
        EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.Heal, 0, owner.gameObject);
        AudioManager.instance.Play(SoundType.Heal);
    }

    override public Texture2D getIconTexture()
    {
        return EffectManager.instance.GetEffectIconTextures(GrammarCardEffect.Heal)[active ? 0 : 1];
    }
}
