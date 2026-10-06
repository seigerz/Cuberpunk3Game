namespace Cyberpunk3
{
    public static class GameBoardGeneratorTests
    {
        private const int SeedsToCheck = 500;

        /// <summary>
        /// Запустить все тесты.
        /// </summary>
        public static bool RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Тестирование генерации поля");
            Console.WriteLine("========================================\n");

            bool allPassed = true;
            allPassed &= TestWalls();
            allPassed &= TestBoundTouchedWalls();
            allPassed &= TestItems();
            allPassed &= TestConnectivity();
            allPassed &= TestWallRejection();
            allPassed &= TestReproducibility();           

            Console.WriteLine(allPassed ? "  ВСЕ ТЕСТЫ ГЕНЕРАЦИИ ПРОЙДЕНЫ\n" : "  ЕСТЬ ОШИБКИ В ТЕСТАХ ГЕНЕРАЦИИ\n");
            return allPassed;
        }

        /// <summary>
        /// Тест: 8 стен, 8 разных видов, по 5 сегментов, цепочка сплошная, ребра не повторяются.
        /// </summary>
        private static bool TestWalls()
        {
            Console.WriteLine($"Тест: Структура стен ({SeedsToCheck} полей)");
            bool passed = true;

            for (int seed = 0; seed < SeedsToCheck && passed; seed++)
            {
                var board = GameBoardGenerator.Generate(seed);

                if (board.Walls.Count != GameConstants.WallCount)
                {
                    Console.WriteLine($"  X seed {seed}: стен {board.Walls.Count}, ожидалось {GameConstants.WallCount}");
                    passed = false;
                    break;
                }

                var configs = new HashSet<int>();
                var allEdges = new HashSet<(int q, int r, int dir)>();

                foreach (var wall in board.Walls)
                {
                    configs.Add(wall.ConfigIndex);

                    if (wall.Segments.Length != GameConstants.WallLength)
                    {
                        Console.WriteLine($"  X seed {seed}: в стене {wall.Segments.Length} сегментов");
                        passed = false;
                    }

                    for (int i = 0; i < wall.Segments.Length; i++)
                    {
                        var seg = wall.Segments[i];
                        var f = seg.FirstHex;
                        var s = seg.SecondHex;

                        if (!HexGrid.IsInBounds(f.q, f.r) || !HexGrid.IsInBounds(s.q, s.r))
                        {
                            Console.WriteLine($"  X seed {seed}: сегмент вне поля {seg}");
                            passed = false;
                        }
                        if (!allEdges.Add(seg.EdgeKey))
                        {
                            Console.WriteLine($"  X seed {seed}: ребро занято дважды {seg}");
                            passed = false;
                        }
                        if (i > 0 && !AreContinuous(wall.Segments[i - 1], seg))
                        {
                            Console.WriteLine($"  X seed {seed}: разрыв в стене между {wall.Segments[i - 1]} и {seg}");
                            passed = false;
                        }
                    }
                }

                if (configs.Count != Wall.WallConfigurations.Length)
                {
                    Console.WriteLine($"  X seed {seed}: использовано {configs.Count} видов стен из {Wall.WallConfigurations.Length}");
                    passed = false;
                }
            }

            Console.WriteLine(passed ? "  OK\n" : "");
            return passed;
        }


        /// <summary>
        /// Тест: проверка касания стенами границ поля.
        /// </summary>
        private static bool TestBoundTouchedWalls()
        {
            Console.WriteLine($"Тест: Стены от края поля ({SeedsToCheck} полей, минимум {GameConstants.MinBorderWalls} на поле)");
            bool passed = true;

            for (int seed = 0; seed < SeedsToCheck && passed; seed++)
            {
                var board = GameBoardGenerator.Generate(seed);

                int touching = 0;
                foreach (var wall in board.Walls)
                    if (TouchesBorder(wall)) touching++;

                if (touching < GameConstants.MinBorderWalls)
                {
                    Console.WriteLine($"  X seed {seed}: стен у края {touching}, ожидалось не меньше {GameConstants.MinBorderWalls}");
                    passed = false;
                }
            }

            Console.WriteLine(passed ? "  OK\n" : "");
            return passed;
        }

        /// <summary>
        /// Стена касается края, если хотя бы одна вершина её сегмента лежит на границе поля,
        /// то есть один из двух общих соседей сегмента находится за полем.
        /// Реализация нарочно независима от GameBoard.WallTouchesBorder.
        /// </summary>
        private static bool TouchesBorder(Wall wall)
        {
            foreach (var seg in wall.Segments)
            {
                var f = seg.FirstHex;
                var s = seg.SecondHex;
                var common = HexGrid.GetCommonNeighbors(f.q, f.r, s.q, s.r);

                if (!HexGrid.IsInBounds(common.first.q, common.first.r) ||
                    !HexGrid.IsInBounds(common.second.q, common.second.r))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Два ребра идут подряд, если у них общий гекс, а два других гекса соседние (ребра сходятся в вершине).
        /// </summary>
        private static bool AreContinuous(WallSegment x, WallSegment y)
        {
            var hx = new[] { x.FirstHex, x.SecondHex };
            var hy = new[] { y.FirstHex, y.SecondHex };

            foreach (var p in hx)
            {
                foreach (var s in hy)
                {
                    if (p != s) continue;
                    var otherX = hx[0] == p ? hx[1] : hx[0];
                    var otherY = hy[0] == s ? hy[1] : hy[0];
                    return HexGrid.Distance(otherX.q, otherX.r, otherY.q, otherY.r) == 1;
                }
            }
            return false;
        }

        /// <summary>
        /// Тест: 12 предметов (10 + 2), не соприкасаются, не на стартовых гексах.
        /// </summary>
        private static bool TestItems()
        {
            Console.WriteLine($"Тест: Предметы ({SeedsToCheck} полей)");
            bool passed = true;

            for (int seed = 0; seed < SeedsToCheck && passed; seed++)
            {
                var board = GameBoardGenerator.Generate(seed);
                var items = board.Items;

                int ammo = 0, mines = 0;
                foreach (var item in items)
                {
                    if (item.Type == ItemType.Ammo) ammo++; else mines++;
                }

                if (items.Count != GameConstants.TotalItems ||
                    ammo != GameConstants.AmmoPackCount || mines != GameConstants.MineCount)
                {
                    Console.WriteLine($"  X seed {seed}: предметов {items.Count} (боекомплектов {ammo}, мин {mines})");
                    passed = false;
                    break;
                }

                var starts = new HashSet<(int q, int r)>(GameBoard.GetCyborgStartPositions());
                starts.UnionWith(GameBoard.GetGoblinStartPositions());

                for (int i = 0; i < items.Count; i++)
                {
                    if (starts.Contains((items[i].Q, items[i].R)))
                    {
                        Console.WriteLine($"  X seed {seed}: предмет на стартовом гексе {items[i]}");
                        passed = false;
                    }

                    for (int j = i + 1; j < items.Count; j++)
                    {
                        int dist = HexGrid.Distance(items[i].Q, items[i].R, items[j].Q, items[j].R);
                        if (dist <= GameConstants.MinItemDistance)
                        {
                            Console.WriteLine($"  X seed {seed}: предметы слишком близко ({dist}): {items[i]} и {items[j]}");
                            passed = false;
                        }
                    }
                }
            }

            Console.WriteLine(passed ? "  OK\n" : "");
            return passed;
        }

        /// <summary>
        /// Тест (главный): из любого гекса можно дойти до любого.
        /// Обход делаем из КАЖДОГО гекса, а не из одного: проверка не опирается на IsFieldConnected.
        /// </summary>
        private static bool TestConnectivity()
        {
            Console.WriteLine($"Тест: Связность поля ({SeedsToCheck} полей, обход из каждого гекса)");
            bool passed = true;

            var hexes = HexGrid.GetAllHexes();

            for (int seed = 0; seed < SeedsToCheck && passed; seed++)
            {
                var board = GameBoardGenerator.Generate(seed);

                foreach (var hex in hexes)
                {
                    int reachable = board.CountReachableHexes(hex.q, hex.r);
                    if (reachable != GameConstants.TotalHexes)
                    {
                        Console.WriteLine($"  X seed {seed}: из {HexGrid.HexToString(hex.q, hex.r)} достижимо {reachable} из {GameConstants.TotalHexes}");
                        passed = false;
                        break;
                    }
                }

                if (passed && !board.IsFieldConnected())
                {
                    Console.WriteLine($"  X seed {seed}: IsFieldConnected() вернул false, хотя обход успешен");
                    passed = false;
                }
            }

            Console.WriteLine(passed ? "  OK\n" : "");
            return passed;
        }

        /// <summary>
        /// Тест: проверка самого проверяющего. Стена, замыкающая гекс, должна отклоняться,
        /// стена с одним проходом, наоборот, приниматься.
        /// </summary>
        private static bool TestWallRejection()
        {
            Console.WriteLine("Тест: Стена, изолирующая гекс, отклоняется");
            bool passed = true;

            // Гекс offset(2,0) на верхнем краю: 5 соседей внутри поля. Стена из 5 рёбер
            // (NE, SE, S, SW, NW) полностью запирает его.
            var pocket = HexGrid.OffsetToAxial(2, 0);
            var pocketWall = new Wall(0, EdgesAround(pocket));

            var empty = new List<Wall>();
            if (GameBoardGenerator.CanPlaceWall(empty, pocketWall))
            {
                Console.WriteLine("  X Стена, запирающая гекс, принята");
                passed = false;
            }

            // Прямая проверка связности: поле с такой стеной должно развалиться
            var isolating = new GameBoard(new List<Wall> { pocketWall }, new List<Item>());
            if (isolating.IsFieldConnected())
            {
                Console.WriteLine("  X Контроль: стена вокруг (2,0) не изолирует гекс, тест бесполезен");
                passed = false;
            }

            // Те же 5 рёбер вокруг гекса в центре: открыт проход на север, поле остаётся связным
            var center = HexGrid.OffsetToAxial(4, 4);
            var centerWall = new Wall(0, EdgesAround(center));

            if (!GameBoardGenerator.CanPlaceWall(empty, centerWall))
            {
                Console.WriteLine("  X Допустимая стена отклонена");
                passed = false;
            }

            var withCenter = new GameBoard(new List<Wall> { centerWall }, new List<Item>());
            if (!withCenter.IsFieldConnected())
            {
                Console.WriteLine("  X Поле с допустимой стеной оказалось несвязным");
                passed = false;
            }

            Console.WriteLine(passed ? "  OK\n" : "");
            return passed;
        }

        /// <summary>
        /// 5 ребер вокруг гекса: NE(1), SE(0), S(5), SW(4), NW(3). N (2) оставлен открытым.
        /// </summary>
        private static List<(int q, int r, int dir)> EdgesAround((int q, int r) hex)
        {
            var edges = new List<(int q, int r, int dir)>();
            foreach (int dir in new[] { 1, 0, 5, 4, 3 })
                edges.Add((hex.q, hex.r, dir));
            return edges;
        }

        /// <summary>
        /// Тест: один seed – одно и то же поле.
        /// </summary>
        private static bool TestReproducibility()
        {
            Console.WriteLine("Тест: Воспроизводимость по seed");

            var b1 = GameBoardGenerator.Generate(12345);
            var b2 = GameBoardGenerator.Generate(12345);

            bool passed = b1.Walls.Count == b2.Walls.Count && b1.Items.Count == b2.Items.Count;

            for (int i = 0; passed && i < b1.Walls.Count; i++)
                passed = b1.Walls[i].WallToString() == b2.Walls[i].WallToString();
            for (int i = 0; passed && i < b1.Items.Count; i++)
                passed = b1.Items[i].ItemToString() == b2.Items[i].ItemToString();

            Console.WriteLine(passed ? "  OK\n" : "  X Поля с одним seed различаются\n");
            return passed;
        }
    }
}
