# GC Playground - Практическое изучение Garbage Collection в .NET

Комплексный набор экспериментов и сценариев для глубокого понимания работы GC в .NET.

## 📋 Структура проекта

```
GcPlayground/
├── GcPlayground.Console/          # Основное консольное приложение с меню
│   ├── Scenarios/
│   │   ├── GenerationsScenario.cs # Блок 1: Поколения объектов
│   │   ├── LohScenario.cs         # Блок 2: Large Object Heap
│   │   ├── CardTableScenario.cs   # Блок 4: Card Table
│   │   ├── PinningScenario.cs     # Блок 5: Pinning
│   │   └── LatencyScenario.cs     # Блок 3: Latency
│   ├── Monitoring/
│   │   └── GcMonitor.cs           # Встроенный мониторинг GC
│   └── Program.cs                 # Главное меню
├── GcPlayground.BadService/       # Пример сервиса с антипаттернами (Задание 9)
├── GcPlayground.GoodService/      # Оптимизированный сервис (Задание 10)
└── README.md
```

## 🚀 Быстрый старт

### Установка необходимых инструментов

```bash
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-trace
dotnet tool install -g dotnet-dump
```

### Запуск основного приложения

```bash
cd GcPlayground.ConsoleApp
dotnet run
```

Выберите нужный сценарий из меню.

### Быстрый мониторинг (в отдельном терминале)

```bash
# Найти PID ConsoleApp процесса
Get-Process | Where-Object {$_.ProcessName -like "*ConsoleApp*"} | Select-Object Id, ProcessName

# Начать мониторинг GC метрик
dotnet-counters monitor --process-id <PID> System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate,loh-size,time-in-gc]
```

## 🎯 Блоки заданий

### Блок 1: Поколения и время жизни объектов

**Задание 1.1: Gen0 Allocations Test**
- Наблюдает за аллокацией объектов в Gen0
- Показывает частоту сборок Gen0
- Демонстрирует, что память не накапливается для короткоживущих объектов

Запуск: Выберите опцию `1` в меню

**Задание 1.2: Object Promotion Test**
- Показывает, как объекты переходят из Gen0 в Gen1/Gen2
- Демонстрирует рост памяти при удержании ссылок
- Объясняет концепцию promotion

Запуск: Выберите опцию `2` в меню

**Задание 2: Memory Leak Simulation**
- Воспроизводит типичный memory leak через вечный кэш
- Показывает постоянный рост Gen2
- Демонстрирует OutOfMemoryException

Запуск: Выберите опцию `3` в меню

### Блок 2: LOH и фрагментация

**Задание 3: LOH Allocations Test**
- Показывает поведение Large Object Heap (объекты > 85KB)
- Демонстрирует, что LOH собирается вместе с Gen2

Запуск: Выберите опцию `4` в меню

**Задание 3.2: LOH Fragmentation Test**
- Воспроизводит фрагментацию LOH
- Показывает, что память не возвращается ОС
- Демонстрирует проблему "дыр" в heap

Запуск: Выберите опцию `5` в меню

**Задание 4: LOH Compaction Test**
- Показывает принудительную компакцию LOH
- Сравнивает использование памяти до/после компакции
- Демонстрирует влияние на производительность

Запуск: Выберите опцию `6` в меню

### Блок 3: Паузы и Stop-the-world

**Задание 5: Latency Test**
- Измеряет влияние GC на latency приложения
- Показывает latency percentiles (p50, p95, p99)
- Демонстрирует latency spikes во время GC

Запуск: Выберите опцию `7` в меню

**Задание 6: Server vs Workstation GC Test**
- Сравнивает производительность режимов GC
- Показывает разницу в throughput и latency
- Помогает выбрать оптимальный режим

Запуск: Выберите опцию `8` в меню

### Блок 4: Card Table (критично для senior)

**Задание 7.1: CardTable Simple Test**
- Показывает overhead Card Table при простых Gen2→Gen0 ссылках
- Демонстрирует, как Card Table отслеживает межпоколенные ссылки

Запуск: Выберите опцию `9` в меню

