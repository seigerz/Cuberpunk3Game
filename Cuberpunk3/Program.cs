using Cuberpunk3;

Console.WriteLine("=== Создание юнитов ===\n");
// Создаём киборга – 1 объект вместо 8 переменных!
Cyborg cyborg = new Cyborg(0, 0);
// Создаём двух гоблинов – 2 объекта вместо 10 переменных
Goblin goblin1 = new Goblin(1, 2);
Goblin goblin2 = new Goblin(2, 2);

Console.WriteLine("\n=== Начало боя ===\n");
// Киборг стреляет в первого гоблина
cyborg.Shoot(goblin1.X, goblin1.Y);

// Первый гоблин отвечает
goblin1.Shoot(cyborg.X, cyborg.Y);
// Второй гоблин тоже стреляет
goblin2.Shoot(cyborg.X, cyborg.Y);

Console.WriteLine("\n=== Движение ===\n");
// Киборг двигается
cyborg.Move(1, 0);

// Гоблины преследуют
goblin1.Move(2, 1);
goblin2.Move(1, 2);

Console.WriteLine("\n=== Еще выстрелы ===\n");

// Киборг снова стреляет
cyborg.Shoot(goblin1.X, goblin1.Y);

// Проверка патронов
Console.WriteLine($"\nПатроны киборга: {cyborg.Ammo}");
Console.WriteLine($"Патроны гоблина 1: {goblin1.Ammo}");
Console.WriteLine($"Патроны гоблина 2: {goblin2.Ammo}");
