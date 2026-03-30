# План реализации учебных заданий по GC в .NET

## 📋 Общая структура обучения

**Цель:** Практическое освоение принципов работы GC через измеримые эксперименты

**Методология:** Для каждого задания:
1. **Гипотеза** - что ожидаем увидеть
2. **Реализация** - код эксперимента
3. **Измерение** - инструменты мониторинга
4. **Анализ** - что реально произошло и почему

**Инструменты (установить заранее):**
- `dotnet-counters` - мониторинг метрик GC в реальном времени
- `dotnet-trace` - запись трейсов для детального анализа
- `dotnet-dump` - анализ дампов памяти
- PerfView (опционально) - продвинутый анализ

---

## 🎯 Блок 1: Поколения и время жизни объектов

### Задание 1.1: "Кто куда попадает" (Gen0 аллокации)

**Связь с лекцией:** Поколение 0 - для новых объектов

**Цель:** Понять, как короткоживущие объекты обрабатываются в Gen0

**Шаги реализации:**

1. **Создать проект:**
   ```bash
   dotnet new console -n GcPlayground.Gen0
   cd GcPlayground.Gen0
   ```

2. **Реализовать код (Program.cs):**
   ```csharp
   // Эксперимент 1: Короткоживущие объекты
   Console.WriteLine("=== Gen0 Allocations Test ===");
   Console.WriteLine("Press any key to start...");
   Console.ReadKey();
   
   while (true)
   {
       var arr = new byte[1000]; // маленький объект
       Thread.Sleep(1); // небольшая задержка для наблюдения
   }
   ```

3. **Запустить мониторинг:**
   ```bash
   # В отдельном терминале
   dotnet-counters monitor --process-id <PID> System.Runtime
   ```

4. **Метрики для наблюдения:**
   - `gen-0-gc-count` - количество сборок Gen0
   - `alloc-rate` - скорость аллокации (bytes/sec)
   - `gen-1-gc-count`, `gen-2-gc-count` - должны быть минимальны

5. **Ожидаемый результат:**
   - Высокая частота Gen0 collections
   - Gen1/Gen2 почти не растут
   - Память не накапливается

---

### Задание 1.2: "Удержание объектов" (Promotion в Gen1/Gen2)

**Связь с лекцией:** Объекты, выжившие после сборки, переходят в старшие поколения

**Цель:** Увидеть promotion объектов между поколениями

**Шаги реализации:**

1. **Модифицировать код:**
   ```csharp
   Console.WriteLine("=== Object Promotion Test ===");
   var list = new List<byte[]>();
   
   while (true)
   {
       var arr = new byte[1000];
       list.Add(arr); // удерживаем ссылку
       
       if (list.Count % 1000 == 0)
       {
           Console.WriteLine($"Objects held: {list.Count}");
       }
       
       Thread.Sleep(1);
   }
   ```

2. **Наблюдать изменения:**
   - Gen0 collections продолжаются
   - Gen1 collections начинают расти
   - Gen2 collections появляются
   - Память растет

3. **Ключевой инсайт:**
   > GC работает на основе **достижимости**, а не размера объектов

---

### Задание 2: "Псевдо-кэш убивает GC" (Memory Leak)

**Связь с лекцией:** Долгоживущие объекты попадают в Gen2

**Цель:** Воспроизвести типичный memory leak через вечный кэш

**Шаги реализации:**

1. **Создать новый проект:**
   ```bash
   dotnet new console -n GcPlayground.MemoryLeak
   ```

2. **Реализовать код:**
   ```csharp
   static List<object> cache = new();
   
   Console.WriteLine("=== Memory Leak Simulation ===");
   
   while (true)
   {
       cache.Add(new object());
       
       if (cache.Count % 10000 == 0)
       {
           Console.WriteLine($"Cache size: {cache.Count}");
           Console.WriteLine($"Gen2 size: {GC.GetGeneration(cache)} / Collections: {GC.CollectionCount(2)}");
       }
   }
   ```

3. **Вопросы для анализа:**
   - Почему Gen2 растет?
   - Почему память не освобождается?
   - Как это выглядит в production?

4. **Ожидаемый результат:**
   - Постоянный рост Gen2
   - Увеличение частоты Gen2 collections
   - OutOfMemoryException (если не остановить)

---

## 🎯 Блок 2: LOH и фрагментация

