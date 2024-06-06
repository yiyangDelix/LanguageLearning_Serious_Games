using UnityEngine;

public class FreezeEffect : Effect
{
    public static FrozenEffect frozenEffectPrefab;

    public FreezeEffect(ObjectCard _owner) : base(_owner)
    {
        // Show the freeze effect on apply because during attacking the sword animation is shown
        EffectManager.instance.InstantiateAnimationAtParent(GrammarCardEffect.FreezeTurn, 0, owner.gameObject);
        AudioManager.instance.Play(SoundType.FreezeBuff);

        OnGlobalEvent(GlobalEvent.AttackPhaseBegin, () =>
        {
            ObjectCard target = BattlefieldManager.instance.GetOppositeCard(owner);
            if (target != null)
            {
                target.AddEffect(new FrozenEffect(target));
                target.canAttack = false; // necessary, because the frozen would only be able to handle later turns
            }
            
            
        });

        OnGlobalEvent(GlobalEvent.AttackPhaseEnd, () => 
        {
            //Deactivate the effect after attacking no matter if the target is null or not
            Deactivate(true);

        });
    }

    override public Texture2D getIconTexture()
    {
        return EffectManager.instance.GetEffectIconTextures(GrammarCardEffect.FreezeTurn)[active ? 0 : 1];
    }
}
