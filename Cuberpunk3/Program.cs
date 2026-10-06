using Cyberpunk3;
using System.Text;


Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("========================================");
Console.WriteLine("  Киберпанк-3: Засада на UNL-2");
Console.WriteLine("  Flat-top, Even-q Offset");
Console.WriteLine("========================================\n");

// ===== Тесты =====
bool testsPass = HexGridTests.RunAllTests();
if (!testsPass) { Console.WriteLine("ВНИМАНИЕ: Тесты HexGrid не пройдены!\n"); Console.ReadKey(); return; }

testsPass = GameBoardGeneratorTests.RunAllTests();
if (!testsPass) { Console.WriteLine("ВНИМАНИЕ: Тесты генерации не пройдены!\n"); Console.ReadKey(); return; }

// ===== Генерация поля =====
var board = GameBoardGenerator.Generate(42);
board.PrintBoardInfo();

var viz = new HexGridViz();

// Проверка генерации: Ia / Im
viz.ShowItemTypes = true;
viz.LoadBoard(board);
viz.Render();
viz.ShowItemTypes = false;

// ===== Выбор стартовых позиций =====
var cyborgStarts = GameBoard.GetCyborgStartPositions();
var chosen = ChooseCyborgPositions(cyborgStarts, GameConstants.MaxCyborgs);
var gs = GameBoard.GetGoblinStartPositions();

Console.WriteLine($"\nМожно создать киборга: {Cyborg.CanCreateCyborg()}");
Console.WriteLine($"Можно создать гоблина: {Goblin.CanCreateGoblin()}\n");

// ===== Создание юнитов =====
var cyborg1 = new Cyborg(chosen[0].q, chosen[0].r, Direction.North);
var cyborg2 = new Cyborg(chosen[1].q, chosen[1].r, Direction.North);
var goblin1 = new Goblin(gs[0].q, gs[0].r);
var goblin2 = new Goblin(gs[1].q, gs[1].r);
var goblin3 = new Goblin(gs[2].q, gs[2].r);
var goblin4 = new Goblin(gs[3].q, gs[3].r);

var cyborgs = new[] { cyborg1, cyborg2 };
var goblins = new[] { goblin1, goblin2, goblin3, goblin4 };

Console.WriteLine($"\nВсего киборгов: {Cyborg.GetTotalCyborgs()}, гоблинов: {Goblin.GetTotalGoblins()}");

// ===== Перерисовка =====
void Draw(string title)
{
    Console.WriteLine($"\n--- {title} ---");
    viz.ClearUnits();
    viz.LoadBoard(board);
    foreach (var c in cyborgs) if (c.IsAlive) viz.SetCyborg(c.Q, c.R, c.ID, c.Facing);
    foreach (var g in goblins) if (g.IsAlive) viz.SetGoblin(g.Q, g.R, g.ID);
    viz.Render();
}

Draw("Начальная расстановка");

// ===== Демонстрация боя =====
Console.WriteLine("\n=== Начало боя ===");

Console.WriteLine("\n--- Ход киборгов ---");
cyborg1.PrintStatus();
cyborg1.MoveForward(board);
cyborg1.MoveForward(board);

// Пулемёт по линии взгляда
var targets = cyborg1.GetShootTargets(board);
if (targets.Count > 0)
    cyborg1.Shoot(board, targets[0].q, targets[0].r);
else
    Console.WriteLine("Нет целей по линии взгляда");

cyborg1.MoveBackward(board);
cyborg1.Turn(1);

// Ракета
var explosion = cyborg1.ShootRocket(board);
foreach (var hit in explosion)
    Console.WriteLine($"  Взрыв: {HexGrid.HexToString(hit.q, hit.r)}, ран: {hit.wounds}, поправка: {hit.modifier}");

cyborg1.PrintStatus();
Draw("После хода киборгов");

Console.WriteLine("\n--- Ход гоблинов ---");
goblin1.PrintStatus();
var step = HexGrid.GetNeighbor(goblin1.Q, goblin1.R, (int)Direction.South);
goblin1.Move(board, step.q, step.r);

var gTargets = goblin1.GetShootTargets(board);
if (gTargets.Count > 0)
    goblin1.Shoot(board, gTargets[0].q, gTargets[0].r);
else
    Console.WriteLine("Нет целей для гоблина");

goblin1.PrintStatus();
Draw("После хода гоблинов");

// ===== PathFinder =====
Console.WriteLine("\n=== PathFinder ===");
Console.WriteLine($"Путь от C1 до G1: {PathFinder.GetPathLength(board, cyborg1.Q, cyborg1.R, goblin1.Q, goblin1.R)} шагов");
Console.WriteLine($"Достижимо: {PathFinder.IsReachable(board, cyborg1.Q, cyborg1.R, goblin1.Q, goblin1.R)}");

var gReachable = PathFinder.GetReachableHexes(board, goblin1.Q, goblin1.R, GameConstants.GoblinActionPoints);
Console.WriteLine($"Гексов доступно гоблину: {gReachable.Count}");

var cReachable = PathFinder.GetCyborgReachableHexes(board, cyborg1.Q, cyborg1.R, (int)cyborg1.Facing, GameConstants.CyborgActionPoints);
Console.WriteLine($"Гексов доступно киборгу: {cReachable.Count}");

Console.WriteLine($"Киборги рядом: {PathFinder.AreNeighbors(cyborg1.Q, cyborg1.R, cyborg2.Q, cyborg2.R)}");

// ===== Константы =====
Console.WriteLine("\n=== Константы ===");
Console.WriteLine($"Размер поля: {GameConstants.BoardWidth}x{GameConstants.BoardHeight}");
Console.WriteLine($"Всего гексов: {GameConstants.TotalHexes}");
Console.WriteLine($"Дальность: {GameConstants.WeaponRange}");

Console.WriteLine("\n========================================");
Console.WriteLine("  Демонстрация завершена");
Console.WriteLine("========================================");
Console.ReadKey();
        

static List<(int q, int r)> ChooseCyborgPositions(List<(int q, int r)> available, int count)
{
    var free = new List<(int q, int r)>(available);
    var result = new List<(int q, int r)>();

    while (result.Count < count)
    {
        Console.Write($"Выберите столбец для киборга {result.Count + 1} (свободно:");
        foreach (var p in free) Console.Write(" " + HexGrid.AxialToOffset(p.q, p.r).col);
        Console.Write("): ");

        string? line = Console.ReadLine();
        if (line == null) { result.Add(free[0]); free.RemoveAt(0); continue; }

        int col;
        if (!int.TryParse(line.Trim(), out col)) { Console.WriteLine("  Введите число."); continue; }

        int index = free.FindIndex(p => HexGrid.AxialToOffset(p.q, p.r).col == col);
        if (index < 0) { Console.WriteLine("  Столбец недоступен."); continue; }

        result.Add(free[index]);
        free.RemoveAt(index);
    }
    return result;
}