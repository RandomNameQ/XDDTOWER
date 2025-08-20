# 📚 LINQ Руководство для Unity

## 🎯 Что такое LINQ?

**LINQ (Language Integrated Query)** - это мощная технология Microsoft для работы с коллекциями данных в C#. Позволяет писать читаемые и эффективные запросы к данным.

---

## 🚀 Основные концепции

### 📋 IEnumerable<T>
Базовый интерфейс для всех коллекций, с которыми работает LINQ:
- `List<T>`
- `Array`
- `Dictionary<TKey, TValue>`
- `HashSet<T>`

---

## 🔧 Основные методы LINQ (по популярности)

### 🥇 1. Фильтрация данных

#### `Where` - Фильтрация элементов
// 🟢 Фильтруем числа больше 5
List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
List<int> filteredNumbers = numbers.Where(n => n > 5).ToList();
// Результат: [6, 7, 8, 9, 10]

// 🟢 Фильтруем объекты по условию
List<Player> players = GetPlayers();
List<Player> activePlayers = players.Where(p => p.IsActive && p.Health > 0).ToList();

#### `First` / `FirstOrDefault` - Первый элемент
// 🟢 Получаем первый элемент, удовлетворяющий условию
Player firstPlayer = players.First(p => p.Level > 10);
Player firstOrDefault = players.FirstOrDefault(p => p.Level > 100); // null если не найден

#### `Last` / `LastOrDefault` - Последний элемент
Player lastPlayer = players.Last(p => p.Level > 5);
Player lastOrDefault = players.LastOrDefault(p => p.Level > 50);

### 🥈 2. Поиск элементов

#### `Any` - Проверка существования
// 🟢 Проверяем, есть ли хотя бы один элемент
bool hasHighLevelPlayer = players.Any(p => p.Level > 20);
bool hasAnyPlayers = players.Any(); // проверяем, не пуста ли коллекция

#### `All` - Проверка всех элементов
// 🟢 Проверяем, все ли элементы удовлетворяют условию
bool allPlayersActive = players.All(p => p.IsActive);
bool allPlayersHaveHealth = players.All(p => p.Health > 0);

#### `Contains` - Содержит ли коллекция элемент
bool hasPlayer = players.Contains(specificPlayer);
bool hasNumber = numbers.Contains(5);

### 🥉 3. Преобразование данных

#### `Select` - Проекция элементов
// 🟢 Преобразуем Player в PlayerInfo
List<PlayerInfo> playerInfos = players.Select(p => new PlayerInfo 
{
    Name = p.Name,
    Level = p.Level,
    IsActive = p.IsActive
}).ToList();

// 🟢 Получаем только имена игроков
List<string> playerNames = players.Select(p => p.Name).ToList();

// 🟢 Преобразуем числа в строки
List<string> numberStrings = numbers.Select(n => $"Число: {n}").ToList();

#### `SelectMany` - Разворачивание коллекций
// 🟢 Получаем все предметы всех игроков
List<Item> allItems = players.SelectMany(p => p.Inventory).ToList();

// 🟢 Разворачиваем вложенные массивы
List<List<int>> nestedLists = new List<List<int>> 
{
    new List<int> { 1, 2, 3 },
    new List<int> { 4, 5, 6 }
};
List<int> flattened = nestedLists.SelectMany(list => list).ToList(); // [1, 2, 3, 4, 5, 6]

### 🏅 4. Сортировка

#### `OrderBy` / `OrderByDescending` - Сортировка
// 🟢 Сортировка по возрастанию
List<Player> sortedPlayers = players.OrderBy(p => p.Level).ToList();

// 🟢 Сортировка по убыванию
List<Player> sortedDescending = players.OrderByDescending(p => p.Level).ToList();

// 🟢 Множественная сортировка
List<Player> multiSorted = players
    .OrderBy(p => p.Level)
    .ThenBy(p => p.Name)
    .ToList();

