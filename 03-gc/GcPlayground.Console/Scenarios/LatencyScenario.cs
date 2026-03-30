using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;

namespace GcPlayground.ConsoleApp.Scenarios
{
    /// <summary>
    /// Демонстрирует влияние сборки мусора на задержку (latency) приложения.
    /// 
    /// Latency - это время отклика приложения на запрос.
    /// GC может вызвать паузы (stop-the-world), которые увеличивают latency.
    /// 
    /// Ключевые метрики:
    /// - Average latency - средняя задержка
    /// - P95 latency - 95-й процентиль (95% запросов быстрее этого)
    /// - P99 latency - 99-й процентиль (99% запросов быстрее этого)
    /// - Max latency - максимальная задержка (худший случай)
    /// 
    /// GC влияет на latency:
    /// - Gen0 сборка - быстрая, но частая
    /// - Gen2 сборка - медленная, но редкая
    /// - Server GC - параллельная сборка (лучше для latency)
    /// - Workstation GC - последовательная сборка (хуже для latency)
    /// </summary>
    public class LatencyScenario
    {
        /// <summary>
        /// Тест latency с большими выделениями памяти.
        /// 
        /// Что происходит:
        /// 1. Симулируем обработку запроса (выделение 10MB памяти)
        /// 2. Измеряем время каждого "запроса"
        /// 3. Собираем статистику по latency (среднее, P95, P99, максимум)
        /// 4. Наблюдаем как GC влияет на задержки
        /// 
        /// Что смотреть:
        /// - Average latency должна быть стабильной
        /// - Max latency может быть намного выше среднего (GC паузы)
        /// - P95 и P99 показывают "хвост" распределения
        /// - Когда происходят Gen2 сборки, latency резко возрастает
        /// 
        /// В реальных приложениях:
        /// - Высокий P99 latency - это проблема для пользователей
        /// - Нужно минимизировать паузы GC
        /// - Server GC часто лучше для latency-sensitive приложений
        /// </summary>
        public static void RunLatencyTest()
        {
            Console.WriteLine("=== Latency Test ===");
            Console.WriteLine("Simulating request processing with large allocations...");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int iterations = 0;
            // Список для сбора времени выполнения каждого "запроса"
            var latencies = new List<long>();

            while (true)
            {
                // Измеряем время выполнения одного "запроса"
                var iterSw = System.Diagnostics.Stopwatch.StartNew();
                
                // Симулируем обработку запроса - выделяем 10MB памяти
                // Это может вызвать GC сборку, что добавит паузу
                var data = new byte[10_000_000];
                
                iterSw.Stop();
                // Сохраняем время выполнения
                latencies.Add(iterSw.ElapsedMilliseconds);
                iterations++;

                // Каждые 100 итераций выводим статистику
                if (iterations % 100 == 0)
                {
                    sw.Stop();
                    // Вычисляем статистику по latency
                    var avgLatency = latencies.Average();
                    var maxLatency = latencies.Max();
                    // P95 - 95-й процентиль (95% запросов быстрее этого значения)
                    var p95 = latencies.OrderBy(x => x).Skip((int)(latencies.Count * 0.95)).First();
                    // P99 - 99-й процентиль (99% запросов быстрее этого значения)
                    var p99 = latencies.OrderBy(x => x).Skip((int)(latencies.Count * 0.99)).First();

                    Console.WriteLine($"Iterations: {iterations}, Avg: {avgLatency}ms, Max: {maxLatency}ms, " +
                        $"P95: {p95}ms, P99: {p99}ms, " +
                        $"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");

                    // Очищаем список для следующей порции данных
                    latencies.Clear();
                    sw.Restart();
                }

                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Сравнение Server и Workstation режимов GC.
        /// 
        /// Что происходит:
        /// 1. Выводим текущий режим GC (Server или Workstation)
        /// 2. Симулируем обработку запросов с выделением памяти
        /// 3. Измеряем latency в текущем режиме
        /// 4. Сравниваем результаты
        /// 
        /// Различия между режимами:
        /// 
        /// Workstation GC:
        /// - Одна куча для всех потоков
        /// - Сборка мусора на одном потоке
        /// - Может быть более эффективен для однопоточных приложений
        /// - Худше для многопоточных приложений (высокий latency)
        /// 
        /// Server GC:
        /// - Отдельная куча для каждого процессорного ядра
        /// - Параллельная сборка мусора
        /// - Лучше для многопоточных приложений
        /// - Лучше для latency-sensitive приложений
        /// - Требует больше памяти
        /// 
        /// Что смотреть:
        /// - Если Server=true, то используется Server GC (лучше для latency)
        /// - Если Server=false, то используется Workstation GC
        /// - Сравните latency между режимами (запустите дважды с разными настройками)
        /// </summary>
        public static void RunServerVsWorkstationTest()
        {
            Console.WriteLine("=== Server vs Workstation GC Test ===");
            // Выводим текущий режим GC
            Console.WriteLine($"Current GC Mode - Server: {GCSettings.IsServerGC}");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int iterations = 0;
            // Список для сбора времени выполнения каждого "запроса"
            var latencies = new List<long>();

            while (true)
            {
                // Измеряем время выполнения одного "запроса"
                var iterSw = System.Diagnostics.Stopwatch.StartNew();
                
                // Симулируем обработку запроса - выделяем 5MB памяти
                var data = new byte[5_000_000];
                
                iterSw.Stop();
                // Сохраняем время выполнения
                latencies.Add(iterSw.ElapsedMilliseconds);
                iterations++;

                // Каждые 50 итераций выводим статистику
                if (iterations % 50 == 0)
                {
                    sw.Stop();
                    // Вычисляем статистику по latency
                    var avgLatency = latencies.Average();
                    var maxLatency = latencies.Max();

                    Console.WriteLine($"Iterations: {iterations}, Avg: {avgLatency}ms, Max: {maxLatency}ms, " +
                        $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");

                    // Очищаем список для следующей порции данных
                    latencies.Clear();
                    sw.Restart();
                }

                Thread.Sleep(10);
            }
        }
    }
}