**Задание 7.2: CardTable Stress Test**
- Воспроизводит performance killer - массовые Gen2→Gen0 ссылки
- Показывает резкий рост времени итерации
- Объясняет, почему это проблема в high-load системах

Запуск: Выберите опцию `10` в меню

### Блок 5: Pinning (важно для интервью)

**Задание 8.1: Fixed Pinning Test**
- Демонстрирует краткосрочный pinning через `fixed`
- Показывает минимальное влияние на производительность

Запуск: Выберите опцию `11` в меню

**Задание 8.2: GCHandle Pinning Test**
- Показывает долгосрочный pinning через GCHandle
- Демонстрирует, что pinning препятствует compaction
- Объясняет, почему pinning - это проблема

Запуск: Выберите опцию `12` в меню

## 🌐 Web-приложения (Production сценарии)

### GcPlayground.BadService - Пример плохого сервиса

Демонстрирует типичные антипаттерны:

```bash
cd GcPlayground.BadService
dotnet run
```

**Эндпоинты:**
- `GET /cache/{key}` - Вечный кэш (Антипаттерн 1)
- `GET /holders` - Gen2→Gen0 ссылки (Антипаттерн 3)
- `GET /pin` - Pinning (Антипаттерн 4)
- `GET /health` - Health check

**Мониторинг:**

В отдельном терминале:
```bash
# Получить PID процесса
dotnet-counters monitor --process-id <PID> System.Runtime
```

**Нагрузочное тестирование:**

```bash
# Установить bombardier (если не установлен)
# Windows: scoop install bombardier
# или скачать с https://github.com/codesenberg/bombardier

bombardier -c 10 -d 60s http://localhost:5000/cache/test
bombardier -c 10 -d 60s http://localhost:5000/holders
```

**Ожидаемые проблемы:**
- Рост памяти
- Увеличение GC pause time
- Падение throughput
- Latency spikes

### GcPlayground.GoodService - Оптимизированный сервис

Демонстрирует best practices:

```bash
cd GcPlayground.GoodService
dotnet run
```

**Оптимизации:**
- **Ограниченный кэш с TTL** - Автоматическая очистка устаревших данных
- **ArrayPool** - Переиспользование буферов вместо аллокации
- **Value types** - Использование struct вместо class для избежания Gen2→Gen0 ссылок
- **Stackalloc** - Аллокация на стеке вместо heap

**Эндпоинты:**
- `GET /cache/{key}` - Кэш с TTL
- `GET /pooled` - ArrayPool для больших объектов
- `GET /holders` - Value types вместо reference types
- `GET /span` - Stackalloc для буферов
- `GET /health` - Health check

**Сравнение метрик:**

```bash
# Запустить оба сервиса и сравнить:
# - Throughput (req/sec)
# - Latency (avg, p95, p99)
# - Memory usage
# - GC collections
```

## 🛠️ Использование проектов и утилит

### 📋 Общий порядок работы

1. **Установка инструментов** (однократно):
   ```bash
   dotnet tool install -g dotnet-counters
   dotnet tool install -g dotnet-trace
   dotnet tool install -g dotnet-dump
   ```

2. **Запуск консольного приложения**:
   ```bash
   cd GcPlayground.ConsoleApp
   dotnet run
   ```

3. **Параллельный мониторинг** (в отдельном терминале):
   ```bash
   # Получить PID ConsoleApp процесса
   Get-Process | Where-Object {$_.ProcessName -like "*ConsoleApp*"} | Select-Object Id, ProcessName
   
   # Начать мониторинг
   dotnet-counters monitor --process-id <PID> System.Runtime
   ```

4. **Выбор сценария** из меню и наблюдение за метриками

### 🎯 GcPlayground.ConsoleApp - Основное приложение

**Запуск:**
```bash
cd GcPlayground.ConsoleApp
dotnet run
```

**Функциональность:**
- Интерактивное меню с 12 сценариями
- Реальное отображение метрик GC
- Подробные комментарии в коде
- Возможность остановки и запуска разных тестов

