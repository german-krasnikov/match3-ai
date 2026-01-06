# Unity MVP Architecture Guide
## Руководство по Архитектуре, Декомпозиции и Лучшим Практикам

---

## 📋 Содержание

1. [Введение](#введение)
2. [Архитектура MVP + Entry Point + Service Locator](#архитектура-mvp--entry-point--service-locator)
3. [SOLID Принципы в Unity](#solid-принципы-в-unity)
4. [Композиция vs Наследование](#композиция-vs-наследование)
5. [Дробление Фич на Компоненты](#дробление-фич-на-компоненты)
6. [Структура Проекта](#структура-проекта)
7. [Паттерны и Практики](#паттерны-и-практики)
8. [Чеклисты](#чеклисты)
9. [Примеры Реализации](#примеры-реализации)
10. [Типичные Ошибки](#типичные-ошибки)

---

## Введение

### Цель документа

Этот гайд описывает архитектурный подход **MVP (Model-View-Presenter) + Entry Point + Service Locator** для Unity-проектов. Подход сочетает:

- **Гибкость** — легко тестировать, расширять и поддерживать
- **Чистую архитектуру** — разделение логики и представления
- **Композицию** — переиспользуемые компоненты вместо наследования
- **SOLID принципы** — качественный, масштабируемый код

### Ключевые преимущества

| Аспект | Преимущество |
|--------|--------------|
| **Тестируемость** | Model и Presenter — чистый C#, тестируются без Unity |
| **Гибкость** | Сервисы легко подменять (моки для тестов) |
| **Модульность** | Фичи изолированы друг от друга |
| **Читаемость** | Чёткое разделение ответственности |
| **Масштабируемость** | Легко добавлять новые фичи |

---

## Архитектура MVP + Entry Point + Service Locator

### Обзор архитектуры

```
┌─────────────────────────────────────────────────────────────────┐
│                        СЦЕНА ЗАГРУЖЕНА                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      GameEntryPoint.Awake()                      │
│                    (Script Execution Order: -100)                │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
┌─────────────────────────┐       ┌─────────────────────────┐
│     SetupServices()     │       │      SetupGame()        │
│                         │       │                         │
│  • ServiceLocator       │       │  • View.Initialize()    │
│  • IAudioService        │       │  • new Model()          │
│  • IInputService        │       │  • new Presenter()      │
│  • IGameConfig          │       │                         │
└─────────────────────────┘       └─────────────────────────┘
              │                               │
              └───────────────┬───────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         ИГРА ГОТОВА                              │
└─────────────────────────────────────────────────────────────────┘
```

### Компоненты архитектуры

| Компонент | Тип | Что делает |
|-----------|-----|------------|
| **GameEntryPoint** | MonoBehaviour | Создаёт сервисы, связывает объекты, запускает игру |
| **ServiceLocator** | Чистый C# | Dictionary сервисов по типу |
| **Services** | Static class | Глобальный доступ `Services.Get<T>()` |
| **Model** | Чистый C# | Бизнес-логика и состояние (тестируется) |
| **Presenter** | Чистый C# | Связывает Model ↔ View (тестируется) |
| **View** | MonoBehaviour | Пассивное отображение, UI, эффекты |
| **Configs** | ScriptableObject | Настройки игры |

### Service Locator

```csharp
// ServiceLocator.cs
namespace Core
{
    public class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
            
            throw new Exception($"Сервис {typeof(T).Name} не зарегистрирован!");
        }

        public void Clear() => _services.Clear();
    }
}
```

```csharp
// Services.cs — глобальный доступ
namespace Core
{
    public static class Services
    {
        private static ServiceLocator _locator;

        public static void Initialize(ServiceLocator locator) => _locator = locator;
        public static T Get<T>() where T : class => _locator.Get<T>();
        public static void Reset() => _locator = null;
    }
}
```

### Game Entry Point

```csharp
// GameEntryPoint.cs
namespace Core
{
    /// <summary>
    /// Точка входа игры.
    /// Настраивает сервисы, связывает объекты, запускает игру.
    /// 
    /// Настройка:
    /// 1. Создайте пустой GameObject "[EntryPoint]"
    /// 2. Добавьте этот скрипт
    /// 3. Перетащите зависимости в Inspector
    /// 4. Установите Script Execution Order: -100
    /// </summary>
    public class GameEntryPoint : MonoBehaviour
    {
        [Header("=== Configs ===")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private AudioConfig _audioConfig;
        
        [Header("=== Scene References ===")]
        [SerializeField] private PlayerView _playerView;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private UIManager _uiManager;
        
        [Header("=== MonoBehaviour Services ===")]
        [SerializeField] private AudioSourcePool _audioSourcePool;

        private ServiceLocator _locator;

        private void Awake()
        {
            SetupServices();
            SetupGame();
        }

        private void SetupServices()
        {
            _locator = new ServiceLocator();
            Services.Initialize(_locator);
            
            // Регистрируем сервисы
            _locator.Register<IGameConfig>(_gameConfig);
            _locator.Register<IAudioService>(new AudioService(_audioConfig, _audioSourcePool));
            _locator.Register<IInputService>(new InputService());
            
            Debug.Log("[EntryPoint] Сервисы готовы");
        }

        private void SetupGame()
        {
            // View инициализируются ПОСЛЕ регистрации сервисов
            _playerView.Initialize();
            _enemySpawner.Initialize(_playerView);
            _uiManager.Initialize();
            
            Debug.Log("[EntryPoint] Игра запущена");
        }

        private void OnDestroy()
        {
            _locator?.Clear();
            Services.Reset();
        }
    }
}
```

### MVP Паттерн для фичи

#### Model (чистый C#)

```csharp
// PlayerModel.cs
namespace Features.Player
{
    /// <summary>
    /// Бизнес-логика игрока.
    /// Чистый C# — тестируется без Unity.
    /// </summary>
    public class PlayerModel
    {
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        public event Action<int, int> OnHealthChanged; // current, max
        public event Action OnDied;

        public PlayerModel(int maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            if (!IsAlive || damage <= 0) return;
            
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            
            if (!IsAlive)
                OnDied?.Invoke();
        }

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}
```

#### View Interface

```csharp
// IPlayerView.cs
namespace Features.Player
{
    /// <summary>
    /// Контракт View для тестирования Presenter без Unity.
    /// </summary>
    public interface IPlayerView
    {
        void UpdateHealthBar(float normalized);
        void PlayHitEffect();
        void PlayDeathEffect();
        
        event Action OnHealRequested;
    }
}
```

#### Presenter (чистый C#)

```csharp
// PlayerPresenter.cs
namespace Features.Player
{
    /// <summary>
    /// Связывает Model и View.
    /// Чистый C# — тестируется без Unity.
    /// </summary>
    public class PlayerPresenter : IDisposable
    {
        private readonly PlayerModel _model;
        private readonly IPlayerView _view;
        private readonly IAudioService _audio;

        public PlayerPresenter(PlayerModel model, IPlayerView view, IAudioService audio)
        {
            _model = model;
            _view = view;
            _audio = audio;
            
            // Подписки на Model
            _model.OnHealthChanged += HandleHealthChanged;
            _model.OnDied += HandleDeath;
            
            // Подписки на View
            _view.OnHealRequested += HandleHealRequested;
            
            // Начальное состояние
            UpdateHealthDisplay();
        }

        public void TakeDamage(int damage)
        {
            _model.TakeDamage(damage);
            
            if (damage > 0 && _model.IsAlive)
            {
                _view.PlayHitEffect();
                _audio.PlaySound("Hit");
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            UpdateHealthDisplay();
        }

        private void HandleDeath()
        {
            _view.PlayDeathEffect();
            _audio.PlaySound("Death");
        }

        private void HandleHealRequested()
        {
            _model.Heal(25);
            _audio.PlaySound("Heal");
        }

        private void UpdateHealthDisplay()
        {
            float normalized = (float)_model.CurrentHealth / _model.MaxHealth;
            _view.UpdateHealthBar(normalized);
        }

        public void Dispose()
        {
            _model.OnHealthChanged -= HandleHealthChanged;
            _model.OnDied -= HandleDeath;
            _view.OnHealRequested -= HandleHealRequested;
        }
    }
}
```

#### View (MonoBehaviour)

```csharp
// PlayerView.cs
namespace Features.Player
{
    /// <summary>
    /// Пассивное представление игрока.
    /// Только отображение и пользовательский ввод.
    /// </summary>
    public class PlayerView : MonoBehaviour, IPlayerView
    {
        [Header("UI")]
        [SerializeField] private Slider _healthBar;
        [SerializeField] private Button _healButton;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem _hitVFX;
        [SerializeField] private ParticleSystem _deathVFX;

        public event Action OnHealRequested;

        private PlayerPresenter _presenter;

        /// <summary>
        /// Вызывается из EntryPoint ПОСЛЕ регистрации сервисов.
        /// </summary>
        public void Initialize()
        {
            // Получаем сервисы
            var config = Services.Get<IGameConfig>();
            var audio = Services.Get<IAudioService>();
            
            // Создаём Model и Presenter
            var model = new PlayerModel(config.PlayerMaxHealth);
            _presenter = new PlayerPresenter(model, this, audio);
            
            // UI подписки
            _healButton.onClick.AddListener(() => OnHealRequested?.Invoke());
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }

        // === IPlayerView реализация ===

        public void UpdateHealthBar(float normalized)
        {
            _healthBar.value = normalized;
        }

        public void PlayHitEffect()
        {
            _hitVFX?.Play();
        }

        public void PlayDeathEffect()
        {
            _deathVFX?.Play();
        }

        // === Public API для внешних систем ===

        public void ApplyDamage(int damage)
        {
            _presenter.TakeDamage(damage);
        }
    }
}
```

---

## SOLID Принципы в Unity

### S — Single Responsibility Principle

**Определение:** Каждый класс/компонент имеет только одну причину для изменения.

#### ✅ Правильно: разделённые ответственности

```csharp
// Model — только бизнес-логика
public class PlayerModel
{
    public int CurrentHealth { get; private set; }
    public void TakeDamage(int damage) { /* логика */ }
}

// View — только отображение
public class PlayerView : MonoBehaviour, IPlayerView
{
    public void UpdateHealthBar(float normalized) { /* UI */ }
    public void PlayHitEffect() { /* эффекты */ }
}

// Presenter — только связывание
public class PlayerPresenter
{
    public void TakeDamage(int damage)
    {
        _model.TakeDamage(damage);
        _view.PlayHitEffect();
        _audio.PlaySound("Hit");
    }
}

// Сервис — только воспроизведение звука
public class AudioService : IAudioService
{
    public void PlaySound(string name) { /* звук */ }
}
```

#### ❌ Неправильно: God Object

```csharp
// Всё в одном классе — плохо!
public class Player : MonoBehaviour
{
    void Update()
    {
        HandleInput();      // Ввод
        Move();             // Движение
        CheckHealth();      // Логика здоровья
        UpdateHealthBar();  // UI
        PlaySounds();       // Звук
        PlayEffects();      // Эффекты
        // ... 500 строк кода
    }
}
```

### O — Open/Closed Principle

**Определение:** Открыт для расширения, закрыт для модификации.

#### ✅ Правильно: расширение через события

```csharp
// Model публикует события
public class PlayerModel
{
    public event Action OnDied;
    public event Action<int, int> OnHealthChanged;
    
    public void TakeDamage(int damage)
    {
        _health -= damage;
        OnHealthChanged?.Invoke(_health, _maxHealth);
        
        if (_health <= 0)
            OnDied?.Invoke();
    }
}

// Presenter подписывается и расширяет поведение
// БЕЗ изменения Model
public class PlayerPresenter
{
    public PlayerPresenter(...)
    {
        _model.OnDied += HandleDeath;
        _model.OnHealthChanged += HandleHealthChanged;
    }
    
    private void HandleDeath()
    {
        _view.PlayDeathEffect();
        _audio.PlaySound("Death");
        // Можно добавить что угодно без изменения Model
    }
}
```

#### ✅ Правильно: система условий

```csharp
// Компонент с расширяемыми условиями
public class ActionComponent
{
    private readonly List<Func<bool>> _conditions = new();
    
    public void AddCondition(Func<bool> condition)
        => _conditions.Add(condition);
    
    public bool CanExecute()
    {
        foreach (var condition in _conditions)
            if (!condition()) return false;
        return true;
    }
}

// Расширяем БЕЗ модификации ActionComponent
actionComponent.AddCondition(() => _model.IsAlive);
actionComponent.AddCondition(() => !_isStunned);
actionComponent.AddCondition(() => _cooldown.IsReady);
```

### L — Liskov Substitution Principle

**Определение:** Подтипы должны быть заменяемы базовыми типами.

#### ✅ Правильно: работа через интерфейсы

```csharp
// Интерфейс
public interface IDamageable
{
    void TakeDamage(int damage);
    bool IsAlive { get; }
}

// Разные реализации
public class PlayerModel : IDamageable { /* ... */ }
public class EnemyModel : IDamageable { /* ... */ }
public class DestructibleModel : IDamageable { /* ... */ }

// Код работает с любой реализацией
public class DamageDealer
{
    public void DealDamage(IDamageable target, int damage)
    {
        if (target.IsAlive)
            target.TakeDamage(damage);
    }
}
```

### I — Interface Segregation Principle

**Определение:** Много маленьких интерфейсов лучше одного большого.

#### ✅ Правильно: маленькие интерфейсы

```csharp
public interface IDamageable
{
    void TakeDamage(int damage);
}

public interface IHealable
{
    void Heal(int amount);
}

public interface IMovable
{
    void Move(Vector3 direction);
}

// Класс реализует только то, что нужно
public class PlayerModel : IDamageable, IHealable
{
    public void TakeDamage(int damage) { /* ... */ }
    public void Heal(int amount) { /* ... */ }
    // Не реализует IMovable — это не его ответственность
}
```

#### ❌ Неправильно: толстый интерфейс

```csharp
// Слишком много методов
public interface IGameEntity
{
    void TakeDamage(int damage);
    void Heal(int amount);
    void Move(Vector3 direction);
    void Attack();
    void UseItem(Item item);
    void LevelUp();
}
```

### D — Dependency Inversion Principle

**Определение:** Зависимости от абстракций, а не от конкретных реализаций.

#### ✅ Правильно: зависимость от интерфейсов

```csharp
// Presenter зависит от интерфейсов
public class PlayerPresenter
{
    private readonly IPlayerView _view;      // Интерфейс
    private readonly IAudioService _audio;   // Интерфейс
    
    public PlayerPresenter(
        PlayerModel model,
        IPlayerView view,      // Можно подменить mock'ом
        IAudioService audio)   // Можно подменить mock'ом
    {
        _view = view;
        _audio = audio;
    }
}

// В тестах легко подменить
var mockView = Substitute.For<IPlayerView>();
var mockAudio = Substitute.For<IAudioService>();
var presenter = new PlayerPresenter(model, mockView, mockAudio);
```

---

## Композиция vs Наследование

### Правило: 95% — композиция, 5% — наследование

### ✅ Используйте композицию когда:

1. **Нужна гибкость в комбинировании**

```csharp
// Разные комбинации компонентов
PlayerModel + HealthComponent + InventoryComponent + MovementModel
EnemyModel + HealthComponent + AIComponent
TurretModel + HealthComponent + ShootingComponent
```

2. **Компонент переиспользуется**

```csharp
// HealthComponent используется везде
public class PlayerModel { private HealthComponent _health; }
public class EnemyModel { private HealthComponent _health; }
public class DestructibleModel { private HealthComponent _health; }
```

3. **Нужна независимость компонентов**

```csharp
// Легко добавлять/убирать функциональность
_model.OnDied += () => _view.PlayDeathEffect();
_model.OnDied += () => _audio.PlaySound("Death");
_model.OnDied += () => _particles.Play();
// Можно добавить что угодно без изменения Model
```

### ✅ Используйте наследование когда:

1. **Template Method — общий алгоритм с вариативными шагами**

```csharp
public abstract class BasePatrolModel
{
    protected Vector3[] _points;
    
    protected abstract Vector3[] InitPoints();
    
    public Vector3 GetCurrentPoint() => _points[_currentIndex];
    public void NextPoint() => _currentIndex = (_currentIndex + 1) % _points.Length;
}

public class WaypointPatrolModel : BasePatrolModel
{
    protected override Vector3[] InitPoints() => _waypoints;
}

public class RandomPatrolModel : BasePatrolModel
{
    protected override Vector3[] InitPoints() => GenerateRandomPoints();
}
```

2. **80%+ общей логики, 1-2 метода различаются**

```csharp
// Только если действительно большая часть логики общая
public abstract class BaseWeaponModel
{
    public int Damage { get; protected set; }
    public float FireRate { get; protected set; }
    
    public virtual void Fire() { /* общая логика */ }
    protected abstract void ApplyDamage(IDamageable target);
}
```

### Правило принятия решения

```
ВОПРОС: Наследование или композиция?

1. Есть ли общий алгоритм с 1-2 вариативными шагами?
   ДА → Рассмотри наследование (Template Method)
   НЕТ → Используй композицию

2. Могу ли я описать отношение как "X является Y"?
   ДА → Рассмотри наследование
   НЕТ → Используй композицию

3. Нужно ли комбинировать это поведение с другими?
   ДА → Используй композицию
   НЕТ → Рассмотри наследование

4. Сомневаешься?
   → Используй композицию (всегда безопаснее!)
```

---

## Дробление Фич на Компоненты

### Алгоритм декомпозиции

```
1. ИДЕНТИФИКАЦИЯ
   Опиши фичу одним предложением
   Пример: "Игрок может получать урон и умирать"

2. ДЕКОМПОЗИЦИЯ
   Какие аспекты включает фича?
   - Логика (хранение здоровья, расчёт урона)
   - Визуализация (UI здоровья, эффект урона)
   - Звук (звук удара, звук смерти)
   - Состояние (жив/мёртв)

3. РАСПРЕДЕЛЕНИЕ ПО MVP
   - Model: логика и состояние
   - View: визуализация
   - Presenter: связывание + звук через сервис

4. ИНТЕРФЕЙСЫ
   Нужны ли интерфейсы для полиморфизма?
   - IDamageable для Model — да (враги тоже получают урон)
   - IPlayerView для View — да (для тестирования)

5. СОБЫТИЯ
   Какие моменты должны уведомлять другие системы?
   - OnHealthChanged → обновление UI
   - OnDied → игровое событие смерти

6. ВАЛИДАЦИЯ
   □ Model содержит только логику?
   □ View содержит только отображение?
   □ Presenter связывает, но не содержит бизнес-логику?
   □ Компоненты тестируемы независимо?
```

### Пример: Система здоровья

#### Шаг 1: Идентификация

> "Игрок имеет здоровье, может получать урон, лечиться и умирать"

#### Шаг 2: Декомпозиция

| Аспект | Ответственность |
|--------|-----------------|
| Логика здоровья | Хранение HP, расчёт урона, проверка смерти |
| UI здоровья | Отображение полоски здоровья |
| Эффект урона | Визуальный эффект при получении урона |
| Звук | Звуки удара, смерти, лечения |

#### Шаг 3: Распределение по MVP

```csharp
// === MODEL ===
public class HealthModel
{
    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    
    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;
    
    public void TakeDamage(int damage) { /* логика */ }
    public void Heal(int amount) { /* логика */ }
}

// === VIEW INTERFACE ===
public interface IHealthView
{
    void UpdateHealthBar(float normalized);
    void PlayDamageEffect();
    void PlayDeathEffect();
}

// === VIEW ===
public class HealthView : MonoBehaviour, IHealthView
{
    [SerializeField] private Slider _healthBar;
    [SerializeField] private ParticleSystem _damageVFX;
    [SerializeField] private ParticleSystem _deathVFX;
    
    public void UpdateHealthBar(float normalized) => _healthBar.value = normalized;
    public void PlayDamageEffect() => _damageVFX?.Play();
    public void PlayDeathEffect() => _deathVFX?.Play();
}

// === PRESENTER ===
public class HealthPresenter : IDisposable
{
    private readonly HealthModel _model;
    private readonly IHealthView _view;
    private readonly IAudioService _audio;
    
    public void TakeDamage(int damage)
    {
        _model.TakeDamage(damage);
        
        if (damage > 0 && _model.IsAlive)
        {
            _view.PlayDamageEffect();
            _audio.PlaySound("Hit");
        }
    }
}
```

### Пример: Система стрельбы

#### Декомпозиция

| Аспект | Компонент |
|--------|-----------|
| Логика стрельбы | ShootingModel |
| Перезарядка | ReloadModel |
| Визуализация | ShootingView |
| Связывание | ShootingPresenter |

```csharp
// === MODELS ===
public class ShootingModel
{
    public event Action OnFired;
    
    private readonly List<Func<bool>> _conditions = new();
    
    public void AddCondition(Func<bool> condition)
        => _conditions.Add(condition);
    
    public bool TryFire()
    {
        if (!CanFire()) return false;
        
        OnFired?.Invoke();
        return true;
    }
    
    private bool CanFire()
    {
        foreach (var condition in _conditions)
            if (!condition()) return false;
        return true;
    }
}

public class ReloadModel
{
    public float MaxTime { get; }
    public float CurrentTime { get; private set; }
    public bool IsReady => CurrentTime >= MaxTime;
    
    public void Update(float deltaTime)
    {
        if (CurrentTime < MaxTime)
            CurrentTime += deltaTime;
    }
    
    public void Reload() => CurrentTime = 0;
}

// === PRESENTER ===
public class ShootingPresenter : IDisposable
{
    private readonly ShootingModel _shooting;
    private readonly ReloadModel _reload;
    private readonly IShootingView _view;
    private readonly IAudioService _audio;
    
    public ShootingPresenter(...)
    {
        // Добавляем условия стрельбы
        _shooting.AddCondition(() => _reload.IsReady);
        _shooting.AddCondition(() => _ammo.HasAmmo);
        
        _shooting.OnFired += HandleFired;
    }
    
    public void TryFire()
    {
        if (_shooting.TryFire())
        {
            _reload.Reload();
        }
    }
    
    private void HandleFired()
    {
        _view.PlayMuzzleFlash();
        _view.SpawnBullet();
        _audio.PlaySound("Shoot");
    }
}
```

---

## Структура Проекта

```
Assets/
├── Scenes/
│   └── Game.unity                    # Основная сцена
│
├── Scripts/
│   ├── Core/                         # Ядро архитектуры
│   │   ├── GameEntryPoint.cs         # Точка входа
│   │   ├── ServiceLocator.cs         # Реестр сервисов
│   │   └── Services.cs               # Статический доступ
│   │
│   ├── Services/                     # Глобальные сервисы
│   │   ├── Audio/
│   │   │   ├── IAudioService.cs
│   │   │   ├── AudioService.cs
│   │   │   └── AudioSourcePool.cs
│   │   ├── Input/
│   │   │   ├── IInputService.cs
│   │   │   └── InputService.cs
│   │   └── Save/
│   │       ├── ISaveService.cs
│   │       └── SaveService.cs
│   │
│   ├── Configs/                      # ScriptableObjects
│   │   ├── GameConfig.cs
│   │   ├── AudioConfig.cs
│   │   └── PlayerConfig.cs
│   │
│   ├── Common/                       # Утилиты
│   │   ├── AndCondition.cs
│   │   ├── Extensions.cs
│   │   └── Constants.cs
│   │
│   └── Features/                     # Фичи (MVP)
│       ├── Player/
│       │   ├── Models/
│       │   │   ├── PlayerModel.cs
│       │   │   └── HealthModel.cs
│       │   ├── Views/
│       │   │   ├── IPlayerView.cs
│       │   │   └── PlayerView.cs
│       │   └── Presenters/
│       │       └── PlayerPresenter.cs
│       │
│       ├── Enemy/
│       │   ├── Models/
│       │   │   ├── EnemyModel.cs
│       │   │   └── AIModel.cs
│       │   ├── Views/
│       │   │   ├── IEnemyView.cs
│       │   │   └── EnemyView.cs
│       │   └── Presenters/
│       │       └── EnemyPresenter.cs
│       │
│       └── UI/
│           ├── HUD/
│           │   ├── HUDModel.cs
│           │   ├── IHUDView.cs
│           │   ├── HUDView.cs
│           │   └── HUDPresenter.cs
│           └── Menu/
│               └── ...
│
├── Tests/
│   ├── EditMode/
│   │   ├── PlayerModelTests.cs
│   │   └── PlayerPresenterTests.cs
│   └── PlayMode/
│       └── IntegrationTests.cs
│
└── Resources/
    └── Configs/
        ├── GameConfig.asset
        └── AudioConfig.asset
```

---

## Паттерны и Практики

### 1. Event-Driven Architecture

```csharp
// Model публикует события
public class PlayerModel
{
    public event Action OnDied;
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnScoreChanged;
}

// Presenter подписывается
public class PlayerPresenter
{
    public PlayerPresenter(...)
    {
        _model.OnHealthChanged += HandleHealthChanged;
        _model.OnDied += HandleDeath;
    }
    
    public void Dispose()
    {
        _model.OnHealthChanged -= HandleHealthChanged;
        _model.OnDied -= HandleDeath;
    }
}
```

### 2. Condition System

```csharp
// Универсальный класс условий
public sealed class AndCondition
{
    private readonly List<Func<bool>> _conditions = new();

    public void AddCondition(Func<bool> condition)
        => _conditions.Add(condition);

    public void RemoveCondition(Func<bool> condition)
        => _conditions.Remove(condition);

    public bool IsTrue()
    {
        foreach (var condition in _conditions)
            if (!condition()) return false;
        return true;
    }
}

// Использование
public class ActionModel
{
    private readonly AndCondition _conditions = new();
    
    public void AddCondition(Func<bool> condition)
        => _conditions.AddCondition(condition);
    
    public bool CanExecute() => _conditions.IsTrue();
}

// Настройка условий в Presenter
_jumpModel.AddCondition(() => _groundChecker.IsGrounded);
_jumpModel.AddCondition(() => _health.IsAlive);
_jumpModel.AddCondition(() => _cooldown.IsReady);
```

### 3. Service Pattern

```csharp
// Интерфейс сервиса
public interface IAudioService
{
    void PlaySound(string name);
    void PlayMusic(string name);
    void StopMusic();
    void SetVolume(float volume);
}

// Реализация
public class AudioService : IAudioService
{
    private readonly AudioConfig _config;
    private readonly AudioSourcePool _pool;

    public AudioService(AudioConfig config, AudioSourcePool pool)
    {
        _config = config;
        _pool = pool;
    }

    public void PlaySound(string name)
    {
        var clip = FindClip(name);
        if (clip != null)
            _pool.PlayOneShot(clip, _config.SFXVolume * _config.MasterVolume);
    }
}

// Использование через Services
var audio = Services.Get<IAudioService>();
audio.PlaySound("Explosion");
```

### 4. Config Pattern (ScriptableObjects)

```csharp
// Интерфейс для тестирования
public interface IGameConfig
{
    int PlayerMaxHealth { get; }
    float PlayerSpeed { get; }
    int StartingLives { get; }
}

// ScriptableObject реализация
[CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/Game")]
public class GameConfig : ScriptableObject, IGameConfig
{
    [Header("Player")]
    [SerializeField] private int _playerMaxHealth = 100;
    [SerializeField] private float _playerSpeed = 5f;
    
    [Header("Game")]
    [SerializeField] private int _startingLives = 3;

    public int PlayerMaxHealth => _playerMaxHealth;
    public float PlayerSpeed => _playerSpeed;
    public int StartingLives => _startingLives;
}

// Регистрация в EntryPoint
_locator.Register<IGameConfig>(_gameConfig);

// Использование
var config = Services.Get<IGameConfig>();
var model = new PlayerModel(config.PlayerMaxHealth);
```

### 5. Factory Pattern для создания объектов

```csharp
// Фабрика врагов
public interface IEnemyFactory
{
    EnemyPresenter Create(EnemyType type, Vector3 position);
}

public class EnemyFactory : IEnemyFactory
{
    private readonly IGameConfig _config;
    private readonly IAudioService _audio;
    private readonly EnemyView _prefab;

    public EnemyFactory(IGameConfig config, IAudioService audio, EnemyView prefab)
    {
        _config = config;
        _audio = audio;
        _prefab = prefab;
    }

    public EnemyPresenter Create(EnemyType type, Vector3 position)
    {
        var view = Object.Instantiate(_prefab, position, Quaternion.identity);
        var model = new EnemyModel(_config.GetEnemyHealth(type));
        var presenter = new EnemyPresenter(model, view, _audio);
        
        return presenter;
    }
}
```

---

## Чеклисты

### Чеклист: Создание новой фичи

```
ПОДГОТОВКА
□ Фича описана одним предложением
□ Определены все аспекты фичи (логика, UI, звук, эффекты)
□ Аспекты распределены по MVP

MODEL
□ Чистый C# (без UnityEngine, кроме Math)
□ Содержит только бизнес-логику
□ Публикует события для важных изменений
□ Реализует интерфейс (если нужен полиморфизм)
□ Покрыт unit-тестами

VIEW INTERFACE
□ Определён интерфейс IXxxView
□ Интерфейс содержит только методы отображения
□ События пользовательского ввода (OnButtonClicked и т.д.)

VIEW (MonoBehaviour)
□ Реализует IXxxView
□ Только отображение и ввод
□ НЕ содержит бизнес-логику
□ Initialize() вызывается из EntryPoint
□ Создаёт Model и Presenter в Initialize()
□ Dispose Presenter в OnDestroy()

PRESENTER
□ Чистый C# (IDisposable)
□ Связывает Model и View
□ Подписывается на события Model и View
□ Использует сервисы через интерфейсы
□ Отписывается в Dispose()
□ Покрыт unit-тестами

ИНТЕГРАЦИЯ
□ View добавлен на сцену
□ View указан в EntryPoint
□ Сервисы зарегистрированы до Initialize()
```

### Чеклист: SOLID проверка

```
S — SINGLE RESPONSIBILITY
□ Model содержит ТОЛЬКО логику и состояние?
□ View содержит ТОЛЬКО отображение?
□ Presenter содержит ТОЛЬКО связывание?
□ Каждый сервис делает ОДНУ вещь?

O — OPEN/CLOSED
□ Новое поведение добавляется через события?
□ Условия добавляются через AddCondition?
□ Не требуется менять существующий код?

L — LISKOV SUBSTITUTION
□ Подтипы заменяемы базовыми типами?
□ Код работает с интерфейсами?

I — INTERFACE SEGREGATION
□ Интерфейсы маленькие и целевые?
□ Классы не реализуют лишние методы?

D — DEPENDENCY INVERSION
□ Зависимости от интерфейсов?
□ Конкретные реализации инжектируются?
□ Можно подменить mock'ами для тестов?
```

### Чеклист: Code Review

```
АРХИТЕКТУРА
□ Соблюдена структура MVP?
□ View не содержит логику?
□ Model не зависит от Unity?
□ Presenter использует интерфейсы?

СОБЫТИЯ
□ Подписка в конструкторе/Initialize?
□ Отписка в Dispose/OnDestroy?
□ Используется ?.Invoke()?

СЕРВИСЫ
□ Сервис зарегистрирован в EntryPoint?
□ Доступ через Services.Get<T>()?
□ Сервис имеет интерфейс?

ТЕСТЫ
□ Model покрыт тестами?
□ Presenter покрыт тестами с mock'ами?
□ Тесты проходят?

ИМЕНОВАНИЕ
□ XxxModel, XxxPresenter, XxxView, IXxxView?
□ Интерфейсы начинаются с I?
□ События начинаются с On?
□ Приватные поля с _?
```

---

## Примеры Реализации

### Полный пример: Враг с AI

```csharp
// === MODELS ===

public class EnemyModel : IDamageable
{
    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    
    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;
    
    public EnemyModel(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }
    
    public void TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0) return;
        
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        
        if (!IsAlive)
            OnDied?.Invoke();
    }
}

public class AIModel
{
    public AIState CurrentState { get; private set; }
    public Transform Target { get; private set; }
    
    public event Action<AIState> OnStateChanged;
    
    public void SetState(AIState state)
    {
        if (CurrentState == state) return;
        CurrentState = state;
        OnStateChanged?.Invoke(state);
    }
    
    public void SetTarget(Transform target) => Target = target;
}

public enum AIState { Idle, Patrol, Chase, Attack }

// === VIEW ===

public interface IEnemyView
{
    void UpdateHealthBar(float normalized);
    void PlayHitEffect();
    void PlayDeathEffect();
    void SetAnimationState(AIState state);
    
    Transform Transform { get; }
}

public class EnemyView : MonoBehaviour, IEnemyView
{
    [Header("UI")]
    [SerializeField] private Slider _healthBar;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem _hitVFX;
    [SerializeField] private ParticleSystem _deathVFX;
    
    [Header("Animation")]
    [SerializeField] private Animator _animator;
    
    public Transform Transform => transform;
    
    private EnemyPresenter _presenter;
    
    public void Initialize(EnemyModel enemyModel, AIModel aiModel)
    {
        var audio = Services.Get<IAudioService>();
        _presenter = new EnemyPresenter(enemyModel, aiModel, this, audio);
    }
    
    private void OnDestroy() => _presenter?.Dispose();
    
    // IEnemyView
    public void UpdateHealthBar(float normalized) => _healthBar.value = normalized;
    public void PlayHitEffect() => _hitVFX?.Play();
    public void PlayDeathEffect() => _deathVFX?.Play();
    public void SetAnimationState(AIState state) => _animator.SetInteger("State", (int)state);
    
    // Public API
    public void ApplyDamage(int damage) => _presenter.TakeDamage(damage);
}

// === PRESENTER ===

public class EnemyPresenter : IDisposable
{
    private readonly EnemyModel _enemy;
    private readonly AIModel _ai;
    private readonly IEnemyView _view;
    private readonly IAudioService _audio;
    
    public EnemyPresenter(EnemyModel enemy, AIModel ai, IEnemyView view, IAudioService audio)
    {
        _enemy = enemy;
        _ai = ai;
        _view = view;
        _audio = audio;
        
        _enemy.OnHealthChanged += HandleHealthChanged;
        _enemy.OnDied += HandleDeath;
        _ai.OnStateChanged += HandleStateChanged;
        
        UpdateHealthDisplay();
    }
    
    public void TakeDamage(int damage)
    {
        _enemy.TakeDamage(damage);
        
        if (damage > 0 && _enemy.IsAlive)
        {
            _view.PlayHitEffect();
            _audio.PlaySound("EnemyHit");
        }
    }
    
    private void HandleHealthChanged(int current, int max)
    {
        UpdateHealthDisplay();
    }
    
    private void HandleDeath()
    {
        _view.PlayDeathEffect();
        _audio.PlaySound("EnemyDeath");
    }
    
    private void HandleStateChanged(AIState state)
    {
        _view.SetAnimationState(state);
    }
    
    private void UpdateHealthDisplay()
    {
        float normalized = (float)_enemy.CurrentHealth / _enemy.MaxHealth;
        _view.UpdateHealthBar(normalized);
    }
    
    public void Dispose()
    {
        _enemy.OnHealthChanged -= HandleHealthChanged;
        _enemy.OnDied -= HandleDeath;
        _ai.OnStateChanged -= HandleStateChanged;
    }
}
```

### Пример: Unit-тесты

```csharp
// EnemyModelTests.cs
[TestFixture]
public class EnemyModelTests
{
    [Test]
    public void TakeDamage_ReducesHealth()
    {
        var model = new EnemyModel(100);
        
        model.TakeDamage(30);
        
        Assert.AreEqual(70, model.CurrentHealth);
    }
    
    [Test]
    public void TakeDamage_WhenKilled_FiresOnDied()
    {
        var model = new EnemyModel(100);
        bool died = false;
        model.OnDied += () => died = true;
        
        model.TakeDamage(100);
        
        Assert.IsTrue(died);
        Assert.IsFalse(model.IsAlive);
    }
    
    [Test]
    public void TakeDamage_WhenAlreadyDead_DoesNothing()
    {
        var model = new EnemyModel(100);
        model.TakeDamage(100); // Kill
        
        model.TakeDamage(50); // Try damage dead enemy
        
        Assert.AreEqual(0, model.CurrentHealth);
    }
}

// EnemyPresenterTests.cs
[TestFixture]
public class EnemyPresenterTests
{
    private EnemyModel _enemy;
    private AIModel _ai;
    private IEnemyView _view;
    private IAudioService _audio;
    private EnemyPresenter _presenter;
    
    [SetUp]
    public void Setup()
    {
        _enemy = new EnemyModel(100);
        _ai = new AIModel();
        _view = Substitute.For<IEnemyView>();
        _audio = Substitute.For<IAudioService>();
        
        _presenter = new EnemyPresenter(_enemy, _ai, _view, _audio);
    }
    
    [TearDown]
    public void TearDown()
    {
        _presenter.Dispose();
    }
    
    [Test]
    public void TakeDamage_UpdatesHealthBar()
    {
        _presenter.TakeDamage(50);
        
        _view.Received().UpdateHealthBar(0.5f);
    }
    
    [Test]
    public void TakeDamage_PlaysEffectAndSound()
    {
        _presenter.TakeDamage(10);
        
        _view.Received().PlayHitEffect();
        _audio.Received().PlaySound("EnemyHit");
    }
    
    [Test]
    public void WhenDied_PlaysDeathEffectAndSound()
    {
        _presenter.TakeDamage(100);
        
        _view.Received().PlayDeathEffect();
        _audio.Received().PlaySound("EnemyDeath");
    }
}
```

---

## Типичные Ошибки

### ❌ Логика в View

```csharp
// ПЛОХО
public class PlayerView : MonoBehaviour
{
    public void TakeDamage(int damage)
    {
        _health -= damage;  // Логика в View!
        if (_health <= 0)
            Die();
    }
}

// ХОРОШО
public class PlayerView : MonoBehaviour
{
    public void ApplyDamage(int damage)
    {
        _presenter.TakeDamage(damage);  // Делегируем Presenter
    }
}
```

### ❌ Прямой доступ к сервисам в Model

```csharp
// ПЛОХО
public class PlayerModel
{
    public void TakeDamage(int damage)
    {
        _health -= damage;
        Services.Get<IAudioService>().PlaySound("Hit");  // Model знает о сервисах!
    }
}

// ХОРОШО — звук в Presenter
public class PlayerPresenter
{
    public void TakeDamage(int damage)
    {
        _model.TakeDamage(damage);
        _audio.PlaySound("Hit");  // Presenter управляет звуком
    }
}
```

### ❌ Забыли отписаться от событий

```csharp
// ПЛОХО — утечка памяти
public class PlayerPresenter
{
    public PlayerPresenter(...)
    {
        _model.OnDied += HandleDeath;
        // Нет отписки!
    }
}

// ХОРОШО
public class PlayerPresenter : IDisposable
{
    public PlayerPresenter(...)
    {
        _model.OnDied += HandleDeath;
    }
    
    public void Dispose()
    {
        _model.OnDied -= HandleDeath;
    }
}
```

### ❌ View создаёт Model до регистрации сервисов

```csharp
// ПЛОХО — сервисы ещё не готовы
public class PlayerView : MonoBehaviour
{
    private void Awake()
    {
        var config = Services.Get<IGameConfig>();  // Ошибка!
    }
}

// ХОРОШО — Initialize вызывается из EntryPoint
public class PlayerView : MonoBehaviour
{
    public void Initialize()  // Вызывается ПОСЛЕ SetupServices
    {
        var config = Services.Get<IGameConfig>();  // OK
    }
}
```

### ❌ God Presenter

```csharp
// ПЛОХО — Presenter делает всё
public class GamePresenter
{
    // Управляет игроком, врагами, UI, звуком, сохранениями...
    // 1000+ строк кода
}

// ХОРОШО — отдельные Presenter для каждой фичи
public class PlayerPresenter { }
public class EnemyPresenter { }
public class HUDPresenter { }
public class MenuPresenter { }
```

---

## Настройка сцены

### Иерархия сцены

```
Game.unity
├── [EntryPoint]              ← GameEntryPoint.cs (Script Order: -100)
├── [Services]
│   └── AudioPool             ← AudioSourcePool.cs
├── [Game]
│   ├── Player                ← PlayerView.cs
│   └── EnemySpawner          ← EnemySpawner.cs
└── [UI]
    ├── HUD                   ← HUDView.cs
    └── Menu                  ← MenuView.cs
```

### Script Execution Order

```
Edit → Project Settings → Script Execution Order

GameEntryPoint: -100   ← Выполняется ПЕРВЫМ
Default Time: 0
```

### Настройка EntryPoint в Inspector

```
Game Entry Point (Script)
├── Configs
│   ├── Game Config: GameConfig.asset
│   └── Audio Config: AudioConfig.asset
├── Scene References
│   ├── Player View: Player
│   ├── Enemy Spawner: EnemySpawner
│   └── HUD View: HUD
└── MonoBehaviour Services
    └── Audio Source Pool: AudioPool
```

---

## Итого

### Ключевые принципы

1. **MVP** — Model (логика), View (отображение), Presenter (связывание)
2. **Entry Point** — единая точка входа, инициализирует всё
3. **Service Locator** — централизованный доступ к сервисам
4. **Композиция > Наследование** — в 95% случаев
5. **SOLID** — качественный, масштабируемый код
6. **Event-Driven** — слабое связывание через события
7. **Тестируемость** — Model и Presenter тестируются без Unity

### Порядок выполнения

```
1. Сцена загружается
2. GameEntryPoint.Awake() (Script Order: -100)
3. SetupServices() — регистрация сервисов
4. SetupGame() — View.Initialize()
5. View создаёт Model и Presenter
6. Игра готова
```

### Быстрый справочник

| Компонент | Тип | Содержит | Тестируется |
|-----------|-----|----------|-------------|
| Model | Чистый C# | Логика, состояние | Да, unit-тесты |
| View | MonoBehaviour | UI, эффекты | Нет |
| Presenter | Чистый C# | Связывание | Да, с mock'ами |
| Service | Чистый C# | Глобальная функциональность | Да |
| Config | ScriptableObject | Настройки | Нет |

---

**Версия:** 1.0  
**Для использования с:** Claude Code  
**Архитектура:** MVP + Entry Point + Service Locator
