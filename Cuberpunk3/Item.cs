namespace Cyberpunk3
{
    /// <summary>
    /// Перечисление типов.
    /// </summary>
    public enum ItemType { Ammo, Mine }

    /// <summary>Итем на игровом поле.</summary>
    public class Item
    {
        // ==== СВОЙСТВА ====
        /// <summary>Координаты итема на сетке.</summary>
        public int Q { get; private set; }
        public int R { get; private set; }
        /// <summary>Тип итема.</summary>
        public ItemType Type { get; private set; }
        /// <summary>Подобран или нет.</summary>
        public bool IsPickedUp { get; private set; }


        // ==== КОНСТРУКТОР ====
        /// <param name="type">Тип стены</param>
        public Item(int q, int r, ItemType type)
        {
            Q = q; R = r; Type = type;
            IsPickedUp = false;
        }


        // ==== МЕТОДЫ ====
        /// <summary>
        /// Поднимаем итем.
        /// </summary>
        public void PickUp() { IsPickedUp = true; }

        /// <summary>
        /// Вывод информации об итеме.
        /// </summary>
        public string ItemToString()
        {
            string status = IsPickedUp ? " (подобран)" : "";
            return (Type == ItemType.Ammo ? "Боекомплект" : "Мина") + $" на ({Q},{R}){status}";
        }
    }
}
