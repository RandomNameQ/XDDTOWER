using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Пример использования InputManager в другом классе
/// </summary>
public class InputManagerExample : MonoBehaviour
{
    private void Start()
    {
        // Включить управление игрока
        InputManager.Instance.EnablePlayerControls();
        
        // Подписаться на события
        InputManager.Instance.OnClick += HandleClick;
        InputManager.Instance.OnRightClick += HandleRightClick;
        InputManager.Instance.OnScrollWheel += HandleScrollWheel;
    }

    private void OnDestroy()
    {
        // Отписаться от событий при уничтожении объекта
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnClick -= HandleClick;
            InputManager.Instance.OnRightClick -= HandleRightClick;
            InputManager.Instance.OnScrollWheel -= HandleScrollWheel;
        }
    }

    private void Update()
    {
        // Получить текущую позицию мыши
        Vector2 mousePosition = InputManager.Instance.MousePosition;
        
        // Проверить, нажата ли кнопка мыши
        if (InputManager.Instance.IsMouseButtonPressed())
        {
            Debug.Log("Кнопка мыши удерживается");
        }
    }

    // Обработчики событий
    private void HandleClick(InputAction.CallbackContext context)
    {
        // Обработка нажатия левой кнопки мыши
        if (context.performed)
        {
            Debug.Log("Клик левой кнопкой мыши");
        }
    }

    private void HandleRightClick(InputAction.CallbackContext context)
    {
        // Обработка нажатия правой кнопки мыши
        if (context.performed)
        {
            Debug.Log("Клик правой кнопкой мыши");
        }
    }

    private void HandleScrollWheel(InputAction.CallbackContext context)
    {
        // Обработка прокрутки колесика мыши
        Vector2 scrollValue = context.ReadValue<Vector2>();
        Debug.Log($"Прокрутка: {scrollValue}");
    }

    // Пример переключения между картами действий
    public void SwitchToUIControls()
    {
        InputManager.Instance.EnableUIControls();
    }

    public void SwitchToPlayerControls()
    {
        InputManager.Instance.EnablePlayerControls();
    }
}