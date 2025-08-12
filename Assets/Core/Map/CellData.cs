using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class CellData : MonoBehaviour
{
    public enum CellVariant
    {
        None,
        Shop,
        Monster,
        Start
    }
    public CellVariant cellVariant;
    public GameActionBase action;
    // если true то это клетка которую можно использовать
    public bool isAvailable;
    // если true то это клетка которую уже нельзя использовать
    public bool isUsed;
    public int cellOrder;
    public bool isPlayer;

    public Image image;
    // куда из этой клетки можно попасть
    public List<CellData> connectedCells = new();
    // пути которые соединяют клетки
    public List<GameObject> paths = new();

    private Camera mainCamera;
    private Collider2D cellCollider;
    private Color originalColor;
    private bool isMouseOverThisObject;

    private void Awake()
    {
        InitMouseHandling();
    }



    private void InitMouseHandling()
    {
        mainCamera = Camera.main;
        cellCollider = GetComponent<Collider2D>();
        originalColor = image.color;
    }

    private void Start()
    {
        SubscribeToInputEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromInputEvents();
    }

    private void SubscribeToInputEvents()
    {
        InputManager.Instance.OnClick += HandleClick;
    }

    private void UnsubscribeFromInputEvents()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnClick -= HandleClick;
        }
    }

    private void HandleClick(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

        if (isMouseOverThisObject && isAvailable)
        {
            ExecuteClickLogic();
        }
    }

    private void OnMouseEnter()
    {
        if (isUsed) return;

        isMouseOverThisObject = true;

        if (isAvailable)
        {
            image.color = Color.green;
        }
    }

    private void OnMouseExit()
    {
        isMouseOverThisObject = false;
        image.color = originalColor;
    }

    private void ExecuteClickLogic()
    {
        FindAnyObjectByType<PlayerController>().ChangePositionOnMap(this);
    }

    public void InitAction(GameActionBase action)
    {
        ClearData();
        GetComponent<GameActionSlot>().slot = action;
        this.action = action;
        image.sprite = action.icon;
    }

    public void ClearData()
    {
        // image.sprite = null;
        GetComponent<GameActionSlot>().slot = null;
        action = null;
        isAvailable = false;
        isUsed = false;
        cellOrder = 0;
    }


    public void UpdatePreviousPosition()
    {
        isPlayer = false;
    }

    public void ResetDataAfterMove()
    {
        connectedCells.ForEach(cell => cell.isAvailable = false);
        if (action != null)
            image.sprite = action.icon;
        else
            image.sprite = null;


    }
    public void ChangePosition()
    {
        var allCells = FindObjectsByType<CellData>(FindObjectsSortMode.None);
        foreach (var cell in allCells)
            if (cell != this)
                cell.ResetDataAfterMove();

        var playerController = FindAnyObjectByType<PlayerController>();

        if (playerController != null)
        {
            image.sprite = playerController.image;
        }
        else
        {
            Debug.LogError("PlayerController not found!");
            return;
        }



        isPlayer = true;
        isUsed = true;
        connectedCells.ForEach(cell => cell.isAvailable = true);
    }




}
