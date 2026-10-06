namespace Cyberpunk3
{
    /// <summary>
    /// Класс для генерации случайных чисел (бросков кубика).
    /// Все члены статические - не нужно создавать экземпляр класса.
    /// </summary>
    public static class DiceRoller
    {
        // ===== ПОЛЯ =====
        /// <summary>
        /// Статическое поле - единственный генератор для всей игры.
        /// </summary>
        private static Random random;


        // ===== КОНСТРУКТОР =====
        /// <summary>
        /// Статический конструктор - вызывается один раз при первом обращении к классу.
        /// </summary>        
        static DiceRoller()
        {
            random = new Random();
            Console.WriteLine("[DiceRoller] Генератор случайных чисел инициализирован");
        }


        // ===== МЕТОДЫ =====
        /// <summary>
        /// Бросок одного шестигранного кубика (1-6).
        /// </summary>
        public static int Roll()
        {
            return random.Next(1, 7); // от 1 до 6 включительно
        }

        /// <summary>
        /// Бросок кубика с выводом результата в консоль.
        /// </summary>
        public static int RollWithLog(string context = "")
        {
            int result = Roll();
            if (!string.IsNullOrEmpty(context))
                Console.WriteLine($"[Кубик] {context}: выпало {result}");
            else
                Console.WriteLine($"[Кубик] Выпало {result}");

            return result;
        }

        /// <summary>
        /// Бросок кубика с модификатором (результат = бросок - модификатор).
        /// Используется для расчета урона с учетом брони.
        /// </summary>
        public static int RollWithModifier(int modifier)
        {
            int result = Roll() - modifier;
            return result < 1 ? 1 : result; // минимум 1
        }

        /// <summary>
        /// Проверка успеха: бросок >= целевое число.
        /// </summary>
        public static bool CheckSuccess(int targetNumber)
        {
            int result = Roll();
            return result >= targetNumber;
        }
    }
}
