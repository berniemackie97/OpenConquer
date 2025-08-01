using OpenConquer.Domain.Enums;

namespace OpenConquer.Domain.Entities
{
    public class LevelStat
    {
        public int ID { get; set; }
        public Profession Profession { get; set; }
        public byte Level { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Vitality { get; set; }
        public int Spirit { get; set; }
        public int Health { get; set; }
        public int Mana { get; set; }
    }
}
