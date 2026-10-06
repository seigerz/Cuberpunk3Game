namespace Cyberpunk3
{
    /// <summary>
    /// Генератор игрового поля - расставляет стены и предметы по правилам.
    /// </summary>
    public static class GameBoardGenerator
    {
        // ===== ПОЛЯ =====
        /// <summary>Сгенерировать полное игровое поле.</summary>
        private const int MaxBoardAttempts = 100;
        private const int MaxWallAttempts = 1000;


        // ===== МЕТОДЫ =====
        /// <summary>
        /// Сгенерировать поле. Один seed – одно и то же поле.
        /// </summary>
        public static GameBoard Generate(int seed)
        {
            var rnd = new Random(seed);

            for (int attempt = 0; attempt < MaxBoardAttempts; attempt++)
            {
                var walls = TryPlaceWalls(rnd);
                if (walls == null) continue;
                var items = TryPlaceItems(rnd);
                if (items == null) continue;
                return new GameBoard(walls, items);
            }
            throw new InvalidOperationException("Не удалось сгенерировать поле");
        }

        // ===== Стены =====
        /// <summary>
        /// Попытка расстановки стен.
        /// </summary>
        private static List<Wall>? TryPlaceWalls(Random rnd)
        {
            int count = GameConstants.WallCount;

            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
            }

            var placed = new List<Wall>();
            for (int n = 0; n < count; n++)
            {
                bool fromBorder = n < GameConstants.MinBorderWalls;
                bool ok = false;
                for (int i = 0; i < MaxWallAttempts && !ok; i++)
                {
                    var path = BuildRandomPath(rnd, fromBorder);
                    if (path == null) continue;

                    var wall = new Wall(order[n], path);
                    if (CanPlaceWall(placed, wall))
                    {
                        placed.Add(wall);
                        ok = true;
                    }
                }
                if (!ok) return null;
            }
            return placed;
        }

        /// <summary>
        /// Можно ли добавить стену к уже расставленным (public – для тестов).
        /// </summary>
        public static bool CanPlaceWall(List<Wall> placed, Wall wall)
        {
            var occupied = new HashSet<(int q, int r, int dir)>();
            foreach (var w in placed)
                foreach (var seg in w.Segments)
                    occupied.Add(seg.EdgeKey);

            foreach (var seg in wall.Segments)
            {
                var f = seg.FirstHex;
                var s = seg.SecondHex;
                if (!HexGrid.IsInBounds(f.q, f.r) || !HexGrid.IsInBounds(s.q, s.r)) return false;
                if (!occupied.Add(seg.EdgeKey)) return false;
            }

            if (!IsFarEnoughFromWalls(placed, wall)) return false;

            var all = new List<Wall>(placed);
            all.Add(wall);
            return new GameBoard(all, new List<Item>()).IsFieldConnected();
        }

        // ===== Расстояние между стенами =====
        /// <summary>
        /// Стена не ближе WallSeparation рёбер к уже стоящим стенам.
        /// </summary>
        private static bool IsFarEnoughFromWalls(List<Wall> placed, Wall newWall)
        {
            int separation = GameConstants.WallSeparation;
            if (separation <= 0 || placed.Count == 0) return true;

            var blocked = new Dictionary<string, (int q, int r)[]>();
            var frontier = new List<(int q, int r)[]>();

            foreach (var wall in placed)
            {
                foreach (var v in wall.GetVertices())
                {
                    string key = HexGrid.VertexKey(v);
                    if (!blocked.ContainsKey(key))
                    {
                        blocked[key] = v;
                        frontier.Add(v);
                    }
                }
            }

            for (int step = 1; step < separation; step++)
            {
                var next = new List<(int q, int r)[]>();
                foreach (var v in frontier)
                {
                    foreach (var n in HexGrid.GetNeighborVertices(v))
                    {
                        string key = HexGrid.VertexKey(n);
                        if (!blocked.ContainsKey(key))
                        {
                            blocked[key] = n;
                            next.Add(n);
                        }
                    }
                }
                frontier = next;
            }

            foreach (var v in newWall.GetVertices())
                if (blocked.ContainsKey(HexGrid.VertexKey(v))) return false;

            return true;
        }

        /// <summary>
        /// Случайная цепочка из 5 ребер вдоль линий сетки.
        /// Ребро – это общая сторона гексов A и B. 
        /// Гекс C соседствует и с A, и с B: ребра A|B, A|C и B|C сходятся в одной вершине. 
        /// Выбрав C, мы выбираем, через какой конец ребра пойдём дальше. 
        /// На каждом шаге стена поворачивает влево или вправо (ребро A|C или B|C), назад не возвращается.
        /// </summary>
        private static List<(int q, int r, int dir)>? BuildRandomPath(Random rnd, bool fromBorder)
        {
            var edges = new List<(int q, int r, int dir)>();

            var a = HexGrid.OffsetToAxial(rnd.Next(GameConstants.BoardWidth),
                                          rnd.Next(GameConstants.BoardHeight));
            var b = HexGrid.GetNeighbor(a.q, a.r, rnd.Next(6));

            var common = HexGrid.GetCommonNeighbors(a.q, a.r, b.q, b.r);
            (int q, int r) c;

            if (fromBorder)
            {
                if (!HexGrid.IsInBounds(b.q, b.r)) return null;

                bool firstIn = HexGrid.IsInBounds(common.first.q, common.first.r);
                bool secondIn = HexGrid.IsInBounds(common.second.q, common.second.r);
                if (firstIn == secondIn) return null;

                c = firstIn ? common.first : common.second;
            }
            else
            {
                c = rnd.Next(2) == 0 ? common.first : common.second;
            }

            for (int i = 0; i < GameConstants.WallLength; i++)
            {
                if (!HexGrid.IsInBounds(a.q, a.r) || !HexGrid.IsInBounds(b.q, b.r))
                    return null;

                edges.Add((a.q, a.r, HexGrid.GetDirectionBetween(a.q, a.r, b.q, b.r)));
                if (i == GameConstants.WallLength - 1) break;

                if (rnd.Next(2) == 0)
                {
                    var d = OtherCommonNeighbor(a, c, b);
                    b = c;
                    c = d;
                }
                else
                {
                    var d = OtherCommonNeighbor(b, c, a);
                    a = b;
                    b = c;
                    c = d;
                }
            }
            return edges;
        }

        /// <summary>
        /// Из двух общих соседей гексов x и y берём тот, что не равен exclude.
        /// </summary>
        private static (int q, int r) OtherCommonNeighbor((int q, int r) x, (int q, int r) y, (int q, int r) exclude)
        {
            var common = HexGrid.GetCommonNeighbors(x.q, x.r, y.q, y.r);
            return common.first == exclude ? common.second : common.first;
        }

        // ===== Предметы =====
        /// <summary>
        /// Попытка расстановки предметов.
        /// </summary>
        private static List<Item>? TryPlaceItems(Random rnd)
        {
            var forbidden = new HashSet<(int q, int r)>(GameBoard.GetCyborgStartPositions());
            forbidden.UnionWith(GameBoard.GetGoblinStartPositions());

            var candidates = new List<(int q, int r)>();
            foreach (var hex in HexGrid.GetAllHexes())
                if (!forbidden.Contains(hex)) candidates.Add(hex);

            var items = new List<Item>();
            for (int i = 0; i < GameConstants.TotalItems; i++)
            {
                if (candidates.Count == 0) return null;

                var pos = candidates[rnd.Next(candidates.Count)];
                var type = i < GameConstants.AmmoPackCount ? ItemType.Ammo : ItemType.Mine;
                items.Add(new Item(pos.q, pos.r, type));

                candidates.RemoveAll(hex =>
                    HexGrid.Distance(hex.q, hex.r, pos.q, pos.r) <= GameConstants.MinItemDistance);
            }
            return items;
        }
    }
}