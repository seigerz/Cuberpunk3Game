using System;
using System.Collections.Generic;
using System.Text;

namespace Cyberpunk3
{
    public class Goblin
    {
        // ===== СТАТИЧЕСКИЕ ЧЛЕНЫ =====
        /// <summary>Счетчик созданных гоблинов.</summary>
        private static int totalGoblinsCreated = 0;
        /// <summary>Максимальное здоровье каждой части тела (общее для всех гоблинов). </summary>
        private static int maxHealthPerBodyPart = GameConstants.GoblinBodyPartMaxHP;
        /// <summary>Начальное количество патронов (общее для всех гоблинов).</summary>
        private static int startingAmmo = GameConstants.GoblinStartAmmo;
        /// <summary>Очки действия за ход (общее для всех гоблинов). </summary>
        private static int actionPointsPerTurn = GameConstants.GoblinActionPoints;

        /// <summary>
        /// Получить общее количество созданных гоблинов.
        /// </summary>
        public static int GetTotalGoblins() { return totalGoblinsCreated; }

        /// <summary>
        /// Проверить, не превышен ли лимит гоблинов.
        /// </summary>
        public static bool CanCreateGoblin() { return totalGoblinsCreated < GameConstants.MaxGoblins; }

        /// <summary>
        /// Сбросить счётчик (новая партия, тесты).
        /// </summary>
        public static void ResetCounter() { totalGoblinsCreated = 0; }   // новая игра / тесты

        // ===== ПОЛЯ  =====
        private int id;                  // уникальный ID
        private int q, r;                // координаты в Axial системе
        private int actionPoints;        // очки действия
        private int leftArmHP;           // здоровье левой руки (рукопашная)
        private int rightArmHP;          // здоровье правой руки (огнестрел)
        private int headHP;              // здоровье головы
        private int legsHP;              // здоровье ног
        private int ammo;                // патроны
        private int shotsThisTurn;       // количество выстрелов за текущий ход


        // ===== СВОЙСТВА  =====
        // Свойства только для чтения, есть get нет set
        public int ID { get { return id; } }
        public int Q { get { return q; } }
        public int R { get { return r; } }
        public bool IsAlive { get { return headHP > 0; } }

        /// <summary>
        /// Свойство с проверкой – патроны и очки действия не могут быть отрицательными.
        /// </summary> 
        public int Ammo
        {
            get { return ammo; }
            set { ammo = (value < 0) ? 0 : value; }
        }
        public int ActionPoints
        {
            get { return actionPoints; }
            set
            {
                if (value < 0)
                    actionPoints = 0;
                else if (value > actionPointsPerTurn)
                    actionPoints = actionPointsPerTurn;
                else
                    actionPoints = value;
            }
        }


        // ===== КОНСТРУКТОР =====
        public Goblin(int q, int r)
        {
            if (!CanCreateGoblin())
                throw new InvalidOperationException(
                    $"Нельзя создать больше {GameConstants.MaxGoblins} гоблинов!");

            totalGoblinsCreated++;
            id = totalGoblinsCreated;
            this.q = q;
            this.r = r;
            leftArmHP = maxHealthPerBodyPart;
            rightArmHP = maxHealthPerBodyPart;
            headHP = maxHealthPerBodyPart;
            legsHP = maxHealthPerBodyPart;
            ammo = startingAmmo;
            actionPoints = actionPointsPerTurn;
            shotsThisTurn = 0;

            Console.WriteLine($"[Гоблин #{id}] создан на {HexGrid.HexToString(q, r)}");
        }


        // ===== МЕТОДЫ =====
        /// <summary>
        /// Все гексы, по которым гоблин может стрелять (6 направлений).
        /// </summary>
        public List<(int q, int r, int modifier)> GetShootTargets(GameBoard board)
        {
            var all = new List<(int q, int r, int modifier)>();
            for (int dir = 0; dir < 6; dir++)
                all.AddRange(board.GetLineOfFire(q, r, dir));
            return all;
        }

