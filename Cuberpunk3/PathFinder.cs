namespace Cyberpunk3
{
    /// <summary>
    /// Статический класс для поиска путей и проверки связности на гексагональном поле.
    /// </summary>
    public static class PathFinder
    {

        /// <summary>
        /// Длина кратчайшего пути в шагах с учётом стен и границ; -1, если пути нет.
        /// </summary>
        public static int GetPathLength(GameBoard board, int fromQ, int fromR, int toQ, int toR)
        {
            if (!HexGrid.IsInBounds(fromQ, fromR) || !HexGrid.IsInBounds(toQ, toR)) return -1;
            if (fromQ == toQ && fromR == toR) return 0;
            var dist = GetDistances(board, fromQ, fromR, int.MaxValue);
            int d;
            return dist.TryGetValue((toQ, toR), out d) ? d : -1;
        }

        /// <summary>
        /// Проверить, существует ли путь между двумя гексами.
        /// Упрощенная реализация без учета стен (для демонстрации).
        /// </summary>
        public static bool IsReachable(GameBoard board, int fromQ, int fromR, int toQ, int toR)
        {
            return GetPathLength(board, fromQ, fromR, toQ, toR) >= 0;
        }

        /// <summary>
        /// Получить все гексы, доступные для движения с заданным количеством очков действия.
        /// Упрощенная версия без учета препятствий.
        /// </summary>
        public static List<(int q, int r)> GetReachableHexes(GameBoard board, int startQ, int startR, int maxSteps)
        {
            var result = new List<(int q, int r)>();
            if (!HexGrid.IsInBounds(startQ, startR)) return result;
            foreach (var pair in GetDistances(board, startQ, startR, maxSteps))
                if (pair.Value > 0) result.Add(pair.Key);
            return result;
        }

        /// <summary>
        /// Проверить, находятся ли два гекса рядом (на расстоянии 1).
        /// </summary>
        public static bool AreNeighbors(int q1, int r1, int q2, int r2)
        {
            return HexGrid.Distance(q1, r1, q2, r2) == 1;
        }

        /// <summary>
        /// Все достижимые состояния киборга: (q, r, facing) → стоимость в ОД.
        /// Учитывает стоимость шага вперёд (1), назад (2) и поворота (1 за грань).
        /// </summary>
        public static Dictionary<(int q, int r, int facing), int> GetCyborgReachable(
            GameBoard board, int startQ, int startR, int startFacing, int maxAP)
        {
            var dist = new Dictionary<(int q, int r, int facing), int>();
            var state = (startQ, startR, startFacing);
            dist[state] = 0;

            // Очередь с приоритетом заменена на простой список (поле маленькое)
            var queue = new SortedList<int, List<(int q, int r, int f)>>();
            Enqueue(queue, 0, state);

            while (queue.Count > 0)
            {
                var first = queue.Keys[0];
                var list = queue[first];
                var cur = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                if (list.Count == 0) queue.RemoveAt(0);

                int cost = dist[cur];
                if (cost > first) continue;   // устаревшая запись

                // Поворот влево и вправо
                for (int delta = -1; delta <= 1; delta += 2)
                {
                    int nf = (cur.f + delta + 6) % 6;
                    int nc = cost + GameConstants.CyborgTurnCost;
                    if (nc <= maxAP) TryRelax(dist, queue, (cur.q, cur.r, nf), nc);
                }

                // Шаг вперёд
                TryStep(board, dist, queue, cur, cur.f,
                        cost + GameConstants.CyborgForwardMoveCost, maxAP);

                // Шаг назад (направление не меняется)
                TryStep(board, dist, queue, cur, (cur.f + 3) % 6,
                        cost + GameConstants.CyborgBackwardMoveCost, maxAP);
            }

            return dist;
        }

        /// <summary>
        /// Гексы, куда киборг может дойти (без учёта facing).
        /// </summary>
        public static List<(int q, int r)> GetCyborgReachableHexes(
            GameBoard board, int startQ, int startR, int startFacing, int maxAP)
        {
            var states = GetCyborgReachable(board, startQ, startR, startFacing, maxAP);
            var hexes = new HashSet<(int q, int r)>();
            foreach (var kv in states)
                hexes.Add((kv.Key.q, kv.Key.r));
            hexes.Remove((startQ, startR));
            return new List<(int q, int r)>(hexes);
        }

        // ===== Приватные методы =====
        /// <summary>
        /// Вычисление расстояний.
        /// </summary>
        private static Dictionary<(int q, int r), int> GetDistances(
            GameBoard board, int startQ, int startR, int maxSteps)
        {
            var dist = new Dictionary<(int q, int r), int> { { (startQ, startR), 0 } };
            var queue = new Queue<(int q, int r)>();
            queue.Enqueue((startQ, startR));

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                int d = dist[cur];
                if (d >= maxSteps) continue;

                foreach (var n in HexGrid.GetAllNeighbors(cur.q, cur.r))
                {
                    if (!HexGrid.IsInBounds(n.q, n.r) || dist.ContainsKey(n)) continue;
                    if (board.IsPassageBlocked(cur.q, cur.r, n.q, n.r)) continue;
                    dist[n] = d + 1;
                    queue.Enqueue(n);
                }
            }
            return dist;
        }

        /// <summary>
        /// Проверить возможность шага.
        /// </summary>
        private static void TryStep(
            GameBoard board,
            Dictionary<(int q, int r, int facing), int> dist,
            SortedList<int, List<(int q, int r, int f)>> queue,
            (int q, int r, int f) cur, int moveDir, int newCost, int maxAP)
        {
            if (newCost > maxAP) return;
            var next = HexGrid.GetNeighbor(cur.q, cur.r, moveDir);
            if (!HexGrid.IsInBounds(next.q, next.r)) return;
            if (board.IsPassageBlocked(cur.q, cur.r, next.q, next.r)) return;
            TryRelax(dist, queue, (next.q, next.r, cur.f), newCost);
        }

        /// <summary>
        /// "Релаксация" состояния.
        /// </summary>
        private static void TryRelax(
            Dictionary<(int q, int r, int facing), int> dist,
            SortedList<int, List<(int q, int r, int f)>> queue,
            (int q, int r, int f) state, int cost)
        {
            int old;
            if (dist.TryGetValue(state, out old) && old <= cost) return;
            dist[state] = cost;
            Enqueue(queue, cost, state);
        }

        /// <summary>
        /// Постановка в очередь.
        /// </summary>
        private static void Enqueue(
            SortedList<int, List<(int q, int r, int f)>> queue,
            int priority, (int q, int r, int f) state)
        {
            List<(int q, int r, int f)>? list;
            if (!queue.TryGetValue(priority, out list))
            {
                list = new List<(int q, int r, int f)>();
                queue.Add(priority, list);
            }
            list.Add(state);
        }
    }
}
