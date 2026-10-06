namespace Cyberpunk3
{
    /// <summary>
    /// Направление взгляда киборга (для расчета брони и движения).
    /// </summary>
    public enum Direction
    {
        SouthEast = 0,
        NorthEast = 1,
        North = 2,
        NorthWest = 3,
        SouthWest = 4,
        South = 5
    }

    /// <summary>
    /// Класс киборга
    /// </summary>
    public class Cyborg
    {
        // ===== СТАТИЧЕСКИЕ ЧЛЕНЫ =====
        /// <summary>Счетчик созданных киборгов.</summary>
        private static int totalCyborgsCreated = 0;
        /// <summary>Максимальное здоровье каждой части тела (общее для всех киборгов). </summary>
        private static readonly int maxHealthPerBodyPart = GameConstants.CyborgBodyPartMaxHP;
        /// <summary>Начальное количество патронов (общее для всех киборгов).</summary>
        private static readonly int startingAmmo = GameConstants.CyborgStartAmmo;
        /// <summary>Очки действия за ход (общее для всех гоблинов). </summary>
        private static readonly int actionPointsPerTurn = GameConstants.CyborgActionPoints;

        /// <summary>Броня по направлению атаки относительно взгляда: 0 – фронт, 3 – тыл.</summary>
        private static readonly int[] ArmorValues = { 3, 2, 1, 0, 1, 2 };

        /// <summary>
        /// Получить общее количество созданных киборгов.
        /// </summary>
        public static int GetTotalCyborgs() { return totalCyborgsCreated; }

        /// <summary>
        /// Проверить, не превышен ли лимит киборгов.
        /// </summary>
        public static bool CanCreateCyborg() { return totalCyborgsCreated < GameConstants.MaxCyborgs; }

        /// <summary>
        /// Сбросить счётчик (новая партия, тесты).
        /// </summary>
        public static void ResetCounter() { totalCyborgsCreated = 0; }

        /// <summary>Броня против атаки, пришедшей с указанной стороны света.</summary>
        public static int GetArmorAgainst(Direction facing, Direction attackFrom)
        {
            int relative = ((int)attackFrom - (int)facing + 6) % 6;
            return ArmorValues[relative];
        }

        // ===== ПОЛЯ =====
        private int id;              // уникальный ID
        private int q, r;            // координаты в Axial системе
        private int actionPoints;    // очки действия
        private Direction facing;    // направление
        private int leftArmHP;       // здоровье правой руки (пулемёт)
        private int rightArmHP;      // здоровье правой руки (ракеты)
        private int generatorHP;     // здоровье генератора
        private int commandBlockHP;  // здоровье командного блока
        private int legsHP;          // здоровье ног
        private int ammo;            // патроны        
        private int shotsThisTurn;   // выстрелов в текущем ходу

        // ===== СВОЙСТВА =====
        // Свойства только для чтения, есть get нет set
        public int ID { get { return id; } }
        public int Q { get { return q; } }
        public int R { get { return r; } }
        public Direction Facing { get { return facing; } }
        public bool IsAlive { get { return commandBlockHP > 0; } }

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
                if (value < 0) actionPoints = 0;
                else if (value > actionPointsPerTurn) actionPoints = actionPointsPerTurn;
                else actionPoints = value;
            }
        }

        // ===== КОНСТРУКТОР =====
        public Cyborg(int q, int r, Direction initialFacing = Direction.North)
        {
            if (!CanCreateCyborg())
                throw new InvalidOperationException(
                    $"Нельзя создать больше {GameConstants.MaxCyborgs} киборгов!");

            totalCyborgsCreated++;
            id = totalCyborgsCreated;
            this.q = q;
            this.r = r;
            facing = initialFacing;
            leftArmHP = maxHealthPerBodyPart;
            rightArmHP = maxHealthPerBodyPart;
            generatorHP = maxHealthPerBodyPart;
            commandBlockHP = maxHealthPerBodyPart;
            legsHP = maxHealthPerBodyPart;
            ammo = startingAmmo;
            actionPoints = actionPointsPerTurn;
            shotsThisTurn = 0;

            Console.WriteLine($"[Киборг #{id}] создан на {HexGrid.HexToString(q, r)}, взгляд: {facing}");
        }


        // ===== МЕТОДЫ =====
        /// <summary>
        /// Гексы, в которые может попасть пулемёт (только по линии взгляда).
        /// </summary>
        public List<(int q, int r, int modifier)> GetShootTargets(GameBoard board)
        {
            return board.GetLineOfFire(q, r, (int)facing);
        }

        /// <summary>
        /// Выстрел. Киборг стреляет только по линии своего взгляда.
        /// </summary>
        public void Shoot(GameBoard board, int targetQ, int targetR)
        {
            if (!IsAlive) { Console.WriteLine($"[Киборг #{id}] Уничтожен!"); return; }
            if (leftArmHP <= 0) { Console.WriteLine($"[Киборг #{id}] Пулемёт уничтожен!"); return; }
            if (ammo <= 0) { Console.WriteLine($"[Киборг #{id}] Боезапас кончился!"); return; }
            if (actionPoints < GameConstants.ShootCost)
            { Console.WriteLine($"[Киборг #{id}] Недостаточно ОД!"); return; }
            if (shotsThisTurn >= GameConstants.MaxShotsPerTurn)
            { Console.WriteLine($"[Киборг #{id}] Лимит выстрелов за ход!"); return; }

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
            { Console.WriteLine($"[Киборг #{id}] Цель вне линии огня!"); return; }

            int distance = HexGrid.ShotDistance(q, r, targetQ, targetR);

            ammo--;
            actionPoints -= GameConstants.ShootCost;
            shotsThisTurn++;

            int dice = DiceRoller.RollWithLog($"Киборг #{id} стреляет");
            if (dice == 1)
            {
                Console.WriteLine($"[Киборг #{id}] Осечка!");
            }
            else if (dice >= distance)
            {
                Console.WriteLine($"[Киборг #{id}] Попадание! Цель: {HexGrid.HexToString(targetQ, targetR)}, поправка: {modifier}");
            }
            else
            {
                Console.WriteLine($"[Киборг #{id}] Промах! Нужно: {distance}, выпало: {dice}");
            }
        }

        /// <summary>
        /// Стрельба ракетой. Возвращает список поражённых гексов с ранениями и поправками.
        /// </summary>
        public List<(int q, int r, int wounds, int modifier)> ShootRocket(GameBoard board)
        {
            var none = new List<(int q, int r, int wounds, int modifier)>();

            if (!IsAlive) { Console.WriteLine($"[Киборг #{id}] Уничтожен!"); return none; }
            if (rightArmHP <= 0) { Console.WriteLine($"[Киборг #{id}] Ракетница уничтожена!"); return none; }
            if (ammo <= 0) { Console.WriteLine($"[Киборг #{id}] Боезапас кончился!"); return none; }
            if (actionPoints < GameConstants.ShootCost)
            { Console.WriteLine($"[Киборг #{id}] Недостаточно ОД!"); return none; }
            if (shotsThisTurn >= GameConstants.MaxShotsPerTurn)
            { Console.WriteLine($"[Киборг #{id}] Лимит выстрелов за ход!"); return none; }

            ammo--;
            actionPoints -= GameConstants.ShootCost;
            shotsThisTurn++;

            int dice = DiceRoller.RollWithLog($"Киборг #{id} пускает ракету");
            if (dice == 1)
            {
                Console.WriteLine($"[Киборг #{id}] Осечка!");
                return none;
            }

            var landing = board.GetRocketLanding(q, r, (int)facing, dice);
            Console.WriteLine($"[Киборг #{id}] Ракета упала на {HexGrid.HexToString(landing.q, landing.r)}");
            return board.GetExplosion(landing.q, landing.r);
        }

        /// <summary>
        /// Поворот на steps граней (положительное число – по возрастанию номера Direction).
        /// Стоимость считается по кратчайшему повороту: 5 граней = 1 грань в другую сторону.
        /// </summary>
        public void Turn(int steps)
        {
            if (!IsAlive) { Console.WriteLine($"[Киборг #{id}] Уничтожен!"); return; }

            steps %= 6;
            if (steps > 3) steps -= 6;
            else if (steps < -2) steps += 6;
            if (steps == 0) return;

            int cost = Math.Abs(steps) * GameConstants.CyborgTurnCost;
            if (actionPoints < cost)
            {
                Console.WriteLine($"[Киборг #{id}] Недостаточно ОД для поворота! Нужно: {cost}, есть: {actionPoints}");
                return;
            }

            int newFacing = ((int)facing + steps + 6) % 6;
            facing = (Direction)newFacing;
            actionPoints -= cost;
            Console.WriteLine($"[Киборг #{id}] повернулся. Направление: {facing}. ОД: {actionPoints}");
        }

        /// <summary>
        /// Шаг вперёд (по взгляду).
        /// </summary>
        public void MoveForward(GameBoard board)
        {
            Step(board, (int)facing, GameConstants.CyborgForwardMoveCost, "переместился вперёд");
        }

        /// <summary>
        /// Шаг назад. Направление взгляда не меняется.
        /// </summary>
        public void MoveBackward(GameBoard board)
        {
            Step(board, ((int)facing + 3) % 6, GameConstants.CyborgBackwardMoveCost, "отступил");
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
        /// Вывод статуса.
        /// </summary>
        public void PrintStatus()
        {
            Console.WriteLine($"===== Киборг #{id} =====");
            Console.WriteLine($"Позиция: {HexGrid.HexToString(q, r)}");
            Console.WriteLine($"Направление: {facing}");
            Console.WriteLine($"Патроны: {ammo}/{startingAmmo}");
            Console.WriteLine($"ОД: {actionPoints}/{actionPointsPerTurn}");
            Console.WriteLine($"Пулемёт: {leftArmHP}/{maxHealthPerBodyPart}");
            Console.WriteLine($"Ракеты: {rightArmHP}/{maxHealthPerBodyPart}");
            Console.WriteLine($"Генератор: {generatorHP}/{maxHealthPerBodyPart}");
            Console.WriteLine($"Ком. блок: {commandBlockHP}/{maxHealthPerBodyPart}");
            Console.WriteLine($"Ноги: {legsHP}/{maxHealthPerBodyPart}");
            Console.WriteLine($"Статус: {(IsAlive ? "Активен" : "Уничтожен")}");
            Console.WriteLine("===================");
        }

        // ===== ПРИВАТНЫЕ МЕТОДЫ =====
        /// <summary>
        /// Общая часть шага вперёд и назад: проверки, стена, перемещение, оплата.
        /// </summary>
        private void Step(GameBoard board, int direction, int cost, string verb)
        {
            if (!IsAlive) { Console.WriteLine($"[Киборг #{id}] Уничтожен!"); return; }
            if (legsHP <= 0)
            { Console.WriteLine($"[Киборг #{id}] Ноги выведены из строя!"); return; }
            if (actionPoints < cost)
            { Console.WriteLine($"[Киборг #{id}] Недостаточно ОД! Нужно: {cost}, есть: {actionPoints}"); return; }

            var newPos = HexGrid.GetNeighbor(q, r, direction);
            if (!HexGrid.IsInBounds(newPos.q, newPos.r))
            { Console.WriteLine($"[Киборг #{id}] Выход за пределы поля!"); return; }
            if (board.IsPassageBlocked(q, r, newPos.q, newPos.r))
            { Console.WriteLine($"[Киборг #{id}] Путь преграждает стена!"); return; }

            q = newPos.q;
            r = newPos.r;
            actionPoints -= cost;
            Console.WriteLine($"[Киборг #{id}] {verb} на {HexGrid.HexToString(q, r)}. ОД: {actionPoints}");
        }

        /// <summary>
        /// Получение урона
        /// </summary>
        private void TakeWound()
        {

        }

        /// <summary>
        /// Ремонт
        /// </summary>
        private void Repair()
        {

        }

        /// <summary>
        /// Активация электрополя
        /// </summary>
        private void FieldActivate()
        {

        }

    }
}
