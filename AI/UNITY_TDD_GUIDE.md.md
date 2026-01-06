# 🧪 Полное руководство по TDD и тестированию бизнес-логики в Unity

> *"Чистый код — это код, который был написан кем-то, кто заботился о нём"*  
> — Роберт Мартин (Дядя Боб)

---

## 📋 Содержание

1. [Введение в TDD](#введение-в-tdd)
2. [Настройка окружения](#настройка-окружения)
3. [MVP Pattern для Unity](#mvp-pattern-для-unity)
4. [Паттерн AAA (Arrange-Act-Assert)](#паттерн-aaa-arrange-act-assert)
5. [Конвенции именования тестов](#конвенции-именования-тестов)
6. [NUnit в Unity: полное руководство](#nunit-в-unity-полное-руководство)
7. [Mocking с NSubstitute](#mocking-с-nsubstitute)
8. [Практические примеры с MVP](#практические-примеры-с-mvp)
9. [Best Practices](#best-practices)
10. [Антипаттерны](#антипаттерны)
11. [Чеклист для Code Review](#чеклист-для-code-review)

---

## Введение в TDD

### Три закона TDD (Роберт Мартин)

1. **Не пиши production-код**, пока не напишешь failing unit test
2. **Не пиши больше unit-теста**, чем достаточно для его провала (не компилируется = провал)
3. **Не пиши больше production-кода**, чем достаточно для прохождения текущего теста

### Цикл Red-Green-Refactor

```
┌─────────────────────────────────────────────────────┐
│                                                     │
│   🔴 RED         🟢 GREEN        🔵 REFACTOR        │
│   ─────────────────────────────────────────────     │
│                                                     │
│   Написать      Написать        Улучшить код        │
│   failing       минимальный     без изменения       │
│   test          код для         поведения           │
│                 прохождения                         │
│                                                     │
│        └──────────────┴──────────────┘              │
│                       ↑              │              │
│                       └──────────────┘              │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Преимущества TDD в Unity

- **Безопасный рефакторинг** — уверенность при изменении кода
- **Живая документация** — тесты описывают ожидаемое поведение
- **Лучший дизайн** — принуждает к слабой связанности
- **Быстрая обратная связь** — баги обнаруживаются мгновенно
- **Снижение страха** — нет боязни что-то сломать

---

## Настройка окружения

### Структура проекта с MVP

```
Assets/
├── Scripts/
│   ├── Runtime/
│   │   ├── Runtime.asmdef
│   │   │
│   │   ├── Models/                 # Данные и бизнес-логика (чистый C#)
│   │   │   ├── PlayerModel.cs
│   │   │   ├── InventoryModel.cs
│   │   │   └── CombatModel.cs
│   │   │
│   │   ├── Views/                  # MonoBehaviours, UI (Passive)
│   │   │   ├── Interfaces/
│   │   │   │   ├── IPlayerView.cs
│   │   │   │   └── IInventoryView.cs
│   │   │   ├── PlayerView.cs
│   │   │   └── InventoryView.cs
│   │   │
│   │   ├── Presenters/             # Связывает Model и View (чистый C#)
│   │   │   ├── PlayerPresenter.cs
│   │   │   └── InventoryPresenter.cs
│   │   │
│   │   └── Services/               # Внешние зависимости
│   │       ├── Interfaces/
│   │       └── Implementations/
│   │
│   └── Tests/
│       ├── EditMode/
│       │   ├── EditModeTests.asmdef
│       │   ├── Models/
│       │   └── Presenters/
│       │
│       └── PlayMode/
│           ├── PlayModeTests.asmdef
│           └── Integration/
```

### Настройка Assembly Definition для тестов

**EditModeTests.asmdef:**
```json
{
    "name": "EditModeTests",
    "references": [
        "Runtime"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "optionalUnityReferences": [
        "TestAssemblies"
    ],
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll",
        "NSubstitute.dll"
    ]
}
```

### Установка NSubstitute

1. Скачайте NSubstitute.dll с [NuGet](https://www.nuget.org/packages/NSubstitute/)
2. Поместите в `Assets/Plugins/NSubstitute/`
3. Добавьте ссылку в asmdef тестов

---

## MVP Pattern для Unity

### Что такое MVP?

**Model-View-Presenter (MVP)** — архитектурный паттерн, который разделяет приложение на три слоя с чёткими ответственностями. Это делает код тестируемым, поддерживаемым и масштабируемым.

### Диаграмма MVP

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              MVP PATTERN                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│    ┌──────────────┐         ┌──────────────┐         ┌──────────────┐   │
│    │              │         │              │         │              │   │
│    │    MODEL     │◄────────│  PRESENTER   │────────►│     VIEW     │   │
│    │              │         │              │         │              │   │
│    │  (Данные +   │         │ (Логика      │         │ (UI +        │   │
│    │   Бизнес-    │         │  представ-   │         │  MonoBehav-  │   │
│    │   логика)    │────────►│  ления)      │◄────────│  iour)       │   │
│    │              │  Events │              │  Events │              │   │
│    └──────────────┘         └──────────────┘         └──────────────┘   │
│                                                                          │
│    ✅ Чистый C#            ✅ Чистый C#             ❌ Unity-зависим    │
│    ✅ Тестируемый          ✅ Тестируемый           ❌ Не тестируем     │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Ответственности компонентов

| Компонент | Ответственность | Unity-зависимость | Тестируемость |
|-----------|-----------------|-------------------|---------------|
| **Model** | Хранение данных, бизнес-логика, правила игры | ❌ Нет | ✅ Высокая |
| **View** | Отображение UI, обработка input, Unity API | ✅ Да | ❌ Низкая |
| **Presenter** | Связь Model↔View, форматирование данных | ❌ Нет | ✅ Высокая |

### Варианты MVP

#### 1. Passive View (Рекомендуется для Unity)

View максимально "глупый" — только отображает данные и передаёт события.

```csharp
// View ничего не знает о логике
public interface IHealthView
{
    void SetHealthText(string text);
    void SetHealthBarFill(float fill);
    void SetHealthColor(Color color);
    event Action OnHealButtonClicked;
    event Action OnDamageButtonClicked;
}
```

**Преимущества:**
- Максимальная тестируемость
- View легко заменить (разные платформы)
- Вся логика в Presenter

#### 2. Supervising Controller

View может делать простой data binding, Presenter вмешивается только для сложной логики.

```csharp
// View может сам биндить простые данные
public interface IHealthView
{
    int Health { set; }  // View сам форматирует
    event Action<int> OnDamageRequested;
}
```

**Преимущества:**
- Меньше boilerplate кода
- Подходит для простых случаев

### Полный пример MVP: Система здоровья

#### Model — Бизнес-логика

```csharp
// ═══════════════════════════════════════════════════════════════
// MODEL: Чистая бизнес-логика, независима от Unity
// ═══════════════════════════════════════════════════════════════

public class HealthModel
{
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
    
    public event Action<int> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnRevived;
    
    public HealthModel(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }
    
    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;
        
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        OnHealthChanged?.Invoke(CurrentHealth);
        
        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }
    
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth);
    }
    
    public void Revive(int healthPercent = 50)
    {
        if (!IsDead) return;
        
        CurrentHealth = (int)(MaxHealth * (healthPercent / 100f));
        OnRevived?.Invoke();
        OnHealthChanged?.Invoke(CurrentHealth);
    }
    
    public void SetMaxHealth(int newMaxHealth, bool scaleCurrentHealth = true)
    {
        if (newMaxHealth <= 0) return;
        
        if (scaleCurrentHealth)
        {
            float ratio = HealthPercent;
            MaxHealth = newMaxHealth;
            CurrentHealth = (int)(MaxHealth * ratio);
        }
        else
        {
            MaxHealth = newMaxHealth;
            CurrentHealth = Math.Min(CurrentHealth, MaxHealth);
        }
        
        OnHealthChanged?.Invoke(CurrentHealth);
    }
}
```

#### View Interface — Контракт с UI

```csharp
// ═══════════════════════════════════════════════════════════════
// VIEW INTERFACE: Определяет что View умеет делать
// ═══════════════════════════════════════════════════════════════

public interface IHealthView
{
    // === Методы отображения (вызывает Presenter) ===
    void SetHealthText(string text);
    void SetHealthBarFill(float normalizedValue);
    void SetHealthBarColor(Color color);
    void ShowDeathScreen();
    void HideDeathScreen();
    void PlayDamageEffect();
    void PlayHealEffect();
    
    // === События от UI (слушает Presenter) ===
    event Action OnHealButtonClicked;
    event Action<int> OnDamageButtonClicked;
    event Action OnReviveButtonClicked;
}
```

#### View Implementation — MonoBehaviour

```csharp
// ═══════════════════════════════════════════════════════════════
// VIEW: MonoBehaviour, работает с Unity UI
// ═══════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using System;

public class HealthView : MonoBehaviour, IHealthView
{
    [Header("UI Elements")]
    [SerializeField] private Text _healthText;
    [SerializeField] private Image _healthBarFill;
    [SerializeField] private GameObject _deathScreen;
    
    [Header("Buttons")]
    [SerializeField] private Button _healButton;
    [SerializeField] private Button _damageButton;
    [SerializeField] private Button _reviveButton;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem _damageParticles;
    [SerializeField] private ParticleSystem _healParticles;
    
    // === Events ===
    public event Action OnHealButtonClicked;
    public event Action<int> OnDamageButtonClicked;
    public event Action OnReviveButtonClicked;
    
    private void Awake()
    {
        // Подписка на UI события
        _healButton?.onClick.AddListener(() => OnHealButtonClicked?.Invoke());
        _damageButton?.onClick.AddListener(() => OnDamageButtonClicked?.Invoke(10));
        _reviveButton?.onClick.AddListener(() => OnReviveButtonClicked?.Invoke());
    }
    
    private void OnDestroy()
    {
        // Отписка для избежания утечек памяти
        _healButton?.onClick.RemoveAllListeners();
        _damageButton?.onClick.RemoveAllListeners();
        _reviveButton?.onClick.RemoveAllListeners();
    }
    
    // === IHealthView Implementation ===
    
    public void SetHealthText(string text)
    {
        if (_healthText != null)
            _healthText.text = text;
    }
    
    public void SetHealthBarFill(float normalizedValue)
    {
        if (_healthBarFill != null)
            _healthBarFill.fillAmount = Mathf.Clamp01(normalizedValue);
    }
    
    public void SetHealthBarColor(Color color)
    {
        if (_healthBarFill != null)
            _healthBarFill.color = color;
    }
    
    public void ShowDeathScreen()
    {
        _deathScreen?.SetActive(true);
    }
    
    public void HideDeathScreen()
    {
        _deathScreen?.SetActive(false);
    }
    
    public void PlayDamageEffect()
    {
        _damageParticles?.Play();
    }
    
    public void PlayHealEffect()
    {
        _healParticles?.Play();
    }
}
```

#### Presenter — Связующее звено

```csharp
// ═══════════════════════════════════════════════════════════════
// PRESENTER: Связывает Model и View, содержит presentation logic
// ═══════════════════════════════════════════════════════════════

using UnityEngine;
using System;

public class HealthPresenter : IDisposable
{
    private readonly HealthModel _model;
    private readonly IHealthView _view;
    
    // Настройки отображения
    private readonly Color _healthyColor = Color.green;
    private readonly Color _warnColor = Color.yellow;
    private readonly Color _criticalColor = Color.red;
    private readonly float _warnThreshold = 0.5f;
    private readonly float _criticalThreshold = 0.25f;
    
    public HealthPresenter(HealthModel model, IHealthView view)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _view = view ?? throw new ArgumentNullException(nameof(view));
        
        // Подписка на события Model
        _model.OnHealthChanged += HandleHealthChanged;
        _model.OnDeath += HandleDeath;
        _model.OnRevived += HandleRevived;
        
        // Подписка на события View
        _view.OnHealButtonClicked += HandleHealRequest;
        _view.OnDamageButtonClicked += HandleDamageRequest;
        _view.OnReviveButtonClicked += HandleReviveRequest;
        
        // Начальное обновление UI
        UpdateHealthDisplay();
        _view.HideDeathScreen();
    }
    
    public void Dispose()
    {
        // Отписка от Model
        _model.OnHealthChanged -= HandleHealthChanged;
        _model.OnDeath -= HandleDeath;
        _model.OnRevived -= HandleRevived;
        
        // Отписка от View
        _view.OnHealButtonClicked -= HandleHealRequest;
        _view.OnDamageButtonClicked -= HandleDamageRequest;
        _view.OnReviveButtonClicked -= HandleReviveRequest;
    }
    
    // === Event Handlers ===
    
    private void HandleHealthChanged(int newHealth)
    {
        UpdateHealthDisplay();
    }
    
    private void HandleDeath()
    {
        _view.ShowDeathScreen();
    }
    
    private void HandleRevived()
    {
        _view.HideDeathScreen();
    }
    
    private void HandleHealRequest()
    {
        _model.Heal(20);
        _view.PlayHealEffect();
    }
    
    private void HandleDamageRequest(int damage)
    {
        _model.TakeDamage(damage);
        
        if (!_model.IsDead)
        {
            _view.PlayDamageEffect();
        }
    }
    
    private void HandleReviveRequest()
    {
        _model.Revive(50);
    }
    
    // === Presentation Logic ===
    
    private void UpdateHealthDisplay()
    {
        // Форматирование текста
        string healthText = FormatHealthText(_model.CurrentHealth, _model.MaxHealth);
        _view.SetHealthText(healthText);
        
        // Health bar
        _view.SetHealthBarFill(_model.HealthPercent);
        
        // Цвет в зависимости от уровня здоровья
        Color barColor = GetHealthColor(_model.HealthPercent);
        _view.SetHealthBarColor(barColor);
    }
    
    private string FormatHealthText(int current, int max)
    {
        return $"{current} / {max}";
    }
    
    private Color GetHealthColor(float healthPercent)
    {
        if (healthPercent <= _criticalThreshold)
            return _criticalColor;
        if (healthPercent <= _warnThreshold)
            return _warnColor;
        return _healthyColor;
    }
}
```

#### Инициализация (Composition Root)

```csharp
// ═══════════════════════════════════════════════════════════════
// COMPOSITION ROOT: Собирает всё вместе
// ═══════════════════════════════════════════════════════════════

using UnityEngine;

public class HealthSystemInitializer : MonoBehaviour
{
    [SerializeField] private HealthView _healthView;
    [SerializeField] private int _maxHealth = 100;
    
    private HealthModel _model;
    private HealthPresenter _presenter;
    
    private void Awake()
    {
        // Создание Model
        _model = new HealthModel(_maxHealth);
        
        // Создание Presenter (связывает Model и View)
        _presenter = new HealthPresenter(_model, _healthView);
    }
    
    private void OnDestroy()
    {
        _presenter?.Dispose();
    }
    
    // Публичный доступ к Model для других систем
    public HealthModel Model => _model;
}
```

### Тестирование MVP

#### Тесты Model

```csharp
[TestFixture]
public class HealthModelTests
{
    private HealthModel _model;
    
    [SetUp]
    public void SetUp()
    {
        _model = new HealthModel(maxHealth: 100);
    }
    
    [Test]
    public void Constructor_SetsHealthToMax()
    {
        Assert.AreEqual(100, _model.CurrentHealth);
        Assert.AreEqual(100, _model.MaxHealth);
    }
    
    [Test]
    public void TakeDamage_ReducesHealth()
    {
        _model.TakeDamage(30);
        
        Assert.AreEqual(70, _model.CurrentHealth);
    }
    
    [Test]
    public void TakeDamage_HealthCannotGoNegative()
    {
        _model.TakeDamage(150);
        
        Assert.AreEqual(0, _model.CurrentHealth);
    }
    
    [Test]
    public void TakeDamage_RaisesOnHealthChangedEvent()
    {
        int receivedHealth = -1;
        _model.OnHealthChanged += h => receivedHealth = h;
        
        _model.TakeDamage(30);
        
        Assert.AreEqual(70, receivedHealth);
    }
    
    [Test]
    public void TakeDamage_WhenHealthReachesZero_RaisesOnDeathEvent()
    {
        bool deathRaised = false;
        _model.OnDeath += () => deathRaised = true;
        
        _model.TakeDamage(100);
        
        Assert.IsTrue(deathRaised);
    }
    
    [Test]
    public void TakeDamage_WhenAlreadyDead_DoesNothing()
    {
        _model.TakeDamage(100); // Убиваем
        int deathCount = 0;
        _model.OnDeath += () => deathCount++;
        
        _model.TakeDamage(50); // Пытаемся ударить мёртвого
        
        Assert.AreEqual(0, deathCount);
        Assert.AreEqual(0, _model.CurrentHealth);
    }
    
    [Test]
    public void Heal_IncreasesHealth()
    {
        _model.TakeDamage(50);
        
        _model.Heal(30);
        
        Assert.AreEqual(80, _model.CurrentHealth);
    }
    
    [Test]
    public void Heal_CannotExceedMaxHealth()
    {
        _model.TakeDamage(10);
        
        _model.Heal(50);
        
        Assert.AreEqual(100, _model.CurrentHealth);
    }
    
    [Test]
    public void Heal_WhenDead_DoesNothing()
    {
        _model.TakeDamage(100);
        
        _model.Heal(50);
        
        Assert.AreEqual(0, _model.CurrentHealth);
        Assert.IsTrue(_model.IsDead);
    }
    
    [Test]
    public void Revive_WhenDead_RestoresHealth()
    {
        _model.TakeDamage(100);
        
        _model.Revive(50);
        
        Assert.AreEqual(50, _model.CurrentHealth);
        Assert.IsFalse(_model.IsDead);
    }
    
    [Test]
    public void Revive_WhenAlive_DoesNothing()
    {
        _model.Revive(100);
        
        Assert.AreEqual(100, _model.CurrentHealth);
    }
    
    [TestCase(100, 100, 1.0f)]
    [TestCase(50, 100, 0.5f)]
    [TestCase(0, 100, 0f)]
    [TestCase(25, 100, 0.25f)]
    public void HealthPercent_CalculatesCorrectly(int current, int max, float expected)
    {
        var model = new HealthModel(max);
        model.TakeDamage(max - current);
        
        Assert.AreEqual(expected, model.HealthPercent, 0.001f);
    }
}
```

#### Тесты Presenter с Mock View

```csharp
[TestFixture]
public class HealthPresenterTests
{
    private HealthModel _model;
    private IHealthView _mockView;
    private HealthPresenter _presenter;
    
    [SetUp]
    public void SetUp()
    {
        _model = new HealthModel(100);
        _mockView = Substitute.For<IHealthView>();
        _presenter = new HealthPresenter(_model, _mockView);
    }
    
    [TearDown]
    public void TearDown()
    {
        _presenter.Dispose();
    }
    
    [Test]
    public void Constructor_UpdatesViewWithInitialHealth()
    {
        // View должен получить начальные значения
        _mockView.Received().SetHealthText("100 / 100");
        _mockView.Received().SetHealthBarFill(1.0f);
        _mockView.Received().HideDeathScreen();
    }
    
    [Test]
    public void WhenHealthChanges_UpdatesViewHealthText()
    {
        _model.TakeDamage(30);
        
        _mockView.Received().SetHealthText("70 / 100");
    }
    
    [Test]
    public void WhenHealthChanges_UpdatesHealthBarFill()
    {
        _model.TakeDamage(50);
        
        _mockView.Received().SetHealthBarFill(0.5f);
    }
    
    [Test]
    public void WhenHealthBelowCritical_SetsRedColor()
    {
        _model.TakeDamage(80); // 20% health - critical
        
        _mockView.Received().SetHealthBarColor(Color.red);
    }
    
    [Test]
    public void WhenHealthBelowWarning_SetsYellowColor()
    {
        _model.TakeDamage(60); // 40% health - warning
        
        _mockView.Received().SetHealthBarColor(Color.yellow);
    }
    
    [Test]
    public void WhenPlayerDies_ShowsDeathScreen()
    {
        _model.TakeDamage(100);
        
        _mockView.Received().ShowDeathScreen();
    }
    
    [Test]
    public void WhenPlayerRevived_HidesDeathScreen()
    {
        _model.TakeDamage(100);
        _mockView.ClearReceivedCalls();
        
        _model.Revive(50);
        
        _mockView.Received().HideDeathScreen();
    }
    
    [Test]
    public void WhenHealButtonClicked_HealsPlayer()
    {
        _model.TakeDamage(50);
        
        _mockView.OnHealButtonClicked += Raise.Event<Action>();
        
        Assert.AreEqual(70, _model.CurrentHealth); // +20 heal
    }
    
    [Test]
    public void WhenHealButtonClicked_PlaysHealEffect()
    {
        _mockView.OnHealButtonClicked += Raise.Event<Action>();
        
        _mockView.Received().PlayHealEffect();
    }
    
    [Test]
    public void WhenDamageButtonClicked_DamagesPlayer()
    {
        _mockView.OnDamageButtonClicked += Raise.Event<Action<int>>(10);
        
        Assert.AreEqual(90, _model.CurrentHealth);
    }
    
    [Test]
    public void WhenDamageButtonClicked_AndNotDead_PlaysDamageEffect()
    {
        _mockView.OnDamageButtonClicked += Raise.Event<Action<int>>(10);
        
        _mockView.Received().PlayDamageEffect();
    }
    
    [Test]
    public void WhenDamageButtonClicked_AndDies_DoesNotPlayDamageEffect()
    {
        _mockView.ClearReceivedCalls();
        
        _mockView.OnDamageButtonClicked += Raise.Event<Action<int>>(100);
        
        _mockView.DidNotReceive().PlayDamageEffect();
    }
    
    [Test]
    public void WhenReviveButtonClicked_RevivesPlayer()
    {
        _model.TakeDamage(100);
        
        _mockView.OnReviveButtonClicked += Raise.Event<Action>();
        
        Assert.IsFalse(_model.IsDead);
        Assert.AreEqual(50, _model.CurrentHealth);
    }
    
    [Test]
    public void Dispose_UnsubscribesFromModelEvents()
    {
        _presenter.Dispose();
        _mockView.ClearReceivedCalls();
        
        _model.TakeDamage(50);
        
        _mockView.DidNotReceive().SetHealthText(Arg.Any<string>());
    }
}
```

---

## Паттерн AAA (Arrange-Act-Assert)

### Структура теста

```csharp
[Test]
public void Player_TakeDamage_WhenHealthIsAboveZero_ReducesHealth()
{
    // ═══════════════════════════════════════════════════════
    // ARRANGE — Подготовка тестовых данных и зависимостей
    // ═══════════════════════════════════════════════════════
    var model = new HealthModel(maxHealth: 100);
    
    // ═══════════════════════════════════════════════════════
    // ACT — Выполнение тестируемого действия (ОДНО действие!)
    // ═══════════════════════════════════════════════════════
    model.TakeDamage(30);
    
    // ═══════════════════════════════════════════════════════
    // ASSERT — Проверка результата
    // ═══════════════════════════════════════════════════════
    Assert.AreEqual(70, model.CurrentHealth);
}
```

### Вариация: Given-When-Then (BDD-стиль)

```csharp
[Test]
public void Given_PlayerWithFullHealth_When_TakesDamage_Then_HealthIsReduced()
{
    // Given (Arrange)
    var model = new HealthModel(maxHealth: 100);
    
    // When (Act)
    model.TakeDamage(30);
    
    // Then (Assert)
    Assert.AreEqual(70, model.CurrentHealth);
}
```

### Правила AAA

| Правило | Описание |
|---------|----------|
| **Одно действие** | В секции Act должно быть только ОДНО действие |
| **Один assert** | Один логический assert на тест (можно несколько Assert для одного состояния) |
| **Изоляция** | Каждый тест независим от других |
| **Без логики** | Никаких if/else/loops в тестах |
| **Читаемость** | Тест должен читаться как спецификация |

---

## Конвенции именования тестов

### Популярные конвенции

#### 1. MethodName_StateUnderTest_ExpectedBehavior

```csharp
[Test]
public void TakeDamage_WhenHealthIsPositive_ReducesHealth() { }

[Test]
public void Heal_WhenDead_DoesNothing() { }

[Test]
public void Revive_WhenDead_RestoresHealth() { }
```

#### 2. Given_When_Then (BDD-стиль)

```csharp
[Test]
public void Given_FullHealth_When_TakeDamage_Then_HealthReduced() { }

[Test]
public void Given_DeadPlayer_When_Heal_Then_HealthUnchanged() { }

[Test]
public void Given_EmptyInventory_When_AddItem_Then_InventoryContainsItem() { }
```

#### 3. Should_When (краткий стиль)

```csharp
[Test]
public void Should_ReduceHealth_When_TakingDamage() { }

[Test]
public void Should_TriggerDeathEvent_When_HealthReachesZero() { }

[Test]
public void Should_NotExceedMaxHealth_When_Healing() { }
```

#### 4. Behavior-focused (описательный)

```csharp
[Test]
public void DealsCriticalDamageWhenTargetIsStunned() { }

[Test]
public void UpdatesViewWhenModelChanges() { }

[Test]
public void NotifiesObserversWhenStateChanges() { }
```

### Рекомендации по именованию

✅ **DO:**
- Описывайте **поведение**, а не реализацию
- Используйте единую конвенцию в проекте
- Имена должны объяснять что тестируется без чтения кода
- Используйте snake_case или PascalCase для разделения частей имени

❌ **DON'T:**
- Не включайте имя метода, если оно может измениться
- Не используйте номера тестов: `Test1`, `Test2`
- Не пишите `Test` в начале имени
- Избегайте общих слов: `TestSomething`, `CheckResult`

---

## NUnit в Unity: полное руководство

### Основные атрибуты

#### [Test] — Базовый тест

```csharp
[Test]
public void Addition_TwoPositiveNumbers_ReturnsSum()
{
    var calculator = new Calculator();
    var result = calculator.Add(2, 3);
    Assert.AreEqual(5, result);
}
```

#### [TestCase] — Параметризованные тесты

```csharp
[TestCase(100, 30, 70)]
[TestCase(50, 50, 0)]
[TestCase(100, 150, 0)]  // Health не может быть отрицательным
public void TakeDamage_VariousDamageValues_CalculatesCorrectHealth(
    int initialHealth, int damage, int expectedHealth)
{
    var model = new HealthModel(initialHealth);
    model.TakeDamage(damage);
    Assert.AreEqual(expectedHealth, model.CurrentHealth);
}
```

#### [TestCaseSource] — Сложные тестовые данные

```csharp
private static IEnumerable<TestCaseData> DamageTestCases
{
    get
    {
        yield return new TestCaseData(100, 30, 70)
            .SetName("NormalDamage_ReducesHealth");
        yield return new TestCaseData(100, 0, 100)
            .SetName("ZeroDamage_HealthUnchanged");
        yield return new TestCaseData(50, 100, 0)
            .SetName("OverkillDamage_HealthBecomesZero");
    }
}

[TestCaseSource(nameof(DamageTestCases))]
public void TakeDamage_WithTestCaseSource(int initial, int damage, int expected)
{
    var model = new HealthModel(initial);
    model.TakeDamage(damage);
    Assert.AreEqual(expected, model.CurrentHealth);
}
```

#### [SetUp] и [TearDown]

```csharp
public class HealthModelTests
{
    private HealthModel _model;
    
    [SetUp]
    public void SetUp()
    {
        _model = new HealthModel(maxHealth: 100);
    }
    
    [TearDown]
    public void TearDown()
    {
        _model = null;
    }
    
    [Test]
    public void SomeTest()
    {
        // _model уже инициализирован
    }
}
```

### Assert-методы

#### Базовые assertions

```csharp
// Равенство
Assert.AreEqual(expected, actual);
Assert.AreNotEqual(unexpected, actual);

// Булевы
Assert.IsTrue(condition);
Assert.IsFalse(condition);

// Null-проверки
Assert.IsNull(obj);
Assert.IsNotNull(obj);

// Ссылки
Assert.AreSame(expected, actual);
Assert.AreNotSame(unexpected, actual);

// Типы
Assert.IsInstanceOf<ExpectedType>(obj);
```

#### Проверка коллекций

```csharp
// Содержит элемент
Assert.Contains(item, collection);

// Сравнение коллекций
CollectionAssert.AreEqual(expected, actual);      // Порядок важен
CollectionAssert.AreEquivalent(expected, actual); // Порядок не важен
CollectionAssert.IsEmpty(collection);
CollectionAssert.IsNotEmpty(collection);
```

#### Проверка исключений

```csharp
[Test]
public void Constructor_NegativeMaxHealth_ThrowsException()
{
    Assert.Throws<ArgumentException>(() => 
        new HealthModel(maxHealth: -10));
}

[Test]
public void Withdraw_InsufficientFunds_ThrowsWithMessage()
{
    var account = new BankAccount(100);
    
    var ex = Assert.Throws<InsufficientFundsException>(() => 
        account.Withdraw(500));
    
    Assert.That(ex.Message, Does.Contain("insufficient"));
}
```

#### Fluent Assertions с Assert.That

```csharp
// Числа
Assert.That(value, Is.GreaterThan(5));
Assert.That(value, Is.InRange(1, 10));
Assert.That(value, Is.EqualTo(3.14f).Within(0.01f));

// Строки
Assert.That(result, Does.StartWith("Hello"));
Assert.That(result, Does.Contain("world"));

// Коллекции
Assert.That(list, Has.Count.EqualTo(5));
Assert.That(list, Has.Some.GreaterThan(10));
Assert.That(list, Is.Ordered.Ascending);
```

### Unity-специфичные assertions

```csharp
using UnityEngine.TestTools.Utils;

// Сравнение float с погрешностью
Assert.AreApproximatelyEqual(expected, actual);           // tolerance = 0.00001f
Assert.AreApproximatelyEqual(expected, actual, 0.001f);   // custom tolerance

// Сравнение Vector3
var comparer = new Vector3EqualityComparer(0.01f);
Assert.That(actualPosition, Is.EqualTo(expectedPosition).Using(comparer));
```

---

## Mocking с NSubstitute

### Основы NSubstitute

#### Создание substitute

```csharp
using NSubstitute;

// Substitute для интерфейса
var view = Substitute.For<IHealthView>();

// Substitute для нескольких интерфейсов
var multiMock = Substitute.For<IReader, IWriter>();
```

#### Настройка возвращаемых значений

```csharp
public interface IPlayerRepository
{
    Player GetById(int id);
    bool Save(Player player);
}

[Test]
public void GetPlayerStats_ExistingPlayer_ReturnsStats()
{
    // Arrange
    var repository = Substitute.For<IPlayerRepository>();
    var expectedPlayer = new Player { Id = 1, Name = "Hero" };
    
    // Настройка: при вызове GetById(1) вернуть expectedPlayer
    repository.GetById(1).Returns(expectedPlayer);
    
    var service = new PlayerService(repository);
    
    // Act
    var stats = service.GetPlayerStats(1);
    
    // Assert
    Assert.AreEqual("Hero", stats.Name);
}
```

#### Returns для любых аргументов

```csharp
// Любой int
repository.GetById(Arg.Any<int>()).Returns(defaultPlayer);

// Аргумент соответствует условию
repository.GetById(Arg.Is<int>(id => id > 0)).Returns(player);
```

### Проверка вызовов

#### Received — проверка что метод был вызван

```csharp
[Test]
public void WhenHealthChanges_UpdatesView()
{
    var view = Substitute.For<IHealthView>();
    var model = new HealthModel(100);
    var presenter = new HealthPresenter(model, view);
    
    model.TakeDamage(30);
    
    // Assert — проверяем что SetHealthText был вызван
    view.Received().SetHealthText(Arg.Any<string>());
    view.Received().SetHealthText("70 / 100");
    
    // Проверка количества вызовов
    view.Received(1).SetHealthBarFill(0.7f);
    
    // Проверка что НЕ был вызван
    view.DidNotReceive().ShowDeathScreen();
}
```

### Вызов событий в тестах

```csharp
[Test]
public void WhenHealButtonClicked_HealsModel()
{
    var view = Substitute.For<IHealthView>();
    var model = new HealthModel(100);
    model.TakeDamage(50);
    var presenter = new HealthPresenter(model, view);
    
    // Симулируем нажатие кнопки - вызываем событие
    view.OnHealButtonClicked += Raise.Event<Action>();
    
    Assert.AreEqual(70, model.CurrentHealth);
}

[Test]
public void WhenDamageButtonClicked_WithAmount_DamagesModel()
{
    var view = Substitute.For<IHealthView>();
    var model = new HealthModel(100);
    var presenter = new HealthPresenter(model, view);
    
    // Вызываем событие с параметром
    view.OnDamageButtonClicked += Raise.Event<Action<int>>(25);
    
    Assert.AreEqual(75, model.CurrentHealth);
}
```

### Callbacks с When...Do

```csharp
[Test]
public void Save_CapturesArgument()
{
    var repository = Substitute.For<IPlayerRepository>();
    Player capturedPlayer = null;
    
    repository
        .When(r => r.Save(Arg.Any<Player>()))
        .Do(callInfo => capturedPlayer = callInfo.Arg<Player>());
    
    var service = new PlayerService(repository);
    service.CreatePlayer("Hero");
    
    Assert.AreEqual("Hero", capturedPlayer.Name);
}
```

---

## Практические примеры с MVP

### Пример 1: Система инвентаря

```csharp
// ═══════════════════════════════════════════════════════════════
// MODEL
// ═══════════════════════════════════════════════════════════════

public class InventoryModel
{
    private readonly List<InventorySlot> _slots;
    
    public int Capacity { get; }
    public int ItemCount => _slots.Count(s => !s.IsEmpty);
    
    public event Action<IItem, int> OnItemAdded;
    public event Action<IItem, int> OnItemRemoved;
    public event Action OnInventoryFull;
    
    public InventoryModel(int capacity)
    {
        Capacity = capacity;
        _slots = new List<InventorySlot>();
        for (int i = 0; i < capacity; i++)
            _slots.Add(new InventorySlot());
    }
    
    public bool TryAddItem(IItem item, int count, out int remainder)
    {
        remainder = count;
        int added = 0;
        
        // Заполняем существующие стаки
        foreach (var slot in _slots.Where(s => !s.IsEmpty && s.Item.Id == item.Id && !s.IsFull))
        {
            int toAdd = Math.Min(remainder, slot.AvailableSpace);
            slot.Add(item, toAdd);
            remainder -= toAdd;
            added += toAdd;
            if (remainder == 0) break;
        }
        
        // Заполняем пустые слоты
        foreach (var slot in _slots.Where(s => s.IsEmpty))
        {
            if (remainder == 0) break;
            int toAdd = Math.Min(remainder, item.StackLimit);
            slot.Add(item, toAdd);
            remainder -= toAdd;
            added += toAdd;
        }
        
        if (added > 0)
            OnItemAdded?.Invoke(item, added);
        
        if (remainder > 0 && ItemCount >= Capacity)
            OnInventoryFull?.Invoke();
        
        return remainder < count;
    }
    
    public int GetItemCount(string itemId)
    {
        return _slots
            .Where(s => !s.IsEmpty && s.Item.Id == itemId)
            .Sum(s => s.Count);
    }
}

// ═══════════════════════════════════════════════════════════════
// VIEW INTERFACE
// ═══════════════════════════════════════════════════════════════

public interface IInventoryView
{
    void SetSlotData(int slotIndex, Sprite icon, int count);
    void ClearSlot(int slotIndex);
    void SetCapacityText(string text);
    void ShowFullInventoryMessage();
    void PlayPickupSound();
    
    event Action<int> OnSlotClicked;
    event Action<int, int> OnItemDropped;
}

// ═══════════════════════════════════════════════════════════════
// PRESENTER
// ═══════════════════════════════════════════════════════════════

public class InventoryPresenter : IDisposable
{
    private readonly InventoryModel _model;
    private readonly IInventoryView _view;
    private readonly IItemDatabase _itemDatabase;
    
    public InventoryPresenter(InventoryModel model, IInventoryView view, IItemDatabase itemDatabase)
    {
        _model = model;
        _view = view;
        _itemDatabase = itemDatabase;
        
        _model.OnItemAdded += HandleItemAdded;
        _model.OnItemRemoved += HandleItemRemoved;
        _model.OnInventoryFull += HandleInventoryFull;
        
        _view.OnSlotClicked += HandleSlotClicked;
        
        UpdateCapacityDisplay();
    }
    
    public void Dispose()
    {
        _model.OnItemAdded -= HandleItemAdded;
        _model.OnItemRemoved -= HandleItemRemoved;
        _model.OnInventoryFull -= HandleInventoryFull;
        _view.OnSlotClicked -= HandleSlotClicked;
    }
    
    private void HandleItemAdded(IItem item, int count)
    {
        RefreshAllSlots();
        UpdateCapacityDisplay();
        _view.PlayPickupSound();
    }
    
    private void HandleItemRemoved(IItem item, int count)
    {
        RefreshAllSlots();
        UpdateCapacityDisplay();
    }
    
    private void HandleInventoryFull()
    {
        _view.ShowFullInventoryMessage();
    }
    
    private void HandleSlotClicked(int slotIndex)
    {
        // Логика использования предмета
    }
    
    private void UpdateCapacityDisplay()
    {
        string text = $"{_model.ItemCount} / {_model.Capacity}";
        _view.SetCapacityText(text);
    }
    
    private void RefreshAllSlots()
    {
        // Обновление всех слотов UI
    }
}

// ═══════════════════════════════════════════════════════════════
// TESTS
// ═══════════════════════════════════════════════════════════════

[TestFixture]
public class InventoryModelTests
{
    private IItem CreateItem(string id, int stackLimit = 99)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        item.StackLimit.Returns(stackLimit);
        return item;
    }
    
    [Test]
    public void TryAddItem_EmptyInventory_AddsItem()
    {
        var inventory = new InventoryModel(capacity: 3);
        var sword = CreateItem("sword");
        
        bool added = inventory.TryAddItem(sword, 1, out int remainder);
        
        Assert.IsTrue(added);
        Assert.AreEqual(0, remainder);
        Assert.AreEqual(1, inventory.GetItemCount("sword"));
    }
    
    [Test]
    public void TryAddItem_FullInventory_RaisesOnInventoryFullEvent()
    {
        var inventory = new InventoryModel(capacity: 1);
        var item = CreateItem("item", stackLimit: 1);
        inventory.TryAddItem(item, 1, out _);
        
        bool eventRaised = false;
        inventory.OnInventoryFull += () => eventRaised = true;
        
        inventory.TryAddItem(item, 1, out _);
        
        Assert.IsTrue(eventRaised);
    }
}

[TestFixture]
public class InventoryPresenterTests
{
    [Test]
    public void WhenItemAdded_PlaysPickupSound()
    {
        var model = new InventoryModel(10);
        var view = Substitute.For<IInventoryView>();
        var db = Substitute.For<IItemDatabase>();
        var presenter = new InventoryPresenter(model, view, db);
        
        var item = Substitute.For<IItem>();
        item.Id.Returns("sword");
        item.StackLimit.Returns(1);
        
        model.TryAddItem(item, 1, out _);
        
        view.Received().PlayPickupSound();
    }
    
    [Test]
    public void WhenInventoryFull_ShowsMessage()
    {
        var model = new InventoryModel(1);
        var view = Substitute.For<IInventoryView>();
        var db = Substitute.For<IItemDatabase>();
        var presenter = new InventoryPresenter(model, view, db);
        
        var item = Substitute.For<IItem>();
        item.Id.Returns("item");
        item.StackLimit.Returns(1);
        
        model.TryAddItem(item, 1, out _);
        model.TryAddItem(item, 1, out _);
        
        view.Received().ShowFullInventoryMessage();
    }
}
```

### Пример 2: Боевая система с MVP

```csharp
// ═══════════════════════════════════════════════════════════════
// MODEL
// ═══════════════════════════════════════════════════════════════

public interface IRandomProvider
{
    float NextFloat();
}

public class CombatModel
{
    private readonly IRandomProvider _random;
    
    public event Action<DamageResult> OnDamageDealt;
    public event Action<string> OnCombatLog;
    
    public CombatModel(IRandomProvider random)
    {
        _random = random;
    }
    
    public DamageResult CalculateAttack(AttackData attack, DefenseData defense)
    {
        bool isCritical = _random.NextFloat() < attack.CritChance;
        bool isDodged = _random.NextFloat() < defense.DodgeChance;
        
        if (isDodged)
        {
            var dodgeResult = new DamageResult { IsDodged = true };
            OnDamageDealt?.Invoke(dodgeResult);
            OnCombatLog?.Invoke("Attack dodged!");
            return dodgeResult;
        }
        
        int rawDamage = attack.BaseDamage;
        if (isCritical)
            rawDamage = (int)(rawDamage * attack.CritMultiplier);
        
        int finalDamage = Math.Max(1, rawDamage - defense.Armor);
        
        var result = new DamageResult
        {
            Damage = finalDamage,
            IsCritical = isCritical,
            Blocked = rawDamage - finalDamage
        };
        
        OnDamageDealt?.Invoke(result);
        OnCombatLog?.Invoke(FormatDamageLog(result));
        
        return result;
    }
    
    private string FormatDamageLog(DamageResult result)
    {
        string log = result.IsCritical ? "CRITICAL! " : "";
        log += $"Dealt {result.Damage} damage";
        if (result.Blocked > 0)
            log += $" ({result.Blocked} blocked)";
        return log;
    }
}

public struct DamageResult
{
    public int Damage;
    public bool IsCritical;
    public bool IsDodged;
    public int Blocked;
}

public struct AttackData
{
    public int BaseDamage;
    public float CritChance;
    public float CritMultiplier;
}

public struct DefenseData
{
    public int Armor;
    public float DodgeChance;
}

// ═══════════════════════════════════════════════════════════════
// VIEW INTERFACE
// ═══════════════════════════════════════════════════════════════

public interface ICombatView
{
    void ShowDamageNumber(int damage, bool isCritical);
    void ShowDodgeText();
    void AddCombatLogEntry(string text);
    void PlayHitEffect();
    void PlayCriticalHitEffect();
    void PlayDodgeEffect();
    
    event Action OnAttackButtonClicked;
}

// ═══════════════════════════════════════════════════════════════
// PRESENTER
// ═══════════════════════════════════════════════════════════════

public class CombatPresenter : IDisposable
{
    private readonly CombatModel _model;
    private readonly ICombatView _view;
    private readonly Func<AttackData> _getAttackData;
    private readonly Func<DefenseData> _getDefenseData;
    
    public CombatPresenter(
        CombatModel model, 
        ICombatView view,
        Func<AttackData> getAttackData,
        Func<DefenseData> getDefenseData)
    {
        _model = model;
        _view = view;
        _getAttackData = getAttackData;
        _getDefenseData = getDefenseData;
        
        _model.OnDamageDealt += HandleDamageDealt;
        _model.OnCombatLog += HandleCombatLog;
        _view.OnAttackButtonClicked += HandleAttackClicked;
    }
    
    public void Dispose()
    {
        _model.OnDamageDealt -= HandleDamageDealt;
        _model.OnCombatLog -= HandleCombatLog;
        _view.OnAttackButtonClicked -= HandleAttackClicked;
    }
    
    private void HandleDamageDealt(DamageResult result)
    {
        if (result.IsDodged)
        {
            _view.ShowDodgeText();
            _view.PlayDodgeEffect();
        }
        else if (result.IsCritical)
        {
            _view.ShowDamageNumber(result.Damage, isCritical: true);
            _view.PlayCriticalHitEffect();
        }
        else
        {
            _view.ShowDamageNumber(result.Damage, isCritical: false);
            _view.PlayHitEffect();
        }
    }
    
    private void HandleCombatLog(string text)
    {
        _view.AddCombatLogEntry(text);
    }
    
    private void HandleAttackClicked()
    {
        var attack = _getAttackData();
        var defense = _getDefenseData();
        _model.CalculateAttack(attack, defense);
    }
}

// ═══════════════════════════════════════════════════════════════
// TESTS
// ═══════════════════════════════════════════════════════════════

[TestFixture]
public class CombatModelTests
{
    [Test]
    public void CalculateAttack_NormalHit_ReturnsBaseDamageMinusArmor()
    {
        var random = Substitute.For<IRandomProvider>();
        random.NextFloat().Returns(0.99f); // Не крит, не уворот
        var model = new CombatModel(random);
        
        var attack = new AttackData { BaseDamage = 50, CritChance = 0.2f, CritMultiplier = 2f };
        var defense = new DefenseData { Armor = 10, DodgeChance = 0.1f };
        
        var result = model.CalculateAttack(attack, defense);
        
        Assert.AreEqual(40, result.Damage);
        Assert.IsFalse(result.IsCritical);
        Assert.IsFalse(result.IsDodged);
    }
    
    [Test]
    public void CalculateAttack_CriticalHit_AppliesMultiplier()
    {
        var random = Substitute.For<IRandomProvider>();
        random.NextFloat().Returns(0.1f, 0.99f); // Крит, не уворот
        var model = new CombatModel(random);
        
        var attack = new AttackData { BaseDamage = 50, CritChance = 0.2f, CritMultiplier = 2f };
        var defense = new DefenseData { Armor = 10, DodgeChance = 0.05f };
        
        var result = model.CalculateAttack(attack, defense);
        
        Assert.AreEqual(90, result.Damage); // (50 * 2) - 10
        Assert.IsTrue(result.IsCritical);
    }
    
    [Test]
    public void CalculateAttack_Dodged_ReturnsZeroDamage()
    {
        var random = Substitute.For<IRandomProvider>();
        random.NextFloat().Returns(0.99f, 0.05f); // Не крит, уворот
        var model = new CombatModel(random);
        
        var attack = new AttackData { BaseDamage = 50, CritChance = 0.2f, CritMultiplier = 2f };
        var defense = new DefenseData { Armor = 10, DodgeChance = 0.1f };
        
        var result = model.CalculateAttack(attack, defense);
        
        Assert.IsTrue(result.IsDodged);
        Assert.AreEqual(0, result.Damage);
    }
    
    [Test]
    public void CalculateAttack_RaisesOnDamageDealtEvent()
    {
        var random = Substitute.For<IRandomProvider>();
        random.NextFloat().Returns(0.99f);
        var model = new CombatModel(random);
        
        DamageResult receivedResult = default;
        model.OnDamageDealt += r => receivedResult = r;
        
        var attack = new AttackData { BaseDamage = 50 };
        var defense = new DefenseData { Armor = 10 };
        
        model.CalculateAttack(attack, defense);
        
        Assert.AreEqual(40, receivedResult.Damage);
    }
}

[TestFixture]
public class CombatPresenterTests
{
    [Test]
    public void WhenCriticalHit_PlaysCriticalEffect()
    {
        var random = Substitute.For<IRandomProvider>();
        random.NextFloat().Returns(0.1f, 0.99f); // Крит, не уворот
        var model = new CombatModel(random);
        var view = Substitute.For<ICombatView>();
        
        var presenter = new CombatPresenter(
            model, view,
            () => new AttackData { BaseDamage = 50, CritChance = 0.2f, CritMultiplier = 2f },
            () => new DefenseData { Armor = 0, DodgeChance = 0.05f });
        
        view.OnAttackButtonClicked += Raise.Event<Action>();
        
        view.Received().PlayCriticalHitEffect();
        view.Received().ShowDamageNumber(100, true);
    }
    
    [Test]
    public void WhenDodged_PlaysDodgeEffect()
    {
        var random = Substitute.For<IRandomProvider>();
        random.NextFloat().Returns(0.99f, 0.01f); // Не крит, уворот
        var model = new CombatModel(random);
        var view = Substitute.For<ICombatView>();
        
        var presenter = new CombatPresenter(
            model, view,
            () => new AttackData { BaseDamage = 50 },
            () => new DefenseData { DodgeChance = 0.1f });
        
        view.OnAttackButtonClicked += Raise.Event<Action>();
        
        view.Received().PlayDodgeEffect();
        view.Received().ShowDodgeText();
    }
}
```

---

## Best Practices

### 1. MVP Best Practices

```
┌────────────────────────────────────────────────────────────────┐
│                    MVP GOLDEN RULES                            │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  1. View должен быть МАКСИМАЛЬНО ГЛУПЫМ                        │
│     • Только отображение данных                                │
│     • Только передача событий                                  │
│     • Никакой бизнес-логики                                    │
│                                                                │
│  2. Model НЕ ЗНАЕТ о View                                      │
│     • Чистый C# (без Unity)                                    │
│     • Только данные и бизнес-правила                           │
│     • События для уведомлений                                  │
│                                                                │
│  3. Presenter — ЕДИНСТВЕННЫЙ посредник                         │
│     • Получает интерфейс View (не конкретный класс)            │
│     • Подписывается на события Model и View                    │
│     • Форматирует данные для отображения                       │
│                                                                │
│  4. Используйте ИНТЕРФЕЙСЫ для View                            │
│     • IHealthView, IInventoryView                              │
│     • Позволяет мокать в тестах                                │
│     • Позволяет менять реализацию                              │
│                                                                │
│  5. ОДИН Presenter — ОДИН View                                 │
│     • Если View сложный — разбейте на под-view                 │
│     • Presenter может иметь несколько Model                    │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 2. Правила F.I.R.S.T. для тестов

```
┌────────────────────────────────────────────────────────────────┐
│                    F.I.R.S.T. Principles                       │
├────────────────────────────────────────────────────────────────┤
│  F — Fast       │ Тесты должны выполняться быстро            │
│  I — Independent│ Тесты не зависят друг от друга             │
│  R — Repeatable │ Результат одинаков при каждом запуске      │
│  S — Self-valid │ Тест сам определяет pass/fail              │
│  T — Timely     │ Тесты пишутся вовремя (до кода в TDD)      │
└────────────────────────────────────────────────────────────────┘
```

### 3. Организация тестов для MVP

```csharp
// Структура тестовых классов
[TestFixture]
public class HealthModelTests { }        // Тесты бизнес-логики

[TestFixture]
public class HealthPresenterTests { }    // Тесты presentation logic

// Интеграционные тесты (PlayMode)
[TestFixture]
public class HealthSystemIntegrationTests { }
```

### 4. Test Data Builders

```csharp
public class AttackDataBuilder
{
    private int _baseDamage = 10;
    private float _critChance = 0f;
    private float _critMultiplier = 2f;
    
    public AttackDataBuilder WithBaseDamage(int damage)
    {
        _baseDamage = damage;
        return this;
    }
    
    public AttackDataBuilder WithCritChance(float chance)
    {
        _critChance = chance;
        return this;
    }
    
    public AttackDataBuilder WithCritMultiplier(float multiplier)
    {
        _critMultiplier = multiplier;
        return this;
    }
    
    public AttackData Build() => new AttackData
    {
        BaseDamage = _baseDamage,
        CritChance = _critChance,
        CritMultiplier = _critMultiplier
    };
    
    public static implicit operator AttackData(AttackDataBuilder b) => b.Build();
}

// Использование
[Test]
public void Test()
{
    AttackData attack = new AttackDataBuilder()
        .WithBaseDamage(100)
        .WithCritChance(0.5f);
}
```

### 5. Тестирование событий

```csharp
[Test]
public void TakeFatalDamage_RaisesOnDeathEvent()
{
    var model = new HealthModel(50);
    
    // Способ 1: Флаг
    bool eventRaised = false;
    model.OnDeath += () => eventRaised = true;
    
    model.TakeDamage(100);
    
    Assert.IsTrue(eventRaised);
}

[Test]
public void TakeFatalDamage_RaisesOnDeathEvent_WithNSubstitute()
{
    var model = new HealthModel(50);
    var handler = Substitute.For<Action>();
    model.OnDeath += handler;
    
    model.TakeDamage(100);
    
    handler.Received(1).Invoke();
}
```

---

## Антипаттерны

### ❌ Логика во View

```csharp
// ❌ ПЛОХО: View содержит бизнес-логику
public class HealthView : MonoBehaviour
{
    public void TakeDamage(int damage)
    {
        // Логика в View - плохо!
        health = Mathf.Max(0, health - damage);
        if (health <= 0)
        {
            Die();
        }
        UpdateUI();
    }
}

// ✅ ХОРОШО: View только отображает
public class HealthView : MonoBehaviour, IHealthView
{
    public void SetHealth(int current, int max)
    {
        _healthText.text = $"{current}/{max}";
        _healthBar.fillAmount = (float)current / max;
    }
}
```

### ❌ Presenter знает о Unity

```csharp
// ❌ ПЛОХО: Presenter использует Unity API
public class BadPresenter
{
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))  // Unity API!
        {
            // ...
        }
        transform.position = newPos;  // Unity API!
    }
}

// ✅ ХОРОШО: Presenter работает через интерфейсы
public class GoodPresenter
{
    private readonly IInputProvider _input;
    
    public void ProcessInput()
    {
        if (_input.IsJumpPressed)
        {
            // ...
        }
    }
}
```

### ❌ Model хранит ссылку на View

```csharp
// ❌ ПЛОХО: Model знает о View
public class BadModel
{
    private IHealthView _view;  // Плохо!
    
    public void TakeDamage(int damage)
    {
        _health -= damage;
        _view.UpdateHealth(_health);  // Прямой вызов View
    }
}

// ✅ ХОРОШО: Model использует события
public class GoodModel
{
    public event Action<int> OnHealthChanged;
    
    public void TakeDamage(int damage)
    {
        _health -= damage;
        OnHealthChanged?.Invoke(_health);  // Через события
    }
}
```

### ❌ Тестирование реализации вместо поведения

```csharp
// ❌ ПЛОХО: Тестируем внутреннее состояние
[Test]
public void TestInternalState()
{
    var model = new HealthModel(100);
    
    // Рефлексия для доступа к приватному полю
    var field = typeof(HealthModel).GetField("_isDamaged", ...);
    
    model.TakeDamage(10);
    
    Assert.IsTrue((bool)field.GetValue(model));
}

// ✅ ХОРОШО: Тестируем публичное поведение
[Test]
public void TakeDamage_ReducesCurrentHealth()
{
    var model = new HealthModel(100);
    
    model.TakeDamage(10);
    
    Assert.AreEqual(90, model.CurrentHealth);
}
```

### ❌ Слишком много моков

```csharp
// ❌ ПЛОХО: 10 моков = класс делает слишком много
[Test]
public void Test_TooManyMocks()
{
    var view = Substitute.For<IView>();
    var logger = Substitute.For<ILogger>();
    var config = Substitute.For<IConfig>();
    var analytics = Substitute.For<IAnalytics>();
    var audio = Substitute.For<IAudio>();
    var network = Substitute.For<INetwork>();
    // ... ещё 5 моков
    
    // Сигнал что класс нужно разбить!
}

// ✅ ХОРОШО: 2-3 зависимости
[Test]
public void Test_FewDependencies()
{
    var model = new HealthModel(100);
    var view = Substitute.For<IHealthView>();
    var presenter = new HealthPresenter(model, view);
}
```

---

## Чеклист для Code Review

### MVP Architecture

- [ ] Model не содержит Unity-зависимостей
- [ ] Model не знает о View (нет ссылок)
- [ ] View реализует интерфейс
- [ ] View не содержит бизнес-логики
- [ ] Presenter получает IView, не конкретный класс
- [ ] Presenter корректно отписывается от событий (Dispose)
- [ ] Composition Root собирает компоненты

### Качество теста

- [ ] Имя теста ясно описывает что тестируется
- [ ] Следует паттерну AAA (Arrange-Act-Assert)
- [ ] Один логический assert на тест
- [ ] Нет if/else/loops в тесте
- [ ] Тест независим от других тестов
- [ ] Тест детерминирован (всегда один результат)

### Моки

- [ ] Мокаются только внешние зависимости
- [ ] Не мокаем то что тестируем
- [ ] Моки настроены на конкретные аргументы
- [ ] Received проверяется только для важных взаимодействий

---

## Полезные ресурсы

### Книги

- **"Test-Driven Development: By Example"** — Kent Beck
- **"Clean Code"** — Robert C. Martin  
- **"The Art of Unit Testing"** — Roy Osherove
- **"Working Effectively with Legacy Code"** — Michael Feathers

### Unity-специфичные

- [Unity Learn: MVC and MVP patterns](https://learn.unity.com/tutorial/65e0cfacedbc2a2351773054)
- [Unity Test Framework Documentation](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NSubstitute Documentation](https://nsubstitute.github.io/)

### Статьи

- [Martin Fowler: Passive View](https://martinfowler.com/eaaDev/PassiveScreen.html)
- [Martin Fowler: Supervising Controller](https://martinfowler.com/eaaDev/SupervisingPresenter.html)
- [How to TDD in Unity using MVP](https://medium.com/etermax-technology/how-to-tdd-in-unity-using-the-mvp-pattern-a646ffbe996f)

---

## Заключение

> *"Я не великий программист — я просто хороший программист с великими привычками."*  
> — Kent Beck

MVP + TDD в Unity — это мощная комбинация:

1. **MVP** разделяет код на тестируемые слои
2. **Model** содержит бизнес-логику — легко тестировать
3. **Presenter** содержит presentation logic — тестируем с mock View
4. **View** — MonoBehaviour, который просто отображает данные

Главное правило: **View должен быть максимально глупым**. Вся логика — в Model и Presenter.

Помните: **тесты — это инвестиция в будущее вашего проекта**.

---

*Документ создан на основе best practices от Unity Technologies, Martin Fowler, Kent Beck, Robert C. Martin и сообщества разработчиков.*
