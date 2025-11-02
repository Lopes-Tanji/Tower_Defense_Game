using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using Tower_Defense_Game.GameLogic;
using Tower_Defense_Game.GameObjekt;

namespace Tower_Defense_Game.GameState
{
    // Das GameStateModel ist das Bindeglied zwischen Spiel-Logik und UI.
    // Alles, was die Benutzeroberfläche anzeigen soll (Leben, Gold, Türme, Gegner)
    // wird hier gespeichert.
    public class GameStateModel : INotifyPropertyChanged
    {
        // Interne Felder zum Speichern der Werte Leben und Gold
        private int _lives = 20;
        private int _gold = 100;

        // Öffentliche Properties, die von der UI über Binding ausgelesen werden.
        // Wenn sich der Wert ändert -> UI aktualisiert sich automatisch.
        public int Lives
        {
            get => _lives;
            set { _lives = value; OnPropertyChanged(); } // UI informieren
        }

        public int Gold
        {
            get => _gold;
            set { _gold = value; OnPropertyChanged(); } // UI informieren
        }

        // Das Pfad-Objekt für die Gegnerbewegung
        public GOPath GamePath { get; }

        // Sammlung von Punkten, die WPF als Linienzug zeichnen kann
        public PointCollection PathPoints { get; }

        public GameStateModel()
        {
            // Hole Beispielpfad
            GamePath = Path_Map1.SimplePath();

            // Wandelt die Wegpunkte vom Spiel in WPF-Points um,
            // damit Polyline sie anzeigen kann
            var pc = new PointCollection();
            foreach (var p in GamePath.Points)
                pc.Add(new System.Windows.Point(p.X, p.Y));

            PathPoints = pc; // Jetzt kann XAML darauf binden
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