**Рекомендуемый порядок изучения:**
1. **Block 1** (опции 1-3): Основы поколений
2. **Block 2** (опции 4-6): Large Object Heap
3. **Block 3** (опции 7-8): Latency и производительность
4. **Block 4** (опции 9-10): Card Table (для senior)
5. **Block 5** (опции 11-12): Pinning (интервью-вопросы)

**Встроенный мониторинг:**
```csharp
// Можно использовать в коде сценариев
GcMonitor.StartMonitoring();  // Начать мониторинг
// ... ваш код ...
GcMonitor.StopMonitoring();   // Остановить мониторинг
```

### 🌐 GcPlayground.BadService - Антипаттерны

**Запуск:**
```bash
cd GcPlayground.BadService
dotnet run
```

**Порты по умолчанию:**
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

**Эндпоинты для тестирования:**

| Эндпоинт | Антипаттерн | Описание |
|----------|-------------|----------|
| `GET /cache/{key}` | Memory Leak | Вечный кэш без TTL |
| `GET /holders` | Card Table | Gen2→Gen0 ссылки |
| `GET /pin` | Pinning | Накопление pinned объектов |
| `GET /health` | - | Health check |

**Нагрузочное тестирование:**
```bash
# Установка bombardier (если не установлен)
# Windows: scoop install bombardier
# macOS: brew install bombardier
# Linux: sudo apt-get install bombardier

# Тестирование кэша (memory leak)
bombardier -c 10 -d 60s http://localhost:5000/cache/test

# Тестирование holders (Card Table)
bombardier -c 10 -d 60s http://localhost:5000/holders

# Тестирование pinning
bombardier -c 10 -d 60s http://localhost:5000/pin
```

**Ожидаемые проблемы:**
- Постоянный рост памяти (кэш)
- Увеличение времени GC (pinning)
- Деградация производительности (Card Table)

### 🚀 GcPlayground.GoodService - Best Practices

**Запуск:**
```bash
cd GcPlayground.GoodService
dotnet run
```

**Порты по умолчанию:**
- HTTP: http://localhost:5000 (измените если BadService уже запущен)
- HTTPS: https://localhost:5001

**Эндпоинты с оптимизациями:**

| Эндпоинт | Оптимизация | Описание |
|----------|-------------|----------|
| `GET /cache/{key}` | TTL Cache | Кэш с автоматической очисткой |
| `GET /pooled` | ArrayPool | Переиспользование буферов |
| `GET /holders` | Value Types | Struct вместо class |
| `GET /span` | Stackalloc | Аллокация на стеке |
| `GET /health` | - | Health check |

**Сравнительное тестирование:**
```bash
# Запустить BadService на порту 5000
# Запустить GoodService на порту 5001
# Сравнить метрики:

# BadService
bombardier -c 10 -d 60s http://localhost:5000/cache/test

# GoodService  
bombardier -c 10 -d 60s http://localhost:5001/cache/test
```

### 📊 dotnet-counters - Мониторинг в реальном времени

**Базовое использование:**
```bash
# Все метрики GC
dotnet-counters monitor --process-id <PID> System.Runtime

# Только GC метрики
dotnet-counters monitor --process-id <PID> \
  System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate,loh-size,time-in-gc]

# С фильтрацией
dotnet-counters monitor --process-id <PID> \
  System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count] \
  --refresh-interval 1
```

**Получение PID процесса:**
```bash
# PowerShell - фильтрация для конкретного приложения
Get-Process dotnet | Where-Object {$_.MainWindowTitle -like "*ConsoleApp*"} | Select-Object Id, ProcessName, MainWindowTitle
# Или по имени процесса
Get-Process | Where-Object {$_.ProcessName -like "*ConsoleApp*"} | Select-Object Id, ProcessName

# Bash/Linux
ps aux | grep -i consoleapp
# Или более точный поиск
pgrep -f "GcPlayground.ConsoleApp"
```

**Если много dotnet процессов:**
```powershell
# Показать все dotnet процессы с деталями
Get-Process dotnet | Format-Table Id, ProcessName, StartTime, CPU, MainWindowTitle -AutoSize

# Найти свежезапущенный ConsoleApp (сортировка по времени запуска)
Get-Process dotnet | Sort-Object StartTime -Descending | Select-Object -First 3 | Format-Table Id, ProcessName, StartTime, MainWindowTitle

# Фильтр по названию окна (если консольное приложение имеет заголовок)
Get-Process dotnet | Where-Object {$_.MainWindowTitle -match "Console|GcPlayground"} | Select-Object Id, ProcessName, MainWindowTitle
```

