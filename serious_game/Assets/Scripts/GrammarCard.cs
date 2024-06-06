using Lean.Touch;
using System.Collections.Generic;

public class GrammarCard : Card
{
    public GrammarCardData grammarCardData = new GrammarCardData();
    public void SetCard(int _id, string _cardName, string _effect, CardOwner owner = CardOwner.Player, Grammars grammar = Grammars.NominativeSingular, GrammarCardEffect effect = GrammarCardEffect.AttackBuff)
    {
        cardData = grammarCardData as CardData;
        base.SetCard(_id, _cardName, owner);
        grammarCardData.effect = effect;
        grammarCardData.grammar = grammar;

        Destroy(gameObject.GetComponent<ObjectCard>());
        cardDisplay.ShowCard();
    }

    public override void ApplyCard(CardSlot slot)
    {

        AudioManager.instance.Play(SoundType.AcceptCard);

        // Get the object card on the slot and apply the grammar card effect on it
        var objectCard = slot.GetCard();
        PlayerManager.instance.UsedGrammarCard(this, objectCard);
        Destroy(gameObject.GetComponent<LeanSelectableByFinger>());
        //Animation effect
    }

    public void SetCorrectAnswerObjectCardNames(List<string> _correctAnswerObjectCardNames)
    {
        grammarCardData.correctAnswerObjectCardNames = _correctAnswerObjectCardNames.ToArray();
    }
}

