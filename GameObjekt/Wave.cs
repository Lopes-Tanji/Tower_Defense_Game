using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tower_Defense_Game.GameObjekt
{
    // klasse Waves
    public class Wave
    {
        // Anzahl Enemies in der Wave
        public int Count { get; set; }
        // Leben der Gegner in der Wave
        public double Hp { get; set; }
        // Geschwindigkeit der gegner in der Wave
        public double Speed { get; set; }
        // Die Spawnrate: wie lange bis der nächste gegner gespawned wird
        public double Interval { get; set; }

        // Konstriktor von Wave
        public Wave(int count, double hp, double speed, double interval)
        {
            this.Count = count;
            this.Hp = hp;
            this.Speed = speed;
            this.Interval = interval;
        }
    }
}