### Задание 3: "Large Object Heap"

**Связь с лекцией:** Большие объекты (>85KB) попадают в LOH, который дефрагментируется через sweep

**Цель:** Понять поведение LOH

**Шаги реализации:**

1. **Создать проект:**
   ```bash
   dotnet new console -n GcPlayground.LOH
   ```

2. **Эксперимент 3.1 - Короткоживущие большие объекты:**
   ```csharp
   Console.WriteLine("=== LOH Allocations Test ===");
   
   while (true)
   {
       var arr = new byte[100_000]; // > 85KB → LOH
       Thread.Sleep(1);
   }
   ```

3. **Наблюдать через dotnet-counters:**
   - `loh-size` - размер LOH
   - `gen-2-gc-count` - LOH собирается вместе с Gen2

4. **Эксперимент 3.2 - Фрагментация LOH:**
   ```csharp
   Console.WriteLine("=== LOH Fragmentation Test ===");
   var list = new List<byte[]>();
   
   while (true)
   {
       list.Add(new byte[100_000]);
       
       if (list.Count > 1000)
           list.RemoveAt(0); // создаем "дыры"
       
       if (list.Count % 100 == 0)
       {
           Console.WriteLine($"List size: {list.Count}, LOH collections: {GC.CollectionCount(2)}");
       }
   }
   ```

5. **Ожидаемый результат:**
   - Рост памяти из-за фрагментации
   - LOH не компактифицируется автоматически
   - Память не возвращается ОС

---

### Задание 4: "Принудительная компакция LOH"

**Связь с лекцией:** LOH можно компактифицировать принудительно

**Цель:** Сравнить поведение с/без компакции

**Шаги реализации:**

1. **Модифицировать предыдущий код:**
   ```csharp
   Console.WriteLine("=== LOH Compaction Test ===");
   var list = new List<byte[]>();
   int iteration = 0;
   
   while (true)
   {
       list.Add(new byte[100_000]);
       
       if (list.Count > 1000)
           list.RemoveAt(0);
       
       iteration++;
       
       // Каждые 5000 итераций - компактим
       if (iteration % 5000 == 0)
       {
           Console.WriteLine("Triggering LOH compaction...");
           GCSettings.LargeObjectHeapCompactionMode = 
               GCLargeObjectHeapCompactionMode.CompactOnce;
           GC.Collect();
           Console.WriteLine("Compaction done");
       }
   }
   ```

2. **Сравнить метрики:**
   - Использование памяти до/после компакции
   - Время паузы GC
   - Фрагментация

---

## 🎯 Блок 3: Паузы и Stop-the-world

### Задание 5: "Latency в веб-приложении"

**Связь с лекцией:** GC создает паузы (Stop-the-world)

**Цель:** Измерить влияние GC на latency

**Шаги реализации:**

1. **Создать Minimal API проект:**
   ```bash
   dotnet new web -n GcPlayground.WebLatency
   ```

