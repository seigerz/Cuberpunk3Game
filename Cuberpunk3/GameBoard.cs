namespace Cyberpunk3
{
    /// <summary>
    /// Игровое поле - содержит гексы, стены, предметы.
    /// </summary>
    public class GameBoard
    {
        // ===== СТАТИЧЕСКИЕ ЧЛЕНЫ =====
        // Стартовые гексы на верхнем и нижнем краях через 1 гекс.
        /// <summary>
        /// Киборги: нижний ряд, чётные столбцы (визуально нижний край).
        /// </summary>
        public static List<(int q, int r)> GetCyborgStartPositions()
        {
            var list = new List<(int q, int r)>();
            for (int col = 0; col < GameConstants.BoardWidth; col += 2)
                list.Add(HexGrid.OffsetToAxial(col, GameConstants.BoardHeight - 1));
            return list;
        }

        /// <summary>
        /// Гоблины: верхний ряд, нечётные столбцы (они приподняты, это визуально верхний край).
        /// </summary>
        public static List<(int q, int r)> GetGoblinStartPositions()
        {
            var list = new List<(int q, int r)>();
            for (int col = 1; col < GameConstants.BoardWidth; col += 2)
                list.Add(HexGrid.OffsetToAxial(col, 0));
            return list;
        }

        // ===== ПОЛЯ =====
        /// <summary>Списки стен и итемов.</summary>
        private readonly List<Wall> walls;
        private readonly List<Item> items;

        /// <summary>Ребро (каноническая запись) -> сегмент стены на нём.</summary>
        private readonly Dictionary<(int q, int r, int dir), WallSegment> segmentsByEdge =
            new Dictionary<(int q, int r, int dir), WallSegment>();


        // ===== СВОЙСТВА =====
        /// <summary>Размер поля собирается из констант.</summary>
        public int Width { get { return GameConstants.BoardWidth; } }
        public int Height { get { return GameConstants.BoardHeight; } }
        public List<Wall> Walls { get { return new List<Wall>(walls); } }
        public List<Item> Items { get { return new List<Item>(items); } }


        // ===== КОНСТРУКТОРЫ =====
        /// <summary>
        /// Пустое поле (для тестов и временных проверок).
        /// </summary>
        public GameBoard() : this(new List<Wall>(), new List<Item>()) { }

        /// <summary>
        /// Поле с готовой расстановкой.
        /// </summary>
        public GameBoard(IList<Wall> walls, IList<Item> items)
        {
            this.walls = new List<Wall>(walls);
            this.items = new List<Item>(items);

            foreach (var wall in this.walls)
            {
                foreach (var seg in wall.Segments)
                {
                    var f = seg.FirstHex;
                    var s = seg.SecondHex;
                    if (!HexGrid.IsInBounds(f.q, f.r) || !HexGrid.IsInBounds(s.q, s.r))
                        throw new ArgumentException($"Сегмент вне поля: {seg}");
                    if (segmentsByEdge.ContainsKey(seg.EdgeKey))
                        throw new ArgumentException($"Ребро занято дважды: {seg}");
                    segmentsByEdge[seg.EdgeKey] = seg;
                }
            }
        }


        // ===== МЕТОДЫ =====
        // Стены 
        /// <summary>
        /// Сегмент стены на ребре между двумя соседними гексами (или null).
        /// </summary>
        public WallSegment? GetWallBetween(int q1, int r1, int q2, int r2)
        {
            if (HexGrid.Distance(q1, r1, q2, r2) != 1) return null;
            int dir = HexGrid.GetDirectionBetween(q1, r1, q2, r2);
            if (dir < 0) return null;
            WallSegment? segment;
            segmentsByEdge.TryGetValue(HexGrid.NormalizeEdge(q1, r1, dir), out segment);
            return segment;
        }

        /// <summary>
        /// Любая стена непреодолима для движения.
        /// </summary>
        public bool IsPassageBlocked(int q1, int r1, int q2, int r2)
        {
            return GetWallBetween(q1, r1, q2, r2) != null;
        }

        /// <summary>
        /// Гексы, в которые можно попасть из (q, r) по направлению dir:
        /// не дальше WeaponRange (по ShotDistance) и до первой стены, закрывающей выстрел.
        /// Гекс за такой стеной в набор не входит.
        /// </summary>
        public List<(int q, int r, int modifier)> GetLineOfFire(int q, int r, int dir)
        {
            var result = new List<(int q, int r, int modifier)>();
            var cur = (q: q, r: r);
            int accumulatedModifier = 0;

            for (int step = 0; step < GameConstants.WeaponRange - 1; step++)
            {
                var next = HexGrid.GetNeighbor(cur.q, cur.r, dir);
                if (!HexGrid.IsInBounds(next.q, next.r)) break;

                var wall = GetWallBetween(cur.q, cur.r, next.q, next.r);
                if (wall != null)
                {
                    if (wall.BlocksShot()) break;
                    accumulatedModifier += wall.GetDamageModifier();
                }

                result.Add((next.q, next.r, accumulatedModifier));
                cur = next;
            }
            return result;
        }

        /// <summary>
        /// Гекс падения ракеты. Бросок roll задаёт дальность (2 = соседний гекс).
        /// Высокая стена останавливает ракету, и она падает на гекс перед стеной.
        /// Если ракета вылетела бы за поле, падает на последний гекс внутри поля.
        /// </summary>
        public (int q, int r) GetRocketLanding(int q, int r, int dir, int roll)
        {
            var cur = (q: q, r: r);
            for (int step = 0; step < roll - 1; step++)
            {
                var next = HexGrid.GetNeighbor(cur.q, cur.r, dir);
                if (!HexGrid.IsInBounds(next.q, next.r)) break;

                var wall = GetWallBetween(cur.q, cur.r, next.q, next.r);
                if (wall != null && wall.BlocksRocketFlight()) break;

                cur = next;
            }
            return cur;
        }

        /// <summary>
        /// Ранения от взрыва ракеты. Эпицентр: 2 ранения, modifier=0.
        /// Соседи: 1 ранение. Средняя и высокая стена полностью защищает.
        /// Низкая стена: modifier=-1 (вычитается из броска ранения, а не из числа ран).
        /// </summary>
        public List<(int q, int r, int wounds, int modifier)> GetExplosion(int q, int r)
        {
            var result = new List<(int q, int r, int wounds, int modifier)>();
            result.Add((q, r, 2, 0));

            foreach (var n in HexGrid.GetAllNeighbors(q, r))
            {
                if (!HexGrid.IsInBounds(n.q, n.r)) continue;

                int modifier = 0;
                var wall = GetWallBetween(q, r, n.q, n.r);
                if (wall != null)
                {
                    if (wall.BlocksExplosion()) continue;
                    modifier = wall.GetDamageModifier();
                }
                result.Add((n.q, n.r, 1, modifier));
            }
            return result;
        }


        // Связность
        /// <summary>
        /// Сколько гексов достижимо из заданного (поиск в ширину с учётом стен).
        /// </summary>
        public int CountReachableHexes(int startQ, int startR)
        {
            if (!HexGrid.IsInBounds(startQ, startR)) return 0;

            var visited = new HashSet<(int q, int r)> { (startQ, startR) };
            var queue = new Queue<(int q, int r)>();
            queue.Enqueue((startQ, startR));

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var n in HexGrid.GetAllNeighbors(cur.q, cur.r))
                {
                    if (!HexGrid.IsInBounds(n.q, n.r)) continue;
                    if (visited.Contains(n)) continue;
                    if (IsPassageBlocked(cur.q, cur.r, n.q, n.r)) continue;
                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }
            return visited.Count;
        }

        /// <summary>
        /// Из любого гекса можно дойти до любого (граф неориентированный,
        /// поэтому достаточно обойти поле из одной точки).
        /// </summary>
        public bool IsFieldConnected()
        {
            var start = HexGrid.OffsetToAxial(0, 0);
            return CountReachableHexes(start.q, start.r) == GameConstants.TotalHexes;
        }


        // Предметы 
        /// <summary>
        /// Получение итема из текущей координаты
        /// </summary>
        public Item? GetItemAt(int q, int r)
        {
            foreach (var item in items)
                if (item.Q == q && item.R == r && !item.IsPickedUp) return item;
            return null;
        }

        // ===== Вывод =====
        public void PrintBoardInfo()
        {
            Console.WriteLine("===== Информация о поле =====");
            Console.WriteLine($"Размер: {Width} x {Height} ({GameConstants.TotalHexes} гексов)");

            int activeItems = 0;
            foreach (var item in items)
                if (!item.IsPickedUp) activeItems++;

            Console.WriteLine($"Стен: {walls.Count}, предметов: {activeItems}");
            foreach (var wall in walls) Console.WriteLine("  " + wall);
            Console.WriteLine($"Связность: {(IsFieldConnected() ? "ОК" : "ОШИБКА")}");
            Console.WriteLine("============================");
        }
    }
}