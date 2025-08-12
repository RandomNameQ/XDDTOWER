using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static InputSystem_Actions;
using static UnityEngine.InputSystem.InputAction;

/// <summary>
/// Менеджер ввода для новой системы ввода Unity.
/// Реализован как синглтон для глобального доступа.
/// </summary>
public class InputManager : Singleton<InputManager>, IPlayerActions, IUIActions
{
    #region Singleton



    #endregion

    #region Fields

    private InputSystem_Actions _inputActions;
    private bool _isPlayerMapActive;
    private bool _isUIMapActive;

    #endregion

    #region Properties

    public Vector2 MousePosition => _inputActions.Player.Point.ReadValue<Vector2>();
    public bool IsPlayerMapActive => _isPlayerMapActive;
    public bool IsUIMapActive => _isUIMapActive;

    #endregion

    #region Events

    // Player Events
    public event Action<CallbackContext> OnNavigate;
    public event Action<CallbackContext> OnSubmit;
    public event Action<CallbackContext> OnCancel;
    public event Action<CallbackContext> OnPoint;
    public event Action<CallbackContext> OnClick;
    public event Action<CallbackContext> OnRightClick;
    public event Action<CallbackContext> OnMiddleClick;
    public event Action<CallbackContext> OnScrollWheel;
    public event Action<CallbackContext> OnTrackedDevicePosition;
    public event Action<CallbackContext> OnTrackedDeviceOrientation;

    // UI Events
    public event Action<CallbackContext> OnUINavigate;
    public event Action<CallbackContext> OnUISubmit;
    public event Action<CallbackContext> OnUICancel;
    public event Action<CallbackContext> OnUIPoint;
    public event Action<CallbackContext> OnUIClick;
    public event Action<CallbackContext> OnUIRightClick;
    public event Action<CallbackContext> OnUIMiddleClick;
    public event Action<CallbackContext> OnUIScrollWheel;
    public event Action<CallbackContext> OnUITrackedDevicePosition;
    public event Action<CallbackContext> OnUITrackedDeviceOrientation;

    #endregion

    #region Unity Lifecycle

    private new void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        InitializeInputSystem();
    }

    private void OnEnable()
    {
        EnablePlayerControls();
    }

    private void OnDisable()
    {
        DisableAllControls();
    }

    private new void OnDestroy()
    {
        base.OnDestroy();
        if (_inputActions != null)
        {
            DisableAllControls();
            _inputActions.Dispose();
        }
    }

    #endregion

    #region Initialization

    private void InitializeInputSystem()
    {
        _inputActions = new InputSystem_Actions();

        // Регистрация обратных вызовов
        _inputActions.Player.SetCallbacks(this);
        _inputActions.UI.SetCallbacks(this);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Включает управление игрока и отключает управление UI
    /// </summary>
    public void EnablePlayerControls()
    {
        _inputActions.Player.Enable();
        _inputActions.UI.Disable();
        _isPlayerMapActive = true;
        _isUIMapActive = false;
    }

    /// <summary>
    /// Включает управление UI и отключает управление игрока
    /// </summary>
    public void EnableUIControls()
    {
        _inputActions.UI.Enable();
        _inputActions.Player.Disable();
        _isUIMapActive = true;
        _isPlayerMapActive = false;
    }

    /// <summary>
    /// Включает оба набора управления
    /// </summary>
    public void EnableAllControls()
    {
        _inputActions.Player.Enable();
        _inputActions.UI.Enable();
        _isPlayerMapActive = true;
        _isUIMapActive = true;
    }

    /// <summary>
    /// Отключает все управление
    /// </summary>
    public void DisableAllControls()
    {
        _inputActions.Player.Disable();
        _inputActions.UI.Disable();
        _isPlayerMapActive = false;
        _isUIMapActive = false;
    }

    /// <summary>
    /// Проверяет, нажата ли кнопка мыши
    /// </summary>
    /// <returns>true, если кнопка мыши нажата</returns>
    public bool IsMouseButtonPressed()
    {
        return _inputActions.Player.Click.IsPressed();
    }

    /// <summary>
    /// Проверяет, нажата ли правая кнопка мыши
    /// </summary>
    /// <returns>true, если правая кнопка мыши нажата</returns>
    public bool IsRightMouseButtonPressed()
    {
        return _inputActions.Player.RightClick.IsPressed();
    }

    /// <summary>
    /// Получает текущее значение скроллинга
    /// </summary>
    /// <returns>Значение скроллинга как Vector2</returns>
    public Vector2 GetScrollValue()
    {
        return _inputActions.Player.ScrollWheel.ReadValue<Vector2>();
    }

    #endregion

    #region IPlayerActions Implementation

    void IPlayerActions.OnNavigate(InputAction.CallbackContext context)
    {
        OnNavigate?.Invoke(context);
    }

    void IPlayerActions.OnSubmit(InputAction.CallbackContext context)
    {
        OnSubmit?.Invoke(context);
    }

    void IPlayerActions.OnCancel(InputAction.CallbackContext context)
    {
        OnCancel?.Invoke(context);
    }

    void IPlayerActions.OnPoint(InputAction.CallbackContext context)
    {
        OnPoint?.Invoke(context);
    }

    void IPlayerActions.OnClick(InputAction.CallbackContext context)
    {
        OnClick?.Invoke(context);
    }

    void IPlayerActions.OnRightClick(InputAction.CallbackContext context)
    {
        OnRightClick?.Invoke(context);
    }

    void IPlayerActions.OnMiddleClick(InputAction.CallbackContext context)
    {
        OnMiddleClick?.Invoke(context);
    }

    void IPlayerActions.OnScrollWheel(InputAction.CallbackContext context)
    {
        OnScrollWheel?.Invoke(context);
    }

    void IPlayerActions.OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
        OnTrackedDevicePosition?.Invoke(context);
    }

    void IPlayerActions.OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
        OnTrackedDeviceOrientation?.Invoke(context);
    }

    #endregion

    #region IUIActions Implementation

    void IUIActions.OnNavigate(InputAction.CallbackContext context)
    {
        OnUINavigate?.Invoke(context);
    }

    void IUIActions.OnSubmit(InputAction.CallbackContext context)
    {
        OnUISubmit?.Invoke(context);
    }

    void IUIActions.OnCancel(InputAction.CallbackContext context)
    {
        OnUICancel?.Invoke(context);
    }

    void IUIActions.OnPoint(InputAction.CallbackContext context)
    {
        OnUIPoint?.Invoke(context);
    }

    void IUIActions.OnClick(InputAction.CallbackContext context)
    {
        OnUIClick?.Invoke(context);
    }

    void IUIActions.OnRightClick(InputAction.CallbackContext context)
    {
        OnUIRightClick?.Invoke(context);
    }

    void IUIActions.OnMiddleClick(InputAction.CallbackContext context)
    {
        OnUIMiddleClick?.Invoke(context);
    }

    void IUIActions.OnScrollWheel(InputAction.CallbackContext context)
    {
        OnUIScrollWheel?.Invoke(context);
    }

    void IUIActions.OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
        OnUITrackedDevicePosition?.Invoke(context);
    }

    void IUIActions.OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
        OnUITrackedDeviceOrientation?.Invoke(context);
    }

    #endregion

}