2. **Реализовать эндпоинт (Program.cs):**
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   var app = builder.Build();
   
   app.MapGet("/", () =>
   {
       var data = new byte[10_000_000]; // 10MB аллокация
       return "ok";
   });
   
   app.MapGet("/health", () => "healthy");
   
   app.Run();
   ```

3. **Нагрузочное тестирование:**
   ```bash
   # Установить bombardier
   # Windows: scoop install bombardier
   # или скачать с GitHub
   
   bombardier -c 10 -d 60s http://localhost:5000/
   ```

4. **Наблюдать:**
   - Latency percentiles (p50, p95, p99)
   - Latency spikes во время GC
   - Throughput

5. **Запустить с мониторингом:**
   ```bash
   dotnet-counters monitor --process-id <PID> System.Runtime
   ```

---

### Задание 6: "Server GC vs Workstation GC"

**Связь с лекцией:** Разные режимы GC для разных сценариев

**Цель:** Сравнить производительность режимов GC

**Шаги реализации:**

1. **Конфигурация Workstation GC (по умолчанию):**
   ```json
   // runtimeconfig.template.json или csproj
   {
     "configProperties": {
       "System.GC.Server": false,
       "System.GC.Concurrent": true
     }
   }
   ```

2. **Запустить тест и записать метрики:**
   ```bash
   bombardier -c 10 -d 60s http://localhost:5000/ > workstation-results.txt
   ```

3. **Конфигурация Server GC:**
   ```json
   {
     "configProperties": {
       "System.GC.Server": true,
       "System.GC.Concurrent": true
     }
   }
   ```

4. **Повторить тест:**
   ```bash
   bombardier -c 10 -d 60s http://localhost:5000/ > server-results.txt
   ```

5. **Сравнить:**
   - Throughput (req/sec)
   - Latency (avg, p95, p99)
   - CPU usage
   - Memory usage

6. **Ожидаемые различия:**
   - Server GC: выше throughput, больше памяти
   - Workstation GC: меньше latency, меньше памяти

---

## 🎯 Блок 4: Card Table (критично для senior)

### Задание 7: "Ссылки Gen2 → Gen0"

**Связь с лекцией:** CardTable отслеживает ссылки из старших поколений на младшие

**Цель:** Понять overhead CardTable при межпоколенных ссылках

**Шаги реализации:**

1. **Создать проект:**
   ```bash
   dotnet new console -n GcPlayground.CardTable
   ```

2. **Эксперимент 7.1 - Простой случай:**
   ```csharp
   class Holder
   {
       public object? Ref;
   }
   
   Console.WriteLine("=== CardTable Simple Test ===");
   
   var holder = new Holder();
   
   // Принудительно переводим holder в Gen2
   GC.Collect();
   GC.Collect();
   GC.Collect();
   
   Console.WriteLine($"Holder generation: {GC.GetGeneration(holder)}");
   
   while (true)
   {
       holder.Ref = new object(); // Gen2 → Gen0 ссылка
       Thread.Sleep(1);
   }
   ```

3. **Эксперимент 7.2 - Массовый случай (performance killer):**
   ```csharp
   Console.WriteLine("=== CardTable Stress Test ===");
   var holders = new List<Holder>();
   
   // Создаем миллион holders
   for (int i = 0; i < 1_000_000; i++)
   {
       holders.Add(new Holder());
       if (i % 100000 == 0)
           Console.WriteLine($"Created {i} holders");
   }
   
   // Переводим в Gen2
   GC.Collect();
   GC.Collect();
   GC.Collect();
   
   Console.WriteLine("Starting reference updates...");
   var sw = System.Diagnostics.Stopwatch.StartNew();
   
   while (true)
   {
       foreach (var h in holders)
       {
           h.Ref = new object(); // массовые Gen2→Gen0 ссылки
       }
       
       sw.Stop();
       Console.WriteLine($"Iteration time: {sw.ElapsedMilliseconds}ms");
       sw.Restart();
   }
   ```

4. **Наблюдать:**
   - Резкий рост времени итерации
   - Увеличение Gen0 collection time
   - Деградация производительности

5. **Ключевой инсайт:**
   > Это реальный performance killer в high-load системах с кэшами

---

## 🎯 Блок 5: Pinning (важно для интервью)

### Задание 8: "Fixed vs GCHandle"

**Связь с лекцией:** Pinning препятствует compaction

**Цель:** Понять влияние pinning на GC

**Шаги реализации:**

1. **Создать проект:**
   ```bash
   dotnet new console -n GcPlayground.Pinning
   ```

2. **Эксперимент 8.1 - Fixed (краткосрочный pinning):**
   ```csharp
   Console.WriteLine("=== Fixed Pinning Test ===");
   
   while (true)
   {
       var arr = new byte[1000];
       
       unsafe
       {
           fixed (byte* ptr = arr)
           {
               // Используем указатель
               *ptr = 42;
           } // pinning снимается здесь
       }
       
       Thread.Sleep(1);
   }
   ```

3. **Эксперимент 8.2 - GCHandle (долгосрочный pinning):**
   ```csharp
   Console.WriteLine("=== GCHandle Pinning Test ===");
   var handles = new List<GCHandle>();
   
   for (int i = 0; i < 10000; i++)
   {
       var arr = new byte[1000];
       var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
       handles.Add(handle);
       
       if (i % 1000 == 0)
           Console.WriteLine($"Pinned {i} objects");
   }
   
   Console.WriteLine("All objects pinned. Triggering GC...");
   
   // Пытаемся вызвать компакцию
   for (int i = 0; i < 10; i++)
   {
       GC.Collect();
       Console.WriteLine($"GC #{i} completed");
       Thread.Sleep(1000);
   }
   
   // Освобождаем
   foreach (var h in handles)
       h.Free();
   ```

4. **Измерить через dotnet-trace:**
   ```bash
   dotnet-trace collect --process-id <PID> --providers Microsoft-Windows-DotNETRuntime:0x1:4
   ```

5. **Анализировать:**
   - Fragmentation heap
   - GC pause time
   - Compaction efficiency

---

## 🎯 Блок 6: Production сценарии

### Задание 9: "Плохой сервис"

**Связь с лекцией:** Комбинация всех антипаттернов

**Цель:** Создать реалистичный "плохой" сервис

**Шаги реализации:**

1. **Создать проект:**
   ```bash
   dotnet new web -n GcPlayground.BadService
   ```

2. **Реализовать сервис с антипаттернами:**
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   var app = builder.Build();
   
   // Антипаттерн 1: Вечный кэш
   static Dictionary<string, byte[]> cache = new();
   
   // Антипаттерн 2: Большие объекты в кэше
   app.MapGet("/cache/{key}", (string key) =>
   {
       if (!cache.ContainsKey(key))
       {
           cache[key] = new byte[200_000]; // LOH
       }
       return "cached";
   });
   
   // Антипаттерн 3: Gen2→Gen0 ссылки
   static List<Holder> holders = new();
   
   app.MapGet("/holders", () =>
   {
       if (holders.Count == 0)
       {
           for (int i = 0; i < 100_000; i++)
               holders.Add(new Holder());
       }
       
       // Массовое обновление ссылок
       foreach (var h in holders)
           h.Ref = new object();
       
       return "updated";
   });
   
   // Антипаттерн 4: Pinning
   static List<GCHandle> pinnedHandles = new();
   
   app.MapGet("/pin", () =>
   {
       var arr = new byte[1000];
       var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
       pinnedHandles.Add(handle);
       return $"pinned {pinnedHandles.Count}";
   });
   
   app.Run();
   
   class Holder { public object? Ref; }
   ```

