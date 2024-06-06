using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Effect
{
    protected ObjectCard owner;
    protected bool active = true;
    private List<Tuple<LocalEvent, Action<ObjectCard>>> localActions;
    private List<Tuple<GlobalEvent, Action>> globalActions;

    public Effect(ObjectCard _owner)
    {
        owner = _owner;
        localActions = new();
        globalActions = new();
    }

    public ObjectCard GetOwner()
    {
        return owner;
    }

    public bool IsActive()
    {
        return active;
    }

    public abstract Texture2D getIconTexture();

    protected void OnLocalEvent(LocalEvent context, Action<ObjectCard> action)
    {
        owner.RegisterLocalEventAction(context, action);
        localActions.Add(new(context, action));
    }

    protected void OnGlobalEvent(GlobalEvent context, Action action)
    {
        EffectManager.instance.RegisterGlobalEventAction(context, action);
        globalActions.Add(new(context, action));
    }

    protected void Deactivate(bool updateIcon)
    {
        active = false;
        RemoveListeners();
        if (updateIcon)
        {
            owner.cardDisplay.UpdateEffectIcon(this);
        }
    }

    protected void RemoveListeners()
    {
        localActions.ForEach(tuple => owner.DeRegisterLocalEventAction(tuple.Item1, tuple.Item2));
        globalActions.ForEach(tuple => EffectManager.instance.DeRegisterGlobalEventAction(tuple.Item1, tuple.Item2));
    }

    public enum LocalEvent
    {
        Attacking,
        Attacked,
        UnableToAttack,
    }

    public enum GlobalEvent
    {
        AttackPhaseBegin,
        AttackPhaseEnd,
    }
}
