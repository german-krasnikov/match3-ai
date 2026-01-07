# Unity TDD Guide
## Руководство по Test-Driven Development

---

## 🎯 Главный принцип

> **Простота превыше всего. Тесты — инвестиция, а не бюрократия.**

- Тестируй поведение, не реализацию
- Один тест — одна проверка
- Читаемость важнее краткости

---

## 📋 Цикл Red-Green-Refactor

```
┌─────────────────────────────────────────────────────┐
│   🔴 RED         🟢 GREEN        🔵 REFACTOR        │
│                                                     │
│   Написать      Написать        Улучшить код       │
│   failing       минимальный     без изменения      │
│   test          код для         поведения          │
│                 прохождения                        │
│                                                     │
│        └──────────────┴──────────────┘             │
│                       ↑              │             │
│                       └──────────────┘             │
└─────────────────────────────────────────────────────┘
```

### Три закона TDD

1. Не пиши код, пока нет failing теста
2. Не пиши больше теста, чем нужно для провала
3. Не пиши больше кода, чем нужно для прохождения

---

## 📁 Структура проекта

```
Assets/
├── Scripts/
│   ├── Game.asmdef
│   ├── Core/
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
        ├── Tests.EditMode.asmdef
        └── Features/
            └── {Feature}/
                ├── {Feature}ModelTests.cs
                └── {Feature}PresenterTests.cs
```

### Assembly Definition для тестов

**Tests.EditMode.asmdef:**
```json
{
    "name": "Tests.EditMode",
    "references": ["Game"],
    "includePlatforms": ["Editor"],
    "optionalUnityReferences": ["TestAssemblies"],
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll",
        "NSubstitute.dll"
    ]
}
```

### Namespace-конвенция

```csharp
// Код: Assets/Scripts/Features/Player/PlayerModel.cs
namespace Features.Player
{
    public class PlayerModel { }
}

// Тест: Assets/Tests/EditMode/Features/Player/PlayerModelTests.cs
namespace Features.Player  // ТОТ ЖЕ namespace
{
    [TestFixture]
    public class PlayerModelTests { }  // Суффикс Tests
}
```

**Преимущество:** Доступ к internal без `[InternalsVisibleTo]`

---

## 📐 Паттерн AAA

```csharp
[Test]
public void TakeDamage_WhenAlive_ReducesHealth()
{
    // Arrange — подготовка
    var model = new PlayerModel(maxHealth: 100);
    
    // Act — действие (ОДНО!)
    model.TakeDamage(30);
    
    // Assert — проверка
    Assert.AreEqual(70, model.CurrentHealth);
}
```

### Правила

| Правило | Описание |
|---------|----------|
| Одно действие | В Act только ОДНО действие |
| Один assert | Один логический assert на тест |
| Без логики | Никаких if/else/loops в тестах |
| Изоляция | Тесты независимы друг от друга |

---

## 📝 Именование тестов

### Конвенция: Method_Condition_ExpectedResult

```csharp
[Test]
public void TakeDamage_WhenHealthIsPositive_ReducesHealth() { }

[Test]
public void Heal_WhenDead_DoesNothing() { }

[Test]
public void Revive_WhenDead_RestoresHealth() { }
```

### ❌ Плохие имена

```csharp
public void Test1() { }
public void TestHealth() { }
public void CheckDamage() { }
```

---

## 🧪 NUnit: Основное

### Атрибуты

```csharp
[TestFixture]
public class PlayerModelTests
{
    private PlayerModel _model;
    
    [SetUp]
    public void SetUp()
    {
        _model = new PlayerModel(100);
    }
    
    [TearDown]
    public void TearDown()
    {
        _model = null;
    }
    
    [Test]
    public void SimpleTest() { }
    
    [TestCase(100, 30, 70)]
    [TestCase(50, 50, 0)]
    [TestCase(100, 150, 0)]
    public void ParameterizedTest(int initial, int damage, int expected)
    {
        var model = new PlayerModel(initial);
        model.TakeDamage(damage);
        Assert.AreEqual(expected, model.CurrentHealth);
    }
}
```

