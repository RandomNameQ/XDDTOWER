using Core.Card;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot_Shop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CardData cardData;
    public Color selectedColor = Color.green;
    public Color defaultColor = Color.white;
    public Image image => GetComponent<Image>();
    public ShopBase shop;
    private bool isHovered;

    private void Start()
    {
        var canvas = GetComponentInParent<Canvas>();
        shop = canvas.GetComponentInChildren<ShopBase>();

        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.Mouse1.performed += SelectCard;
        }
        else
        {
            Debug.LogWarning("InputManager is not initialized yet. Retrying in OnEnable.");
        }
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.Mouse1.performed += SelectCard;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.Mouse1.performed -= SelectCard;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void SelectCard(InputAction.CallbackContext context)
    {
        if (!isHovered) return;
        shop.SelectCardInShop(gameObject);
    }
}
