namespace Cyberpunk3
{
    /// <summary>
    /// Тесты для проверки корректности работы HexGrid.
    /// </summary>
    public static class HexGridTests
    {
        /// <summary>
        /// Запустить все тесты HexGrid.
        /// </summary>
        public static bool RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Тестирование HexGrid");
            Console.WriteLine("========================================\n");

            bool allPassed = true;

            allPassed &= TestCoordinateConversion();
            allPassed &= TestDistanceCalculation();
            allPassed &= TestOffsetNeighbors();
            allPassed &= TestDirections();
            allPassed &= TestAxialOffsetRoundTrip();
            allPassed &= TestLineDetection();
            allPassed &= TestBounds();

            Console.WriteLine("\n========================================");
            if (allPassed)
            {
                Console.WriteLine("  ✓ ВСЕ ТЕСТЫ ПРОЙДЕНЫ");
            }
            else
            {
                Console.WriteLine("  ✗ ЕСТЬ ОШИБКИ В ТЕСТАХ");
            }
            Console.WriteLine("========================================\n");

            return allPassed;
        }

        /// <summary>
        /// Тест: Преобразование координат Axial ↔ Offset.
        /// </summary>
        private static bool TestCoordinateConversion()
        {
            Console.WriteLine("Тест 1: Преобразование координат Offset <-> Axial");
            bool passed = true;

            // Значения посчитаны вручную: q = col, r = row - (col + (col & 1)) / 2
            var cases = new (int col, int row, int q, int r)[]
            {
                (0, 0, 0, 0),
                (1, 0, 1, -1),   // нечётный столбец: гекс приподнят
                (1, 1, 1, 0),
                (2, 0, 2, -1),
                (3, 0, 3, -2),
                (6, 4, 6, 1),
                (7, 8, 7, 4),
                (0, 8, 0, 8)
            };

            foreach (var c in cases)
            {
                var axial = HexGrid.OffsetToAxial(c.col, c.row);
                if (axial.q != c.q || axial.r != c.r)
                {
                    Console.WriteLine($"  X Offset({c.col},{c.row}): ожидалось Axial({c.q},{c.r}), получено ({axial.q},{axial.r})");
                    passed = false;
                }
            }

            // Круговой тест: для каждой клетки поля Offset -> Axial -> Offset
            for (int col = 0; col < GameConstants.BoardWidth; col++)
            {
                for (int row = 0; row < GameConstants.BoardHeight; row++)
                {
                    var axial = HexGrid.OffsetToAxial(col, row);
                    var back = HexGrid.AxialToOffset(axial.q, axial.r);
                    if (back.col != col || back.row != row)
                    {
                        Console.WriteLine($"  X Круговое преобразование ({col},{row}) -> ({back.col},{back.row})");
                        passed = false;
                    }
                }
            }

            Console.WriteLine(passed ? "  OK\n" : "");
            return passed;
        }

        /// <summary>
        /// Тест: Вычисление расстояний.
        /// </summary>
        private static bool TestDistanceCalculation()
        {
            Console.WriteLine("Тест 2: Вычисление расстояний");
            bool passed = true;

            var cases = new (int q1, int r1, int q2, int r2, int expected)[]
            {
                (0, 0, 0, 0, 0),
                (0, 0, 1, 0, 1),
                (0, 0, 3, 0, 3),
                (0, 0, 0, 3, 3),
                (0, 0, 2, 2, 4),
                (0, 0, 3, -3, 3),   // x=3, y=0, z=-3 -> (3+0+3)/2
                (2, 3, 5, 1, 3)     // dx=3, dy=1, dz=2 -> (3+1+2)/2
            };

            foreach (var c in cases)
            {
                int actual = HexGrid.Distance(c.q1, c.r1, c.q2, c.r2);
                if (actual != c.expected)
                {
                    Console.WriteLine($"  X ({c.q1},{c.r1})->({c.q2},{c.r2}): ожидалось {c.expected}, получено {actual}");
                    passed = false;
                }
            }

            // Расстояния по Offset (проверка на границах поля)
            var a = HexGrid.OffsetToAxial(0, 0);
            var b = HexGrid.OffsetToAxial(7, 8);
            int diagonal = HexGrid.Distance(a.q, a.r, b.q, b.r);   // 7 шагов по столбцам + 4 вниз
            if (diagonal != 11)
            {
                Console.WriteLine($"  X Offset(0,0)->(7,8): ожидалось 11, получено {diagonal}");
                passed = false;
            }

            Console.WriteLine(passed ? "  OK\n" : "");
            return passed;
        }

        /// <summary>
        /// Тест: соседи в Offset координатах. Проверяет, что именно нечётные столбцы приподняты.
        /// </summary>
        private static bool TestOffsetNeighbors()
        {
            Console.WriteLine("Тест: Соседи в Offset координатах (нечётные столбцы выше)");
            bool passed = true;

            passed &= CheckNeighbors(2, 4, new[] { (2, 3), (2, 5), (3, 4), (3, 5), (1, 4), (1, 5) });  // чётный
            passed &= CheckNeighbors(3, 4, new[] { (3, 3), (3, 5), (4, 3), (4, 4), (2, 3), (2, 4) });  // нечётный

            Console.WriteLine(passed ? "  OK\n" : "");
            return passed;
        }

