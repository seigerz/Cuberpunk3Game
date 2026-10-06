namespace Cyberpunk3
{
    /// <summary>
    /// Утилиты для работы с гексагональной сеткой.
    /// СИСТЕМА КООРДИНАТ:
    /// - Внутренняя логика: Axial (q, r)
    /// - Визуализация: Even-q Offset (col, row)
    /// - Ориентация: Flat-top (плоская сторона сверху/снизу) 
    /// </summary>
    public static class HexGrid
    {
        // ===== СТАТИЧЕСКИЕ ЧЛЕНЫ =====
        /// <summary>Векторы направлений для Flat-top в Axial координатах.</summary>
        private static readonly (int dq, int dr)[] DirectionVectors = new[]
        {
            ( 1,  0),   
            ( 1, -1),   
            ( 0, -1),   
            (-1,  0),   
            (-1,  1),   
            ( 0,  1)    
        };

        /// <summary>Названия направлений.</summary>
        private static readonly string[] DirectionNames =
        {
            "Юго-восток", 
            "Северо-восток", 
            "Север",
            "Северо-запад", 
            "Юго-запад", 
            "Юг"
        };


        // ===== МЕТОДЫ =====
        // Гексы
        /// <summary>
        /// Все гексы поля (в Axial), порядок детерминирован.
        /// </summary>
        public static List<(int q, int r)> GetAllHexes()
        {
            var hexes = new List<(int q, int r)>();
            for (int col = 0; col < GameConstants.BoardWidth; col++)
                for (int row = 0; row < GameConstants.BoardHeight; row++)
                    hexes.Add(OffsetToAxial(col, row));
            return hexes;
        }

        /// <summary>
        /// Два гекса, соседних сразу с обоими соседними гексами 1 и 2.
        /// Это третьи гексы двух вершин, на концах общего ребра.
        /// </summary>
        public static ((int q, int r) first, (int q, int r) second) GetCommonNeighbors(int q1, int r1, int q2, int r2)
        {
            if (Distance(q1, r1, q2, r2) != 1)
                throw new ArgumentException("Гексы должны быть соседними");

            int d = GetDirectionBetween(q1, r1, q2, r2);
            return (GetNeighbor(q1, r1, (d + 1) % 6), GetNeighbor(q1, r1, (d + 5) % 6));
        }

        /// <summary>
        /// Вывод списка гексов сходящихся в точке.
        /// </summary>
        public static string VertexKey((int q, int r)[] v)
        {
            return v[0].q + "," + v[0].r + "|" + v[1].q + "," + v[1].r + "|" + v[2].q + "," + v[2].r;
        }

        /// <summary>
        /// Получить координаты соседнего гекса в заданном направлении.
        /// </summary>
        public static (int q, int r) GetNeighbor(int q, int r, int direction)
        {
            var offset = GetDirectionVector(direction);
            return (q + offset.dq, r + offset.dr);
        }

        /// <summary>
        /// Получить координаты всех 6 соседних гексов.
        /// </summary>
        public static List<(int q, int r)> GetAllNeighbors(int q, int r)
        {
            var neighbors = new List<(int q, int r)>(6);

            for (int dir = 0; dir < DirectionVectors.Length; dir++)
            {
                neighbors.Add(GetNeighbor(q, r, dir));
            }

            return neighbors;
        }

        /// <summary>
        /// Получить все гексы в радиусе N от заданной точки.
        /// Использует алгоритм перебора ромба в Axial координатах.
        /// </summary>
        public static List<(int q, int r)> GetHexesInRadius(int centerQ, int centerR, int radius)
        {
            var hexes = new List<(int q, int r)>();

            // Перебираем все возможные смещения в пределах радиуса
            for (int dq = -radius; dq <= radius; dq++)
            {
                // Ограничения для dr, чтобы оставаться в пределах радиуса
                int r1 = Math.Max(-radius, -dq - radius);
                int r2 = Math.Min(radius, -dq + radius);

                for (int dr = r1; dr <= r2; dr++)
                {
                    int q = centerQ + dq;
                    int r = centerR + dr;

                    if (IsInBounds(q, r))
                    {
                        hexes.Add((q, r));
                    }
                }
            }

            return hexes;
        }


        // Ребра
        /// <summary>
        /// Каноническая запись ребра. Ребро (гекс, направление) и ребро
        /// (сосед, противоположное направление) – одно и то же ребро.
        /// Приводим к виду, где direction всегда 0..2.
        /// </summary>
        public static (int q, int r, int dir) NormalizeEdge(int q, int r, int dir)
        {
            if (dir < 3) return (q, r, dir);
            var n = GetNeighbor(q, r, dir);
            return (n.q, n.r, dir - 3);
        }


        // Вершины 
        /// <summary>
        /// Вершина = три взаимно соседних гекса. Сортируем, чтобы запись была однозначной.
        /// </summary>
        public static (int q, int r)[] MakeVertex((int q, int r) a, (int q, int r) b, (int q, int r) c)
        {
            var v = new[] { a, b, c };
            Array.Sort(v);
            return v;
        }

        /// <summary>
        /// Три вершины, соединённые с данным ребром сетки.
        /// </summary>
        public static List<(int q, int r)[]> GetNeighborVertices((int q, int r)[] v)
        {
            var result = new List<(int q, int r)[]>();
            for (int skip = 0; skip < 3; skip++)
            {
                // Ребро между двумя гексами вершины; третий гекс (skip) остаётся «за спиной»
                var x = v[(skip + 1) % 3];
                var y = v[(skip + 2) % 3];
                var z = v[skip];

                // Ребро x|y нужно, только если оно лежит на поле (хотя бы один гекс внутри)
                if (!HexGrid.IsInBounds(x.q, x.r) && !HexGrid.IsInBounds(y.q, y.r)) continue;

                var common = HexGrid.GetCommonNeighbors(x.q, x.r, y.q, y.r);
                var other = common.first == z ? common.second : common.first;
                result.Add(MakeVertex(x, y, other));
            }
            return result;
        }

        /// <summary>
        /// Преобразование Axial → even-q Offset координаты.
        /// Формула для Flat-top, even-q (нечётные столбцы выше).
        /// </summary>
        public static (int col, int row) AxialToOffset(int q, int r)
        {
            int col = q;
            int row = r + (q + (q & 1)) / 2;
            return (col, row);
        }

        /// <summary>
        /// Преобразование even-q Offset → Axial координаты.
        /// Формула для Flat-top, even-q (нечётные столбцы выше).
        /// </summary>
        public static (int q, int r) OffsetToAxial(int col, int row)
        {
            int q = col;
            int r = row - (col + (col & 1)) / 2;
            return (q, r);
        }

        /// <summary>
        /// Вычисление расстояния между двумя гексами через кубические координаты.
        /// Формула: (|dx| + |dy| + |dz|) / 2, где x+y+z=0
        /// </summary>
        public static int Distance(int q1, int r1, int q2, int r2)
        {
            // Преобразуем Axial в кубические
            int x1 = q1;
            int z1 = r1;
            int y1 = -x1 - z1;

            int x2 = q2;
            int z2 = r2;
            int y2 = -x2 - z2;

            // Манхэттенское расстояние в кубических координатах
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int dz = Math.Abs(z2 - z1);

            return (dx + dy + dz) / 2;
        }

        /// <summary>
        /// Дистанция выстрела: гекс стрелка считается за 1, сосед за 2 и тд. 
        /// Бросок d6 должен быть >= этой дистанции для попадания.
        /// Пример: враг через 2 пустых гекса → Distance=3 → ShotDistance=4 → нужно выбросить ≥4.
        /// </summary>
        public static int ShotDistance(int q1, int r1, int q2, int r2)
        {
            return Distance(q1, r1, q2, r2) + 1;
        }

        /// <summary>
        /// Получить вектор смещения для заданного направления.
        /// </summary>
        /// <param name="direction">Направление (0-5)</param>
        public static (int dq, int dr) GetDirectionVector(int direction)
        {
            if (direction < 0 || direction >= DirectionVectors.Length)
                throw new ArgumentException($"Направление должно быть от 0 до 5, получено: {direction}");

            return DirectionVectors[direction];
        }
  
        /// <summary>
        /// Получить все гексы вдоль направления до максимальной дальности.
        /// ВАЖНО: для стрельбы - снаряд летит строго по одному из 6 направлений!
        /// </summary>
        /// <param name="startQ">Начальная координата q</param>
        /// <param name="startR">Начальная координата r</param>
        /// <param name="direction">Направление (0-5)</param>
        /// <param name="maxRange">Максимальная дальность в гексах</param>
        /// <param name="includeStart">Включать ли стартовый гекс в результат</param>
        public static List<(int q, int r)> GetHexesAlongDirection(int startQ, int startR, int direction, int maxRange, bool includeStart = false)
        {
            var hexes = new List<(int q, int r)>();
            var vector = GetDirectionVector(direction);

            int startIndex = includeStart ? 0 : 1;

            for (int i = startIndex; i <= maxRange; i++)
            {
                int q = startQ + vector.dq * i;
                int r = startR + vector.dr * i;
                hexes.Add((q, r));
            }

            return hexes;
        }

        /// <summary>
        /// Определить направление от одного гекса к другому.
        /// Работает только если гексы лежат строго на одной из 6 линий направления.
        /// </summary>
        /// <returns>Направление (0-5) или -1 если гексы не на одной линии</returns>
        public static int GetDirectionBetween(int fromQ, int fromR, int toQ, int toR)
        {
            if (fromQ == toQ && fromR == toR)
                return -1; // Один и тот же гекс

            int dq = toQ - fromQ;
            int dr = toR - fromR;

            // Вычисляем НОД для нормализации вектора
            int gcd = GCD(Math.Abs(dq), Math.Abs(dr));
            if (gcd == 0) gcd = 1;

            int normDq = dq / gcd;
            int normDr = dr / gcd;

            // Ищем соответствующее направление
            for (int dir = 0; dir < DirectionVectors.Length; dir++)
            {
                var vec = DirectionVectors[dir];
                if (vec.dq == normDq && vec.dr == normDr)
                    return dir;
            }

            return -1; // Не на одной из 6 линий
        }

        /// <summary>
        /// Проверить, находятся ли два гекса на одной из 6 линий направления.
        /// Важно для стрельбы - можно стрелять только вдоль направлений!
        /// </summary>
        public static bool AreOnSameLine(int q1, int r1, int q2, int r2)
        {
            return GetDirectionBetween(q1, r1, q2, r2) != -1;
        }

        /// <summary>
        /// Наибольший общий делитель (НОД) для нормализации направления.
        /// </summary>
        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        /// <summary>
        /// Проверка, находятся ли Axial координаты в пределах игрового поля.
        /// Поле: 8 столбцов × 9 рядов в Offset координатах.
        /// </summary>
        public static bool IsInBounds(int q, int r)
        {
            var offset = AxialToOffset(q, r);
            return offset.col >= 0 && offset.col < GameConstants.BoardWidth &&
                   offset.row >= 0 && offset.row < GameConstants.BoardHeight;
        }

        /// <summary>
        /// Получить название направления для отладки.
        /// </summary>
        public static string GetDirectionName(int direction)
        {
            if (direction < 0 || direction >= DirectionNames.Length)
                return "Неизвестное направление";

            return DirectionNames[direction];
        }

        /// <summary>
        /// Вывести гекс в читаемом виде с двумя системами координат.
        /// </summary>
        public static string HexToString(int q, int r)
        {
            var offset = AxialToOffset(q, r);
            return $"Axial({q},{r}) = Offset(col={offset.col}, row={offset.row})";
        }
    }
}
