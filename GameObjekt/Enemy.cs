using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Tower_Defense_Game.GameObjekt
{
    public class Enemy : INotifyPropertyChanged
    {
        // globaler Multiplikator zum schnellen Anpassen aller Enemy-Geschwindigkeiten

        // X und Y Possizionen der Enemys auf dem canvas
        private double _e_x_pos;
        private double _e_y_pos;
        private double _rotation;

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

        // Neue Property: Rotation in Grad (0 = nach rechts)
        public double Rotation
        {
            get => _rotation;
            private set { _rotation = value; OnPropertyChanged(); }
        }

        //Lebenspunkte des Gegners MaxHP wird wenn die Zeit nicht reicht evt nicht gebraucht.
        public double HP { get; private set; }
        public double MaxHP { get; }

        // Geschwindigkeit (wie viele Pixel pro Sekunde) wirde noch vorbereitet für Slows von Turm 2
        public double Speed { get; set; }

        // Ist der gegner verlangsamt bool
        public bool IsSlowed { get; set; } = false;

        // Ein int wo wenn verlangsamt wird hoch zählt bis eine zahl erreicht wird(sec * 30)
        public int SlowTimer { get; set; } = 0;

        
        public int EffSlowTime { get; set; } 
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
        public Enemy(GOPath path, double hp, double speed, int bounty)
        {
            this._path = path;
            this.HP = MaxHP = hp;
            this.Speed = speed;
            this.Bounty = bounty;

            var (x, y) = _path.PositionAt(0);
            this.E_x_pos = x;
            this.E_y_pos = y;

            // Initiale Rotation setzen
            UpdateRotationAtDistance(0);
        }

        // Methode zum Updaten der Enemy Position
        public void Update(double deltaTickTime)
        {
            // Ist der Gegner Tot oder hat das ende erreicht wird abgebrochen
            if(IsDead || ReachedEnd)
            {
                return;
            }

            if(IsSlowed) // Wenn der Enemy verlangsamt ist
            {
                if(SlowTimer == 0) // Wenn erster verlangsamter tick ist
                {
                    Speed /= 2; // Wird die geschwindigkeit durch 2 gerechnet
                    SlowTimer++;
                }
                else
                {
                    SlowTimer++; // Frame wird hochgezählt
                }
                

                if(SlowTimer >= 30 * EffSlowTime) // Wenn die Slow zeit erreicht wurde
                {
                    Speed *= 2; // Speed wieder Normal
                    SlowTimer = 0; // SlowTime wird zurückgesetzt
                    IsSlowed = false; // Enemy ist nicht mehr verlangsamt
                }
            }

            // Posizion des Gegners auf dem weg wird Berechnet
            _distanceOnPath += Speed * deltaTickTime; // deltaTickTime in Sekunden
            var (x, y) = _path.PositionAt(_distanceOnPath); // Neue Position auf dem weg
            this.E_x_pos = x; // X pos setzen
            this.E_y_pos = y; // Y pos setzen

            // Rotation anhand Tangente berechnen
            UpdateRotationAtDistance(_distanceOnPath);
        }

        private void UpdateRotationAtDistance(double distance) // Berechnet die Rotation des Gegners basierend auf seiner Position auf dem Pfad
        {
            // Kleinen Schritt vorwärts zum Berechnen der Tangentenrichtung
            double step = Math.Max(1.0, Speed * 0.05); // Mindestens 1 Pixel oder 5% der Geschwindigkeit
            double next = Math.Min(distance + step, _path.TotalLength); // Nächste Position auf dem Pfad

            var (x1, y1) = _path.PositionAt(distance); // Aktuelle Position
            var (x2, y2) = _path.PositionAt(next); // Nächste Position

            double dx = x2 - x1; // Delta X
            double dy = y2 - y1; // Delta Y

            if (Math.Abs(dx) < 1e-6 && Math.Abs(dy) < 1e-6) return; // Keine Bewegung, Rotation nicht ändern

            // Atan2 -> Grad, 0° = rechts, +Y nach unten (WPF Koordinaten)
            double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;

            // Offset +90° damit Sprite um 90° nach rechts verschoben ist
            angle += 90.0;

            Rotation = angle; // Rotation setzen
        }

        // Gegner bekommt schaden
        public void TakeDamage(double damage)
        {
            HP -= damage;
        }

        public void Slow(int zeit)
        {
            EffSlowTime = zeit;
            IsSlowed = true;
        }

        // Dieses Event gehört zum INotifyPropertyChanged-Interface.
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public double GetProgress() => _distanceOnPath; // Gibt den Fortschritt des Gegners auf dem Pfad zurück
    }
}
