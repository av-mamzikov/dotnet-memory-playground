# 🔥 Общая идея обучения

Каждое задание должно отвечать на 3 вопроса:

1. **Что я ожидаю?**
2. **Что реально происходит?**
3. **Почему GC делает именно так?**

Инструменты (обязательно):

* `dotnet-counters`
* `dotnet-trace`
* `dotnet-dump`
* PerfView (если готов углубляться)

---

# 🧪 Блок 1. Поколения и время жизни объектов

## Задание 1: “Кто куда попадает”

Напиши код:

```csharp
while (true)
{
    var arr = new byte[1000]; // маленький объект
}
```

### Что сделать:

* Запусти с `dotnet-counters monitor System.Runtime`
* Смотри:

    * Gen 0 collections
    * Allocation rate

### Потом:

Добавь:

```csharp
var list = new List<byte[]>();

while (true)
{
    var arr = new byte[1000];
    list.Add(arr); // удерживаем
}
```

### Что увидишь:

* Объекты начинают “проживать”
* Пойдут Gen1 / Gen2

👉 **Осознание:**
GC — это не про размер, а про **достижимость**

---

## Задание 2: “Псевдо-кэш убивает GC”

```csharp
static List<object> cache = new();

while (true)
{
    cache.Add(new object());
}
```

### Вопросы:

* Почему Gen2 растёт?
* Почему память не освобождается?

👉 Это 1:1 сценарий реальных memory leaks

---

# 🧪 Блок 2. LOH и фрагментация

## Задание 3: Large Object Heap

```csharp
while (true)
{
    var arr = new byte[100_000]; // > 85k → LOH
}
```

### Изучи:

* LOH не компактифицируется (по умолчанию)
* GC работает иначе

### Усложнение:

```csharp
var list = new List<byte[]>();

while (true)
{
    list.Add(new byte[100_000]);
    if (list.Count > 1000)
        list.RemoveAt(0);
}
```

👉 Получишь:

* фрагментацию
* рост памяти

---

## Задание 4: Принудительная компакция LOH

Добавь:

```csharp
GCSettings.LargeObjectHeapCompactionMode = 
    GCLargeObjectHeapCompactionMode.CompactOnce;

GC.Collect();
```

👉 Сравни поведение ДО / ПОСЛЕ

---

# 🧪 Блок 3. Паузы и Stop-the-world

## Задание 5: Latency

Сделай API (minimal API):

```csharp
app.MapGet("/", () =>
{
    var data = new byte[10_000_000];
    return "ok";
});
```

### Нагрузка:

* bombard через `wrk` / `bombardier`

### Наблюдение:

* latency spikes

---

## Задание 6: Server GC vs Workstation

Включи:

```json
"System.GC.Server": true
```

👉 Сравни:

* throughput
* latency

---

# 🧪 Блок 4. Card Table (самое важное для senior)

## Задание 7: Ссылки Gen2 → Gen0

```csharp
class Holder
{
    public object Ref;
}

var holder = new Holder();

while (true)
{
    holder.Ref = new object(); // старая → молодая ссылка
}
```

👉 Что происходит:

* GC обязан отслеживать эти ссылки через Card Table

---

## Усложнение:

```csharp
var holders = new List<Holder>();

for (int i = 0; i < 1_000_000; i++)
    holders.Add(new Holder());

while (true)
{
    foreach (var h in holders)
        h.Ref = new object();
}
```

👉 Ты увидишь:

* резкий рост нагрузки GC
* деградацию

💡 **Это реальный perf killer в high-load системах**

---

# 🧪 Блок 5. Pinning (очень важно для интервью)

## Задание 8: Fixed vs GCHandle

```csharp
var arr = new byte[1000];

fixed (byte* ptr = arr)
{
    // use
}
```

vs

```csharp
var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
```

### Что проверить:

* fragmentation
* GC pauses

👉 Вывод:

* pinning ломает compaction

---

# 🧪 Блок 6. Реальный production сценарий

## Задание 9: “Плохой сервис”

Сделай сервис:

* кэш (Dictionary)
* большие объекты
* ссылки между объектами

Добавь ошибки:

* вечный кэш
* LOH
* Gen2 → Gen0 ссылки

---

## Задание 10: “Почини прод”

Ты должен:

1. Найти проблему через counters
2. Объяснить причину
3. Исправить:

    * pooling
    * struct / stackalloc
    * уменьшение аллокаций
    * breaking references

---

# 🧠 Самое ценное (инсайты, которые ты должен получить)

Если ты сделал все задания, ты начнёшь **чувствовать GC**, а не просто знать:

### 1. GC — это про граф ссылок, а не память

(ключевой инсайт)

### 2. Gen2 → Gen0 ссылки = зло

### 3. LOH — отдельный мир

### 4. Pinning = враг компакции

### 5. Allocation rate важнее total memory

---

# 🎯 Как превратить это в систему (очень важно)

Сделай себе мини-проект:

> **"GC Playground"**

Сценарии:

* режимы нагрузки
* переключение GC
* включение/выключение антипаттернов

---

# 🚀 Хочешь следующий уровень?

Могу:

* разобрать **реальные GC вопросы с интервью (Amazon / Google / EPAM)**
* или дать **одну большую задачу уровня Senior**, где ты будешь “расследовать продакшн-проблему”

Что выберешь?
