using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class SwapEffect : Effect
{
    public SwapEffect(ObjectCard _owner) : base(_owner)
    {
        EffectManager.instance.StartCoroutine(HandleSwap());
    }

    override public Texture2D getIconTexture()
    {
        return EffectManager.instance.GetEffectIconTextures(GrammarCardEffect.Swap)[active ? 0 : 1];
    }

    private IEnumerator HandleSwap()
    {
        Task<(int, CardOwner)> task = null;

        //Swap will only execute on local client and only after selecting the card, it will get passed to other player
        //Check if target slot ID has a card, if so, swap the cards
        //Also do a check if selected slot is same as the owner slot, if so, do nothing, using this for cancel
        if (NetworkManager.instance.WhoseTurn == CardOwner.Player)
        {
            int ownerCardSlotID = BattlefieldManager.instance.FindCardSlotFromCard(owner);
            task = PlayerManager.instance.SelectCardSlotForSwap(ownerCardSlotID);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.Exception == null)
            {
                Debug.Log("Swap target slot ID: " + task.Result);
                var (targetSlotID, targetOwner) = task.Result;
                if ((targetSlotID != int.MaxValue) && !(ownerCardSlotID == targetSlotID && owner.cardData.owner == targetOwner))
                {
                    // ids remain the same while sending over the network, will swap the ownership on the other side
                    NetworkManager.instance.SendSwapCardToOtherPlayer(ownerCardSlotID, owner.cardData.owner, targetSlotID, targetOwner);
                    PlayerManager.instance.SwapCards(ownerCardSlotID, owner.cardData.owner, targetSlotID, targetOwner);
                }
            }
            else
            {
                Debug.LogError("Getting swapTargetSlotID failed: " + task.Exception);
            }
        }
        else
        {
            //task = NetworkManager.instance.WaitForSwapTarget();
        }

        Deactivate(true);
    }
}