3. **Нагрузить сервис:**
   ```bash
   # Терминал 1: мониторинг
   dotnet-counters monitor --process-id <PID> System.Runtime
   
   # Терминал 2: нагрузка
   bombardier -c 10 -d 60s http://localhost:5000/cache/test
   bombardier -c 10 -d 60s http://localhost:5000/holders
   ```

4. **Наблюдать деградацию:**
   - Рост памяти
   - Увеличение GC pause time
   - Падение throughput
   - Latency spikes

---

### Задание 10: "Почини production"

**Связь с лекцией:** Применение всех знаний для оптимизации

**Цель:** Исправить проблемы из Задания 9

**Шаги реализации:**

1. **Диагностика через counters:**
   ```bash
   dotnet-counters monitor --process-id <PID> \
     System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate,loh-size,time-in-gc]
   ```

2. **Создать исправленную версию:**
   ```bash
   dotnet new web -n GcPlayground.GoodService
   ```

3. **Исправление 1: Ограниченный кэш с TTL:**
   ```csharp
   using System.Collections.Concurrent;
   
   record CacheEntry(byte[] Data, DateTime ExpiresAt);
   
   static ConcurrentDictionary<string, CacheEntry> cache = new();
   
   app.MapGet("/cache/{key}", (string key) =>
   {
       // Очистка устаревших
       var now = DateTime.UtcNow;
       foreach (var k in cache.Keys)
       {
           if (cache.TryGetValue(k, out var entry) && entry.ExpiresAt < now)
               cache.TryRemove(k, out _);
       }
       
       if (!cache.TryGetValue(key, out var cached))
       {
           cached = new CacheEntry(
               new byte[200_000], 
               DateTime.UtcNow.AddMinutes(5)
           );
           cache[key] = cached;
       }
       
       return "cached";
   });
   ```

4. **Исправление 2: Pooling для больших объектов:**
   ```csharp
   using System.Buffers;
   
   app.MapGet("/pooled", () =>
   {
       var buffer = ArrayPool<byte>.Shared.Rent(200_000);
       try
       {
           // Используем buffer
           return "ok";
       }
       finally
       {
           ArrayPool<byte>.Shared.Return(buffer);
       }
   });
   ```

5. **Исправление 3: Избегание Gen2→Gen0 ссылок:**
   ```csharp
   // Вместо хранения ссылок на короткоживущие объекты
   // используем value types или пересоздаем структуры
   
   struct HolderStruct
   {
       public int Value; // value type вместо reference
   }
   
   static List<HolderStruct> holders = new();
   ```