### 🎖️ 5. Агрегация данных

#### `Count` - Подсчет элементов
int totalPlayers = players.Count();
int activePlayersCount = players.Count(p => p.IsActive);
int highLevelPlayers = players.Count(p => p.Level > 10);

#### `Sum` - Сумма значений
int totalHealth = players.Sum(p => p.Health);
int totalExperience = players.Sum(p => p.Experience);

#### `Average` - Среднее значение
double averageLevel = players.Average(p => p.Level);
double averageHealth = players.Average(p => p.Health);

#### `Min` / `Max` - Минимальное/максимальное значение
int minLevel = players.Min(p => p.Level);
int maxLevel = players.Max(p => p.Level);
int minHealth = players.Min(p => p.Health);
int maxHealth = players.Max(p => p.Health);

### 🔗 6. Объединение коллекций

#### `Concat` - Объединение
// 🟢 Объединение
List<int> list1 = new List<int> { 1, 2, 3 };
List<int> list2 = new List<int> { 4, 5, 6 };
List<int> combined = list1.Concat(list2).ToList(); // [1, 2, 3, 4, 5, 6]

#### `Union` - Объединение без дубликатов
List<int> uniqueNumbers = list1.Union(list2).ToList();

#### `Intersect` - Пересечение
List<int> list3 = new List<int> { 2, 3, 4 };
List<int> common = list1.Intersect(list3).ToList(); // [2, 3]

#### `Except` - Разность
List<int> difference = list1.Except(list3).ToList(); // [1]

---

## 🎮 Практические примеры для Unity

### 🎯 Работа с игровыми объектами
// 🟢 Получаем все активные враги в радиусе
List<Enemy> nearbyEnemies = FindObjectsOfType<Enemy>()
    .Where(e => Vector3.Distance(transform.position, e.transform.position) < 10f)
    .Where(e => e.IsActive)
    .ToList();

// 🟢 Получаем ближайшего врага
Enemy closestEnemy = nearbyEnemies
    .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
    .FirstOrDefault();

// 🟢 Получаем всех врагов с высоким здоровьем
List<Enemy> strongEnemies = nearbyEnemies
    .Where(e => e.Health > 50)
    .OrderByDescending(e => e.Health)
    .ToList();

### 🎨 Работа с UI элементами
// 🟢 Получаем все кнопки и подписываемся на события
List<Button> buttons = GetComponentsInChildren<Button>();
buttons.ForEach(button => button.onClick.AddListener(() => OnButtonClick(button)));

// 🟢 Получаем все текстовые поля
List<TextMeshProUGUI> textFields = GetComponentsInChildren<TextMeshProUGUI>()
    .Where(t => t.gameObject.name.Contains("Score"))
    .ToList();

### 🎵 Работа с аудио
// 🟢 Получаем все аудио источники
List<AudioSource> audioSources = GetComponentsInChildren<AudioSource>();

// 🟢 Проверяем, играет ли музыка
bool isMusicPlaying = audioSources.Any(a => a.isPlaying && a.clip.name.Contains("Music"));

// 🟢 Получаем все звуковые эффекты
List<AudioSource> soundEffects = audioSources
    .Where(a => a.clip.name.Contains("SFX"))
    .ToList();

---

## ⚡ Оптимизация производительности

### 🚫 Избегайте множественных итераций
// 🟢 ❌ Плохо - множественные итерации
List<Player> activePlayers = players.Where(p => p.IsActive).ToList();
List<Player> highLevelPlayers = activePlayers.Where(p => p.Level > 10).ToList();
List<Player> sortedPlayers = highLevelPlayers.OrderBy(p => p.Level).ToList();

// 🟢 ✅ Хорошо - одна итерация
List<Player> result = players
    .Where(p => p.IsActive)
    .Where(p => p.Level > 10)
    .OrderBy(p => p.Level)
    .ToList();

