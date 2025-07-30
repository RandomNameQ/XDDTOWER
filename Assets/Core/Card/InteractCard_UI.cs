using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InteractCard_UI : MonoBehaviour, IPointerExitHandler
{
    private PawnCreature interactedCard;
    public Button sellButton;

    public void Init(PawnCreature card, Vector3 mousePosition)
    {
        interactedCard = card;
        sellButton.onClick.AddListener(SellCard);
        transform.position = mousePosition;
    }

    public void SellCard()
    {
        if (interactedCard != null)
        {
            interactedCard.RemoveCard();
            interactedCard = null; // Сбрасываем ссылку сразу
        }
        HideInteractCard();
    }

    public void HideInteractCard()
    {
        gameObject.SetActive(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideInteractCard();
    }
}