using UnityEngine;

public class HealthBuffEffect : Effect
{
    public HealthBuffEffect(ObjectCard _owner) : base(_owner)
    {
        owner.objectCardData.currentHp += EffectManager.STAT_BUFF_AMOUNT;
        owner.objectCardData.hpMax += EffectManager.STAT_BUFF_AMOUNT;
        owner.UpdateHealthStat();
        Deactivate(true);
        EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.HealthBuff, 0, owner.gameObject);
        AudioManager.instance.Play(SoundType.HealthBuff);
    }

    override public Texture2D getIconTexture()
    {
        return EffectManager.instance.GetEffectIconTextures(GrammarCardEffect.HealthBuff)[active ? 0 : 1];
    }
}
