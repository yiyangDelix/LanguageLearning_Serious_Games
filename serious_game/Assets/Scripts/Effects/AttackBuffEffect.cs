using UnityEngine;

public class AttackBuffEffect : Effect
{
    public AttackBuffEffect(ObjectCard _owner) : base(_owner)
    {
        // Adds and calls the event to update the attack stat on card display
        owner.AddAttackStat(EffectManager.STAT_BUFF_AMOUNT);
        EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.AttackBuff, 0, owner.gameObject);
        AudioManager.instance.Play(SoundType.AttackBuff);
        Deactivate(true);
    }

    override public Texture2D getIconTexture()
    {
        return EffectManager.instance.GetEffectIconTextures(GrammarCardEffect.AttackBuff)[active ? 0 : 1];
    }
}
