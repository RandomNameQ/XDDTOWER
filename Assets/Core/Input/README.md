# Система ввода (Input System)

## Обзор

Эта система ввода представляет собой обертку вокруг новой системы ввода Unity (Input System). Она предоставляет простой и понятный интерфейс для получения данных ввода и подписки на события ввода.

## Основные компоненты

### InputManager

`InputManager` - это синглтон, который управляет всеми входными данными в игре. Он реализует интерфейсы `IPlayerActions` и `IUIActions` для обработки событий ввода.

## Использование

### Доступ к InputManager

```csharp
// Получение экземпляра InputManager
InputManager inputManager = InputManager.Instance;
```

### Переключение между картами действий

```csharp
// Включить управление игрока
InputManager.Instance.EnablePlayerControls();

// Включить управление UI
InputManager.Instance.EnableUIControls();

// Включить оба набора управления
InputManager.Instance.EnableAllControls();

// Отключить все управление
InputManager.Instance.DisableAllControls();
```

### Подписка на события

```csharp
// Подписка на событие клика
InputManager.Instance.OnClick += HandleClick;

// Обработчик события
private void HandleClick(InputAction.CallbackContext context)
{
    if (context.performed)
    {
        Debug.Log("Клик левой кнопкой мыши");
    }
}

// Не забудьте отписаться при уничтожении объекта
private void OnDestroy()
{
    if (InputManager.Instance != null)
    {
        InputManager.Instance.OnClick -= HandleClick;
    }
}
```

### Получение данных ввода

```csharp
// Получить текущую позицию мыши
Vector2 mousePosition = InputManager.Instance.MousePosition;

// Проверить, нажата ли кнопка мыши
bool isPressed = InputManager.Instance.IsMouseButtonPressed();

// Проверить, нажата ли правая кнопка мыши
bool isRightPressed = InputManager.Instance.IsRightMouseButtonPressed();

// Получить значение скроллинга
Vector2 scrollValue = InputManager.Instance.GetScrollValue();
```

## Добавление новых действий ввода

1. Откройте файл `InputSystem_Actions.inputactions` в редакторе Unity
2. Добавьте новое действие в соответствующую карту действий (Player или UI)
3. Сохраните файл, чтобы Unity сгенерировала новый код
4. Добавьте новое событие в класс `InputManager`
5. Реализуйте соответствующий метод интерфейса и вызовите событие

## Пример полного использования

См. файл `InputManagerExample.cs` для примера использования `InputManager` в другом классе.