using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TooltipComp : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Tooltip Settings")]
    public GameObject tooltipObject;
    public Vector3 offset = new Vector3(0, 1, 0);
    public float hideDelay = 0.1f;

    private static TooltipComp currentActiveTooltip;
    private bool isUIObject;
    private EventSystem eventSystem;
    private Coroutine hideCoroutine;
    private bool isDragging;
    private Vector3 dragOffset;
    private Camera mainCamera;

    public GameActionBase gameAction;

    private TextTooltip textTooltip;
    private bool isPointerOverTooltip = false;
    private bool isPointerOverSelf = false;

    private void Start()
    {
        InitializeComponents();
        CheckObjectType();
        SetupTooltip();
        textTooltip = tooltipObject.GetComponent<TextTooltip>();
        if (textTooltip != null)
        {
            textTooltip.OnPointerEntered += () => { isPointerOverTooltip = true; StopHideCoroutine(); };
            textTooltip.OnPointerExited += () => { isPointerOverTooltip = false; TryHideTooltip(); };
        }
    }

    private void InitializeComponents()
    {
        gameAction = GetComponentInParent<GameActionSlot>().slot;
        if (gameAction == null)
            gameAction = GetComponent<GameActionSlot>()?.slot;
        if (gameAction == null)
        {
            Debug.LogWarning("net");
        }
        tooltipObject = FindAnyObjectByType<TextTooltip>(FindObjectsInactive.Include).gameObject;

        eventSystem = FindObjectOfType<EventSystem>();
        mainCamera = Camera.main;

    }

    private void CheckObjectType()
    {

        isUIObject = GetComponent<RectTransform>() != null;
    }

    private void SetupTooltip()
    {
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
            MakeTooltipTransparent();
        }
    }

    private void MakeTooltipTransparent()
    {
        if (tooltipObject == null) return;

        Collider[] colliders = tooltipObject.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        Collider2D[] colliders2D = tooltipObject.GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders2D)
        {
            collider.enabled = false;
        }

        // Не трогаем raycastTarget!
    }

    private void OnMouseEnter()
    {
        if (isUIObject) return;

        ShowTooltip();
    }

    private void OnMouseExit()
    {
        if (isUIObject) return;
        StartHideCoroutine();
    }

    private void OnMouseDown()
    {
        if (isUIObject) return;
        StartDragging();
    }

    private void OnMouseDrag()
    {
        if (isUIObject) return;
        UpdateDragPosition();
    }

    private void OnMouseUp()
    {
        if (isUIObject) return;
        StopDragging();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isUIObject) return;
        isPointerOverSelf = true;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isUIObject) return;
        isPointerOverSelf = false;
        TryHideTooltip();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isUIObject) return;
        StartDragging();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isUIObject) return;
        UpdateDragPosition();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isUIObject) return;
        StopDragging();
    }

    private void StartDragging()
    {
        if (tooltipObject == null || !tooltipObject.activeSelf) return;

        isDragging = true;
        StopHideCoroutine();

        if (isUIObject)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                tooltipObject.GetComponent<RectTransform>(),
                Input.mousePosition,
                null,
                out Vector3 worldPos
            );
            dragOffset = tooltipObject.transform.position - worldPos;
        }
        else
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            dragOffset = tooltipObject.transform.position - mouseWorldPos;
        }
    }

    private void UpdateDragPosition()
    {
        if (!isDragging || tooltipObject == null) return;

        Vector3 newPosition;

        if (isUIObject)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                tooltipObject.GetComponent<RectTransform>(),
                Input.mousePosition,
                null,
                out Vector3 worldPos
            );
            newPosition = worldPos + dragOffset;
        }
        else
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            newPosition = mouseWorldPos + dragOffset;
        }

        tooltipObject.transform.position = newPosition;
    }

    private void StopDragging()
    {
        isDragging = false;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    private void ShowTooltip()
    {
        if (tooltipObject == null)
        {
            Debug.LogWarning("TooltipObject не назначен в инспекторе!");
            return;
        }

        if (currentActiveTooltip != null && currentActiveTooltip != this)
        {
            currentActiveTooltip.StopHideCoroutine();
            currentActiveTooltip.HideTooltip();
        }

        var text = tooltipObject.GetComponentInChildren<TextMeshProUGUI>();
        if (gameAction == null)
            return;
        text.text = gameAction.description;

        currentActiveTooltip = this;
        StopHideCoroutine();
        tooltipObject.SetActive(true);
        PositionTooltip();
    }

    private void PositionTooltip()
    {
        if (tooltipObject == null) return;

        Vector3 position;

        if (isUIObject)
        {
            // Получаем RectTransform канваса
            Canvas canvas = tooltipObject.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
            Vector2 localPoint;
            // Переводим экранные координаты мыши в локальные координаты канваса
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint
            );
            // Устанавливаем позицию тултипа
            tooltipRect.anchoredPosition = localPoint + (Vector2)offset;
        }
        else
        {
            position = GetMouseWorldPosition();
            position += offset;
            tooltipObject.transform.position = position;
        }
    }

    private void StartHideCoroutine()
    {
        if (isDragging) return;

        StopHideCoroutine();
        if (gameObject.activeInHierarchy)
            hideCoroutine = StartCoroutine(HideWithDelay());
    }

    private void StopHideCoroutine()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }

    private IEnumerator HideWithDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        if (currentActiveTooltip == this)
        {
            currentActiveTooltip = null;
        }

        HideTooltip();
    }

    private void HideTooltip()
    {
        if (tooltipObject == null) return;

        tooltipObject.SetActive(false);
    }

    private void OnDestroy()
    {
        StopHideCoroutine();
        HideTooltip();

        if (currentActiveTooltip == this)
        {
            currentActiveTooltip = null;
        }
    }

    private void Reset()
    {
        if (GetComponent<Collider>() == null && !isUIObject)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    private void TryHideTooltip()
    {
        if (!isPointerOverSelf && !isPointerOverTooltip)
        {
            StartHideCoroutine();
        }
    }
}