### 🔄 Используйте ToList() только когда нужно
// 🟢 ❌ Плохо - создаем лишний список
List<Player> filtered = players.Where(p => p.IsActive).ToList();
foreach (Player player in filtered) { }

// 🟢 ✅ Хорошо - итерируемся напрямую
foreach (Player player in players.Where(p => p.IsActive)) { }

### 📊 Кэшируйте результаты для сложных вычислений
// 🟢 Кэшируем результат для многократного использования
private List<Player> _cachedActivePlayers;

private void UpdateActivePlayers()
{
    _cachedActivePlayers = players
        .Where(p => p.IsActive)
        .Where(p => p.Health > 0)
        .OrderBy(p => p.Level)
        .ToList();
}

---

## 🎯 Продвинутые техники

### 🔄 Группировка данных
// 🟢 Группируем игроков по уровню
Dictionary<int, List<Player>> playersByLevel = players
    .GroupBy(p => p.Level)
    .ToDictionary(g => g.Key, g => g.ToList());

// 🟢 Группируем по диапазону уровней
Dictionary<string, List<Player>> playersByLevelRange = players
    .GroupBy(p => p.Level switch
    {
        < 10 => "Новичок",
        < 20 => "Опытный",
        < 30 => "Мастер",
        _ => "Легенда"
    })
    .ToDictionary(g => g.Key, g => g.ToList());

### 🔍 Условная фильтрация
// 🟢 Динамическая фильтрация
IQueryable<Player> filteredPlayers = players.AsQueryable();

if (showOnlyActive)
    filteredPlayers = filteredPlayers.Where(p => p.IsActive);

if (minLevel > 0)
    filteredPlayers = filteredPlayers.Where(p => p.Level >= minLevel);

if (maxLevel > 0)
    filteredPlayers = filteredPlayers.Where(p => p.Level <= maxLevel);

List<Player> result = filteredPlayers.ToList();

### 📊 Агрегация с группировкой
// 🟢 Статистика по уровням
List<object> levelStats = players
    .GroupBy(p => p.Level)
    .Select(g => new
    {
        Level = g.Key,
        Count = g.Count(),
        AverageHealth = g.Average(p => p.Health),
        TotalExperience = g.Sum(p => p.Experience)
    })
    .OrderBy(s => s.Level)
    .ToList();

---

## 🚨 Частые ошибки и их решения

### ❌ Ошибка: Изменение коллекции во время итерации
// 🟢 ❌ Плохо - может вызвать исключение
foreach (Player player in players)
{
    if (player.Health <= 0)
        players.Remove(player); // Ошибка!
}

// 🟢 ✅ Хорошо - используем ToList() для копии
foreach (Player player in players.ToList())
{
    if (player.Health <= 0)
        players.Remove(player);
}

// 🟢 ✅ Альтернатива - используем RemoveAll
players.RemoveAll(p => p.Health <= 0);

### ❌ Ошибка: Неправильное использование First
// 🟢 ❌ Плохо - исключение если элемент не найден
Player player = players.First(p => p.Level > 100);

// 🟢 ✅ Хорошо - используем FirstOrDefault
Player player = players.FirstOrDefault(p => p.Level > 100);
if (player != null)
{
    // Работаем с игроком
}

---

## 📚 Полезные ссылки

- [Microsoft LINQ Documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/)
- [101 LINQ Samples](https://github.com/dotnet/try-samples/tree/main/101-linq-samples)
- [Unity C# Best Practices](https://unity.com/how-to/unity-c-best-practices)

---

## 🎉 Заключение

LINQ - это мощный инструмент для работы с данными в C#. Используйте его для:
- ✨ Улучшения читаемости кода
- 🚀 Повышения производительности
- 🔧 Упрощения сложных операций с коллекциями
- 🎯 Создания более элегантного и поддерживаемого кода

Помните: **читаемость кода важнее краткости!** 🎯
