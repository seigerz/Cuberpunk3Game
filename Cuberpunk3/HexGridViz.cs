using System.Text;

namespace Cyberpunk3
{
    /// <summary>
    /// Рисует игровое поле в консоли.
    /// Принимает данные в аксиальных координатах (как GameBoard и юниты),
    /// на экране подписывает гексы в offset-координатах (столбец,ряд),
    /// где (0,0) – левый верхний угол, (7,8) – правый нижний.
    /// </summary>
    public class HexGridViz
    {
        // ===== СТАТИЧЕСКИЕ ЧЛЕНЫ =====
        /// <summary>Позиции символов стены внутри клетки для сторон: N, NE, SE, S, SW, NW.</summary>
        private static readonly int[][][] WallPos =
        {
            new int[][] { new[]{0,2}, new[]{0,3}, new[]{0,4} }, // N
            new int[][] { new[]{1,5}, new[]{2,6} },             // NE
            new int[][] { new[]{3,6}, new[]{4,5} },             // SE
            new int[][] { new[]{4,2}, new[]{4,3}, new[]{4,4} }, // S
            new int[][] { new[]{4,1}, new[]{3,0} },             // SW
            new int[][] { new[]{2,0}, new[]{1,1} },             // NW
        };

        /// <summary>Стрелки в порядке enum Direction: ЮВ, СВ, С, СЗ, ЮЗ, Ю.</summary>
        private static readonly string[] Arrows = { "↓→", "↑→", "↑↑", "↑←", "↓←", "↓↓" };

        /// <summary>
        /// Направление HexGrid (0=ЮВ..5=Ю) -> сторона гекса на экране (0=N, 1=NE, 2=SE, 3=S, 4=SW, 5=NW).
        /// </summary>
        private static int ToScreenSide(int dir)
        {
            return (8 - dir) % 6;
        }

        /// <summary>
        /// Получаем букву по типу сегмента стены.
        /// </summary>
        private static char WallLetter(WallType type)
        {
            return Wall.GetTypeLetter(type);
        }

        /// <summary>
        /// Чётные столбцы опущены на пол-гекса (как в HexGrid.AxialToOffset)
        /// </summary>
        private static int HexX(int col) { return 5 * col; }
        
        private static int HexY(int col, int row) { return 4 * row + (col % 2 == 0 ? 2 : 0); }

        /// <summary>
        /// Отрисовка стен.
        /// </summary>
        private static void Plot(char[,] buf, int y, int x, char c)
        {
            if (y >= 0 && y < buf.GetLength(0) && x >= 0 && x < buf.GetLength(1))
                buf[y, x] = c;
        }

        /// <summary>Пишет строку, центрируя её относительно centerX.</summary>
        private static void PlotText(char[,] buf, int y, int centerX, string text)
        {
            int startX = centerX - text.Length / 2;
            for (int i = 0; i < text.Length; i++)
                Plot(buf, y, startX + i, text[i]);
        }

        private static void DrawContour(char[,] buf, int ox, int oy)
        {
            Plot(buf, oy + 0, ox + 2, '_'); Plot(buf, oy + 0, ox + 3, '_'); Plot(buf, oy + 0, ox + 4, '_');
            Plot(buf, oy + 1, ox + 1, '/'); Plot(buf, oy + 2, ox + 0, '/');
            Plot(buf, oy + 1, ox + 5, '\\'); Plot(buf, oy + 2, ox + 6, '\\');
            Plot(buf, oy + 3, ox + 0, '\\'); Plot(buf, oy + 4, ox + 1, '\\');
            Plot(buf, oy + 3, ox + 6, '/'); Plot(buf, oy + 4, ox + 5, '/');
            Plot(buf, oy + 4, ox + 2, '_'); Plot(buf, oy + 4, ox + 3, '_'); Plot(buf, oy + 4, ox + 4, '_');
        }

        // ===== ПОЛЯ =====
        /// <summary>Размеры поля.</summary>
        private readonly int cols;
        private readonly int rows;

        /// <summary>[столбец, ряд, сторона 0..5], '\0' – стены нет.</summary>
        private readonly char[,,] walls;
        /// <summary>Подпись юнита или null.</summary>
        private readonly string[,] units;
        /// <summary>Наличие итема на поле и его тип.</summary>
        private readonly bool[,] hasItem;
        private readonly ItemType[,] itemTypes;


        // ===== СВОЙСТВА =====
        /// <summary>true – рисовать Ia/Im (для проверки генерации), false – просто I.</summary>
        public bool ShowItemTypes { get; set; }


        // ===== КОНСТРУКТОРЫ =====
        public HexGridViz()
        {
            cols = GameConstants.BoardWidth;
            rows = GameConstants.BoardHeight;
            walls = new char[cols, rows, 6];
            units = new string[cols, rows];
            hasItem = new bool[cols, rows];
            itemTypes = new ItemType[cols, rows];
        }