**Ключевые метрики и их значения:**

| Метрика | Норма | Проблема | Действия |
|---------|-------|----------|----------|
| `gen-0-gc-count` | Высокий | - | Нормально для активных аллокаций |
| `gen-1-gc-count` | Средний | Высокий | Объекты доживают до Gen1 |
| `gen-2-gc-count` | Низкий | Высокий | Memory leak или долгоживущие объекты |
| `alloc-rate` | Зависит | Очень высокий | Избыточные аллокации |
| `loh-size` | Стабильный | Растет | Фрагментация или утечка в LOH |
| `time-in-gc` | <5% | >10% | Слишком много работы GC |

### 🔍 dotnet-trace - Детальная трассировка

**Запись трейса для анализа:**
```bash
# Запись всех событий GC
dotnet-trace collect --process-id <PID> \
  --providers Microsoft-Windows-DotNETRuntime:0xC14CDCBD:4

# Запись только GC событий
dotnet-trace collect --process-id <PID> \
  --providers Microsoft-Windows-DotNETRuntime:0x1:4

# Запись с ограничением размера
dotnet-trace collect --process-id <PID> \
  --providers Microsoft-Windows-DotNETRuntime:0x1:4 \
  --output-file gc-trace.nettrace
```

**Анализ трейсов:**
```bash
# Конвертация в PerfView формат
dotnet-trace convert gc-trace.nettrace

# Анализ в PerfView (Windows)
# или в SpeedScope (веб-инструмент)
```

### 💾 dotnet-dump - Анализ дампов памяти

**Создание дампа:**
```bash
# Полный дамп
dotnet-dump collect --process-id <PID>

# Дамп только управляемой кучи
dotnet-dump collect --process-id <PID> --type heap

# Автоматический дамп при исключении
dotnet-dump collect --process-id <PID> --diag
```

**Анализ дампа:**
```bash
# Запуск анализа
dotnet-dump analyze <dump-file>

# Команды анализа:
dumpheap -stat          # Статистика объектов
dumpheap -type <Type>   # Конкретный тип
gcroot <ObjectAddress>  # Корни объекта
sos DumpHeap -stat      # Статистика кучи
```

### 🔧 Дополнительные утилиты

**PerfView (Windows):**
```bash
# Скачать с https://github.com/microsoft/perfview
# Анализ трейсов, профилирование CPU, allocation
```

**SpeedScope (веб):**
```bash
# Открыть https://www.speedscope.app/
# Загрузить .nettrace файл для визуализации
```

**BenchmarkDotNet (для бенчмарков):**
```bash
# Добавить в проект: dotnet add package BenchmarkDotNet
# Использовать для измерения производительности
```

### 📝 Типичный рабочий процесс

1. **Подготовка:**
   ```bash
   # Терминал 1: Запуск приложения
   cd GcPlayground.ConsoleApp
   dotnet run
   
   # Терминал 2: Поиск процесса и мониторинг
   Get-Process | Where-Object {$_.ProcessName -like "*ConsoleApp*"} | Select-Object Id, ProcessName
   dotnet-counters monitor --process-id <PID> System.Runtime
   ```

2. **Эксперимент:**
   - Выбрать сценарий в меню
   - Наблюдать за метриками
   - Записать интересные паттерны

3. **Анализ:**
   - Если нужна детализация: `dotnet-trace collect`
   - Если есть подозрение на утечку: `dotnet-dump collect`

4. **Сравнение:**
   - Запустить BadService vs GoodService
   - Сравнить метрики под нагрузкой

5. **Документация:**
   - Записать наблюдения
   - Сравнить с теорией
   - Подготовить вопросы для обсуждения

## 📊 Мониторинг и диагностика

### dotnet-counters

Мониторинг метрик GC в реальном времени:

