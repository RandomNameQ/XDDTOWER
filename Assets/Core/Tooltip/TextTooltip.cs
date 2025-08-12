using UnityEngine;
using UnityEngine.EventSystems;

public class TextTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public bool IsPointerOver { get; private set; }
    public System.Action OnPointerEntered;
    public System.Action OnPointerExited;

    private Vector2 dragOffset;
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool isDragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsPointerOver = true;
        OnPointerEntered?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsPointerOver = false;
        OnPointerExited?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (rectTransform == null || canvas == null) return;
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localMousePos
        );
        dragOffset = rectTransform.anchoredPosition - localMousePos;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || rectTransform == null || canvas == null) return;
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localMousePos
        );
        rectTransform.anchoredPosition = localMousePos + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
}