        // ===== МЕТОДЫ =====
        // ===== Загрузка данных =====
        /// <summary>
        /// Загружает стены и предметы поля (старые стены и предметы стираются).
        /// </summary>
        public void LoadBoard(GameBoard board)
        {
            Array.Clear(walls, 0, walls.Length);
            Array.Clear(hasItem, 0, hasItem.Length);

            foreach (var wall in board.Walls)
                foreach (var segment in wall.Segments)
                    SetWall(segment);

            foreach (var item in board.Items)
                if (!item.IsPickedUp)
                    SetItem(item.Q, item.R, item.Type);
        }

        /// <summary>
        /// Отрисовать киборга на поле
        /// </summary>
        public void SetCyborg(int q, int r, int number, Direction direction)
        {
            if (!HexGrid.IsInBounds(q, r)) return;
            var Hex = HexGrid.AxialToOffset(q, r);
            units[Hex.col, Hex.row] = Arrows[(int)direction] + "C" + number;   // 4 символа
        }

        /// <summary>
        /// Отрисовать гоблина на поле
        /// </summary>
        public void SetGoblin(int q, int r, int number)
        {
            if (!HexGrid.IsInBounds(q, r)) return;
            var Hex = HexGrid.AxialToOffset(q, r);
            units[Hex.col, Hex.row] = "G" + number;                            // 2 символа
        }

        /// <summary>
        /// Стереть юнитов с поля
        /// </summary>
        public void ClearUnits()
        {
            Array.Clear(units, 0, units.Length);
        }

        /// <summary>
        /// Установить итемы
        /// </summary>
        private void SetItem(int q, int r, ItemType type)
        {
            if (!HexGrid.IsInBounds(q, r)) return;
            var Hex = HexGrid.AxialToOffset(q, r);
            hasItem[Hex.col, Hex.row] = true;
            itemTypes[Hex.col, Hex.row] = type;
        }

        /// <summary>
        /// Установить стены
        /// </summary>
        private void SetWall(WallSegment segment)
        {
            var edge = segment.EdgeKey;                 // (q, r, dir), dir = 0..2
            if (!HexGrid.IsInBounds(edge.q, edge.r)) return;

            var Hex = HexGrid.AxialToOffset(edge.q, edge.r);
            // Одно ребро – одна запись: рёбра соседних гексов совпадают на экране
            walls[Hex.col, Hex.row, ToScreenSide(edge.dir)] = WallLetter(segment.Type);
        }

        /// <summary>
        /// Отрисовать подписи итемов
        /// </summary>
        private string ItemLabel(int col, int row)
        {
            if (!ShowItemTypes) return "I";
            return itemTypes[col, row] == ItemType.Ammo ? "Ia" : "Im";
        }

        /// <summary>
        /// Отрисовка
        /// </summary>
        public void Render()
        {
            int bufH = 4 * rows + 6;
            int bufW = 5 * cols + 6;
            char[,] buf = new char[bufH, bufW];
            for (int i = 0; i < bufH; i++)
                for (int j = 0; j < bufW; j++) buf[i, j] = ' ';

            // 1) Контуры
            for (int col = 0; col < cols; col++)
                for (int row = 0; row < rows; row++)
                    DrawContour(buf, HexX(col), HexY(col, row));

            // 2) Стены
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    int ox = HexX(col);
                    int oy = HexY(col, row);
                    for (int side = 0; side < 6; side++)
                    {
                        char c = walls[col, row, side];
                        if (c == '\0') continue;
                        foreach (var p in WallPos[side])
                            Plot(buf, oy + p[0], ox + p[1], c);
                    }
                }
            }

            // 3) Координаты, юниты и предметы
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    int ox = HexX(col);
                    int oy = HexY(col, row);

                    PlotText(buf, oy + 2, ox + 3, col + "," + row);

                    string unit = units[col, row];
                    if (unit != null)
                    {
                        PlotText(buf, oy + 3, ox + 3, unit);
                        if (hasItem[col, row])      // юнит и предмет в одной клетке:
                            PlotText(buf, oy + 1, ox + 3, ItemLabel(col, row));   // метка строкой выше
                    }
                    else if (hasItem[col, row])
                    {
                        PlotText(buf, oy + 3, ox + 3, ItemLabel(col, row));
                    }
                }
            }

            // 4) Печать
            for (int i = 0; i < bufH; i++)
            {
                var sb = new StringBuilder();
                for (int j = 0; j < bufW; j++) sb.Append(buf[i, j]);
                Console.WriteLine(sb.ToString().TrimEnd());
            }
        }
    }
}