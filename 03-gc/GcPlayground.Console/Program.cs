using GcPlayground.ConsoleApp.Scenarios;
using GcPlayground.ConsoleApp.Monitoring;

/// <summary>
/// GC Playground - интерактивное приложение для изучения поведения сборщика мусора .NET.
/// 
/// Это приложение предоставляет 12 различных сценариев для демонстрации:
/// - Поколений объектов (Gen0, Gen1, Gen2)
/// - Large Object Heap (LOH) и фрагментации
/// - Влияния GC на задержку (latency)
/// - Card Table механизма оптимизации
/// - Pinning объектов в памяти
/// 
/// Как использовать:
/// 1. Запустите приложение (dotnet run)
/// 2. Выберите сценарий из меню (1-12)
/// 3. Нажмите любую клавишу для начала теста
/// 4. Наблюдайте за метриками GC в реальном времени
/// 5. Откройте другой терминал и запустите: dotnet-counters monitor --process-id <PID> System.Runtime
/// 
/// Рекомендуемый порядок изучения:
/// 1. Начните с Block 1 (Generations) - основные концепции
/// 2. Перейдите к Block 2 (LOH) - большие объекты
/// 3. Изучите Block 3 (Latency) - влияние на производительность
/// 4. Исследуйте Block 4 (Card Table) - оптимизация
/// 5. Завершите Block 5 (Pinning) - продвинутые техники
/// </summary>
class Program
{
    static void Main()
    {
        // Основной цикл приложения - показываем меню, пока пользователь не выберет выход
        while (true)
        {
            // Очищаем экран перед выводом меню
            Console.Clear();
            
            // Выводим красивый заголовок
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║       GC Playground - Main Menu        ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();
            
            // Block 1: Поколения объектов и время жизни
            Console.WriteLine("Block 1: Generations and Object Lifetime");
            Console.WriteLine("  1. Gen0 Allocations Test");
            Console.WriteLine("  2. Object Promotion Test");
            Console.WriteLine("  3. Memory Leak Simulation");
            Console.WriteLine();
            
            // Block 2: Large Object Heap и фрагментация
            Console.WriteLine("Block 2: LOH and Fragmentation");
            Console.WriteLine("  4. LOH Allocations Test");
            Console.WriteLine("  5. LOH Fragmentation Test");
            Console.WriteLine("  6. LOH Compaction Test");
            Console.WriteLine();
            
            // Block 3: Паузы и влияние на latency
            Console.WriteLine("Block 3: Pauses and Stop-the-world");
            Console.WriteLine("  7. Latency Test");
            Console.WriteLine("  8. Server vs Workstation GC Test");
            Console.WriteLine();
            
            // Block 4: Card Table механизм
            Console.WriteLine("Block 4: Card Table");
            Console.WriteLine("  9. CardTable Simple Test");
            Console.WriteLine("  10. CardTable Stress Test");
            Console.WriteLine();
            
            // Block 5: Pinning объектов
            Console.WriteLine("Block 5: Pinning");
            Console.WriteLine("  11. Fixed Pinning Test");
            Console.WriteLine("  12. GCHandle Pinning Test");
            Console.WriteLine();
            
            // Опция выхода
            Console.WriteLine("  0. Exit");
            Console.WriteLine();
            Console.Write("Select option: ");

            // Читаем выбор пользователя
            var choice = Console.ReadLine();

            try
            {
                // Обрабатываем выбор пользователя
                switch (choice)
                {
                    // Block 1: Generations
                    case "1":
                        GenerationsScenario.RunGen0Test();
                        break;
                    case "2":
                        GenerationsScenario.RunPromotionTest();
                        break;
                    case "3":
                        GenerationsScenario.RunMemoryLeakTest();
                        break;
                    
                    // Block 2: LOH
                    case "4":
                        LohScenario.RunLohAllocationTest();
                        break;
                    case "5":
                        LohScenario.RunLohFragmentationTest();
                        break;
                    case "6":
                        LohScenario.RunLohCompactionTest();
                        break;
                    
                    // Block 3: Latency
                    case "7":
                        LatencyScenario.RunLatencyTest();
                        break;
                    case "8":
                        LatencyScenario.RunServerVsWorkstationTest();
                        break;
                    
                    // Block 4: Card Table
                    case "9":
                        CardTableScenario.RunCardTableSimpleTest();
                        break;
                    case "10":
                        CardTableScenario.RunCardTableStressTest();
                        break;
                    
                    // Block 5: Pinning
                    case "11":
                        PinningScenario.RunFixedPinningTest();
                        break;
                    case "12":
                        PinningScenario.RunGcHandlePinningTest();
                        break;
                    
                    // Выход из приложения
                    case "0":
                        return;
                    
                    // Неверный выбор
                    default:
                        Console.WriteLine("Invalid option. Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок при выполнении сценария
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}
