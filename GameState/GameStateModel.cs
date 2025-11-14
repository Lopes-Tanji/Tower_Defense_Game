using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Collections.ObjectModel;
using System.Windows.Controls;


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

        // Gegner-Liste für die UI
        //ObservableCollection<Enemy> Enemies war zu beginn List<Enemy> jedoch ist das Spiel gecrasht wenn der Test-Enemy gelöscht wurde.
        public ObservableCollection<Enemy> Enemies { get; } = new();

        // Speichert, wie viele Sekunden das Spiel bereits gelaufen ist.
        // Der Wert wird im Update()-Loop pro Tick erhöht.
        private double _elapsedSeconds;

        // Öffentliche Property, damit die UI (XAML) den Wert anzeigen kann.
        // Der "private set" verhindert, dass anderer Code diesen Wert verändern kann.
        public double ElapsedSeconds
        {
            // get => gibt den aktuellen Wert zurück (für UI-Binding)
            get => _elapsedSeconds;

            // private set => nur diese Klasse darf den Wert verändern
            // Wenn der Wert geändert wird, ruft OnPropertyChanged() die UI dazu auf,
            // die Anzeige zu aktualisieren.
            private set { _elapsedSeconds = value; OnPropertyChanged(); }
        }

        // Zählt, wie viele "Ticks" (Frames / Updates) bisher vergangen sind.
        // Wir nutzen long, weil diese Zahl im Laufe der Zeit sehr groß wird.
        private double _frameCount;
        // Öffentliche Property, damit die UI die Frameanzahl anzeigen kann.
        // Wird ähnlich wie oben aktualisiert.
        public double FrameCount
        {
            // Gibt den bisherigen Frame-Zähler an die UI zurück
            get => _frameCount;
            // Nur intern setzbar – UI soll nicht Werte verändern können!
            private set { _frameCount = value; OnPropertyChanged(); }
        }

        // GameLoop ist der "Motor" des Spiels.
        // Er ruft regelmäßig (z.B. 30x pro Sekunde) Update(deltaTickTime) auf.
        // readonly bedeutet: einmal beim Erstellen gesetzt → kann später nicht ersetzt werden.
        private readonly GameLoop _loop;

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

            // GameLoop erstellen: ruft untenstehendes Update(deltaTickTime) auf
            _loop = new GameLoop(Update, fps: 30);
            _loop.Start();

            // Test-Enemy erstellt
            Enemies.Add(new Enemy(GamePath, 100, 80, 30));
        }

        // Diese Methode wird vom GameLoop aufgerufen (deltaTickTime = vergangene Sekunden seit letztem Tick)
        private void Update(double deltaTickTime)
        {
            // Beweis dass es tickt: Zeit und Framezahl hochzählen
            ElapsedSeconds += deltaTickTime;
            FrameCount++;

            // HIER kommt die eigentliche Spiellogik hin:

            // - Gegner updaten
            // .ToList() damit nicht ObservableCollection<Enemy> direkt verändert wird somit crasht es nicht.
            foreach (var enemy in Enemies.ToList())
            {
                // Update Methode in Enemy.cs wird ausgeführt und somit die neue position von Enemy gesetzt
                enemy.Update(deltaTickTime);

                // Wenn der Gegner das ende erreicht dann wird ein Leben abgezogen und der Enemy verschwindet
                if(enemy.ReachedEnd)
                {
                    Lives--;
                    Enemies.Remove(enemy);
                    continue;
                }
                // Wenn der Gegner stirbt wird Bounty dem Gold hinzugefügt und enemy Verschwindet
                if(enemy.IsDead)
                {
                    Gold += enemy.Bounty;
                    Enemies.Remove(enemy);
                    continue;
                }

            }

            // - Türme updaten
            // - Projektile updaten
            // - Kollisions- / Treffer-Checks
            // - Aufräumen (tote Gegner/Projektile entfernen)
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
