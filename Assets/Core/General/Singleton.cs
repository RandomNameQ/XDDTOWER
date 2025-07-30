using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    // Свойство для доступа к экземпляру
    public static T Q
    {
        get
        {
            // Если экземпляр уже существует, возвращаем его
            if (_instance != null)
                return _instance;

            // Ищем существующий экземпляр на сцене
            _instance = FindAnyObjectByType<T>();

            // Если не нашли, создаем новый
            if (_instance == null)
            {
                GameObject singletonObject = new GameObject($"[Singleton] {typeof(T).Name}");
                _instance = singletonObject.AddComponent<T>();
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // Если экземпляр еще не задан, делаем текущий объект синглтоном
        if (_instance == null)
        {
            _instance = this as T;
        }
        // Если экземпляр уже существует и это не текущий объект - уничтожаем дубликат
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Уничтожаем дубликат {typeof(T)}");
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        // Если уничтожается текущий экземпляр, обнуляем ссылку
        if (_instance == this)
        {
            _instance = null;
        }
    }
}