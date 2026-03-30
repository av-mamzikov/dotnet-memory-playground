using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace GcPlayground.ConsoleApp.Scenarios
{
    /// <summary>
    /// Демонстрирует работу Card Table в .NET GC.
    /// 
    /// Card Table - это механизм оптимизации для отслеживания ссылок Gen2→Gen0/Gen1.
    /// 
    /// Проблема:
    /// - При сборке Gen0 нужно найти все ссылки из Gen2 на объекты Gen0
    /// - Сканирование всей Gen2 очень дорого
    /// 
    /// Решение (Card Table):
    /// - Память делится на "карточки" (обычно 128 байт)
    /// - Когда Gen2 объект ссылается на Gen0, карточка помечается
    /// - При сборке Gen0 сканируются только помеченные карточки
    /// - Это значительно ускоряет сборку Gen0
    /// 
    /// Побочный эффект:
    /// - Частые обновления ссылок Gen2→Gen0 замедляют работу
    /// - Это может быть узким местом в приложениях с частыми обновлениями
    /// </summary>
    public class CardTableScenario
    {
        /// <summary>
        /// Простой класс-контейнер для демонстрации ссылок между поколениями.
        /// </summary>
        class Holder
        {
            public object? Ref;
        }

        /// <summary>
        /// Простой тест Card Table.
        /// 
        /// Что происходит:
        /// 1. Создаем один объект Holder и повышаем его в Gen2 (3 сборки)
        /// 2. Постоянно обновляем его ссылку на новые объекты Gen0
        /// 3. Каждое обновление требует отметить карточку в Card Table
        /// 4. Наблюдаем как часто происходят сборки
        /// 
        /// Что смотреть:
        /// - Gen0 сборки происходят часто (из-за постоянных выделений)
        /// - Время выполнения может быть нестабильным
        /// - Card Table обновления добавляют небольшой оверхед
        /// </summary>
        public static void RunCardTableSimpleTest()
        {
            Console.WriteLine("=== CardTable Simple Test ===");

            // Создаем объект Holder
            var holder = new Holder();

            // Повышаем его в Gen2 тремя сборками мусора
            // После этого holder будет в Gen2
            GC.Collect();
            GC.Collect();
            GC.Collect();

            Console.WriteLine($"Holder generation: {GC.GetGeneration(holder)}");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int iterations = 0;

            while (true)
            {
                // Обновляем ссылку Gen2 объекта на новый Gen0 объект
                // Это требует обновления Card Table
                holder.Ref = new object();
                iterations++;

                // Каждые 100000 итераций выводим статистику
                if (iterations % 100000 == 0)
                {
                    sw.Stop();
                    Console.WriteLine($"Iterations: {iterations}, Time: {sw.ElapsedMilliseconds}ms, " +
                        $"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");
                    sw.Restart();
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Стресс-тест Card Table.
        /// 
        /// Что происходит:
        /// 1. Создаем 1 миллион объектов Holder и повышаем их в Gen2
        /// 2. Постоянно обновляем ссылки всех объектов на новые Gen0 объекты
        /// 3. Каждое обновление требует отметить карточку
        /// 4. С миллионом объектов это становится узким местом
        /// 
        /// Что смотреть:
        /// - Время выполнения значительно возрастает
        /// - Card Table обновления становятся заметным оверхедом
        /// - Это демонстрирует реальную проблему в приложениях
        /// - Решение: избегать частых обновлений ссылок Gen2→Gen0
        /// </summary>
        public static void RunCardTableStressTest()
        {
            Console.WriteLine("=== CardTable Stress Test ===");
            var holders = new List<Holder>();

            // Создаем большое количество объектов Holder
            Console.WriteLine("Creating 1,000,000 holders...");
            for (int i = 0; i < 1_000_000; i++)
            {
                holders.Add(new Holder());
                if (i % 100000 == 0)
                    Console.WriteLine($"Created {i} holders");
            }

            // Повышаем все объекты в Gen2
            Console.WriteLine("Promoting to Gen2...");
            GC.Collect();
            GC.Collect();
            GC.Collect();

            Console.WriteLine("Starting reference updates...");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int iterations = 0;

            while (true)
            {
                // Обновляем ссылки всех 1 миллиона объектов
                // Каждое обновление требует отметить карточку в Card Table
                // С таким количеством объектов это становится дорогой операцией
                foreach (var h in holders)
                {
                    h.Ref = new object();
                }

                iterations++;
                sw.Stop();

                // Каждые 10 итераций выводим статистику
                if (iterations % 10 == 0)
                {
                    // Обратите внимание на время выполнения - оно значительно выше
                    // чем в простом тесте, несмотря на то же количество операций
                    Console.WriteLine($"Iteration {iterations}: {sw.ElapsedMilliseconds}ms, " +
                        $"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");
                }

                sw.Restart();
            }
        }
    }
}
