using System;
using System.Collections.Generic;
using System.Text;

namespace Cuberpunk3
{
    class Goblin
    {
        // ==== ПОЛЯ – характеристики гоблина ====
        private int x, y;                // координаты на поле
        private int leftArmHP;           // здоровье левой руки
        private int rightArmHP;          // здоровье правой руки
        private int headHP;              // здоровье головы
        private int legsHP;              // здоровье ног
        private int ammo;                // патроны

        // ==== СВОЙСТВА – контролируемый доступ к данным ====
        // Свойства только для чтения, есть get нет set
        public int X { get { return x; } }
        public int Y { get { return y; } }
        // Свойство с проверкой – патроны не могут быть отрицательными
        public int Ammo
        {
            get { return ammo; }
            set { ammo = (value < 0) ? 0 : value; }
        }
        // ==== КОНСТРУКТОР – инициализация при создании ====
        public Goblin(int x, int y)
        {
            this.x = x;          // this.x - поле класса, x - параметр
            this.y = y;          // this.y - поле класса, y - параметр
            this.leftArmHP = 2;  // у всех гоблинов здоровье = 2
            this.rightArmHP = 2;
            this.headHP = 2;
            this.legsHP = 2;
            this.ammo = 4;       // начальные патроны
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
                Console.WriteLine($"Гоблин на ({x}, {y}) попал! Бросок: {dice}, расстояние: {distance}");
                // А теперь нужно понять, в какую часть тела попали...
                // А еще проверить стены...
                int damageDice = this.RollDice();
                // Здесь будет логика урона...
                ammo--;
            }
            else
            {
                Console.WriteLine($"Гоблин на ({x}, {y}) промахнулся. Бросок: {dice}, нужно: {distance}");
            }
        }
        // Движение
        public void Move(int newX, int newY)
        {
            x = newX;
            y = newY;
            Console.WriteLine($"Гоблин переместился на ({x}, {y})");
        }

        // Приватные методы
        // Вычисление расстояния до цели
        // Доступен только внутри класса Goblin
        private int CalculateDistance(int targetX, int targetY)
        {
            // Упрощенное вычисление (Манхэттенское расстояние)
            return Math.Abs(x - targetX) + Math.Abs(y - targetY);
        }

        // Бросок кубика
        // Создаем поле random, которое является экземпляром класса генератора псевдослучайных чисел Random (мы вызываем конструктор Random() используя ключевое слово new) 
        private Random random = new Random();
        private int RollDice()
        {
            return random.Next(1, 7);  // от 1 до 6
        }
    }

}