        /// <summary>
        /// Проверка соседства гексов.
        /// </summary>
        private static bool CheckNeighbors(int col, int row, (int col, int row)[] expected)
        {
            var axial = HexGrid.OffsetToAxial(col, row);
            var actual = new HashSet<(int, int)>();
            foreach (var n in HexGrid.GetAllNeighbors(axial.q, axial.r))
            {
                var o = HexGrid.AxialToOffset(n.q, n.r);
                actual.Add((o.col, o.row));
            }

            var exp = new HashSet<(int, int)>();
            foreach (var e in expected) exp.Add((e.col, e.row));

            if (!actual.SetEquals(exp))
            {
                Console.WriteLine($"  X Соседи Offset({col},{row}) не совпали с ожидаемыми");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Тест: Направления и соседи.
        /// </summary>
        private static bool TestDirections()
        {
            Console.WriteLine("Тест 3: Направления и соседи");

            bool passed = true;

            // Проверка векторов направлений для Flat-top
            var expectedVectors = new (int dq, int dr)[]
            {
                ( 1,  0),   // 0: East
                ( 1, -1),   // 1: North-East
                ( 0, -1),   // 2: North-West
                (-1,  0),   // 3: West
                (-1,  1),   // 4: South-West
                ( 0,  1)    // 5: South-East
            };

            for (int dir = 0; dir < 6; dir++)
            {
                var actual = HexGrid.GetDirectionVector(dir);
                var expected = expectedVectors[dir];

                if (actual.dq != expected.dq || actual.dr != expected.dr)
                {
                    Console.WriteLine($"  ✗ Направление {dir}: ожидалось ({expected.dq},{expected.dr}), получено ({actual.dq},{actual.dr})");
                    passed = false;
                }
            }

            // Проверка соседей для центрального гекса (0, 0)
            var neighbors = HexGrid.GetAllNeighbors(0, 0);

            if (neighbors.Count != 6)
            {
                Console.WriteLine($"  ✗ Количество соседей: ожидалось 6, получено {neighbors.Count}");
                passed = false;
            }

            // Проверка, что все соседи на расстоянии 1
            foreach (var neighbor in neighbors)
            {
                int dist = HexGrid.Distance(0, 0, neighbor.q, neighbor.r);
                if (dist != 1)
                {
                    Console.WriteLine($"  ✗ Сосед ({neighbor.q},{neighbor.r}) на расстоянии {dist}, ожидалось 1");
                    passed = false;
                }
            }

            if (passed)
                Console.WriteLine("  ✓ Направления и соседи определяются корректно\n");
            else
                Console.WriteLine();

            return passed;
        }

        /// <summary>
        /// Тест: Определение линий стрельбы.
        /// </summary>
        private static bool TestLineDetection()
        {
            Console.WriteLine("Тест 4: Определение линий стрельбы");

            bool passed = true;

            // Тестовые случаи: (from, to, shouldBeOnLine, expectedDirection)
            var testCases = new[]
            {
                // На одной линии
                ((q1: 0, r1: 0), (q2: 3, r2: 0), true, 0),    // ЮВ
                ((q1: 0, r1: 0), (q2: 3, r2: -3), true, 1),   // СВ
                ((q1: 0, r1: 0), (q2: 0, r2: -3), true, 2),   // С
                ((q1: 3, r1: 0), (q2: 0, r2: 0), true, 3),    // СЗ
                ((q1: 3, r1: 3), (q2: 0, r2: 6), true, 4),    // ЮЗ
                ((q1: 0, r1: 0), (q2: 0, r2: 3), true, 5),    // Ю
                
                // Не на одной линии
                ((q1: 0, r1: 0), (q2: 2, r2: 1), false, -1),
                ((q1: 0, r1: 0), (q2: 1, r2: 2), false, -1)
            };

            foreach (var (from, to, shouldBeOnLine, expectedDir) in testCases)
            {
                bool onLine = HexGrid.AreOnSameLine(from.q1, from.r1, to.q2, to.r2);

                if (onLine != shouldBeOnLine)
                {
                    Console.WriteLine($"  ✗ ({from.q1},{from.r1}) → ({to.q2},{to.r2}): ожидалось onLine={shouldBeOnLine}, получено {onLine}");
                    passed = false;
                }

                if (shouldBeOnLine)
                {
                    int dir = HexGrid.GetDirectionBetween(from.q1, from.r1, to.q2, to.r2);
                    if (dir != expectedDir)
                    {
                        Console.WriteLine($"  ✗ ({from.q1},{from.r1}) → ({to.q2},{to.r2}): ожидалось направление {expectedDir}, получено {dir}");
                        passed = false;
                    }
                }
            }

            if (passed)
                Console.WriteLine("✓ Определение линий работает корректно\n");
            else
                Console.WriteLine();

            return passed;
        }

        /// <summary>
        /// Тест: Проверка границ поля.
        /// </summary>
        private static bool TestBounds()
        {
            Console.WriteLine("Тест 5: Проверка границ поля (8×9)");

            bool passed = true;

            // Проверяем углы и центр поля в Offset координатах
            var testCases = new[]
            {
                // В пределах поля
                ((col: 0, row: 0), true),
                ((col: 7, row: 8), true),
                ((col: 4, row: 4), true),
                ((col: 0, row: 8), true),
                ((col: 7, row: 0), true),
                
                // За пределами
                ((col: -1, row: 0), false),
                ((col: 0, row: -1), false),
                ((col: 8, row: 0), false),
                ((col: 0, row: 9), false),
                ((col: 10, row: 10), false)
            };

            foreach (var (offset, shouldBeInBounds) in testCases)
            {
                var axial = HexGrid.OffsetToAxial(offset.col, offset.row);
                bool inBounds = HexGrid.IsInBounds(axial.q, axial.r);

                if (inBounds != shouldBeInBounds)
                {
                    Console.WriteLine($"  ✗ Offset({offset.col},{offset.row}) = Axial({axial.q},{axial.r}): ожидалось inBounds={shouldBeInBounds}, получено {inBounds}");
                    passed = false;
                }
            }

            // Проверяем общее количество гексов
            int totalHexes = 0;
            for (int col = 0; col < GameConstants.BoardWidth; col++)
            {
                for (int row = 0; row < GameConstants.BoardHeight; row++)
                {
                    var axial = HexGrid.OffsetToAxial(col, row);
                    if (HexGrid.IsInBounds(axial.q, axial.r))
                    {
                        totalHexes++;
                    }
                }
            }

            if (totalHexes != GameConstants.TotalHexes)
            {
                Console.WriteLine($"  ✗ Общее количество гексов: ожидалось {GameConstants.TotalHexes}, получено {totalHexes}");
                passed = false;
            }

            if (passed)
                Console.WriteLine($"  ✓ Границы поля корректны, всего гексов: {totalHexes}\n");
            else
                Console.WriteLine();

            return passed;
        }


        private static bool TestAxialOffsetRoundTrip()
        {
            Console.WriteLine("Тест: Axial ↔ Offset round-trip для всех гексов");
            bool passed = true;

            for (int col = 0; col < GameConstants.BoardWidth; col++)
            {
                for (int row = 0; row < GameConstants.BoardHeight; row++)
                {
                    var axial = HexGrid.OffsetToAxial(col, row);
                    var back = HexGrid.AxialToOffset(axial.q, axial.r);
                    if (back.col != col || back.row != row)
                    {
                        Console.WriteLine($"  X offset({col},{row}) → axial({axial.q},{axial.r}) → offset({back.col},{back.row})");
                        passed = false;
                    }
                }
            }

            // Отдельно проверяем стартовые позиции
            foreach (var pos in GameBoard.GetCyborgStartPositions())
            {
                var off = HexGrid.AxialToOffset(pos.q, pos.r);
                if (off.row != GameConstants.BoardHeight - 1)
                {
                    Console.WriteLine($"  X Киборг axial({pos.q},{pos.r}) → offset({off.col},{off.row}), ожидался ряд {GameConstants.BoardHeight - 1}");
                    passed = false;
                }
            }

            foreach (var pos in GameBoard.GetGoblinStartPositions())
            {
                var off = HexGrid.AxialToOffset(pos.q, pos.r);
                if (off.row != 0)
                {
                    Console.WriteLine($"  X Гоблин axial({pos.q},{pos.r}) → offset({off.col},{off.row}), ожидался ряд 0");
                    passed = false;
                }
            }

            Console.WriteLine(passed ? "  OK\n" : "");
            return passed;
        }


        /// <summary>
        /// Вывести визуальную демонстрацию направлений.
        /// </summary>
        public static void PrintDirectionsDemo()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Демонстрация направлений (Flat-top)");
            Console.WriteLine("========================================\n");

            Console.WriteLine("Центральный гекс: Axial(0, 0)\n");

            for (int dir = 0; dir < 6; dir++)
            {
                var vec = HexGrid.GetDirectionVector(dir);
                var neighbor = HexGrid.GetNeighbor(0, 0, dir);
                var offset = HexGrid.AxialToOffset(neighbor.q, neighbor.r);

                Console.WriteLine($"{dir}: {HexGrid.GetDirectionName(dir),-15} " +
                                $"вектор ({vec.dq:+0;-#},{vec.dr:+0;-#})  " +
                                $"→ Axial({neighbor.q:+0;-#},{neighbor.r:+0;-#})  " +
                                $"Offset(col={offset.col}, row={offset.row})");
            }

            Console.WriteLine("\n========================================\n");
        }
    }
}