### Assert-методы

```csharp
// Равенство
Assert.AreEqual(expected, actual);
Assert.AreNotEqual(unexpected, actual);

// Булевы
Assert.IsTrue(condition);
Assert.IsFalse(condition);

// Null
Assert.IsNull(obj);
Assert.IsNotNull(obj);

// Исключения
Assert.Throws<ArgumentException>(() => new PlayerModel(-1));

// Float с погрешностью
Assert.AreEqual(0.5f, value, 0.001f);

// Коллекции
CollectionAssert.Contains(list, item);
CollectionAssert.IsEmpty(list);
```

---

## 🎭 NSubstitute: Моки

### Создание мока

```csharp
var view = Substitute.For<IPlayerView>();
```

### Настройка возвращаемых значений

```csharp
var repository = Substitute.For<IPlayerRepository>();
repository.GetById(1).Returns(new Player { Name = "Hero" });
repository.GetById(Arg.Any<int>()).Returns(defaultPlayer);
```

### Проверка вызовов

```csharp
[Test]
public void WhenHealthChanges_UpdatesView()
{
    var view = Substitute.For<IPlayerView>();
    var model = new PlayerModel(100);
    var presenter = new PlayerPresenter(model, view);
    
    model.TakeDamage(30);
    
    // Проверяем что метод был вызван
    view.Received().UpdateHealthBar(0.7f);
    
    // Проверяем количество вызовов
    view.Received(1).UpdateHealthBar(Arg.Any<float>());
    
    // Проверяем что НЕ был вызван
    view.DidNotReceive().PlayDeathEffect();
}
```

### Вызов событий

```csharp
[Test]
public void WhenHealButtonClicked_HealsModel()
{
    var view = Substitute.For<IPlayerView>();
    var model = new PlayerModel(100);
    model.TakeDamage(50);
    var presenter = new PlayerPresenter(model, view);
    
    // Симулируем событие
    view.OnHealRequested += Raise.Event<Action>();
    
    Assert.AreEqual(75, model.CurrentHealth);
}

// С параметром
view.OnDamageRequested += Raise.Event<Action<int>>(25);
```

---

## 📊 Пример: Тесты Model

```csharp
[TestFixture]
public class PlayerModelTests
{
    private PlayerModel _model;
    
    [SetUp]
    public void SetUp()
    {
        _model = new PlayerModel(100);
    }
    
    [Test]
    public void TakeDamage_ReducesHealth()
    {
        _model.TakeDamage(30);
        Assert.AreEqual(70, _model.CurrentHealth);
    }
    
    [Test]
    public void TakeDamage_WhenKilled_FiresOnDied()
    {
        bool died = false;
        _model.OnDied += () => died = true;
        
        _model.TakeDamage(100);
        
        Assert.IsTrue(died);
    }
    
    [Test]
    public void TakeDamage_WhenDead_DoesNothing()
    {
        _model.TakeDamage(100);
        
        _model.TakeDamage(50);
        
        Assert.AreEqual(0, _model.CurrentHealth);
    }
    
    [Test]
    public void Heal_CannotExceedMax()
    {
        _model.TakeDamage(10);
        
        _model.Heal(50);
        
        Assert.AreEqual(100, _model.CurrentHealth);
    }
}
```

---

## 📊 Пример: Тесты Presenter

