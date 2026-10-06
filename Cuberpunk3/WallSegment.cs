namespace Cyberpunk3
{
    /// <summary>
    /// Тип сегмента стены.
    /// </summary>
    public enum WallType
    {
        Low,    // Низкая (н)
        Medium, // Средняя (с)
        High    // Высокая (в)
    }

    /// <summary>Сегмент стены на игровом поле.</summary>
    public class WallSegment
    {
        // ===== СТАТИЧЕСКИЕ ЧЛЕНЫ =====
        /// <summary>Модификатор урона для низкой стены.</summary>
        public static readonly int LowWallDamageModifier = -1;
        /// <summary>Количество созданых сегментов.</summary>
        private static int totalSegmentsCreated = 0;

        /// <summary>
        /// Получить количество созданых сегментов.
        /// </summary>
        public static int GetTotalSegments() { return totalSegmentsCreated; }


        // ===== ПОЛЯ =====
        /// <summary>Тип сегмента и его координаты.</summary>
        private readonly WallType type;
        private readonly int q, r, edge;   // ребро между (q,r) и соседом в направлении edge (0..2)


        // ===== СВОЙСТВА =====
        public WallType Type { get { return type; } }
        public (int q, int r) FirstHex { get { return (q, r); } }
        public (int q, int r) SecondHex { get { return HexGrid.GetNeighbor(q, r, edge); } }
        public (int q, int r, int dir) EdgeKey { get { return (q, r, edge); } }


        // ===== КОНСТРУКТОРЫ =====
        /// <param name="direction">Направление (0-5) от гекса (q,r) к гексу по другую сторону ребра.</param>
        public WallSegment(WallType type, int q, int r, int direction)
        {
            var e = HexGrid.NormalizeEdge(q, r, direction);   // одно ребро – одна запись
            this.type = type;
            this.q = e.q;
            this.r = e.r;
            this.edge = e.dir;
            totalSegmentsCreated++;
        }


        // ===== МЕТОДЫ =====
        /// <summary>
        /// Блокирует ли сегмент обычный выстрел (средняя и высокая – да).
        /// </summary>
        public bool BlocksShot() { return type != WallType.Low; }

        /// <summary>
        /// Блокирует ли сегмент взрыв ракеты (средняя и высокая – да).
        /// </summary>
        public bool BlocksExplosion() { return type != WallType.Low; }

        /// <summary>
        /// Высокая стена останавливает ракету в полёте; низкие и средние она перелетает.
        /// </summary>
        public bool BlocksRocketFlight() { return type == WallType.High; }

        /// <summary>
        /// Поправка к ранению при попадании через низкую стену.
        /// </summary>
        public int GetDamageModifier()
        {
            return type == WallType.Low ? LowWallDamageModifier : 0;
        }

        /// <summary>
        /// Стоит ли сегмент на ребре между двумя соседними гексами.
        /// </summary>
        public bool Separates(int q1, int r1, int q2, int r2)
        {
            if (HexGrid.Distance(q1, r1, q2, r2) != 1) return false;
            int dir = HexGrid.GetDirectionBetween(q1, r1, q2, r2);
            return HexGrid.NormalizeEdge(q1, r1, dir) == EdgeKey;
        }

        /// <summary>
        /// Вывод информации о сегменте стены.
        /// </summary>
        public string WallSegmentToString()
        {
            char c = type == WallType.High ? 'в' : type == WallType.Medium ? 'с' : 'н';
            return $"[{c}]({q},{r},{edge})";
        }
    }
}
