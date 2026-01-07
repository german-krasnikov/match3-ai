# Unity MVP Architecture Guide
## Руководство по Архитектуре MVP

---

## 🎯 Главный принцип

> **Простота превыше всего: SOLID, DRY, KISS — применяй без фанатизма.**

- Минимально достаточные решения
- Не абстракции ради абстракций
- Понятный код важнее "умного" кода
- Если сомневаешься — выбирай проще

---

## 📋 Архитектура MVP + Entry Point + Service Locator

### Обзор

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
└─────────────────────────┘       └─────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         ИГРА ГОТОВА                              │
└─────────────────────────────────────────────────────────────────┘
```

### Компоненты

| Компонент | Тип | Что делает | Тестируется |
|-----------|-----|------------|-------------|
| **Model** | Чистый C# | Бизнес-логика и состояние | ✅ Да |
| **Presenter** | Чистый C# | Связывает Model ↔ View | ✅ Да (с mock) |
| **View** | MonoBehaviour | UI, эффекты, реализует интерфейс | ❌ Нет |
| **Service** | Чистый C# | Глобальная функциональность | ✅ Да |
| **Config** | ScriptableObject | Настройки | ❌ Нет |

---

## 🔧 Service Locator

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

---

## 🎮 MVP: Пример реализации

### Model (чистый C#)

```csharp
namespace Features.Player
{
    public class PlayerModel
    {
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        public event Action<int, int> OnHealthChanged;
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

### View Interface

```csharp
namespace Features.Player
{
    public interface IPlayerView
    {
        void UpdateHealthBar(float normalized);
        void PlayHitEffect();
        void PlayDeathEffect();
        
        event Action OnHealRequested;
    }
}
```

### Presenter (чистый C#)

```csharp
namespace Features.Player
{
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
            
            _model.OnHealthChanged += HandleHealthChanged;
            _model.OnDied += HandleDeath;
            _view.OnHealRequested += HandleHealRequested;
            
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

        private void HandleHealthChanged(int current, int max) => UpdateHealthDisplay();

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

### View (MonoBehaviour)

```csharp
namespace Features.Player
{
    public class PlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private Slider _healthBar;
        [SerializeField] private ParticleSystem _hitVFX;
        [SerializeField] private ParticleSystem _deathVFX;

        public event Action OnHealRequested;

        private PlayerPresenter _presenter;

        public void Initialize()
        {
            var config = Services.Get<IGameConfig>();
            var audio = Services.Get<IAudioService>();
            
            var model = new PlayerModel(config.PlayerMaxHealth);
            _presenter = new PlayerPresenter(model, this, audio);
        }

        private void OnDestroy() => _presenter?.Dispose();

        public void UpdateHealthBar(float normalized) => _healthBar.value = normalized;
        public void PlayHitEffect() => _hitVFX?.Play();
        public void PlayDeathEffect() => _deathVFX?.Play();

        public void ApplyDamage(int damage) => _presenter.TakeDamage(damage);
    }
}
```

---

## 📐 SOLID: Только важное

### SRP — Single Responsibility

Каждый класс делает одно:
- **Model** — логика
- **View** — отображение  
- **Presenter** — связывание
- **Service** — одна функция

```csharp
// ❌ Плохо: God Object
public class Player : MonoBehaviour
{
    void Update()
    {
        HandleInput();
        Move();
        CheckHealth();
        UpdateHealthBar();
        PlaySounds();
        // ... 500 строк
    }
}

// ✅ Хорошо: разделено
public class PlayerModel { /* логика */ }
public class PlayerView : MonoBehaviour { /* UI */ }
public class PlayerPresenter { /* связь */ }
```

### DIP — Dependency Inversion

Зависи от интерфейсов, не от реализаций:

```csharp
// ❌ Плохо: зависимость от реализации
public class PlayerPresenter
{
    private readonly PlayerView _view; // Конкретный класс!
}

// ✅ Хорошо: зависимость от интерфейса
public class PlayerPresenter
{
    private readonly IPlayerView _view; // Интерфейс — можно мокать
}
```

---

## ❌ Типичные ошибки

### Логика в View
```csharp
// ❌ Плохо
public class PlayerView : MonoBehaviour
{
    public void TakeDamage(int damage)
    {
        _health -= damage;  // Логика в View!
    }
}

// ✅ Хорошо
public void ApplyDamage(int damage)
{
    _presenter.TakeDamage(damage);  // Делегируем
}
```

### Сервисы в Model
```csharp
// ❌ Плохо
public class PlayerModel
{
    public void TakeDamage(int damage)
    {
        _health -= damage;
        Services.Get<IAudioService>().PlaySound("Hit");  // Model знает о сервисах!
    }
}

// ✅ Хорошо — звук в Presenter
```

### Забыли отписаться
```csharp
// ❌ Плохо — утечка памяти
public PlayerPresenter(...)
{
    _model.OnDied += HandleDeath;
    // Нет Dispose!
}

// ✅ Хорошо
public void Dispose()
{
    _model.OnDied -= HandleDeath;
}
```

### View создаёт до сервисов
```csharp
// ❌ Плохо
private void Awake()
{
    var config = Services.Get<IGameConfig>();  // Сервисы ещё не готовы!
}

// ✅ Хорошо — Initialize() вызывается из EntryPoint
public void Initialize()
{
    var config = Services.Get<IGameConfig>();  // OK
}
```

---

## 📁 Структура проекта

```
Assets/
├── Scripts/
│   ├── Game.asmdef
│   ├── Core/
│   │   ├── ServiceLocator.cs
│   │   ├── Services.cs
│   │   └── GameEntryPoint.cs
│   ├── Features/
│   │   └── {Feature}/
│   │       ├── {Feature}Model.cs
│   │       ├── {Feature}Presenter.cs
│   │       ├── {Feature}View.cs
│   │       └── I{Feature}View.cs
│   ├── Configs/
│   └── Editor/
│       └── Editor.asmdef
└── Tests/
    └── EditMode/
        └── Tests.EditMode.asmdef
```

---

## 📊 Быстрый справочник

| Вопрос | Ответ |
|--------|-------|
| Где логика? | Model |
| Где UI? | View |
| Где связывание? | Presenter |
| Где звук/сохранения? | Service |
| Что тестируем? | Model + Presenter |
| Что НЕ тестируем? | View |
| Как мокать View? | Через интерфейс |

---

## ✅ Чеклист

- [ ] Model — чистый C#, без Unity
- [ ] Model не знает о View
- [ ] View реализует интерфейс
- [ ] View максимально "глупый"
- [ ] Presenter получает IView, не конкретный класс
- [ ] Presenter реализует IDisposable
- [ ] EntryPoint имеет Script Execution Order: -100
- [ ] Сервисы регистрируются ДО Initialize()

---

**Версия:** 2.0  
**Принцип:** Простота превыше всего