```csharp
[TestFixture]
public class PlayerPresenterTests
{
    private PlayerModel _model;
    private IPlayerView _view;
    private IAudioService _audio;
    private PlayerPresenter _presenter;
    
    [SetUp]
    public void SetUp()
    {
        _model = new PlayerModel(100);
        _view = Substitute.For<IPlayerView>();
        _audio = Substitute.For<IAudioService>();
        _presenter = new PlayerPresenter(_model, _view, _audio);
    }
    
    [TearDown]
    public void TearDown()
    {
        _presenter.Dispose();
    }
    
    [Test]
    public void Constructor_UpdatesViewWithInitialHealth()
    {
        _view.Received().UpdateHealthBar(1.0f);
    }
    
    [Test]
    public void TakeDamage_UpdatesView()
    {
        _presenter.TakeDamage(50);
        
        _view.Received().UpdateHealthBar(0.5f);
        _view.Received().PlayHitEffect();
        _audio.Received().PlaySound("Hit");
    }
    
    [Test]
    public void WhenDied_PlaysDeathEffect()
    {
        _presenter.TakeDamage(100);
        
        _view.Received().PlayDeathEffect();
        _audio.Received().PlaySound("Death");
    }
    
    [Test]
    public void Dispose_UnsubscribesFromEvents()
    {
        _presenter.Dispose();
        _view.ClearReceivedCalls();
        
        _model.TakeDamage(50);
        
        _view.DidNotReceive().UpdateHealthBar(Arg.Any<float>());
    }
}
```

---

## ❌ Антипаттерны

### Логика во View

```csharp
// ❌ Плохо
public class PlayerView : MonoBehaviour
{
    public void TakeDamage(int damage)
    {
        _health -= damage;  // Логика в View!
    }
}

// ✅ Хорошо — View только отображает
public void UpdateHealthBar(float normalized)
{
    _slider.value = normalized;
}
```

### Тестирование реализации

```csharp
// ❌ Плохо — тестируем приватное поле
var field = typeof(PlayerModel).GetField("_isDamaged", ...);

// ✅ Хорошо — тестируем публичное поведение
Assert.AreEqual(70, model.CurrentHealth);
```

### Слишком много моков

```csharp
// ❌ Плохо — 10 моков = класс делает слишком много
var view = Substitute.For<IView>();
var logger = Substitute.For<ILogger>();
var config = Substitute.For<IConfig>();
// ... ещё 7 моков

// ✅ Хорошо — 2-3 зависимости
var model = new PlayerModel(100);
var view = Substitute.For<IPlayerView>();
var presenter = new PlayerPresenter(model, view);
```

### Моки вместо реальных объектов

```csharp
// ❌ Плохо — мокаем то что тестируем
var model = Substitute.For<IPlayerModel>();

// ✅ Хорошо — мокаем только зависимости
var model = new PlayerModel(100);  // Реальный объект
var view = Substitute.For<IPlayerView>();  // Мок зависимости
```

---

## ✅ Чеклист

### Качество теста

- [ ] Имя ясно описывает что тестируется
- [ ] Следует AAA (Arrange-Act-Assert)
- [ ] Один assert на тест
- [ ] Нет if/else/loops
- [ ] Тест независим от других
- [ ] Тест детерминирован

### MVP тестирование

- [ ] Model тестируется без моков
- [ ] Presenter тестируется с mock View
- [ ] View НЕ тестируется unit-тестами
- [ ] Presenter реализует IDisposable
- [ ] События проверяются через Received()

---

## 📖 Что тестировать

| Компонент | Тестировать | Как |
|-----------|-------------|-----|
| Model | ✅ Да | Unit-тесты, без моков |
| Presenter | ✅ Да | Unit-тесты, mock View |
| View | ❌ Нет | Только ручное/PlayMode |
| Service | ✅ Да | Unit-тесты |

---

## 📚 Дополнительно

- Архитектура MVP → [UNITY_MVP_ARCHITECTURE_GUIDE.md](./UNITY_MVP_ARCHITECTURE_GUIDE.md)
- Полный пайплайн → [PIPELINE_GUIDE.md](./PIPELINE_GUIDE.md)

---

**Версия:** 2.0  
**Принцип:** Простота превыше всего