```bash
# Все метрики System.Runtime
dotnet-counters monitor --process-id <PID> System.Runtime

# Конкретные метрики
dotnet-counters monitor --process-id <PID> \
  System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate,loh-size,time-in-gc]
```

**Ключевые метрики:**
- `gen-0-gc-count` - Количество сборок Gen0
- `gen-1-gc-count` - Количество сборок Gen1
- `gen-2-gc-count` - Количество сборок Gen2
- `alloc-rate` - Скорость аллокации (bytes/sec)
- `loh-size` - Размер Large Object Heap
- `time-in-gc` - Время, потраченное на GC (%)

### dotnet-trace

Запись детальных трейсов для анализа:

```bash
# Запись трейса
dotnet-trace collect --process-id <PID> \
  --providers Microsoft-Windows-DotNETRuntime:0x1:4

# Анализ в PerfView (Windows)
# или в других инструментах анализа
```

### dotnet-dump

Анализ дампов памяти:

```bash
# Создание дампа
dotnet-dump collect --process-id <PID>

# Анализ дампа
dotnet-dump analyze <dump-file>
```

## 🔧 Конфигурация GC

### Workstation GC (по умолчанию)

```json
{
  "configProperties": {
    "System.GC.Server": false,
    "System.GC.Concurrent": true
  }
}
```

**Характеристики:**
- Одна GC thread
- Меньше памяти
- Меньше latency
- Подходит для desktop приложений

### Server GC

```json
{
  "configProperties": {
    "System.GC.Server": true,
    "System.GC.Concurrent": true
  }
}
```

**Характеристики:**
- Одна GC thread на CPU core
- Больше памяти
- Выше throughput
- Подходит для server приложений

## 📚 Ключевые концепции

### Поколения (Generations)

- **Gen0** - Новые объекты, часто собирается
- **Gen1** - Объекты, пережившие одну сборку Gen0
- **Gen2** - Долгоживущие объекты, редко собирается

### Large Object Heap (LOH)

- Объекты > 85KB попадают в отдельный heap
- Не компактифицируется автоматически
- Может привести к фрагментации

### Card Table

- Отслеживает ссылки из Gen2 на Gen0/Gen1
- Позволяет GC не сканировать весь Gen2
- Может быть performance killer при массовых Gen2→Gen0 ссылках

### Pinning

- Препятствует перемещению объекта во время compaction
- Может привести к фрагментации
- Следует избегать в production коде

### Stop-the-world

- GC приостанавливает все потоки приложения
- Вызывает latency spikes
- Влияет на пользовательский опыт

## ✅ Чек-лист успешного прохождения

После завершения всех заданий вы должны уметь:

- [ ] Объяснить разницу между Gen0/Gen1/Gen2
- [ ] Предсказать, когда объект попадет в LOH
- [ ] Диагностировать memory leak через dotnet-counters
- [ ] Объяснить, почему Gen2→Gen0 ссылки - это проблема
- [ ] Выбрать между Server и Workstation GC
- [ ] Использовать ArrayPool для уменьшения аллокаций
- [ ] Объяснить влияние pinning на compaction
- [ ] Оптимизировать реальный сервис с GC проблемами

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

## 📚 Ресурсы

- [.NET GC Internals](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/garbage-collection.md)
- [Pro .NET Memory Management](https://prodotnetmemory.com/)
- [PerfView Tutorial](https://github.com/microsoft/perfview/blob/main/documentation/Tutorial.md)
- [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)
- [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
- [dotnet-dump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump)

## 💡 Советы для экспериментов

1. **Используйте мониторинг параллельно** - Запускайте dotnet-counters в отдельном терминале
2. **Варьируйте нагрузку** - Меняйте размер объектов, количество итераций
3. **Сравнивайте результаты** - Записывайте метрики для сравнения
4. **Анализируйте трейсы** - Используйте dotnet-trace для детального анализа
5. **Экспериментируйте с конфигурацией** - Пробуйте разные режимы GC

## 🤝 Вклад

Если вы нашли ошибки или хотите добавить новые сценарии, создайте issue или pull request.

---

**Автор:** GC Learning Project
**Версия:** 1.0
**Последнее обновление:** 2024
