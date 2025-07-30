using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, InputSystem_Actions.IPlayerActions, InputSystem_Actions.IUIActions
{
    // Синглтон-экземпляр
    private static InputManager _instance;
    public static InputManager Instance => _instance;

    // Действия ввода
    private InputSystem_Actions _inputActions;
    public InputSystem_Actions InputActions => _inputActions;

    // Состояние карт действий
    private bool _playerActionsEnabled = true;
    private bool _uiActionsEnabled = true;

    #region События Player Actions

    // События для Player Actions (не статические)
    public event Action<Vector2> OnMoveEvent;
    public event Action<Vector2> OnLookEvent;
    public event Action OnAttackEvent;
    public event Action OnAttackCanceledEvent;
    public event Action OnInteractEvent;
    public event Action OnInteractCanceledEvent;
    public event Action OnCrouchEvent;
    public event Action OnCrouchCanceledEvent;
    public event Action OnJumpEvent;
    public event Action OnJumpCanceledEvent;
    public event Action OnPreviousEvent;
    public event Action OnNextEvent;
    public event Action OnSprintEvent;
    public event Action OnSprintCanceledEvent;

    #endregion

    #region События UI Actions

    // События для UI Actions (не статические)
    public event Action<Vector2> OnNavigateEvent;
    public event Action OnSubmitEvent;
    public event Action OnCancelEvent;
    public event Action<Vector2> OnPointEvent;
    public event Action OnLeftClickEvent;
    public event Action OnLeftClickCanceledEvent;
    public event Action OnRightClickEvent;
    public event Action OnMiddleClickEvent;
    public event Action<Vector2> OnScrollWheelEvent;

    #endregion

    // Свойства для получения текущего состояния ввода
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsAttacking { get; private set; }
    public bool IsInteracting { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsSprinting { get; private set; }

    private void Awake()
    {
        // Проверка на дубликаты синглтона
        if (_instance != null && _instance != this)
        {
            Debug.Log("InputManager already exists");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        // DontDestroyOnLoad(gameObject);

        // Инициализация действий ввода
        _inputActions = new InputSystem_Actions();
        _inputActions.Player.SetCallbacks(this);
        _inputActions.UI.SetCallbacks(this);
    }

    private void OnEnable()
    {
        EnableAllActions();
    }

    private void OnDisable()
    {
        DisableAllActions();
    }

    #region Управление картами действий

    public void EnableAllActions()
    {
        _inputActions.Enable();
        _playerActionsEnabled = true;
        _uiActionsEnabled = true;
    }

    public void DisableAllActions()
    {
        _inputActions.Disable();
        _playerActionsEnabled = false;
        _uiActionsEnabled = false;
    }

    public void EnablePlayerActions()
    {
        _inputActions.Player.Enable();
        _playerActionsEnabled = true;
    }

    public void DisablePlayerActions()
    {
        _inputActions.Player.Disable();
        _playerActionsEnabled = false;
    }

    public void EnableUIActions()
    {
        _inputActions.UI.Enable();
        _uiActionsEnabled = true;
    }

    public void DisableUIActions()
    {
        _inputActions.UI.Disable();
        _uiActionsEnabled = false;
    }

    public bool ArePlayerActionsEnabled() => _playerActionsEnabled;
    public bool AreUIActionsEnabled() => _uiActionsEnabled;

    #endregion

    #region Методы для IPlayerActions

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
        OnMoveEvent?.Invoke(MoveInput);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
        OnLookEvent?.Invoke(LookInput);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsAttacking = true;
            OnAttackEvent?.Invoke();
        }
        else if (context.canceled)
        {
            IsAttacking = false;
            OnAttackCanceledEvent?.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsInteracting = true;
            OnInteractEvent?.Invoke();
        }
        else if (context.canceled)
        {
            IsInteracting = false;
            OnInteractCanceledEvent?.Invoke();
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsCrouching = true;
            OnCrouchEvent?.Invoke();
        }
        else if (context.canceled)
        {
            IsCrouching = false;
            OnCrouchCanceledEvent?.Invoke();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsJumping = true;
            OnJumpEvent?.Invoke();
        }
        else if (context.canceled)
        {
            IsJumping = false;
            OnJumpCanceledEvent?.Invoke();
        }
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnPreviousEvent?.Invoke();
        }
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnNextEvent?.Invoke();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsSprinting = true;
            OnSprintEvent?.Invoke();
        }
        else if (context.canceled)
        {
            IsSprinting = false;
            OnSprintCanceledEvent?.Invoke();
        }
    }

    #endregion

    #region Методы для IUIActions

    public void OnNavigate(InputAction.CallbackContext context)
    {
        OnNavigateEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnSubmitEvent?.Invoke();
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnCancelEvent?.Invoke();
        }
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
        OnPointEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnLeftClickEvent?.Invoke();
        }
        else if (context.canceled)
        {
            OnLeftClickCanceledEvent?.Invoke();
        }
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnRightClickEvent?.Invoke();
        }
    }

    public void OnMiddleClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnMiddleClickEvent?.Invoke();
        }
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        OnScrollWheelEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
        // Не используется в данной реализации
    }

    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
        // Не используется в данной реализации
    }

    #endregion

    #region Вспомогательные методы для проверки состояния ввода

    // Методы для проверки состояния кнопок
    public bool GetMouse1ButtonDown() => _inputActions.Player.Mouse1.WasPressedThisFrame();
    public bool GetMouse1Button() => _inputActions.Player.Mouse1.IsPressed();
    public bool GetMouse2ButtonDown() => _inputActions.Player.Mouse2.WasPressedThisFrame();
    public bool GetMouse2Button() => _inputActions.Player.Mouse2.IsPressed();
    public bool GetJumpButtonDown() => _inputActions.Player.Jump.WasPressedThisFrame();
    public bool GetJumpButton() => _inputActions.Player.Jump.IsPressed();
    public bool GetInteractButtonDown() => _inputActions.Player.Interact.WasPressedThisFrame();
    public bool GetInteractButton() => _inputActions.Player.Interact.IsPressed();
    public bool GetSprintButtonDown() => _inputActions.Player.Sprint.WasPressedThisFrame();
    public bool GetSprintButton() => _inputActions.Player.Sprint.IsPressed();
    public bool GetPreviousButtonDown() => _inputActions.Player.Previous.WasPressedThisFrame();
    public bool GetNextButtonDown() => _inputActions.Player.Next.WasPressedThisFrame();
    public bool GetCrouchButtonDown() => _inputActions.Player.Crouch.WasPressedThisFrame();
    public bool GetCrouchButton() => _inputActions.Player.Crouch.IsPressed();

    // Получение векторов движения и взгляда
    public Vector2 GetMoveVector() => MoveInput;
    public Vector2 GetLookVector() => LookInput;

    public void OnMouse1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnLeftClickEvent?.Invoke();
        }
    }

    public void OnMouse2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnRightClickEvent?.Invoke();
        }
    }

    #endregion
}
