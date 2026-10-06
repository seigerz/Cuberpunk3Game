namespace Cyberpunk3
{
    /// <summary>
    /// Стена на игровом поле, состоящая из 5 сегментов.
    /// </summary>
    public class Wall
    {
        // ===== СТАТИЧЕСКИЕ ЧЛЕНЫ =====
        /// <summary>8 видов стен из правил: н – низкая, с – средняя, в – высокая.</summary>
        public static readonly string[] WallConfigurations =
        {
            "нвнвн", "снвнс", "нсвсн", "вснсв",
            "внвнв", "свснс", "снсвс", "свнвс"
        };

        /// <summary>
        /// Единственный источник соответствия тип↔буква.
        /// </summary>
        public static char GetTypeLetter(WallType type)
        {
            switch (type)
            {
                case WallType.High: return 'в';
                case WallType.Medium: return 'с';
                default: return 'н';
            }
        }

        /// <summary>
        /// Парсинг буквы в тип.
        /// </summary>
        private static WallType ParseType(char c)
        {
            switch (c)
            {
                case 'н': return WallType.Low;
                case 'с': return WallType.Medium;
                case 'в': return WallType.High;
                default: throw new ArgumentException($"Неизвестный символ стены: '{c}'");
            }
        }


        // ===== ПОЛЯ =====
        /// <summary>Индекс конфигурации.</summary>
        private readonly int configIndex;
        /// <summary>Массив сегментов.</summary>
        private readonly WallSegment[] segments;


        // ===== СВОЙСТВА =====
        public int ConfigIndex { get { return configIndex; } }
        public WallSegment[] Segments { get { return (WallSegment[])segments.Clone(); } }


        // ===== КОНСТРУКТОРЫ =====
        /// <param name="configIndex">Вид стены (0-7)</param>
        /// <param name="edges">5 ребер цепочки: (q, r, направление к гексу по другую сторону)</param>
        public Wall(int configIndex, IList<(int q, int r, int dir)> edges)
        {
            if (configIndex < 0 || configIndex >= WallConfigurations.Length)
                throw new ArgumentException($"configIndex {configIndex} вне диапазона 0..{WallConfigurations.Length - 1}");

            string config = WallConfigurations[configIndex];
            if (edges.Count != config.Length)
                throw new ArgumentException($"Ожидается {config.Length} рёбер, получено {edges.Count}");

            this.configIndex = configIndex;
            segments = new WallSegment[edges.Count];
            for (int i = 0; i < edges.Count; i++)
                segments[i] = new WallSegment(ParseType(config[i]), edges[i].q, edges[i].r, edges[i].dir);
        }


        // ===== МЕТОДЫ =====
        /// <summary>
        /// Все вершины стены (у цепочки из 5 рёбер их 6, дубликаты возможны).
        /// </summary>
        public List<(int q, int r)[]> GetVertices()
        {
            var result = new List<(int q, int r)[]>();
            foreach (var segment in segments)
            {
                var a = segment.FirstHex;
                var b = segment.SecondHex;
                var common = HexGrid.GetCommonNeighbors(a.q, a.r, b.q, b.r);
                result.Add(HexGrid.MakeVertex(a, b, common.first));
                result.Add(HexGrid.MakeVertex(a, b, common.second));
            }
            return result;
        }

        /// <summary>
        /// Есть ли у стены вершина на границе поля.
        /// </summary>
        public bool TouchesBorder()
        {
            foreach (var segment in segments)
            {
                var a = segment.FirstHex;
                var b = segment.SecondHex;
                var common = HexGrid.GetCommonNeighbors(a.q, a.r, b.q, b.r);
                if (!HexGrid.IsInBounds(common.first.q, common.first.r) ||
                    !HexGrid.IsInBounds(common.second.q, common.second.r))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Вывод информации о стене.
        /// </summary>
        public string WallToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"Стена[{configIndex}] ");
            foreach (var seg in segments) sb.Append(seg + " ");
            return sb.ToString().TrimEnd();
        }
    }
}
