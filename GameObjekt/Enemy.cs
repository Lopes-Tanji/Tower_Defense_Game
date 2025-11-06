using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Tower_Defense_Game.GameObjekt
{
    public class Enemy : INotifyPropertyChanged
    {
        // X und Y Possizionen der Enemys auf dem canvas
        private double _e_x_pos;

        private double _e_y_pos;

        
        public double E_x_pos
        {
            get => _e_x_pos;

            private set { _e_x_pos = value; OnPropertyChanged(); }
        }

        public double E_y_pos
        {
            get => _e_y_pos;

            private set { _e_y_pos = value; OnPropertyChanged(); }
        }

        //Lebenspunkte des Gegners MaxHP wird wenn die Zeit nicht reicht evt nicht gebraucht.
        public double HP { get; private set; }
        public double MaxHP { get; }

        // Geschwindigkeit (wie viele Pixel pro Sekunde)
        public double Speed { get; }

        // Der weg der Gegner
        private readonly GOPath _path;

        // Gold wert des Gegners
        public int Bounty { get; set; }

        // Wie weit auf dem weg ist der gegner Variable welche auf 0 gesetzt wird
        private double _distanceOnPath = 0;

        // Wenn die Leben 0 oder kleiner sind ist der gegner tot
        public bool IsDead => HP <= 0;

        // Wenn der gegner am ende ankommt hat er das Ende erreicht
        public bool ReachedEnd => _distanceOnPath >= _path.TotalLength;

        //Konstruktor Enemy
        public Enemy(GOPath path, double hp, double speed)
        {
            this._path = path;
            this.HP = MaxHP = hp; // Gegner starten mit 100% Leben deswegen ist HP und MaxHP am anfang gleich
            this.Speed = speed;

            var (x, y) = _path.PositionAt(0); // X und Y Koordinaten werden vom startpunkt von GOPath ausgelesen 

            // Ausgelesene X und Y Koordinaten werden bei den Properties angewendet
            this.E_x_pos = x; 

            this.E_y_pos = y;
        }

        // Methode zum Updaten der Enemy Position
        public void Update(double deltaTickTime)
        {
            // Ist der Gegner Tot oder hat das ende erreicht wird abgebrochen
            if(IsDead || ReachedEnd)
            {
                return;
            }

            // Posizion des Gegners auf dem weg wird Berechnet
            _distanceOnPath += Speed * deltaTickTime;

            var (x, y) = _path.PositionAt(_distanceOnPath); // X und Y Koordinaten werden von GOPath durch _distanceOnPath ausgelesen

            // Ausgelesene X und Y Koordinaten werden bei den Properties angewendet
            this.E_x_pos = x;

            this.E_y_pos = y;
        }

        // Gegner bekommt schaden
        public void TakeDamage(double damage)
        {
            HP -= damage;
        }

        // Dieses Event gehört zum INotifyPropertyChanged-Interface.
        // Es sorgt dafür, dass WPF erkennt, wenn sich ein Wert geändert hat.
        public event PropertyChangedEventHandler? PropertyChanged;
        // Ruft PropertyChanged aus, ohne den Namen manuell schreiben zu müssen.
        // [CallerMemberName] füllt den Namen der Property automatisch ein.
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
