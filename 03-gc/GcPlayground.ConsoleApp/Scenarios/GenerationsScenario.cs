using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace GcPlayground.ConsoleApp.Scenarios
{
    /// <summary>
    /// Демонстрирует поведение сборщика мусора в отношении поколений объектов.
    /// 
    /// В .NET GC используется поколенческая модель:
    /// - Gen0: новые объекты, собираются часто и быстро
    /// - Gen1: объекты, пережившие одну сборку Gen0
    /// - Gen2: долгоживущие объекты, собираются редко
    /// </summary>
    public class GenerationsScenario
    {
        /// <summary>
        /// Тест Gen0 сборки мусора.
        /// 
        /// Что происходит:
        /// 1. Создаем короткоживущие объекты (byte[1000])
        /// 2. Они не сохраняются в переменных, поэтому становятся мусором сразу
        /// 3. GC собирает их в Gen0 (самая быстрая сборка)
        /// 4. Наблюдаем как часто происходят Gen0 сборки
        /// 
        /// Что смотреть:
        /// - Gen0 счетчик растет быстро (много сборок)
        /// - Gen1 и Gen2 почти не меняются
        /// - Время выполнения остается стабильным (Gen0 сборка быстрая)
        /// </summary>
        public static void RunGen0Test()
        {
            var initialCollectionCount = GC.CollectionCount(0);
            var size = 1000;
            Console.WriteLine("=== Gen0 Allocations Test ===");
            Console.Write($"Size (default {size}): ");
            if (int.TryParse(Console.ReadLine()!, out var newsize))
                size = newsize;
            Console.WriteLine($"Using size {size}");
            int iterations = 0;

            var sw = Stopwatch.StartNew();

            while (true)
            {
                // Создаем временный объект (1KB), который сразу становится мусором
                var arr = new byte[size];
                iterations++;

                if (sw.Elapsed.TotalSeconds >= 5)
                {
                    sw.Stop();
                    var totalSize = size * iterations;
                    var gen0Collects = GC.CollectionCount(0) - initialCollectionCount;
                    // GC.CollectionCount(generation) - количество сборок для поколения
                    // GC.GetTotalMemory(false) - общий объем памяти (без принудительной сборки)
                    Console.WriteLine(
                        $"Iterations: {iterations}, TotlSize: {totalSize}, Time: {sw.ElapsedMilliseconds}ms, " +
                        $"Gen0: {gen0Collects} (each {(gen0Collects == 0 ? -1 : (totalSize/gen0Collects))} byte), Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}, " +
                        $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                    sw.Restart();
                }

                // Небольшая задержка для снижения нагрузки на CPU
                Thread.Sleep(1);
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    break;
                }
            }
        }

        /// <summary>
        /// Тест повышения поколения объектов (promotion).
        /// 
        /// Что происходит:
        /// 1. Создаем объекты и сохраняем их в List (они остаются живыми)
        /// 2. Объекты переживают сборку Gen0 и повышаются в Gen1
        /// 3. После еще одной сборки повышаются в Gen2
        /// 4. Наблюдаем как объекты "поднимаются" между поколениями
        /// 
        /// Что смотреть:
        /// - Gen0 сборки происходят часто
        /// - Gen1 сборки происходят реже (когда Gen0 переполняется)
        /// - Gen2 сборки происходят еще реже
        /// - Память растет, так как объекты не удаляются
        /// </summary>
        public static void RunPromotionTest()
        {
            Console.WriteLine("=== Object Promotion Test ===");
            // Список будет держать ссылки на объекты, предотвращая их сборку
            var list = new List<byte[]>();

            while (true)
            {
                // Создаем объект и сохраняем ссылку на него
                var arr = new byte[1000];
                list.Add(arr);

                // Каждые 1000 объектов выводим статистику
                if (list.Count % 1000 == 0)
                {
                    // Объекты в list переживают сборки Gen0 и повышаются в Gen1, затем Gen2
                    Console.WriteLine($"Objects held: {list.Count}, " +
                                      $"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}, " +
                                      $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Симуляция утечки памяти.
        /// 
        /// Что происходит:
        /// 1. Создаем объекты и добавляем их в кэш (List)
        /// 2. Объекты никогда не удаляются из кэша
        /// 3. Память растет бесконечно, пока не исчерпается
        /// 4. Это типичный пример утечки памяти в реальных приложениях
        /// 
        /// Что смотреть:
        /// - Память постоянно растет (никогда не уменьшается)
        /// - Gen2 сборки становятся все чаще (GC пытается освободить память)
        /// - В итоге приложение исчерпает память и упадет
        /// 
        /// Реальные примеры:
        /// - Кэш без TTL (time-to-live)
        /// - Обработчики событий, которые не отписаны
        /// - Статические коллекции, которые растут бесконечно
        /// </summary>
        public static void RunMemoryLeakTest()
        {
            Console.WriteLine("=== Memory Leak Simulation ===");
            // Вечный кэш - объекты никогда не удаляются
            var cache = new List<object>();

            while (true)
            {
                // Добавляем объект в кэш
                cache.Add(new object());

                // Каждые 10000 объектов выводим статистику
                if (cache.Count % 10000 == 0)
                {
                    // Наблюдаем как память растет, а GC не может ничего сделать
                    Console.WriteLine($"Cache size: {cache.Count}, " +
                                      $"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}, " +
                                      $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                }
            }
        }
    }
}