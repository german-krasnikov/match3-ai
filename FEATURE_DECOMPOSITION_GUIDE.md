# Unity Feature Decomposition Guide
## Полное Руководство по Декомпозиции Фич на Компоненты

---

## 📋 Содержание

1. [Мастер Чек-Лист Декомпозиции](#мастер-чек-лист-декомпозиции)
2. [Пошаговый План Декомпозиции](#пошаговый-план-декомпозиции)
3. [Анализ Данных и Логики](#анализ-данных-и-логики)
4. [Уровни Сложности Механик](#уровни-сложности-механик)
5. [Простые Механики (Уровень 1)](#простые-механики-уровень-1)
6. [Средние Механики (Уровень 2)](#средние-механики-уровень-2)
7. [Сложные Механики (Уровень 3)](#сложные-механики-уровень-3)
8. [Продвинутые Механики (Уровень 4)](#продвинутые-механики-уровень-4)
9. [Шаблоны и Паттерны](#шаблоны-и-паттерны)
10. [Примеры из 3D Шутера (GameShooter)](#примеры-из-3d-шутера-gameshooter)
    - [Система Стрельбы](#пример-1-система-стрельбы-уровень-2--средняя)
    - [Автоматическая Турель (AI)](#пример-2-автоматическая-турель-уровень-3--сложная)
    - [Система Слотов Оружия](#пример-3-система-слотов-оружия-паттерн)
    - [Сравнение: Игрок vs AI](#сравнение-игрок-vs-ai-один-набор-компонентов)
    - [Чеклист для Шутера](#чеклист-для-шутера)

---

## Мастер Чек-Лист Декомпозиции

### 📝 Фаза 1: Анализ Требований

```
□ Определена цель фичи (одним предложением)
□ Список всех действий, которые должна выполнять фича
□ Список всех состояний, в которых может находиться фича
□ Определены входные данные (что влияет на фичу)
□ Определены выходные данные (что фича изменяет)
□ Определены внешние зависимости (другие системы)
□ Определены условия активации/деактивации фичи
```

### 🔍 Фаза 2: Идентификация Аспектов

```
□ ДАННЫЕ: Какие данные нужно хранить?
  □ Статические параметры (настройки)
  □ Динамическое состояние (меняется в runtime)
  □ Ссылки на другие объекты

□ ЛОГИКА: Какая логика требуется?
  □ Проверки условий (validation)
  □ Вычисления (calculations)
  □ Управление состоянием (state management)
  □ Обработка событий (event handling)

□ ФИЗИКА: Нужна ли физика?
  □ Движение (movement)
  □ Силы (forces)
  □ Коллизии (collisions)

□ ВИЗУАЛИЗАЦИЯ: Нужна ли визуализация?
  □ Анимация (animations)
  □ Эффекты (VFX)
  □ UI обновления

□ АУДИО: Нужен ли звук?
  □ Звуковые эффекты
  □ Музыка

□ ВВОД: Откуда приходят команды?
  □ Игрок (input)
  □ AI (artificial intelligence)
  □ Сеть (network)
  □ Автоматика (triggers)
```

### 🧩 Фаза 3: Декомпозиция на Компоненты

```
□ Каждый аспект выделен в отдельный компонент?
□ Компоненты соответствуют Single Responsibility?
□ Нет дублирования логики между компонентами?
□ Компоненты можно переиспользовать?
□ Зависимости между компонентами минимальны?
□ Определены интерфейсы для полиморфизма?
□ Определены события для коммуникации?
```

### 🏗️ Фаза 4: Проектирование Архитектуры

```
□ Создана диаграмма компонентов
□ Определены потоки данных между компонентами
□ Определены события и их подписчики
□ Определены условия для каждого компонента
□ Создан оркестратор (главный игровой объект)
□ Определен порядок инициализации
```

### ✅ Фаза 5: Валидация Дизайна

```
□ Каждый компонент делает только одну вещь?
□ Компоненты независимы друг от друга?
□ Можно добавить/удалить компонент без поломки системы?
□ Легко тестировать каждый компонент изолированно?
□ Нет циклических зависимостей?
□ Соблюдены все SOLID принципы?
□ Нет "God Objects"?
```

---

## Пошаговый План Декомпозиции

### Шаг 1: Описание Фичи

**Цель:** Четко сформулировать что должна делать фича.

**Шаблон:**
```
ФИЧА: [Название фичи]

ОПИСАНИЕ: [Одним предложением что делает фича]

ДЕЙСТВИЯ:
- [Действие 1]
- [Действие 2]
- ...

УСЛОВИЯ:
- Когда активна: [условия]
- Когда неактивна: [условия]

ЗАВИСИМОСТИ:
- [Зависимость 1]
- [Зависимость 2]
```

**Пример:**
```
ФИЧА: Система Dash (Рывок)

ОПИСАНИЕ: Персонаж может совершить быстрый рывок в направлении движения

ДЕЙСТВИЯ:
- Применить мгновенное ускорение в направлении
- Сделать персонажа неуязвимым на время рывка
- Проиграть анимацию рывка
- Проиграть звук рывка
- Оставить след из частиц
- Начать cooldown после рывка

УСЛОВИЯ:
- Когда активна: персонаж жив, cooldown готов, есть выносливость
- Когда неактивна: персонаж мертв, в cooldown, нет выносливости

ЗАВИСИМОСТИ:
- LifeComponent (проверка жив ли)
- StaminaComponent (расход выносливости)
```

### Шаг 2: Анализ Аспектов

**Матрица анализа:**

| Аспект | Есть? | Описание | Данные | Логика |
|--------|-------|----------|--------|--------|
| Данные | ✓ | Сила рывка, длительность, cooldown | float _dashForce, float _dashDuration, float _cooldown | - |
| Физика | ✓ | Применение силы к Rigidbody | Rigidbody2D _rigidbody | rb.AddForce() |
| Состояние | ✓ | Tracking когда можно использовать | bool _isReady, float _timer | Проверка timer >= cooldown |
| Иммунитет | ✓ | Временная неуязвимость | bool _isInvulnerable | Установка флага |
| Анимация | ✓ | Визуальный эффект | AnimationClip _dashAnim | animator.Play() |
| VFX | ✓ | След частиц | ParticleSystem _trail | particles.Play() |
| Аудио | ✓ | Звук рывка | AudioClip _dashSound | audioSource.Play() |
| Ввод | ✓ | Команда на рывок | KeyCode | Input.GetKeyDown() |
| Ресурсы | ✓ | Расход выносливости | int _staminaCost | stamina -= cost |

### Шаг 3: Группировка в Компоненты

**Правила группировки:**

1. **Если аспект универсален** → Отдельный переиспользуемый компонент
2. **Если аспекты тесно связаны** → Объединить в один компонент
3. **Если аспект специфичен для фичи** → Специализированный компонент

**Для Dash системы:**

```
ГРУППА 1: Физика рывка + Состояние
→ DashComponent (основная логика)

ГРУППА 2: Cooldown
→ ReloadComponent (переиспользуемый!)

ГРУППА 3: Иммунитет
→ InvulnerabilityComponent (переиспользуемый!)

ГРУППА 4: Анимация
→ DashAnimationComponent

ГРУППА 5: VFX
→ TrailEffectComponent (переиспользуемый!)

ГРУППА 6: Аудио
→ AudioComponent (переиспользуемый!)

ГРУППА 7: Ввод
→ PlayerController (уже существует)

ГРУППА 8: Ресурсы
→ StaminaComponent (переиспользуемый!)
```

### Шаг 4: Определение Интерфейсов

**Когда нужен интерфейс?**

```
□ Компонент взаимодействует с разными типами объектов
□ Нужна полиморфная обработка
□ Множество реализаций одного контракта
```

**Для Dash системы:**

```csharp
// Не нужен - DashComponent специфичен для одного типа объектов
// Но можно создать для расширяемости:

public interface IDashable
{
    event Action OnDashStart;
    event Action OnDashEnd;
    bool CanDash();
    void Dash(Vector2 direction);
}
```

### Шаг 5: Определение События

**Какие события публиковать?**

```
□ Начало важного действия
□ Конец важного действия
□ Изменение состояния
□ Критические моменты (успех/провал)
```

**Для Dash системы:**

```csharp
public class DashComponent : MonoBehaviour
{
    public event Action OnDashStart;      // Начало рывка
    public event Action OnDashEnd;        // Конец рывка
    public event Action OnDashCancelled;  // Отмена рывка
}
```

### Шаг 6: Определение Условий

**Для каждого компонента определить условия работы:**

```csharp
// DashComponent может работать только если:
_dashComponent.AddCondition(_lifeComponent.IsAlive);           // Жив
_dashComponent.AddCondition(_reloadComponent.IsReady);         // Cooldown готов
_dashComponent.AddCondition(_staminaComponent.HasStamina);     // Есть выносливость
_dashComponent.AddCondition(() => !_isStunned);                // Не оглушён
```

### Шаг 7: Создание Диаграммы

```
┌─────────────────────────────────────────────────────────┐
│                    Character (Orchestrator)              │
│  - Подписывается на события                             │
│  - Настраивает условия                                  │
│  - Координирует компоненты                              │
└───────────────────┬─────────────────────────────────────┘
                    │
    ┌───────────────┼───────────────┬────────────────┐
    │               │               │                │
    ▼               ▼               ▼                ▼
┌─────────┐   ┌──────────┐   ┌──────────┐   ┌──────────────┐
│  Dash   │   │ Reload   │   │ Stamina  │   │     Life     │
│Component│   │Component │   │Component │   │  Component   │
└────┬────┘   └──────────┘   └──────────┘   └──────────────┘
     │
     │ OnDashStart
     │ OnDashEnd
     ▼
┌──────────────────────────────────────┐
│         Event Subscribers            │
├──────────────────────────────────────┤
│ - DashAnimationComponent             │
│ - TrailEffectComponent               │
│ - AudioComponent                     │
│ - InvulnerabilityComponent           │
└──────────────────────────────────────┘
```

### Шаг 8: Определение Порядка Инициализации

```csharp
// 1. Awake() - Настройка условий и зависимостей
private void Awake()
{
    _dashComponent.AddCondition(_lifeComponent.IsAlive);
    _dashComponent.AddCondition(_reloadComponent.IsReady);
    _dashComponent.AddCondition(_staminaComponent.HasStamina);
}

// 2. OnEnable() - Подписка на события
private void OnEnable()
{
    _dashComponent.OnDashStart += OnDashStart;
    _dashComponent.OnDashEnd += OnDashEnd;
}

// 3. Start() - Дополнительная инициализация (если нужна)
private void Start()
{
    // Настройки после того как все компоненты готовы
}

// 4. OnDisable() - Отписка от событий
private void OnDisable()
{
    _dashComponent.OnDashStart -= OnDashStart;
    _dashComponent.OnDashEnd -= OnDashEnd;
}
```

---

## Анализ Данных и Логики

### Типы Данных в Компонентах

#### 1. Конфигурационные Данные (Settings)

**Что это:** Параметры, которые настраиваются в Inspector и не меняются в runtime.

```csharp
[Header("Settings")]
[SerializeField] private float _dashForce = 15f;
[SerializeField] private float _dashDuration = 0.2f;
[SerializeField] private float _cooldownTime = 1f;
[SerializeField] private int _staminaCost = 20;
```

**Правила:**
- Всегда `[SerializeField] private`
- Группировать под `[Header("Settings")]`
- Значения по умолчанию для удобства
- Не менять в runtime (если только через специальные методы)

#### 2. Динамическое Состояние (State)

**Что это:** Данные, которые меняются во время игры.

```csharp
private bool _isDashing;
private float _dashTimer;
private Vector2 _dashDirection;
private int _currentStamina;
```

**Правила:**
- Всегда `private`
- Изменяются только внутри компонента
- Доступ через методы или свойства
- Публиковать события при изменении

#### 3. Ссылки на Зависимости (References)

**Что это:** Ссылки на другие компоненты или объекты.

```csharp
[Header("Dependencies")]
[SerializeField] private Rigidbody2D _rigidbody;
[SerializeField] private Animator _animator;
[SerializeField] private ParticleSystem _trailEffect;
```

**Правила:**
- Всегда `[SerializeField] private`
- Группировать под `[Header("Dependencies")]`
- Связывать через Inspector (не GetComponent в runtime)
- Валидировать в `Awake()` или `OnValidate()`

#### 4. Кэшированные Вычисления (Cache)

**Что это:** Результаты вычислений, которые дорого пересчитывать каждый кадр.

```csharp
private Vector2 _cachedVelocity;
private float _cachedDistance;
private Transform _cachedTarget;
```

**Правила:**
- Обновлять только когда необходимо
- Помечать когда становятся невалидными
- Использовать для оптимизации

### Типы Логики в Компонентах

#### 1. Validation (Проверка Условий)

**Цель:** Определить можно ли выполнить действие.

```csharp
public bool CanDash()
{
    return _lifeComponent.IsAlive()
        && _reloadComponent.IsReady()
        && _staminaComponent.HasEnough(_staminaCost)
        && !_isDashing;
}
```

**Паттерн:**
- Метод возвращает `bool`
- Имя начинается с `Can` или `Is`
- Не изменяет состояние
- Быстрая проверка без побочных эффектов

#### 2. Calculation (Вычисления)

**Цель:** Вычислить значение на основе данных.

```csharp
private Vector2 CalculateDashDirection()
{
    Vector2 inputDirection = _moveComponent.MoveDirection;
    if (inputDirection == Vector2.zero)
    {
        inputDirection = _lookComponent.Direction;
    }
    return inputDirection.normalized;
}
```

**Паттерн:**
- Метод возвращает вычисленное значение
- Имя начинается с `Calculate` или `Get`
- Не изменяет состояние (желательно)
- Pure function когда возможно

#### 3. State Management (Управление Состоянием)

**Цель:** Изменить состояние компонента.

```csharp
public void Dash(Vector2 direction)
{
    // Изменяем состояние
    _isDashing = true;
    _dashDirection = direction;
    _dashTimer = _dashDuration;

    // Применяем эффект
    ApplyDashPhysics();

    // Уведомляем
    OnDashStart?.Invoke();
}
```

**Паттерн:**
- Публичный метод (API компонента)
- Изменяет состояние
- Публикует события
- Имя = глагол (действие)

#### 4. Event Handling (Обработка Событий)

**Цель:** Реагировать на события других компонентов.

```csharp
private void OnDashStart()
{
    _staminaComponent.Consume(_staminaCost);
    _invulnerabilityComponent.EnableFor(_dashDuration);
    _dashAnimationComponent.PlayDashAnimation();
    _audioComponent.Play(_dashSound);
}
```

**Паттерн:**
- Приватный метод
- Имя начинается с `On`
- Координирует другие компоненты
- Подписывается в `OnEnable()`, отписывается в `OnDisable()`

#### 5. Update Logic (Логика Обновления)

**Цель:** Обновлять состояние каждый кадр.

```csharp
private void Update()
{
    if (_isDashing)
    {
        _dashTimer -= Time.deltaTime;

        if (_dashTimer <= 0)
        {
            EndDash();
        }
    }
}
```

**Паттерн:**
- В `Update()`, `FixedUpdate()`, или `LateUpdate()`
- Только необходимая логика
- Ранний выход если неактивен
- Делегировать в приватные методы для читаемости

### Матрица: Где Какая Логика Живет

| Тип Логики | Где размещать | Пример |
|------------|---------------|--------|
| **Проверка условий** | В компоненте, который владеет данными | `LifeComponent.IsAlive()` |
| **Вычисления значений** | В компоненте, который использует результат | `DashComponent.CalculateDashDirection()` |
| **Применение эффектов** | В специализированном компоненте | `DashComponent.ApplyDashPhysics()` |
| **Координация** | В оркестраторе (GameObject) | `Character.OnDashStart()` |
| **Ввод** | В контроллере | `PlayerController.Update()` |
| **Визуализация** | В компоненте анимации/VFX | `DashAnimationComponent` |
| **Аудио** | В универсальном аудио компоненте | `AudioComponent.Play()` |
| **Физика** | В компоненте физики или основном компоненте | `DashComponent` с ссылкой на `Rigidbody2D` |

### Принцип Размещения Логики

```
ВОПРОС: Где должна находиться эта логика?

1. Проверь владение данными
   → Логика живет рядом с данными, которые она использует

2. Проверь переиспользуемость
   → Если логика нужна в разных местах → отдельный компонент
   → Если специфична для фичи → в компоненте фичи

3. Проверь связанность
   → Если логика тесно связана с другой логикой → в одном компоненте
   → Если независима → в разных компонентах

4. Проверь тестируемость
   → Логику легче тестировать изолированно → отдельный компонент

5. Проверь SOLID
   → Single Responsibility → каждая логика в своем компоненте
```

---

## Уровни Сложности Механик

### Классификация по Сложности

```
УРОВЕНЬ 1: ПРОСТЫЕ (1-2 компонента)
- Одно действие
- Нет состояния или простое состояние
- Минимальные зависимости
Примеры: Сбор монет, Триггер, Вращение объекта

УРОВЕНЬ 2: СРЕДНИЕ (3-5 компонентов)
- Несколько связанных действий
- Управление состоянием
- Несколько зависимостей
Примеры: Прыжок, Стрельба, Dash

УРОВЕНЬ 3: СЛОЖНЫЕ (6-10 компонентов)
- Множество действий
- Сложное управление состоянием
- Множество зависимостей
- Интеграция с другими системами
Примеры: Система инвентаря, Боевая система, AI враг

УРОВЕНЬ 4: ПРОДВИНУТЫЕ (10+ компонентов)
- Комплексные системы
- Множество подсистем
- Сложная координация
- Расширяемая архитектура
Примеры: Квестовая система, Система крафта, Skill tree
```

---

## Простые Механики (Уровень 1)

### Пример 1: Сбор Коллекционных Предметов (Монеты)

#### Шаг 1: Описание

```
ФИЧА: Сбор Монет

ОПИСАНИЕ: Персонаж собирает монеты при касании, монеты исчезают, счетчик увеличивается

ДЕЙСТВИЯ:
- Обнаружить касание с игроком
- Увеличить счетчик монет
- Проиграть звук сбора
- Уничтожить монету

УСЛОВИЯ:
- Активна: монета существует
- Неактивна: монета собрана

ЗАВИСИМОСТИ:
- Игрок должен иметь компонент сбора
```

#### Шаг 2: Анализ Аспектов

| Аспект | Есть? | Описание | Где логика |
|--------|-------|----------|------------|
| Обнаружение | ✓ | Коллизия с игроком | CollectibleComponent |
| Данные | ✓ | Ценность монеты | CollectibleComponent |
| Счетчик | ✓ | Хранение количества | InventoryComponent (на игроке) |
| Аудио | ✓ | Звук сбора | AudioComponent |
| Визуализация | ✓ | Исчезновение | Destroy() |

#### Шаг 3: Декомпозиция

```
КОМПОНЕНТЫ:

1. CollectibleComponent (на монете)
   - Обнаружение коллизии
   - Передача значения
   - Публикация события

2. InventoryComponent (на игроке)
   - Хранение количества
   - Публикация события изменения

3. AudioComponent (на игроке)
   - Проигрывание звука
```

#### Шаг 4: Реализация

```csharp
// === ИНТЕРФЕЙС ===
public interface ICollector
{
    void Collect(CollectibleType type, int amount);
}

// === КОМПОНЕНТ 1: CollectibleComponent (на монете) ===
using UnityEngine;

public enum CollectibleType
{
    Coin,
    Gem,
    Health,
    Ammo
}

public class CollectibleComponent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CollectibleType _type = CollectibleType.Coin;
    [SerializeField] private int _amount = 1;
    [SerializeField] private LayerMask _collectorLayer;

    [Header("Audio")]
    [SerializeField] private AudioClip _collectSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверка слоя
        if ((_collectorLayer & (1 << collision.gameObject.layer)) == 0)
            return;

        // Попытка собрать
        if (collision.TryGetComponent(out ICollector collector))
        {
            collector.Collect(_type, _amount);

            // Звук (можно оптимизировать через Object Pool)
            if (_collectSound != null)
            {
                AudioSource.PlayClipAtPoint(_collectSound, transform.position);
            }

            // Уничтожение
            Destroy(gameObject);
        }
    }
}

// === КОМПОНЕНТ 2: InventoryComponent (на игроке) ===
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryComponent : MonoBehaviour, ICollector
{
    public event Action<CollectibleType, int> OnCollected;
    public event Action<CollectibleType, int> OnAmountChanged;

    private readonly Dictionary<CollectibleType, int> _items = new();

    public void Collect(CollectibleType type, int amount)
    {
        // Добавляем в словарь
        if (!_items.ContainsKey(type))
        {
            _items[type] = 0;
        }

        _items[type] += amount;

        // События
        OnCollected?.Invoke(type, amount);
        OnAmountChanged?.Invoke(type, _items[type]);
    }

    public int GetAmount(CollectibleType type)
    {
        return _items.ContainsKey(type) ? _items[type] : 0;
    }

    public bool TrySpend(CollectibleType type, int amount)
    {
        if (!_items.ContainsKey(type) || _items[type] < amount)
            return false;

        _items[type] -= amount;
        OnAmountChanged?.Invoke(type, _items[type]);
        return true;
    }
}

// === ОРКЕСТРАТОР: Player (опционально) ===
public class Player : MonoBehaviour
{
    [SerializeField] private InventoryComponent _inventory;
    [SerializeField] private AudioComponent _audioComponent;
    [SerializeField] private AudioClip _coinSound;

    private void OnEnable()
    {
        _inventory.OnCollected += OnItemCollected;
    }

    private void OnDisable()
    {
        _inventory.OnCollected -= OnItemCollected;
    }

    private void OnItemCollected(CollectibleType type, int amount)
    {
        // Дополнительная логика при сборе
        if (type == CollectibleType.Coin)
        {
            _audioComponent.Play(_coinSound);
        }
    }
}
```

#### Шаг 5: Диаграмма

```
         ┌──────────────┐
         │    Coin      │
         │ GameObject   │
         └──────┬───────┘
                │
        ┌───────▼────────┐
        │ Collectible    │ OnTriggerEnter2D
        │  Component     ├──────────┐
        └────────────────┘          │
                                    │
         ┌──────────────┐           │
         │   Player     ◄───────────┘
         │  GameObject  │ ICollector.Collect()
         └──────┬───────┘
                │
        ┌───────▼────────┐
        │  Inventory     │
        │  Component     │ OnCollected event
        └────────────────┘
```

#### Оценка Сложности: ⭐ (Простая)

**Почему простая:**
- 2 основных компонента
- Простая коллизия
- Минимум состояния
- Одно действие

---

### Пример 2: Вращающаяся Платформа

#### Шаг 1: Описание

```
ФИЧА: Вращающаяся Платформа

ОПИСАНИЕ: Платформа постоянно вращается вокруг своей оси с заданной скоростью

ДЕЙСТВИЯ:
- Вращаться с постоянной скоростью
- Можно настроить ось вращения
- Можно настроить скорость

УСЛОВИЯ:
- Всегда активна

ЗАВИСИМОСТИ:
- Нет
```

#### Шаг 2: Анализ Аспектов

| Аспект | Есть? | Описание | Где логика |
|--------|-------|----------|------------|
| Данные | ✓ | Скорость, ось | RotateComponent |
| Логика | ✓ | Вращение каждый кадр | RotateComponent |
| Физика | ✗ | Нет физики | - |

#### Шаг 3: Декомпозиция

```
КОМПОНЕНТЫ:

1. RotateComponent
   - Скорость вращения
   - Ось вращения
   - Логика вращения в Update
```

#### Шаг 4: Реализация

```csharp
// === КОМПОНЕНТ: RotateComponent ===
using UnityEngine;

public class RotateComponent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 _rotationAxis = Vector3.forward;
    [SerializeField] private float _rotationSpeed = 45f; // градусов в секунду
    [SerializeField] private Space _space = Space.Self;

    private void Update()
    {
        transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime, _space);
    }
}

// === РАСШИРЕННАЯ ВЕРСИЯ с условиями ===
using System;
using UnityEngine;

public class ConditionalRotateComponent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 _rotationAxis = Vector3.forward;
    [SerializeField] private float _rotationSpeed = 45f;
    [SerializeField] private Space _space = Space.Self;
    [SerializeField] private bool _rotateOnStart = true;

    private bool _isRotating;
    private readonly AndCondition _condition = new();

    private void Start()
    {
        _isRotating = _rotateOnStart;
    }

    private void Update()
    {
        if (!_isRotating || !_condition.IsTrue())
            return;

        transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime, _space);
    }

    public void StartRotation() => _isRotating = true;
    public void StopRotation() => _isRotating = false;
    public void AddCondition(Func<bool> condition) => _condition.AddCondition(condition);
}
```

#### Шаг 5: Использование

```csharp
// Простое использование - просто добавить компонент
// GameObject -> Add Component -> RotateComponent

// Продвинутое - с условиями
public class ActivatablePlatform : MonoBehaviour
{
    [SerializeField] private ConditionalRotateComponent _rotateComponent;
    [SerializeField] private TriggerComponent _activationTrigger;

    private bool _isActivated;

    private void Awake()
    {
        _rotateComponent.AddCondition(() => _isActivated);
    }

    private void OnEnable()
    {
        _activationTrigger.OnEnter += Activate;
        _activationTrigger.OnExit += Deactivate;
    }

    private void OnDisable()
    {
        _activationTrigger.OnEnter -= Activate;
        _activationTrigger.OnExit -= Deactivate;
    }

    private void Activate() => _isActivated = true;
    private void Deactivate() => _isActivated = false;
}
```

#### Оценка Сложности: ⭐ (Простая)

**Почему простая:**
- 1 компонент
- Нет зависимостей
- Простая логика в Update
- Нет состояния

---

### Пример 3: Триггер Зона

#### Шаг 1: Описание

```
ФИЧА: Триггер Зона

ОПИСАНИЕ: Область, которая обнаруживает вход/выход объектов и публикует события

ДЕЙСТВИЯ:
- Обнаружить вход объекта в зону
- Обнаружить выход объекта из зоны
- Фильтровать по слоям
- Публиковать события

УСЛОВИЯ:
- Всегда активна

ЗАВИСИМОСТИ:
- Collider2D должен быть Trigger
```

#### Шаг 2-4: Реализация

```csharp
// === КОМПОНЕНТ: TriggerComponent ===
using System;
using UnityEngine;

public class TriggerComponent : MonoBehaviour
{
    public event Action<Collider2D> OnEnter;
    public event Action<Collider2D> OnStay;
    public event Action<Collider2D> OnExit;

    [Header("Settings")]
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private bool _filterByTag;
    [SerializeField] private string _requiredTag = "Player";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsValidTarget(collision))
            return;

        OnEnter?.Invoke(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsValidTarget(collision))
            return;

        OnStay?.Invoke(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsValidTarget(collision))
            return;

        OnExit?.Invoke(collision);
    }

    private bool IsValidTarget(Collider2D collision)
    {
        // Проверка слоя
        if ((_targetLayers & (1 << collision.gameObject.layer)) == 0)
            return false;

        // Проверка тега (опционально)
        if (_filterByTag && !collision.CompareTag(_requiredTag))
            return false;

        return true;
    }
}

// === ПРИМЕР ИСПОЛЬЗОВАНИЯ ===
public class CheckpointZone : MonoBehaviour
{
    [SerializeField] private TriggerComponent _triggerComponent;
    [SerializeField] private AudioComponent _audioComponent;
    [SerializeField] private AudioClip _checkpointSound;
    [SerializeField] private ParticleSystem _checkpointVFX;

    private bool _isActivated;

    private void OnEnable()
    {
        _triggerComponent.OnEnter += OnPlayerEnter;
    }

    private void OnDisable()
    {
        _triggerComponent.OnEnter -= OnPlayerEnter;
    }

    private void OnPlayerEnter(Collider2D player)
    {
        if (_isActivated)
            return; // Уже активирован

        _isActivated = true;

        // Сохраняем чекпоинт
        GameManager.Instance.SetCheckpoint(transform.position);

        // Эффекты
        _audioComponent.Play(_checkpointSound);
        _checkpointVFX.Play();
    }
}
```

#### Оценка Сложности: ⭐ (Простая)

---

## Средние Механики (Уровень 2)

### Пример 1: Система Dash (Рывок)

#### Шаг 1: Описание

```
ФИЧА: Dash (Рывок)

ОПИСАНИЕ: Персонаж совершает быстрый рывок в направлении движения с временной неуязвимостью

ДЕЙСТВИЯ:
- Применить импульс силы в направлении
- Сделать неуязвимым на время рывка
- Проиграть анимацию рывка
- Проиграть звук
- Оставить след частиц
- Запустить cooldown

УСЛОВИЯ:
- Можно использовать: жив, cooldown готов, есть выносливость, не оглушён
- Нельзя использовать: мертв, cooldown активен, нет выносливости, оглушён

ЗАВИСИМОСТИ:
- LifeComponent
- StaminaComponent (опционально)
- Rigidbody2D
```

#### Шаг 2: Анализ Аспектов

| Аспект | Есть? | Описание | Компонент |
|--------|-------|----------|-----------|
| Физика | ✓ | Применение силы | DashComponent |
| Состояние | ✓ | Tracking dash | DashComponent |
| Cooldown | ✓ | Задержка между dash | ReloadComponent |
| Иммунитет | ✓ | Временная неуязвимость | InvulnerabilityComponent |
| Анимация | ✓ | Визуальный эффект | DashAnimationComponent |
| VFX | ✓ | След | TrailEffectComponent |
| Аудио | ✓ | Звук | AudioComponent |
| Ресурсы | ✓ | Расход выносливости | StaminaComponent |
| Ввод | ✓ | Команда | PlayerController |

#### Шаг 3: Декомпозиция

```
КОМПОНЕНТЫ:

1. DashComponent (новый)
   - Основная логика dash
   - Применение физики
   - Управление состоянием

2. ReloadComponent (переиспользуемый)
   - Cooldown система

3. InvulnerabilityComponent (новый, переиспользуемый)
   - Временная неуязвимость

4. DashAnimationComponent (новый)
   - Анимация dash

5. TrailEffectComponent (новый, переиспользуемый)
   - VFX след

6. AudioComponent (переиспользуемый)
   - Звук

7. StaminaComponent (переиспользуемый, опционально)
   - Ресурс выносливости
```

#### Шаг 4: Реализация

```csharp
// === ИНТЕРФЕЙС (опционально) ===
public interface IDashable
{
    event Action OnDashStart;
    event Action OnDashEnd;
    bool CanDash();
    void Dash(Vector2 direction);
}

// === КОМПОНЕНТ 1: DashComponent ===
using System;
using UnityEngine;

public class DashComponent : MonoBehaviour, IDashable
{
    public event Action OnDashStart;
    public event Action OnDashEnd;

    [Header("Settings")]
    [SerializeField] private float _dashForce = 15f;
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private bool _resetVelocity = true;

    [Header("Dependencies")]
    [SerializeField] private Rigidbody2D _rigidbody;

    private bool _isDashing;
    private float _dashTimer;
    private Vector2 _dashDirection;
    private readonly AndCondition _condition = new();

    private void Update()
    {
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;

            if (_dashTimer <= 0)
            {
                EndDash();
            }
        }
    }

    public void AddCondition(Func<bool> condition)
    {
        _condition.AddCondition(condition);
    }

    public bool CanDash()
    {
        return !_isDashing && _condition.IsTrue();
    }

    public void Dash(Vector2 direction)
    {
        if (!CanDash())
            return;

        if (direction == Vector2.zero)
            return;

        // Начало dash
        _isDashing = true;
        _dashDirection = direction.normalized;
        _dashTimer = _dashDuration;

        // Применяем физику
        ApplyDashPhysics();

        // Событие
        OnDashStart?.Invoke();
    }

    private void ApplyDashPhysics()
    {
        if (_resetVelocity)
        {
            _rigidbody.velocity = Vector2.zero;
        }

        _rigidbody.AddForce(_dashDirection * _dashForce, ForceMode2D.Impulse);
    }

    private void EndDash()
    {
        _isDashing = false;
        OnDashEnd?.Invoke();
    }

    // Для внешней отмены (например, столкновение со стеной)
    public void CancelDash()
    {
        if (!_isDashing)
            return;

        EndDash();
    }
}

// === КОМПОНЕНТ 2: InvulnerabilityComponent ===
using System;
using UnityEngine;

public class InvulnerabilityComponent : MonoBehaviour
{
    public event Action OnInvulnerabilityStart;
    public event Action OnInvulnerabilityEnd;

    [Header("Settings")]
    [SerializeField] private bool _blinkWhileInvulnerable = true;
    [SerializeField] private float _blinkInterval = 0.1f;

    [Header("Dependencies")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private bool _isInvulnerable;
    private float _invulnerabilityTimer;
    private float _blinkTimer;

    private void Update()
    {
        if (_isInvulnerable)
        {
            _invulnerabilityTimer -= Time.deltaTime;

            if (_invulnerabilityTimer <= 0)
            {
                EndInvulnerability();
            }
            else if (_blinkWhileInvulnerable)
            {
                UpdateBlink();
            }
        }
    }

    public void EnableFor(float duration)
    {
        _isInvulnerable = true;
        _invulnerabilityTimer = duration;
        _blinkTimer = 0;

        OnInvulnerabilityStart?.Invoke();
    }

    public void Disable()
    {
        if (_isInvulnerable)
        {
            EndInvulnerability();
        }
    }

    public bool IsInvulnerable() => _isInvulnerable;

    private void EndInvulnerability()
    {
        _isInvulnerable = false;

        // Восстановить видимость
        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;
        }

        OnInvulnerabilityEnd?.Invoke();
    }

    private void UpdateBlink()
    {
        _blinkTimer += Time.deltaTime;

        if (_blinkTimer >= _blinkInterval)
        {
            _blinkTimer = 0;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = !_spriteRenderer.enabled;
            }
        }
    }
}

// === КОМПОНЕНТ 3: DashAnimationComponent ===
using UnityEngine;
using DG.Tweening;

public class DashAnimationComponent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _scaleMultiplier = 1.2f;
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private Ease _ease = Ease.OutQuad;

    [Header("Dependencies")]
    [SerializeField] private Transform _visualTransform;

    private Vector3 _originalScale;
    private Sequence _currentSequence;

    private void Awake()
    {
        if (_visualTransform == null)
            _visualTransform = transform;

        _originalScale = _visualTransform.localScale;
    }

    public void PlayDashAnimation(Vector2 direction)
    {
        // Остановить предыдущую анимацию
        _currentSequence?.Kill();

        // Создать новую
        _currentSequence = DOTween.Sequence();

        // Растянуть в направлении dash
        Vector3 scaleDirection = new Vector3(
            Mathf.Abs(direction.x) * _scaleMultiplier,
            Mathf.Abs(direction.y) * _scaleMultiplier,
            1f
        );

        _currentSequence.Append(_visualTransform.DOScale(_originalScale + scaleDirection, _duration / 2).SetEase(_ease));
        _currentSequence.Append(_visualTransform.DOScale(_originalScale, _duration / 2).SetEase(_ease));
    }

    private void OnDestroy()
    {
        _currentSequence?.Kill();
    }
}

// === КОМПОНЕНТ 4: TrailEffectComponent ===
using UnityEngine;

public class TrailEffectComponent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TrailRenderer _trailRenderer;
    [SerializeField] private ParticleSystem _particleSystem;

    public void EnableTrail()
    {
        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = true;
        }

        if (_particleSystem != null)
        {
            _particleSystem.Play();
        }
    }

    public void DisableTrail()
    {
        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false;
        }

        if (_particleSystem != null)
        {
            _particleSystem.Stop();
        }
    }
}

// === КОМПОНЕНТ 5: StaminaComponent (опционально) ===
using System;
using UnityEngine;

public class StaminaComponent : MonoBehaviour
{
    public event Action<int> OnStaminaChanged;
    public event Action OnStaminaEmpty;
    public event Action OnStaminaFull;

    [Header("Settings")]
    [SerializeField] private int _maxStamina = 100;
    [SerializeField] private int _currentStamina = 100;
    [SerializeField] private float _regenRate = 10f; // per second
    [SerializeField] private float _regenDelay = 1f; // delay after consumption

    private float _regenTimer;

    private void Start()
    {
        _currentStamina = _maxStamina;
    }

    private void Update()
    {
        if (_currentStamina < _maxStamina)
        {
            _regenTimer += Time.deltaTime;

            if (_regenTimer >= _regenDelay)
            {
                RegenerateStamina(Time.deltaTime);
            }
        }
    }

    public bool HasEnough(int amount)
    {
        return _currentStamina >= amount;
    }

    public bool TryConsume(int amount)
    {
        if (!HasEnough(amount))
            return false;

        _currentStamina -= amount;
        _regenTimer = 0; // Reset regen delay

        OnStaminaChanged?.Invoke(_currentStamina);

        if (_currentStamina <= 0)
        {
            OnStaminaEmpty?.Invoke();
        }

        return true;
    }

    private void RegenerateStamina(float deltaTime)
    {
        int oldStamina = _currentStamina;
        _currentStamina = Mathf.Min(_maxStamina, _currentStamina + Mathf.RoundToInt(_regenRate * deltaTime));

        if (_currentStamina != oldStamina)
        {
            OnStaminaChanged?.Invoke(_currentStamina);

            if (_currentStamina >= _maxStamina)
            {
                OnStaminaFull?.Invoke();
            }
        }
    }

    public float GetStaminaPercent()
    {
        return (float)_currentStamina / _maxStamina;
    }
}

// === ОРКЕСТРАТОР: Character ===
public class Character : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private LifeComponent _lifeComponent;
    [SerializeField] private MoveComponent _moveComponent;
    [SerializeField] private LookComponent _lookComponent;

    [Header("Dash Components")]
    [SerializeField] private DashComponent _dashComponent;
    [SerializeField] private ReloadComponent _dashReloadComponent;
    [SerializeField] private InvulnerabilityComponent _invulnerabilityComponent;
    [SerializeField] private DashAnimationComponent _dashAnimationComponent;
    [SerializeField] private TrailEffectComponent _trailEffectComponent;
    [SerializeField] private StaminaComponent _staminaComponent;

    [Header("Audio")]
    [SerializeField] private AudioComponent _audioComponent;
    [SerializeField] private AudioClip _dashSound;

    [Header("Settings")]
    [SerializeField] private int _dashStaminaCost = 20;

    private void Awake()
    {
        // Настройка условий для dash
        _dashComponent.AddCondition(_lifeComponent.IsAlive);
        _dashComponent.AddCondition(_dashReloadComponent.IsReady);

        if (_staminaComponent != null)
        {
            _dashComponent.AddCondition(() => _staminaComponent.HasEnough(_dashStaminaCost));
        }
    }

    private void OnEnable()
    {
        _dashComponent.OnDashStart += OnDashStart;
        _dashComponent.OnDashEnd += OnDashEnd;
    }

    private void OnDisable()
    {
        _dashComponent.OnDashStart -= OnDashStart;
        _dashComponent.OnDashEnd -= OnDashEnd;
    }

    private void OnDashStart()
    {
        // Расход выносливости
        if (_staminaComponent != null)
        {
            _staminaComponent.TryConsume(_dashStaminaCost);
        }

        // Неуязвимость на время dash
        _invulnerabilityComponent.EnableFor(0.2f);

        // Анимация
        Vector2 direction = _moveComponent.MoveDirection;
        if (direction == Vector2.zero)
        {
            direction = _lookComponent.Direction;
        }
        _dashAnimationComponent.PlayDashAnimation(direction);

        // VFX
        _trailEffectComponent.EnableTrail();

        // Звук
        _audioComponent.Play(_dashSound);

        // Cooldown
        _dashReloadComponent.Reload();
    }

    private void OnDashEnd()
    {
        // Отключить след
        _trailEffectComponent.DisableTrail();
    }
}

// === КОНТРОЛЛЕР ===
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Character _character;
    [SerializeField] private DashComponent _dashComponent;
    [SerializeField] private MoveComponent _moveComponent;

    private void Update()
    {
        // Dash по Shift
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Vector2 direction = _moveComponent.MoveDirection;

            // Если не двигаемся, dash в направлении взгляда
            if (direction == Vector2.zero)
            {
                direction = Vector2.right; // или используйте LookComponent
            }

            _dashComponent.Dash(direction);
        }
    }
}
```

#### Шаг 5: Диаграмма

```
┌─────────────────────────────────────────────────────────────────┐
│                    Character (Orchestrator)                      │
│  - Настраивает условия для DashComponent                        │
│  - Подписывается на события OnDashStart/OnDashEnd               │
│  - Координирует все dash компоненты                             │
└────────────────────────┬────────────────────────────────────────┘
                         │
     ┌───────────────────┼────────────────┬─────────────────┐
     │                   │                │                 │
     ▼                   ▼                ▼                 ▼
┌─────────┐      ┌──────────────┐   ┌──────────┐    ┌──────────┐
│  Dash   │      │   Reload     │   │ Stamina  │    │   Life   │
│Component│      │  Component   │   │Component │    │Component │
└────┬────┘      └──────────────┘   └──────────┘    └──────────┘
     │
     │ OnDashStart
     │ OnDashEnd
     ▼
┌──────────────────────────────────────────────┐
│           Event Subscribers                   │
├──────────────────────────────────────────────┤
│ → StaminaComponent.TryConsume()              │
│ → InvulnerabilityComponent.EnableFor()       │
│ → DashAnimationComponent.PlayDashAnimation() │
│ → TrailEffectComponent.EnableTrail()         │
│ → AudioComponent.Play()                      │
│ → ReloadComponent.Reload()                   │
└──────────────────────────────────────────────┘
```

#### Оценка Сложности: ⭐⭐ (Средняя)

**Почему средняя:**
- 7 компонентов
- Управление состоянием (isDashing, timer)
- Несколько зависимостей
- События и координация
- Но логика каждого компонента проста

---

### Пример 2: Система Стрельбы (Простая)

#### Шаг 1: Описание

```
ФИЧА: Стрельба

ОПИСАНИЕ: Персонаж стреляет снарядами в направлении взгляда

ДЕЙСТВИЯ:
- Создать снаряд в точке спавна
- Направить снаряд в сторону взгляда
- Проиграть анимацию отдачи
- Проиграть звук выстрела
- Запустить cooldown между выстрелами
- Расходовать патроны

УСЛОВИЯ:
- Можно стрелять: жив, cooldown готов, есть патроны
- Нельзя стрелять: мертв, cooldown активен, нет патронов

ЗАВИСИМОСТИ:
- LifeComponent
- Prefab снаряда
```

#### Шаг 2-4: Реализация

```csharp
// === КОМПОНЕНТ 1: ProjectileComponent (на снаряде) ===
using UnityEngine;

public class ProjectileComponent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifetime = 5f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private LayerMask _targetLayers;

    [Header("Effects")]
    [SerializeField] private GameObject _hitEffectPrefab;

    private Vector2 _direction;
    private float _lifeTimer;

    public void Initialize(Vector2 direction)
    {
        _direction = direction.normalized;
        _lifeTimer = _lifetime;

        // Повернуть снаряд в направлении полета
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        // Движение
        transform.position += (Vector3)_direction * _speed * Time.deltaTime;

        // Время жизни
        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверка слоя
        if ((_targetLayers & (1 << collision.gameObject.layer)) == 0)
            return;

        // Нанести урон
        if (collision.TryGetComponent(out IDamageTaker damageable))
        {
            damageable.TakeDamage(_damage);
        }

        // Эффект попадания
        if (_hitEffectPrefab != null)
        {
            Instantiate(_hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Уничтожить снаряд
        Destroy(gameObject);
    }
}

// === КОМПОНЕНТ 2: ShootComponent ===
using System;
using UnityEngine;

public class ShootComponent : MonoBehaviour
{
    public event Action OnShoot;

    [Header("Settings")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private float _projectileSpeed = 10f;

    [Header("Ammo")]
    [SerializeField] private bool _useAmmo = true;
    [SerializeField] private int _maxAmmo = 30;
    [SerializeField] private int _currentAmmo = 30;

    private readonly AndCondition _condition = new();

    private void Start()
    {
        if (_useAmmo)
        {
            _currentAmmo = _maxAmmo;
        }
    }

    public void AddCondition(Func<bool> condition)
    {
        _condition.AddCondition(condition);
    }

    public bool CanShoot()
    {
        if (!_condition.IsTrue())
            return false;

        if (_useAmmo && _currentAmmo <= 0)
            return false;

        return true;
    }

    public void Shoot(Vector2 direction)
    {
        if (!CanShoot())
            return;

        if (direction == Vector2.zero)
            return;

        // Создать снаряд
        GameObject projectile = Instantiate(_projectilePrefab, _shootPoint.position, Quaternion.identity);

        // Инициализировать снаряд
        if (projectile.TryGetComponent(out ProjectileComponent projectileComponent))
        {
            projectileComponent.Initialize(direction);
        }

        // Расход патронов
        if (_useAmmo)
        {
            _currentAmmo--;
        }

        // Событие
        OnShoot?.Invoke();
    }

    public void Reload(int amount)
    {
        if (!_useAmmo)
            return;

        _currentAmmo = Mathf.Min(_maxAmmo, _currentAmmo + amount);
    }

    public int GetCurrentAmmo() => _currentAmmo;
    public int GetMaxAmmo() => _maxAmmo;
}

// === КОМПОНЕНТ 3: ShootAnimationComponent ===
using UnityEngine;
using DG.Tweening;

public class ShootAnimationComponent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _recoilDistance = 0.1f;
    [SerializeField] private float _recoilDuration = 0.1f;

    [Header("Dependencies")]
    [SerializeField] private Transform _weaponTransform;

    private Vector3 _originalPosition;

    private void Awake()
    {
        if (_weaponTransform != null)
        {
            _originalPosition = _weaponTransform.localPosition;
        }
    }

    public void PlayShootAnimation(Vector2 shootDirection)
    {
        if (_weaponTransform == null)
            return;

        // Отдача назад
        Vector3 recoil = -(Vector3)shootDirection.normalized * _recoilDistance;

        var sequence = DOTween.Sequence();
        sequence.Append(_weaponTransform.DOLocalMove(_originalPosition + recoil, _recoilDuration / 2));
        sequence.Append(_weaponTransform.DOLocalMove(_originalPosition, _recoilDuration / 2));
    }
}

// === ОРКЕСТРАТОР: Character (расширение) ===
public class Character : MonoBehaviour
{
    [Header("Shoot Components")]
    [SerializeField] private ShootComponent _shootComponent;
    [SerializeField] private ReloadComponent _shootReloadComponent;
    [SerializeField] private ShootAnimationComponent _shootAnimationComponent;
    [SerializeField] private LookComponent _lookComponent;

    [Header("Shoot Audio")]
    [SerializeField] private AudioClip _shootSound;

    private void Awake()
    {
        // ... другие настройки

        // Настройка условий для стрельбы
        _shootComponent.AddCondition(_lifeComponent.IsAlive);
        _shootComponent.AddCondition(_shootReloadComponent.IsReady);
    }

    private void OnEnable()
    {
        // ... другие подписки

        _shootComponent.OnShoot += OnShoot;
    }

    private void OnDisable()
    {
        // ... другие отписки

        _shootComponent.OnShoot -= OnShoot;
    }

    private void OnShoot()
    {
        // Анимация отдачи
        _shootAnimationComponent.PlayShootAnimation(_lookComponent.Direction);

        // Звук
        _audioComponent.Play(_shootSound);

        // Cooldown
        _shootReloadComponent.Reload();
    }
}

// === КОНТРОЛЛЕР (расширение) ===
public class PlayerController : MonoBehaviour
{
    [SerializeField] private ShootComponent _shootComponent;
    [SerializeField] private LookComponent _lookComponent;

    private void Update()
    {
        // ... другой ввод

        // Стрельба по ЛКМ
        if (Input.GetMouseButton(0))
        {
            _shootComponent.Shoot(_lookComponent.Direction);
        }
    }
}
```

#### Оценка Сложности: ⭐⭐ (Средняя)

---

---

## Сложные Механики (Уровень 3)

### Пример 1: Система Инвентаря (Продвинутая)

#### Шаг 1: Описание

```
ФИЧА: Инвентарь

ОПИСАНИЕ: Система хранения, управления и использования предметов с UI интерфейсом

ДЕЙСТВИЯ:
- Добавить предмет в инвентарь
- Удалить предмет из инвентаря
- Использовать предмет
- Экипировать предмет
- Снять экипированный предмет
- Объединять стакающиеся предметы
- Сортировать инвентарь
- Сохранять/загружать инвентарь

УСЛОВИЯ:
- Можно добавить: есть свободное место
- Можно использовать: предмет в инвентаре, предмет можно использовать
- Можно экипировать: есть свободный слот экипировки

ЗАВИСИМОСТИ:
- UI система
- Система предметов (Item System)
- Система сохранения (Save System)
```

#### Шаг 2: Анализ Аспектов

| Аспект | Есть? | Описание | Компонент |
|--------|-------|----------|-----------|
| Хранение | ✓ | Список предметов | InventoryComponent |
| Управление слотами | ✓ | Логика слотов | InventorySlot |
| Стакирование | ✓ | Объединение предметов | InventoryComponent |
| Экипировка | ✓ | Отдельные слоты | EquipmentComponent |
| Использование | ✓ | Активация эффекта | IUsable interface |
| UI отображение | ✓ | Визуализация | InventoryUI |
| События | ✓ | Уведомления | Events |

#### Шаг 3: Декомпозиция

```
КОМПОНЕНТЫ:

ДАННЫЕ:
1. ItemData (ScriptableObject)
   - Данные предмета (ID, имя, иконка, max stack)

2. InventorySlot
   - Один слот инвентаря (предмет + количество)

ЛОГИКА:
3. InventoryComponent
   - Хранение слотов
   - Добавление/удаление предметов
   - Поиск предметов
   - События

4. EquipmentComponent
   - Слоты экипировки
   - Бонусы от экипировки
   - События экипировки

5. Item Interfaces
   - IUsable (можно использовать)
   - IEquippable (можно экипировать)
   - IStackable (можно стакать)

UI:
6. InventoryUI
   - Отображение инвентаря
   - Взаимодействие с слотами

7. InventorySlotUI
   - Отображение одного слота
   - Drag & Drop

8. EquipmentUI
   - Отображение экипировки
```

#### Шаг 4: Реализация

```csharp
// === ДАННЫЕ: ItemData (ScriptableObject) ===
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemID;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Properties")]
    public ItemType itemType;
    public ItemRarity rarity;
    public int maxStackSize = 1;
    public int value = 10;

    [Header("Usability")]
    public bool isUsable;
    public bool isEquippable;
    public bool isQuestItem;
}

public enum ItemType
{
    Consumable,
    Equipment,
    Material,
    QuestItem,
    Misc
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

// === ДАННЫЕ: InventorySlot ===
using System;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public InventorySlot()
    {
        item = null;
        quantity = 0;
    }

    public InventorySlot(ItemData item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public bool IsEmpty => item == null || quantity <= 0;
    public bool IsFull => item != null && quantity >= item.maxStackSize;

    public bool CanAddItem(ItemData itemToAdd)
    {
        if (IsEmpty)
            return true;

        if (item != itemToAdd)
            return false;

        return quantity < item.maxStackSize;
    }

    public int AddItem(ItemData itemToAdd, int quantityToAdd)
    {
        if (IsEmpty)
        {
            item = itemToAdd;
            quantity = Mathf.Min(quantityToAdd, itemToAdd.maxStackSize);
            return quantityToAdd - quantity; // Остаток
        }

        if (item != itemToAdd)
            return quantityToAdd; // Не можем добавить

        int spaceLeft = item.maxStackSize - quantity;
        int amountToAdd = Mathf.Min(spaceLeft, quantityToAdd);
        quantity += amountToAdd;

        return quantityToAdd - amountToAdd; // Остаток
    }

    public int RemoveItem(int quantityToRemove)
    {
        int amountRemoved = Mathf.Min(quantity, quantityToRemove);
        quantity -= amountRemoved;

        if (quantity <= 0)
        {
            Clear();
        }

        return amountRemoved;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}

// === ИНТЕРФЕЙСЫ ===
public interface IUsable
{
    bool CanUse(GameObject user);
    void Use(GameObject user);
}

public interface IEquippable
{
    EquipmentSlotType GetSlotType();
    void OnEquip(GameObject wearer);
    void OnUnequip(GameObject wearer);
}

public enum EquipmentSlotType
{
    Head,
    Chest,
    Legs,
    Weapon,
    Shield,
    Accessory
}

// === ЛОГИКА: InventoryComponent ===
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryComponent : MonoBehaviour
{
    public event Action<ItemData, int> OnItemAdded;
    public event Action<ItemData, int> OnItemRemoved;
    public event Action<ItemData> OnItemUsed;
    public event Action OnInventoryChanged;

    [Header("Settings")]
    [SerializeField] private int _capacity = 20;

    private List<InventorySlot> _slots;

    public int Capacity => _capacity;
    public IReadOnlyList<InventorySlot> Slots => _slots;

    private void Awake()
    {
        _slots = new List<InventorySlot>(_capacity);
        for (int i = 0; i < _capacity; i++)
        {
            _slots.Add(new InventorySlot());
        }
    }

    /// <summary>
    /// Добавить предмет в инвентарь
    /// </summary>
    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
            return false;

        int remainingQuantity = quantity;

        // Сначала попытаться добавить в существующие стаки
        if (item.maxStackSize > 1)
        {
            foreach (var slot in _slots)
            {
                if (slot.item == item && !slot.IsFull)
                {
                    remainingQuantity = slot.AddItem(item, remainingQuantity);

                    if (remainingQuantity <= 0)
                    {
                        OnItemAdded?.Invoke(item, quantity);
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }
        }

        // Затем добавить в пустые слоты
        while (remainingQuantity > 0)
        {
            InventorySlot emptySlot = FindEmptySlot();
            if (emptySlot == null)
            {
                // Инвентарь полон
                Debug.LogWarning($"Inventory full! Could not add {remainingQuantity} of {item.itemName}");
                return false;
            }

            remainingQuantity = emptySlot.AddItem(item, remainingQuantity);
        }

        OnItemAdded?.Invoke(item, quantity);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Удалить предмет из инвентаря
    /// </summary>
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
            return false;

        if (!HasItem(item, quantity))
            return false;

        int remainingQuantity = quantity;

        foreach (var slot in _slots)
        {
            if (slot.item == item)
            {
                int removed = slot.RemoveItem(remainingQuantity);
                remainingQuantity -= removed;

                if (remainingQuantity <= 0)
                    break;
            }
        }

        OnItemRemoved?.Invoke(item, quantity);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Использовать предмет
    /// </summary>
    public bool UseItem(ItemData item)
    {
        if (!HasItem(item, 1))
            return false;

        if (!item.isUsable)
            return false;

        // Получить компонент использования (можно через ScriptableObject pattern)
        IUsable usable = item as IUsable;
        if (usable == null || !usable.CanUse(gameObject))
            return false;

        usable.Use(gameObject);

        // Удалить использованный предмет
        RemoveItem(item, 1);

        OnItemUsed?.Invoke(item);
        return true;
    }

    /// <summary>
    /// Проверить наличие предмета
    /// </summary>
    public bool HasItem(ItemData item, int quantity = 1)
    {
        return GetItemCount(item) >= quantity;
    }

    /// <summary>
    /// Получить количество предмета
    /// </summary>
    public int GetItemCount(ItemData item)
    {
        return _slots.Where(s => s.item == item).Sum(s => s.quantity);
    }

    /// <summary>
    /// Найти пустой слот
    /// </summary>
    private InventorySlot FindEmptySlot()
    {
        return _slots.FirstOrDefault(s => s.IsEmpty);
    }

    /// <summary>
    /// Получить слот по индексу
    /// </summary>
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Count)
            return null;

        return _slots[index];
    }

    /// <summary>
    /// Поменять местами слоты (для drag & drop)
    /// </summary>
    public void SwapSlots(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _slots.Count)
            return;
        if (toIndex < 0 || toIndex >= _slots.Count)
            return;

        var temp = _slots[fromIndex];
        _slots[fromIndex] = _slots[toIndex];
        _slots[toIndex] = temp;

        OnInventoryChanged?.Invoke();
    }
}

// === ЛОГИКА: EquipmentComponent ===
using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentComponent : MonoBehaviour
{
    public event Action<EquipmentSlotType, ItemData> OnItemEquipped;
    public event Action<EquipmentSlotType, ItemData> OnItemUnequipped;

    [SerializeField] private Dictionary<EquipmentSlotType, ItemData> _equipment = new();

    /// <summary>
    /// Экипировать предмет
    /// </summary>
    public bool Equip(ItemData item)
    {
        if (item == null || !item.isEquippable)
            return false;

        IEquippable equippable = item as IEquippable;
        if (equippable == null)
            return false;

        EquipmentSlotType slotType = equippable.GetSlotType();

        // Снять текущий предмет если есть
        if (_equipment.ContainsKey(slotType))
        {
            Unequip(slotType);
        }

        // Экипировать новый
        _equipment[slotType] = item;
        equippable.OnEquip(gameObject);

        OnItemEquipped?.Invoke(slotType, item);
        return true;
    }

    /// <summary>
    /// Снять экипированный предмет
    /// </summary>
    public ItemData Unequip(EquipmentSlotType slotType)
    {
        if (!_equipment.ContainsKey(slotType))
            return null;

        ItemData item = _equipment[slotType];
        IEquippable equippable = item as IEquippable;
        equippable?.OnUnequip(gameObject);

        _equipment.Remove(slotType);

        OnItemUnequipped?.Invoke(slotType, item);
        return item;
    }

    /// <summary>
    /// Получить экипированный предмет
    /// </summary>
    public ItemData GetEquipped(EquipmentSlotType slotType)
    {
        return _equipment.ContainsKey(slotType) ? _equipment[slotType] : null;
    }

    /// <summary>
    /// Проверить экипирован ли предмет
    /// </summary>
    public bool IsEquipped(EquipmentSlotType slotType)
    {
        return _equipment.ContainsKey(slotType);
    }
}

// === ПРИМЕР ПРЕДМЕТА: HealthPotion ===
using UnityEngine;

[CreateAssetMenu(fileName = "HealthPotion", menuName = "Game/Items/Health Potion")]
public class HealthPotionData : ItemData, IUsable
{
    [Header("Potion Settings")]
    public int healAmount = 50;

    public bool CanUse(GameObject user)
    {
        if (!user.TryGetComponent(out LifeComponent life))
            return false;

        return life.IsAlive();
    }

    public void Use(GameObject user)
    {
        if (user.TryGetComponent(out LifeComponent life))
        {
            // Предположим есть метод Heal
            // life.Heal(healAmount);
            Debug.Log($"Healed {user.name} for {healAmount} HP");
        }
    }
}
```

#### Шаг 5: Диаграмма

```
┌──────────────────────────────────────────────────────────┐
│                    Player GameObject                      │
└────────┬─────────────────────────────────────────────────┘
         │
    ┌────┴────┬──────────────────┐
    ▼         ▼                  ▼
┌─────────┐ ┌──────────┐ ┌──────────────┐
│Inventory│ │Equipment │ │  Other       │
│Component│ │Component │ │  Components  │
└────┬────┘ └────┬─────┘ └──────────────┘
     │           │
     │ Events    │ Events
     ▼           ▼
┌──────────────────────────────────┐
│         UI System                │
├──────────────────────────────────┤
│  - InventoryUI                   │
│  - EquipmentUI                   │
│  - InventorySlotUI (множество)   │
└──────────────────────────────────┘
```

#### Оценка Сложности: ⭐⭐⭐ (Сложная)

**Почему сложная:**
- 8+ компонентов
- Сложное управление состоянием (множество слотов)
- Множество зависимостей
- Интеграция с UI системой
- Сложная логика (стакирование, экипировка)

---

### Пример 2: AI Враг с Состояниями

#### Шаг 1: Описание

```
ФИЧА: AI Враг

ОПИСАНИЕ: Враг с поведением на основе состояний (патруль, преследование, атака, отступление)

ДЕЙСТВИЯ:
- Патрулировать область
- Обнаруживать игрока
- Преследовать игрока
- Атаковать игрока
- Отступать при низком HP
- Возвращаться к патрулю при потере цели

СОСТОЯНИЯ:
- Idle (ожидание)
- Patrol (патруль)
- Chase (преследование)
- Attack (атака)
- Retreat (отступление)
- Dead (смерть)

ЗАВИСИМОСТИ:
- LifeComponent
- MoveComponent
- DetectionComponent
- AttackComponent
```

#### Шаг 2: Анализ Аспектов

| Аспект | Есть? | Описание | Компонент |
|--------|-------|----------|-----------|
| Обнаружение | ✓ | Поиск игрока | VisionComponent |
| Движение | ✓ | Патруль/преследование | MoveComponent, PatrolComponent |
| Атака | ✓ | Нанесение урона | AttackComponent |
| Состояния | ✓ | State Machine | AIStateMachine |
| Логика AI | ✓ | Решения | AI States |
| Здоровье | ✓ | HP | LifeComponent |

#### Шаг 3: Реализация

```csharp
// === КОМПОНЕНТ: VisionComponent ===
using System;
using UnityEngine;

public class VisionComponent : MonoBehaviour
{
    public event Action<Transform> OnTargetDetected;
    public event Action OnTargetLost;

    [Header("Settings")]
    [SerializeField] private float _visionRange = 10f;
    [SerializeField] private float _visionAngle = 90f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Dependencies")]
    [SerializeField] private Transform _eyePosition;

    private Transform _currentTarget;
    private float _detectionTimer;
    private const float DETECTION_INTERVAL = 0.2f;

    private void Update()
    {
        _detectionTimer += Time.deltaTime;

        if (_detectionTimer >= DETECTION_INTERVAL)
        {
            _detectionTimer = 0;
            DetectTarget();
        }
    }

    private void DetectTarget()
    {
        Collider2D[] targetsInRange = Physics2D.OverlapCircleAll(
            transform.position,
            _visionRange,
            _targetLayer
        );

        Transform bestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var target in targetsInRange)
        {
            Vector2 directionToTarget = (target.transform.position - transform.position).normalized;
            float angleToTarget = Vector2.Angle(transform.right, directionToTarget);

            if (angleToTarget <= _visionAngle / 2)
            {
                float distanceToTarget = Vector2.Distance(transform.position, target.transform.position);

                // Проверка препятствий
                if (!Physics2D.Raycast(_eyePosition.position, directionToTarget, distanceToTarget, _obstacleMask))
                {
                    if (distanceToTarget < closestDistance)
                    {
                        closestDistance = distanceToTarget;
                        bestTarget = target.transform;
                    }
                }
            }
        }

        // Обновить текущую цель
        if (bestTarget != _currentTarget)
        {
            if (_currentTarget != null)
            {
                OnTargetLost?.Invoke();
            }

            _currentTarget = bestTarget;

            if (_currentTarget != null)
            {
                OnTargetDetected?.Invoke(_currentTarget);
            }
        }
    }

    public Transform GetCurrentTarget() => _currentTarget;
    public bool HasTarget() => _currentTarget != null;
    public float GetDistanceToTarget()
    {
        if (_currentTarget == null)
            return float.MaxValue;

        return Vector2.Distance(transform.position, _currentTarget.position);
    }
}

// === STATE MACHINE ===
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIState
{
    protected AIStateMachine stateMachine;

    public AIState(AIStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}

public class AIStateMachine : MonoBehaviour
{
    public event Action<Type> OnStateChanged;

    private AIState _currentState;
    private Dictionary<Type, AIState> _states = new();

    public void RegisterState(AIState state)
    {
        _states[state.GetType()] = state;
    }

    public void SetState<T>() where T : AIState
    {
        var type = typeof(T);

        if (_currentState?.GetType() == type)
            return;

        if (!_states.ContainsKey(type))
        {
            Debug.LogError($"State {type} not registered!");
            return;
        }

        _currentState?.OnExit();
        _currentState = _states[type];
        _currentState.OnEnter();

        OnStateChanged?.Invoke(type);
    }

    public T GetState<T>() where T : AIState
    {
        var type = typeof(T);
        return _states.ContainsKey(type) ? _states[type] as T : null;
    }

    private void Update()
    {
        _currentState?.Update();
    }

    private void FixedUpdate()
    {
        _currentState?.FixedUpdate();
    }
}

// === AI STATES ===

// Idle State
public class IdleState : AIState
{
    private float _idleTime;
    private float _idleTimer;

    public IdleState(AIStateMachine stateMachine, float idleTime = 2f) : base(stateMachine)
    {
        _idleTime = idleTime;
    }

    public override void OnEnter()
    {
        _idleTimer = 0;
    }

    public override void Update()
    {
        _idleTimer += Time.deltaTime;

        if (_idleTimer >= _idleTime)
        {
            stateMachine.SetState<PatrolState>();
        }
    }
}

// Patrol State
public class PatrolState : AIState
{
    private MoveComponent _moveComponent;
    private BasePatrolComponent _patrolComponent;
    private VisionComponent _visionComponent;

    public PatrolState(
        AIStateMachine stateMachine,
        MoveComponent moveComponent,
        BasePatrolComponent patrolComponent,
        VisionComponent visionComponent
    ) : base(stateMachine)
    {
        _moveComponent = moveComponent;
        _patrolComponent = patrolComponent;
        _visionComponent = visionComponent;
    }

    public override void Update()
    {
        // Проверка цели
        if (_visionComponent.HasTarget())
        {
            stateMachine.SetState<ChaseState>();
            return;
        }

        // Патрулирование
        if (_patrolComponent.IsArrived())
        {
            _patrolComponent.NextPoint();
        }

        Vector3 direction = (_patrolComponent.GetCurrentPoint() - stateMachine.transform.position).normalized;
        _moveComponent.SetDirection(direction);
    }
}

// Chase State
public class ChaseState : AIState
{
    private MoveComponent _moveComponent;
    private VisionComponent _visionComponent;
    private float _attackRange;

    public ChaseState(
        AIStateMachine stateMachine,
        MoveComponent moveComponent,
        VisionComponent visionComponent,
        float attackRange
    ) : base(stateMachine)
    {
        _moveComponent = moveComponent;
        _visionComponent = visionComponent;
        _attackRange = attackRange;
    }

    public override void Update()
    {
        if (!_visionComponent.HasTarget())
        {
            stateMachine.SetState<PatrolState>();
            return;
        }

        float distanceToTarget = _visionComponent.GetDistanceToTarget();

        if (distanceToTarget <= _attackRange)
        {
            stateMachine.SetState<AttackState>();
            return;
        }

        // Преследование
        Vector3 direction = (_visionComponent.GetCurrentTarget().position - stateMachine.transform.position).normalized;
        _moveComponent.SetDirection(direction);
    }
}

// Attack State
public class AttackState : AIState
{
    private MoveComponent _moveComponent;
    private VisionComponent _visionComponent;
    private AttackComponent _attackComponent;
    private float _attackRange;

    public AttackState(
        AIStateMachine stateMachine,
        MoveComponent moveComponent,
        VisionComponent visionComponent,
        AttackComponent attackComponent,
        float attackRange
    ) : base(stateMachine)
    {
        _moveComponent = moveComponent;
        _visionComponent = visionComponent;
        _attackComponent = attackComponent;
        _attackRange = attackRange;
    }

    public override void OnEnter()
    {
        _moveComponent.SetDirection(Vector3.zero); // Остановиться
    }

    public override void Update()
    {
        if (!_visionComponent.HasTarget())
        {
            stateMachine.SetState<PatrolState>();
            return;
        }

        float distanceToTarget = _visionComponent.GetDistanceToTarget();

        if (distanceToTarget > _attackRange * 1.2f) // Hysteresis
        {
            stateMachine.SetState<ChaseState>();
            return;
        }

        // Атака
        Vector3 direction = (_visionComponent.GetCurrentTarget().position - stateMachine.transform.position).normalized;
        _attackComponent.Attack(direction);
    }
}

// === КОМПОНЕНТ: AIEnemy (Оркестратор) ===
public class AIEnemy : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private LifeComponent _lifeComponent;
    [SerializeField] private MoveComponent _moveComponent;
    [SerializeField] private BasePatrolComponent _patrolComponent;
    [SerializeField] private VisionComponent _visionComponent;
    [SerializeField] private AttackComponent _attackComponent;
    [SerializeField] private AIStateMachine _stateMachine;

    [Header("Settings")]
    [SerializeField] private float _attackRange = 2f;

    private void Awake()
    {
        // Регистрация состояний
        _stateMachine.RegisterState(new IdleState(_stateMachine));
        _stateMachine.RegisterState(new PatrolState(_stateMachine, _moveComponent, _patrolComponent, _visionComponent));
        _stateMachine.RegisterState(new ChaseState(_stateMachine, _moveComponent, _visionComponent, _attackRange));
        _stateMachine.RegisterState(new AttackState(_stateMachine, _moveComponent, _visionComponent, _attackComponent, _attackRange));
    }

    private void Start()
    {
        _stateMachine.SetState<IdleState>();
    }

    private void OnEnable()
    {
        _lifeComponent.OnEmpty += OnDeath;
    }

    private void OnDisable()
    {
        _lifeComponent.OnEmpty -= OnDeath;
    }

    private void OnDeath()
    {
        // Отключить AI
        _stateMachine.enabled = false;
        _moveComponent.SetDirection(Vector3.zero);

        // Уничтожить через время
        Destroy(gameObject, 2f);
    }
}
```

#### Оценка Сложности: ⭐⭐⭐ (Сложная)

---

## Продвинутые Механики (Уровень 4)

### Пример 1: Система Квестов

#### Шаг 1: Описание

```
ФИЧА: Квестовая Система

ОПИСАНИЕ: Управление квестами с различными типами задач, прогрессом и наградами

КОМПОНЕНТЫ:

ДАННЫЕ:
1. QuestData (ScriptableObject) - данные квеста
2. QuestObjectiveData - данные цели квеста
3. QuestReward - награды за квест

ЛОГИКА:
4. QuestManager - глобальный менеджер квестов
5. QuestTracker - отслеживание прогресса
6. QuestObjective - базовый класс цели
7. Различные типы целей (Kill, Collect, Visit и т.д.)

ИНТЕГРАЦИЯ:
8. QuestGiver - NPC дающий квесты
9. QuestUI - интерфейс квестов
10. SaveSystem integration - сохранение прогресса
```

#### Краткая Реализация (Пример структуры)

```csharp
// === ДАННЫЕ ===
[CreateAssetMenu(fileName = "New Quest", menuName = "Game/Quest")]
public class QuestData : ScriptableObject
{
    public string questID;
    public string questName;
    public string description;
    public QuestObjectiveData[] objectives;
    public QuestReward[] rewards;
}

// === MANAGER ===
public class QuestManager : MonoBehaviour
{
    private static QuestManager _instance;
    public static QuestManager Instance => _instance;

    private Dictionary<string, Quest> _activeQuests = new();
    private HashSet<string> _completedQuests = new();

    public void AcceptQuest(QuestData questData) { }
    public void CompleteQuest(string questID) { }
    public void UpdateObjective(string questID, int objectiveIndex, int progress) { }
}

// === RUNTIME QUEST ===
public class Quest
{
    public QuestData data;
    public QuestObjective[] objectives;
    public bool IsCompleted => objectives.All(o => o.IsCompleted);
}
```

#### Оценка Сложности: ⭐⭐⭐⭐ (Продвинутая)

---

## Шаблоны и Паттерны

### Шаблон 1: Простой Компонент

```csharp
using System;
using UnityEngine;

/// <summary>
/// [Описание что делает компонент]
/// </summary>
public class TemplateComponent : MonoBehaviour
{
    // === СОБЫТИЯ ===
    public event Action OnSomethingHappened;

    // === НАСТРОЙКИ ===
    [Header("Settings")]
    [SerializeField] private float _parameter = 1f;

    // === ЗАВИСИМОСТИ ===
    [Header("Dependencies")]
    [SerializeField] private Rigidbody2D _rigidbody;

    // === СОСТОЯНИЕ ===
    private bool _isActive;

    // === УСЛОВИЯ ===
    private readonly AndCondition _condition = new();

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===
    public void DoSomething()
    {
        if (!CanDoSomething())
            return;

        // Логика
        OnSomethingHappened?.Invoke();
    }

    public bool CanDoSomething()
    {
        return _isActive && _condition.IsTrue();
    }

    public void AddCondition(Func<bool> condition)
    {
        _condition.AddCondition(condition);
    }

    // === UNITY CALLBACKS ===
    private void Awake()
    {
        // Инициализация
    }

    private void Update()
    {
        // Логика каждый кадр
    }
}
```

### Шаблон 2: Компонент с Таймером

```csharp
public class TimedComponent : MonoBehaviour
{
    [SerializeField] private float _duration = 1f;
    private float _timer;
    private bool _isActive;

    private void Update()
    {
        if (_isActive)
        {
            _timer -= Time.deltaTime;

            if (_timer <= 0)
            {
                End();
            }
        }
    }

    public void Start(float duration)
    {
        _isActive = true;
        _timer = duration;
    }

    private void End()
    {
        _isActive = false;
        // Логика завершения
    }
}
```

### Шаблон 3: Оркестратор

```csharp
public class EntityOrchestrator : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private ComponentA _componentA;
    [SerializeField] private ComponentB _componentB;

    private void Awake()
    {
        // Настройка условий
        _componentB.AddCondition(_componentA.IsReady);
    }

    private void OnEnable()
    {
        // Подписки
        _componentA.OnEvent += OnEventHandler;
    }

    private void OnDisable()
    {
        // Отписки
        _componentA.OnEvent -= OnEventHandler;
    }

    private void OnEventHandler()
    {
        // Координация компонентов
        _componentB.DoSomething();
    }
}
```

---

## Примеры из 3D Шутера (GameShooter)

Этот раздел демонстрирует декомпозицию фич из реального 3D шутер-проекта с турелями, системой стрельбы и AI.

### Пример 1: Система Стрельбы (Уровень 2 — Средняя)

#### Шаг 1: Описание

```
ФИЧА: Система Стрельбы

ОПИСАНИЕ: Персонаж может стрелять из двух рук разным оружием с независимым управлением

ДЕЙСТВИЯ:
- Создать пулю в точке выстрела
- Задать направление движения пули
- Проиграть событие выстрела
- Проверить условия (жив? перезаряжен?)
- Пуля наносит урон при столкновении

УСЛОВИЯ:
- Можно стрелять: персонаж жив, условия выполнены
- Нельзя стрелять: персонаж мертв, не выполнены условия

ЗАВИСИМОСТИ:
- LifeComponent (проверка жив ли)
- MoveComponent (для пули)
- IDamageable (интерфейс урона)
```

#### Шаг 2: Анализ Аспектов

| Аспект | Есть? | Описание | Компонент |
|--------|-------|----------|-----------|
| Создание пули | ✓ | Инстанциирование префаба | ShootComponent |
| Направление | ✓ | Задать вектор движения | ShootComponent → MoveComponent |
| Условия | ✓ | Проверка возможности | AndCondition в ShootComponent |
| Урон | ✓ | Нанесение урона при коллизии | Bullet + IDamageable |
| Слоты оружия | ✓ | Левая/правая рука | IRightHandComponent, ILeftHandComponent |
| Ввод | ✓ | Space, E клавиши | ShootController |

#### Шаг 3: Декомпозиция на Компоненты

```
КОМПОНЕНТЫ:

1. ShootComponent (наследует ConditionComponent)
   - Создание пули
   - Задание направления
   - Публикация OnFire события
   - Проверка условий

2. MoveComponent (переиспользуемый!)
   - На пуле для движения
   - Универсальное движение по направлению

3. Bullet
   - Обнаружение столкновений
   - Нанесение урона через IDamageable

4. IRightHandComponent / ILeftHandComponent
   - Интерфейсы для слотов оружия
   - Позволяют контроллеру не знать о реализации

5. ShootController
   - Обработка ввода
   - Вызов Shoot через интерфейсы
```

#### Шаг 4: Реализация

```csharp
// === ИНТЕРФЕЙСЫ ДЛЯ СЛОТОВ ===
public interface IRightHandComponent
{
    void Shoot();
}

public interface ILeftHandComponent
{
    void Shoot();
}

public interface IDamageable
{
    void TakeDamage(int damage);
}

// === БАЗОВЫЙ КОМПОНЕНТ С УСЛОВИЯМИ ===
public class ConditionComponent : MonoBehaviour
{
    protected AndCondition AndCondition = new();

    public void AddCondition(Func<bool> condition)
    {
        AndCondition.AddCondition(condition);
    }
}

// === КОМПОНЕНТ СТРЕЛЬБЫ ===
public class ShootComponent : ConditionComponent
{
    public event Action OnFire;

    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _shootPoint;

    public void Shoot()
    {
        // Проверка всех условий
        if (!AndCondition.IsTrue())
            return;

        // Создание пули
        var bullet = Instantiate(_bulletPrefab, _shootPoint.position, Quaternion.identity);

        // Задание направления через композицию
        if (bullet.TryGetComponent(out MoveComponent moveComponent))
            moveComponent.SetDirection(_shootPoint.forward);

        // Публикация события
        OnFire?.Invoke();
    }
}

// === КОМПОНЕНТ ДВИЖЕНИЯ (универсальный) ===
public class MoveComponent : MonoBehaviour
{
    [SerializeField] private Transform _root;
    [SerializeField] private float _speed = 3f;
    [SerializeField] private Vector3 _moveDirection;
    [SerializeField] private bool _canMove = true;

    private readonly AndCondition _andCondition = new();

    public void SetDirection(Vector3 direction)
    {
        _moveDirection = direction;
    }

    private void Update()
    {
        if (!_canMove || !_andCondition.IsTrue())
            return;

        _root.position += _moveDirection * _speed * Time.deltaTime;
    }

    public void AddCondition(Func<bool> condition)
    {
        _andCondition.AddCondition(condition);
    }
}

// === ПУЛЯ ===
public class Bullet : MonoBehaviour
{
    [SerializeField] private int _damage = 2;

    private void OnTriggerEnter(Collider other)
    {
        // Работа через интерфейс — полиморфизм
        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(_damage);
        }
    }
}

// === КОНТРОЛЛЕР СТРЕЛЬБЫ ===
public class ShootController : MonoBehaviour
{
    [SerializeField] private GameObject _character;

    private IRightHandComponent _rightHandComponent;
    private ILeftHandComponent _leftHandComponent;

    private void Awake()
    {
        // Работаем с интерфейсами!
        _rightHandComponent = _character.GetComponent<IRightHandComponent>();
        _leftHandComponent = _character.GetComponent<ILeftHandComponent>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            _rightHandComponent.Shoot();

        if (Input.GetKeyDown(KeyCode.E))
            _leftHandComponent.Shoot();
    }
}
```

#### Шаг 5: Оркестрация в Character

```csharp
public class Character : MonoBehaviour, IRightHandComponent, ILeftHandComponent
{
    [SerializeField] private LifeComponent _lifeComponent;
    [SerializeField] private ShootComponent _rightHandShootComponent;
    [SerializeField] private ShootComponent _leftHandShootComponent;

    private void Awake()
    {
        // Настройка условий: стрелять можно только если жив
        _rightHandShootComponent.AddCondition(_lifeComponent.IsAlive);
        _leftHandShootComponent.AddCondition(_lifeComponent.IsAlive);
    }

    // Реализация интерфейсов — делегирование в компоненты
    void IRightHandComponent.Shoot() => _rightHandShootComponent.Shoot();
    void ILeftHandComponent.Shoot() => _leftHandShootComponent.Shoot();
}
```

#### Шаг 6: Диаграмма

```
┌─────────────────────────────────────────────────────────────┐
│                     ShootController                          │
│            Обрабатывает ввод (Space, E)                     │
└────────────┬───────────────────────┬────────────────────────┘
             │                       │
             ▼                       ▼
    IRightHandComponent      ILeftHandComponent
             │                       │
             └───────────┬───────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                      Character                               │
│  Реализует оба интерфейса, делегирует в ShootComponent       │
└──────┬─────────────────────────────────────────┬────────────┘
       │                                         │
       ▼                                         ▼
┌──────────────────┐                    ┌──────────────────┐
│ ShootComponent   │                    │ ShootComponent   │
│ (правая рука)    │                    │ (левая рука)     │
│ + AndCondition   │                    │ + AndCondition   │
└────────┬─────────┘                    └────────┬─────────┘
         │ OnFire                                │ OnFire
         │                                       │
         └────────────────┬──────────────────────┘
                          │
                          ▼
               ┌─────────────────────┐
               │      Bullet         │
               │ + MoveComponent     │
               │ + IDamageable       │
               └─────────────────────┘
```

#### Оценка Сложности: ⭐⭐ (Средняя)

**Почему средняя:**
- 4-5 компонентов
- Система интерфейсов для слотов
- Event-driven архитектура
- Паттерн делегирования

---

### Пример 2: Автоматическая Турель (Уровень 3 — Сложная)

#### Шаг 1: Описание

```
ФИЧА: Автоматическая Турель (AI)

ОПИСАНИЕ: Турель автоматически обнаруживает игрока, поворачивается к нему и стреляет

ДЕЙСТВИЯ:
- Обнаружить игрока в радиусе
- Повернуться в направлении игрока
- Стрелять когда готова
- Перезарядиться после выстрела
- Умереть при получении урона

УСЛОВИЯ:
- Стрельба: жива, есть цель, перезаряжена
- Поворот: жива
- Получение урона: через IDamageable

ЗАВИСИМОСТИ:
- LifeComponent
- ShootComponent
- ReloadComponent
- RotateComponent
- DetectTargetComponent
```

#### Шаг 2: Анализ Аспектов

| Аспект | Есть? | Описание | Компонент |
|--------|-------|----------|-----------|
| Здоровье | ✓ | Получение урона, смерть | LifeComponent |
| Обнаружение | ✓ | Поиск игрока в радиусе | DetectTargetComponent |
| Поворот | ✓ | 3D поворот к цели | RotateComponent |
| Стрельба | ✓ | Создание пуль | ShootComponent |
| Перезарядка | ✓ | Cooldown между выстрелами | ReloadComponent |
| AI логика | ✓ | Автоматическое управление | Tower (оркестратор) |

#### Шаг 3: Декомпозиция на Компоненты

```
КОМПОЗИЦИЯ ТУРЕЛИ:

Tower =
    LifeComponent +           // Здоровье
    RotateComponent +         // 3D поворот к цели
    ShootComponent +          // Стрельба
    ReloadComponent +         // Cooldown
    DetectTargetComponent     // Поиск игрока

ОСОБЕННОСТЬ: AI логика встроена в оркестратор (Update)
```

#### Шаг 4: Реализация Компонентов

```csharp
// === ОБНАРУЖЕНИЕ ЦЕЛИ ===
public class DetectTargetComponent : MonoBehaviour
{
    [SerializeField] private float _detectDistance = 3f;
    [SerializeField] private GameObject _character; // Ссылка на игрока

    public Transform GetTarget()
    {
        var direction = _character.transform.position - transform.position;

        // Оптимизация: sqrMagnitude вместо Distance
        if (direction.sqrMagnitude <= _detectDistance * _detectDistance)
        {
            return _character.transform;
        }

        return null;
    }

    public bool HasTarget() => GetTarget() != null;
}

// === 3D ПОВОРОТ ===
public class RotateComponent : MonoBehaviour
{
    [SerializeField] private Transform _rotationRoot;
    [SerializeField] private float _rotateRate = 0.1f;
    [SerializeField] private bool _canRotate = true;

    private Vector3 _rotateDirection;
    private readonly AndCondition _andCondition = new();

    public void SetDirection(Vector3 direction)
    {
        _rotateDirection = direction;
    }

    public void SetDirection(Transform target)
    {
        if (target == null)
            return;

        var direction = target.position - transform.position;
        direction.y = 0f; // Игнорируем вертикаль
        SetDirection(direction);
    }

    private void Update()
    {
        Rotate();
    }

    private void Rotate()
    {
        if (!_canRotate || !_andCondition.IsTrue())
            return;

        if (_rotateDirection == Vector3.zero)
            return;

        // 3D поворот через Quaternion
        var targetRotation = Quaternion.LookRotation(_rotateDirection, Vector3.up);
        _rotationRoot.rotation = Quaternion.Lerp(
            _rotationRoot.rotation,
            targetRotation,
            _rotateRate
        );
    }

    public void AddCondition(Func<bool> condition)
    {
        _andCondition.AddCondition(condition);
    }
}

// === ПЕРЕЗАРЯДКА ===
public class ReloadComponent : MonoBehaviour
{
    [SerializeField] private float _maxTime = 1f;
    private float _currentTime;
    private bool _isReady;

    private void Update()
    {
        _currentTime += Time.deltaTime;

        if (_currentTime > _maxTime && !_isReady)
        {
            _isReady = true;
        }
    }

    public bool IsReady() => _isReady;

    public void Reload()
    {
        _isReady = false;
        _currentTime = 0f;
    }
}

// === ЗДОРОВЬЕ ===
public class LifeComponent : MonoBehaviour, IDamageable
{
    public event Action OnEmpty;

    [SerializeField] private int _maxPoints = 3;
    [SerializeField] private int _hitPoints = 3;
    [SerializeField] private bool _isDead;

    public void TakeDamage(int damage)
    {
        if (_isDead)
            return;

        _hitPoints -= damage;

        if (_hitPoints <= 0)
        {
            _isDead = true;
            OnEmpty?.Invoke();
        }
    }

    public bool IsAlive() => !_isDead;
}
```

#### Шаг 5: Оркестратор (Tower)

```csharp
public class Tower : MonoBehaviour
{
    [SerializeField] private LifeComponent _lifeComponent;
    [SerializeField] private RotateComponent _rotateComponent;
    [SerializeField] private ShootComponent _shootComponent;
    [SerializeField] private ReloadComponent _reloadComponent;
    [SerializeField] private DetectTargetComponent _detectTargetComponent;

    private void Awake()
    {
        // === НАСТРОЙКА УСЛОВИЙ ===

        // Поворот только если жива
        _rotateComponent.AddCondition(_lifeComponent.IsAlive);

        // Стрельба только если:
        _shootComponent.AddCondition(_lifeComponent.IsAlive);           // 1. Жива
        _shootComponent.AddCondition(_detectTargetComponent.HasTarget); // 2. Есть цель
        _shootComponent.AddCondition(_reloadComponent.IsReady);         // 3. Перезаряжена
    }

    private void OnEnable()
    {
        // === ПОДПИСКА НА СОБЫТИЯ ===
        _shootComponent.OnFire += _reloadComponent.Reload;  // Перезарядка после выстрела
        _lifeComponent.OnEmpty += OnHealthEmpty;            // Обработка смерти
    }

    private void OnDisable()
    {
        // === ОТПИСКА ОТ СОБЫТИЙ ===
        _shootComponent.OnFire -= _reloadComponent.Reload;
        _lifeComponent.OnEmpty -= OnHealthEmpty;
    }

    private void Update()
    {
        // === AI ЛОГИКА ===
        // Поворот к цели (если есть)
        _rotateComponent.SetDirection(_detectTargetComponent.GetTarget());

        // Попытка выстрела (условия проверяются внутри ShootComponent)
        _shootComponent.Shoot();
    }

    private void OnHealthEmpty()
    {
        Destroy(gameObject);
    }
}
```

#### Шаг 6: Диаграмма

```
┌─────────────────────────────────────────────────────────────┐
│                       Tower (Оркестратор)                    │
│  - Настраивает условия в Awake                              │
│  - Подписывается на события в OnEnable                      │
│  - AI логика в Update                                       │
└─────────────────────────────────────────────────────────────┘
         │            │              │             │
    ┌────┴────┐  ┌────┴────┐  ┌─────┴─────┐  ┌────┴────┐
    │  Life   │  │ Rotate  │  │  Shoot    │  │ Reload  │
    │Component│  │Component│  │ Component │  │Component│
    └────┬────┘  └─────────┘  └─────┬─────┘  └────┬────┘
         │                          │             │
         │                          │ OnFire ─────┘
    OnEmpty                         │
         │                          ▼
         ▼                    ┌───────────┐
    Destroy()                 │  Bullet   │
                              └───────────┘

    ┌────────────────────┐
    │ DetectTarget       │ GetTarget(), HasTarget()
    │ Component          │ ◄── условие для ShootComponent
    └────────────────────┘
```

#### Шаг 7: SOLID Валидация

| Принцип | Проверка | Статус |
|---------|----------|--------|
| **S** — Single Responsibility | Каждый компонент: одна задача | ✅ |
| **O** — Open/Closed | Условия через AddCondition | ✅ |
| **L** — Liskov Substitution | IDamageable работает с любой реализацией | ✅ |
| **I** — Interface Segregation | IDamageable — минимальный интерфейс | ✅ |
| **D** — Dependency Inversion | Bullet зависит от IDamageable | ✅ |

#### Оценка Сложности: ⭐⭐⭐ (Сложная)

**Почему сложная:**
- 5+ компонентов
- AI логика
- Множественные условия
- Event-driven связи
- 3D математика (Quaternion)

---

### Пример 3: Система Слотов Оружия (Паттерн)

#### Описание Паттерна

**Проблема:** Персонажу нужно несколько независимых слотов для оружия с разным управлением.

**Решение:** Интерфейсы для каждого слота + делегирование в Character.

#### Шаблон Реализации

```csharp
// === ШАГ 1: Определить интерфейсы для слотов ===
public interface IRightHandComponent
{
    void Shoot();
}

public interface ILeftHandComponent
{
    void Shoot();
}

// Можно добавить больше слотов:
public interface ISpecialAbilityComponent
{
    void UseAbility();
}

public interface IMeleeComponent
{
    void Attack();
}

// === ШАГ 2: Реализовать интерфейсы в Character ===
public class Character : MonoBehaviour,
    IRightHandComponent,
    ILeftHandComponent,
    ISpecialAbilityComponent
{
    [Header("Weapon Slots")]
    [SerializeField] private ShootComponent _rightHandWeapon;
    [SerializeField] private ShootComponent _leftHandWeapon;
    [SerializeField] private AbilityComponent _specialAbility;

    // Делегирование в компоненты
    void IRightHandComponent.Shoot() => _rightHandWeapon.Shoot();
    void ILeftHandComponent.Shoot() => _leftHandWeapon.Shoot();
    void ISpecialAbilityComponent.UseAbility() => _specialAbility.Use();
}

// === ШАГ 3: Контроллер работает через интерфейсы ===
public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject _character;

    private IRightHandComponent _rightHand;
    private ILeftHandComponent _leftHand;
    private ISpecialAbilityComponent _ability;

    private void Awake()
    {
        // Получаем интерфейсы — не знаем о конкретной реализации
        _rightHand = _character.GetComponent<IRightHandComponent>();
        _leftHand = _character.GetComponent<ILeftHandComponent>();
        _ability = _character.GetComponent<ISpecialAbilityComponent>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            _rightHand?.Shoot();

        if (Input.GetKeyDown(KeyCode.E))
            _leftHand?.Shoot();

        if (Input.GetKeyDown(KeyCode.Q))
            _ability?.UseAbility();
    }
}
```

#### Преимущества Паттерна

| Преимущество | Описание |
|--------------|----------|
| **Расширяемость** | Легко добавить новые слоты без изменения контроллера |
| **Полиморфизм** | Разные персонажи могут реализовывать слоты по-разному |
| **Тестируемость** | Можно подменить mock-реализацию для тестов |
| **Гибкость** | Один ShootComponent можно назначить на разные слоты |
| **Независимость** | Контроллер не знает о внутренней реализации персонажа |

---

### Сравнение: Игрок vs AI (один набор компонентов)

#### Ключевой Принцип

Компоненты не знают, кто ими управляет. Источник команд определяется контроллером.

```
┌─────────────────────────────────────────────────────────────┐
│                    ОДИН НАБОР КОМПОНЕНТОВ                    │
│                                                              │
│  LifeComponent + MoveComponent + RotateComponent +           │
│  ShootComponent + ReloadComponent                            │
│                                                              │
└─────────────────────────────────────────────────────────────┘
         │                                    │
         │                                    │
         ▼                                    ▼
┌─────────────────────┐            ┌─────────────────────┐
│   PlayerController  │            │    AIController     │
│   ───────────────── │            │   ─────────────────  │
│   Input.GetKey      │            │   DetectTarget       │
│   Клавиатура/мышь   │            │   Автоматическая     │
│   Мгновенная реакция│            │   логика принятия    │
│                     │            │   решений            │
└─────────────────────┘            └─────────────────────┘
```

#### Сравнительная Таблица

| Аспект | Игрок (Character) | AI (Tower) |
|--------|-------------------|------------|
| **Источник команд** | Input.GetKey | DetectTarget + логика |
| **Компоненты** | Те же самые | Те же самые |
| **Условия** | `IsAlive` | `IsAlive + HasTarget + IsReady` |
| **Update логика** | В контроллере | В оркестраторе |
| **Сложность AI** | N/A | Простой (реактивный) |

---

### Чеклист для Шутера

#### При создании системы стрельбы:

```
□ ShootComponent создан и наследует ConditionComponent?
□ MoveComponent переиспользуется для пули?
□ Bullet использует IDamageable для урона?
□ Интерфейсы для слотов (ILeftHand, IRightHand)?
□ Событие OnFire публикуется?
□ Контроллер работает через интерфейсы?
```

#### При создании AI турели:

```
□ DetectTargetComponent для поиска цели?
□ RotateComponent с 3D поворотом?
□ ReloadComponent для cooldown?
□ Условия: IsAlive + HasTarget + IsReady?
□ События связаны (OnFire → Reload)?
□ AI логика в Update оркестратора?
```

#### Валидация композиции:

```
□ Все компоненты независимы?
□ Можно заменить AI на PlayerController?
□ Интерфейсы минимальны (Interface Segregation)?
□ События используются для связей (слабая связанность)?
□ Условия настраиваются через AddCondition?
```

---

## Заключение

Это руководство дает полный фреймворк для декомпозиции любой фичи на компоненты, от простейших до самых сложных.

**Ключевые принципы:**
1. Всегда начинать с анализа требований
2. Разбивать фичу на аспекты (данные, логика, визуализация, аудио)
3. Группировать аспекты в компоненты по Single Responsibility
4. Использовать события для коммуникации
5. Использовать условия для управления поведением
6. Оркестратор координирует все компоненты

**Следующие шаги:**
1. Практиковаться на простых механиках
2. Постепенно переходить к более сложным
3. Создавать свои переиспользуемые компоненты
4. Строить библиотеку компонентов для проекта

---

**Версия:** 2.0
**Дата:** 2025-11-27
**Автор:** Senior Unity Developer & Prompt Engineer

### История изменений

| Версия | Дата | Изменения |
|--------|------|-----------|
| 1.0 | 2025-11-24 | Начальная версия с примерами декомпозиции фич |
| 2.0 | 2025-11-27 | Добавлен раздел "Примеры из 3D Шутера (GameShooter)": система стрельбы, AI турель, слоты оружия |
