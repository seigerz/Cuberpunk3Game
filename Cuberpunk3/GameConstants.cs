namespace Cyberpunk3
{
    /// <summary>
    /// Константы игры "Киберпанк-3: Засада на UNL-2".
    /// Все значения статические и readonly - можно читать, но нельзя изменять.
    /// </summary>
    public static class GameConstants
    {
        // ===== Размеры игрового поля =====
        /// <summary>Ширина поля в гексах</summary>
        public const int BoardWidth = 8;

        /// <summary>Высота поля в гексах</summary>
        public const int BoardHeight = 9;

        /// <summary>Общее количество гексов на поле</summary>
        public const int TotalHexes = BoardWidth * BoardHeight; // 72

        // ===== Стены =====
        /// <summary>Количество стен на поле</summary>
        public const int WallCount = 8;

        /// <summary>Длина каждой стены в сегментах</summary>
        public const int WallLength = 5;

        /// <summary>Минимальное число свободных рёбер между разными стенами.
        /// 0 – стены могут касаться, 1 – между стенами минимум одно свободное ребро.</summary>
        public const int WallSeparation = 2;

        /// <summary>Минимум стен, у которых один конец упирается в край поля.</summary>
        public const int MinBorderWalls = 3;

        // ===== Предметы =====
        /// <summary>Количество мин</summary>
        public const int MineCount = 2;

        /// <summary>Количество боекомплектов</summary>
        public const int AmmoPackCount = 10;

        /// <summary>Общее количество предметов на поле</summary>
        public const int TotalItems = MineCount + AmmoPackCount;

        /// <summary>Минимальное расстояние между предметами (в гексах)</summary>
        public const int MinItemDistance = 1;

        // ===== Юниты =====
        /// <summary>Максимальное количество киборгов</summary>
        public const int MaxCyborgs = 2;

        /// <summary>Максимальное количество гоблинов</summary>
        public const int MaxGoblins = 4;

        // ===== Правила игры =====
        /// <summary>Максимальное количество выстрелов за ход для любого юнита</summary>
        public const int MaxShotsPerTurn = 2;

        /// <summary>Дальность стрельбы для всего оружия (в гексах)</summary>
        public const int WeaponRange = 6;

        // ===== Киборги =====
        /// <summary>Максимальное здоровье каждой части тела киборга</summary>
        public const int CyborgBodyPartMaxHP = 3;

        /// <summary>Начальное количество патронов у киборга</summary>
        public const int CyborgStartAmmo = 6;

        /// <summary>Очки действия киборга за ход</summary>
        public const int CyborgActionPoints = 7;

        /// <summary>Минимум выпадения для поражения тела киборга</summary>
        public const int CyborgBodyPartMinRoll = 2;

        /// <summary>Максимум выпадения для поражения тела киборга</summary>
        public const int CyborgBodyPartMaxRoll = 6;

        // ===== Гоблины =====
        /// <summary>Максимальное здоровье каждой части тела гоблина</summary>
        public const int GoblinBodyPartMaxHP = 2;

        /// <summary>Начальное количество патронов у гоблина</summary>
        public const int GoblinStartAmmo = 4;

        /// <summary>Очки действия гоблина за ход</summary>
        public const int GoblinActionPoints = 5;

        /// <summary>Количество частей тела гоблина</summary>
        public const int GoblinBodyPartCount = 4;

        // ===== Стоимость действий (в ОД) =====
        /// <summary>Стоимость выстрела</summary>
        public const int ShootCost = 1;

        /// <summary>Стоимость рукопашной атаки</summary>
        public const int MeleeCost = 1;

        /// <summary>Стоимость подбора предмета</summary>
        public const int PickupItemCost = 1;

        /// <summary>Стоимость лечения/ремонта</summary>
        public const int HealRepairCost = 2;

        /// <summary>Стоимость использования электрополя</summary>
        public const int ElectricFieldCost = 1;

        /// <summary>Стоимость движения вперед для киборга</summary>
        public const int CyborgForwardMoveCost = 1;

        /// <summary>Стоимость движения назад для киборга</summary>
        public const int CyborgBackwardMoveCost = 2;

        /// <summary>Стоимость поворота киборга на одну грань гекса</summary>
        public const int CyborgTurnCost = 1;

        /// <summary>Стоимость движения гоблина в любую сторону</summary>
        public const int GoblinMoveCost = 1;
    }
}