        /// <summary>
        /// Стрельба.
        /// </summary>
        public void Shoot(GameBoard board, int targetQ, int targetR)
        {
            if (!IsAlive) { Console.WriteLine($"[Гоблин #{id}] Убит и не может стрелять!"); return; }
            if (rightArmHP <= 0) { Console.WriteLine($"[Гоблин #{id}] Оружие уничтожено!"); return; }
            if (ammo <= 0) { Console.WriteLine($"[Гоблин #{id}] Патроны кончились!"); return; }
            if (actionPoints < GameConstants.ShootCost)
            { Console.WriteLine($"[Гоблин #{id}] Недостаточно ОД!"); return; }
            if (shotsThisTurn >= GameConstants.MaxShotsPerTurn)
            { Console.WriteLine($"[Гоблин #{id}] Лимит выстрелов за ход!"); return; }

            // Ищем цель в наборе допустимых
            int modifier = 0;
            bool found = false;
            foreach (var t in GetShootTargets(board))
            {
                if (t.q == targetQ && t.r == targetR)
                {
                    modifier = t.modifier;
                    found = true;
                    break;
                }
            }
            if (!found)
            { Console.WriteLine($"[Гоблин #{id}] Цель вне линии огня!"); return; }

            int distance = HexGrid.ShotDistance(q, r, targetQ, targetR);

            ammo--;
            actionPoints -= GameConstants.ShootCost;
            shotsThisTurn++;

            int dice = DiceRoller.RollWithLog($"Гоблин #{id} стреляет");
            if (dice == 1)
            {
                Console.WriteLine($"[Гоблин #{id}] Осечка!");
            }
            else if (dice >= distance)
            {
                Console.WriteLine($"[Гоблин #{id}] Попадание! Цель: {HexGrid.HexToString(targetQ, targetR)}, поправка к ранению: {modifier}");
            }
            else
            {
                Console.WriteLine($"[Гоблин #{id}] Промах! Нужно: {distance}, выпало: {dice}");
            }
        }

        /// <summary>
        /// Движение.
        /// </summary>
        public void Move(GameBoard board, int newQ, int newR)
        {
            if (!IsAlive) { Console.WriteLine($"[Гоблин #{id}] Убит и не может двигаться!"); return; }
            if (legsHP <= 0)
            { Console.WriteLine($"[Гоблин #{id}] Не может двигаться - ноги выведены из строя!"); return; }

            int steps = PathFinder.GetPathLength(board, q, r, newQ, newR);
            if (steps < 0)
            { Console.WriteLine($"[Гоблин #{id}] Туда не пройти!"); return; }

            int cost = steps * GameConstants.GoblinMoveCost;
            if (actionPoints < cost)
            { Console.WriteLine($"[Гоблин #{id}] Недостаточно ОД! Нужно: {cost}, есть: {actionPoints}"); return; }

            q = newQ;
            r = newR;
            actionPoints -= cost;
            Console.WriteLine($"[Гоблин #{id}] переместился на {HexGrid.HexToString(q, r)}. ОД: {actionPoints}");
        }

        /// <summary>
        /// Сброс очков действия.
        /// </summary>
        public void ResetActionPoints()
        {
            actionPoints = actionPointsPerTurn;
            shotsThisTurn = 0;
        }
        
        /// <summary>
        /// Напечатать статус гоблина.
        /// </summary>
        public void PrintStatus()
        {
            Console.WriteLine($"===== Гоблин #{id} =====");
            Console.WriteLine($"Позиция: {HexGrid.HexToString(q, r)}");
            Console.WriteLine($"Патроны: {ammo}/{startingAmmo}");
            Console.WriteLine($"ОД: {actionPoints}/{actionPointsPerTurn}");
            Console.WriteLine($"Метал. рука: {leftArmHP}/{maxHealthPerBodyPart}");
            Console.WriteLine($"Рука (оружие): {rightArmHP}/{maxHealthPerBodyPart}");
            Console.WriteLine($"Голова: {headHP}/{maxHealthPerBodyPart}");
            Console.WriteLine($"Ноги: {legsHP}/{maxHealthPerBodyPart}");
            Console.WriteLine($"Статус: {(IsAlive ? "Жив" : "Убит")}");
            Console.WriteLine("===================");
        }


        // ===== ПРИВАТНЫЕ МЕТОДЫ =====
        /// <summary>
        /// Получение урона
        /// </summary>
        private void TakeWound()
        {

        }

        /// <summary>
        /// Ремонт
        /// </summary>
        private void Heal()
        {

        }
    }
}