6. **Исправление 4: Избегание pinning:**
   ```csharp
   // Используем Span<T> вместо pinning
   app.MapGet("/span", () =>
   {
       Span<byte> buffer = stackalloc byte[1000];
       // Работаем с buffer без аллокации в heap
       return "ok";
   });
   ```

7. **Сравнить метрики:**
   - До/после оптимизации
   - Throughput
   - Latency
   - Memory usage
   - GC collections

---

## 🎯 Финальный проект: GC Playground

### Цель: Единое приложение для всех экспериментов

**Шаги реализации:**

1. **Создать решение:**
   ```bash
   dotnet new sln -n GcPlayground
   dotnet new console -n GcPlayground.Console
   dotnet sln add GcPlayground.Console
   ```

2. **Структура проекта:**
   ```
   GcPlayground/
   ├── GcPlayground.Console/
   │   ├── Scenarios/
   │   │   ├── GenerationsScenario.cs
   │   │   ├── LohScenario.cs
   │   │   ├── CardTableScenario.cs
   │   │   ├── PinningScenario.cs
   │   │   └── LatencyScenario.cs
   │   ├── Monitoring/
   │   │   └── GcMonitor.cs
   │   └── Program.cs
   └── README.md
   ```

3. **Реализовать меню выбора сценариев:**
   ```csharp
   class Program
   {
       static void Main()
       {
           while (true)
           {
               Console.Clear();
               Console.WriteLine("=== GC Playground ===");
               Console.WriteLine("1. Generations (Gen0/Gen1/Gen2)");
               Console.WriteLine("2. LOH and Fragmentation");
               Console.WriteLine("3. CardTable Stress Test");
               Console.WriteLine("4. Pinning Comparison");
               Console.WriteLine("5. Latency Test");
               Console.WriteLine("6. Memory Leak Simulation");
               Console.WriteLine("7. Server vs Workstation GC");
               Console.WriteLine("0. Exit");
               
               var choice = Console.ReadLine();
               
               switch (choice)
               {
                   case "1": RunGenerationsScenario(); break;
                   case "2": RunLohScenario(); break;
                   // ...
               }
           }
       }
   }
   ```

4. **Добавить встроенный мониторинг:**
   ```csharp
   class GcMonitor
   {
       public static void StartMonitoring()
       {
           Task.Run(() =>
           {
               while (true)
               {
                   Console.WriteLine($"Gen0: {GC.CollectionCount(0)}, " +
                                   $"Gen1: {GC.CollectionCount(1)}, " +
                                   $"Gen2: {GC.CollectionCount(2)}, " +
                                   $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                   Thread.Sleep(1000);
               }
           });
       }
   }
   ```

5. **Добавить README с инструкциями:**
   - Как запускать каждый сценарий
   - Какие метрики смотреть
   - Ожидаемые результаты

---

## 📊 Чек-лист успешного прохождения

После завершения всех заданий вы должны уметь:

- [ ] Объяснить разницу между Gen0/Gen1/Gen2
- [ ] Предсказать, когда объект попадет в LOH
- [ ] Диагностировать memory leak через dotnet-counters
- [ ] Объяснить, почему Gen2→Gen0 ссылки - это проблема
- [ ] Выбрать между Server и Workstation GC
- [ ] Использовать ArrayPool для уменьшения аллокаций
- [ ] Объяснить влияние pinning на compaction
- [ ] Оптимизировать реальный сервис с GC проблемами

---

## 🚀 Следующие шаги

После завершения базовых заданий:

1. **Продвинутые темы:**
   - Region-based GC (.NET 7+)
   - DATAS (Dynamic Adaptation To Application Sizes)
   - Background GC internals

2. **Production кейсы:**
   - Анализ реальных дампов
   - Оптимизация high-throughput сервисов
   - Debugging memory leaks в production

3. **Подготовка к интервью:**
   - Типичные вопросы по GC (FAANG level)
   - Whiteboard задачи
   - System design с учетом GC

---

## 📚 Ресурсы

- [.NET GC Internals](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/garbage-collection.md)
- [Pro .NET Memory Management](https://prodotnetmemory.com/)
- [PerfView Tutorial](https://github.com/microsoft/perfview/blob/main/documentation/Tutorial.md)
- [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)
