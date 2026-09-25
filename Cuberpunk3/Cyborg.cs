using System;
using System.Collections.Generic;
using System.Text;

namespace Cuberpunk3
{
    class Cyborg
    {
        // ==== ПОЛЯ – характеристики киборга =====
        private int x, y;
        private int leftArmHP;           // рука с пулеметом
        private int rightArmHP;          // рука с ракетами
        private int headHP;              // командный блок
        private int legsHP;              // ноги
        private int ammo;
        private int armor;               // броня

        // ==== СВОЙСТВА – контролируемый доступ к данным ====
        public int X { get { return x; } }
        public int Y { get { return y; } }

        public int Ammo
        {
            get { return ammo; }
            set { ammo = (value < 0) ? 0 : value; }
        }

        // ==== КОНСТРУКТОР – инициализация при создании ====
        public Cyborg(int x, int y)
        {
            this.x = x;
            this.y = y;
            // на самом деле this в последующих полях не нужен
            this.leftArmHP = 3;      // у киборгов здоровье частей = 3
            this.rightArmHP = 3;
            this.headHP = 3;
            this.legsHP = 3;
            this.ammo = 6;           // у киборга больше патронов
            this.armor = 3;
        }
        // ==== МЕТОДЫ – действия, которые может выполнить гоблин ====
        // Публичные методы
        // Стрельба
        public void Shoot(int targetX, int targetY)
        {
            if (ammo <= 0)
            {
                Console.WriteLine("Патроны кончились!");
                return;
            }
            int distance = CalculateDistance(targetX, targetY);
            int dice = this.RollDice();
            if (dice >= distance)
            {
                Console.WriteLine($"Киборг на ({x}, {y}) попал! Бросок: {dice}, расстояние: {distance}");
                // А теперь нужно понять, в какую часть тела попали...
                // А еще проверить стены...
                int damageDice = this.RollDice();
                // Здесь будет логика урона...
                ammo--;
            }
            else
            {
                Console.WriteLine($"Киборг на ({x}, {y}) промахнулся. Бросок: {dice}, нужно: {distance}");
            }
        }
        // Движение
        public void Move(int newX, int newY)
        {
            x = newX;
            y = newY;
            Console.WriteLine($"Киборг переместился на ({x}, {y})");
        }
        // Приватные методы
        // Приватный метод вычисления расстояния
        private int CalculateDistance(int targetX, int targetY)
        {
            return Math.Abs(x - targetX) + Math.Abs(y - targetY);
        }

        // Бросок кубика
        private Random random = new Random();
        private int RollDice()
        {
            return random.Next(1, 7);  // от 1 до 6
        }
    }

}
