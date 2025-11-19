using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
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

        
        // Index der aktuellen wave
        public int CurrentWave => _waves.CurrentIndex;
        // bool zum erkennen ob die aktuelle wave läuft
        public bool WaveRunning => _waves.IsRunning;

        // privater bool zum erkennen ob der startscreen sichtbar ist
        private bool _isStartScreen = true;

        // public bool welcher mit OnPropertyChange sich updated also UI
        public bool IsStartScreen
        {
            get => _isStartScreen;
            set
            {
                _isStartScreen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ContinueLabel)); // ContinueLabel neu melden, da es von IsStartScreen abhängt
            }
        }

        // privater bool um zu erkennen ob das spiel paussiert ist
        private bool _isPaused = true;
        // bool welcher geupdated wird(zwischen runden) also UI
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ContinueLabel)); // ContinueLabel neu melden, da es von IsPaused abhängt
            }
        }

        // privater bool um zu erkennen ob das spiel beendet wurde
        private bool _isEndScreen = false;
        // bool welcher das update der UI aufruft
        public bool IsEndScreen
        {
            get => _isEndScreen;
            set { _isEndScreen = value; OnPropertyChanged(); }
        }

        private string _continueLabel;
        // Aufschrift des Continue Buttons muss noch verändert werden damit sich das nach start noch verändert mit OnPropertyChanged()
        public string ContinueLabel
        {
            get => _continueLabel;
            set { _continueLabel = value; OnPropertyChanged(); }
        }

        // logik hinter dem Continue Button
        public void ContinueButton()
        {          
            // Wenn der startscreen sichtbar ist soll der startscreen nicht mehr sichtbar sein und das spiel soll pausiert werden
            // Das spiel soll zu beginn noch pausiert sein damit türme platziert werden können
            if (IsStartScreen)
            {
                IsStartScreen = false;
                IsPaused = true;

                return;
            }
            // Wenn die wave nicht läuft und es keine enemies gibt
            if (!_waves.IsRunning && Enemies.Count == 0)
            {
                // StartNextWave gibt einen bool zurück welcher dann mit IsPaused das spiel vortführt
                var started = _waves.StartNextWave();
                if (started)
                {
                    IsPaused = false;
                }
                else
                {
                    IsPaused = true;
                }

                // UI spezifisch aktualisiert
                OnPropertyChanged(nameof(CurrentWave));
                OnPropertyChanged(nameof(WaveRunning));
                return;
            }
            if (IsEndScreen) // Wenn der EndScreen sichtbar ist und der Continue Button gedrückt wurde
            {
                Enemies.Clear(); // Lösche alle noch Lebenden Enemies damit das Spiel nicht Freezed
                // RestartWaves(); funktion in WaveManager ausgelöst welcher ein bool zurück gibt.
                // Der zurück gegebene bool wird zwar nicht wirklich benötigt und war für eine ähnliche interaktion wie StartNextWave geplant
                var started = _waves.RestartWaves(); // Resetet die wave
                if(started)
                {
                    WaveAdder(); // Waves neu erstellt
                    Lives = 20; // Leben zurückgesetzt
                    Gold = 100; // Gold zurückgesetzt
                    IsStartScreen = false; // StartScreen auf fals gesetzt um sicher zu gehen
                    IsEndScreen = false; // Endscreen auf fals gesetzt um zu beenden 
                    IsPaused = true; // Es wird pausiert damit türme gekauft werden können
                }
                // UI spezifisch aktualisiert
                OnPropertyChanged(nameof(CurrentWave));
                OnPropertyChanged(nameof(WaveRunning));

            }


        }

        private Tower? _selectedTower;
        public Tower? SelectedTower
        {
            get => _selectedTower;
            set
            {
                if (_selectedTower != null)
                    _selectedTower.ResetHighlight();

                _selectedTower = value;

                if (_selectedTower != null)
                    _selectedTower.Highlight();

                OnPropertyChanged();
            }
        }

        public void UpgradeSelectedTower()
        {
            if (SelectedTower == null) return;

            int cost = SelectedTower.Level == 1 ? 10 : 40;

            if (Gold >= cost)
            {
                bool success = SelectedTower.Upgrade(); // tower upgrades itself
                if (success)
                {
                    Gold -= cost; // reduce coins
                }
            }
        }

        // Das Pfad-Objekt für die Gegnerbewegung
        public GOPath GamePath { get; }

        // Sammlung von Punkten, die WPF als Linienzug zeichnen kann
        public PointCollection PathPoints { get; }

        // Gegner-Liste für die UI
        //ObservableCollection<Enemy> Enemies war zu beginn List<Enemy> jedoch ist das Spiel gecrasht wenn der Test-Enemy gelöscht wurde.
        public ObservableCollection<Enemy> Enemies { get; } = new();

        public ObservableCollection<Tower> Towers { get; set; } = new ObservableCollection<Tower>();


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

        // Wird auf die klasse WaveManager zugegriffen
        private readonly WaveManager _waves;
        
        public void WaveAdder()
        {
            // 100 Waves werden erstellt die mit jeder Runde immer stärker werden
            for (int i = 1; i <= 101; i++)
            {
                _waves.AddWave(new Wave(10 * i, 20 * (2 + (i / 10)), 200 * (1 + (i / 10)), 0.5));
            }
        }
        // Konstruktor
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

            // Neuer WaveManager erstellt
            _waves = new WaveManager(Enemies, GamePath);

            // 100 Waves werden erstellt die mit jeder Runde immer stärker werden
            for (int i = 1; i <= 101; i++)
            {
                _waves.AddWave(new Wave(10 * i, 20 * (2 + (i / 10)), 80 * (1 + (i / 10)), 0.5));
            }
            // Wurde aus GameStateModel Konstruktor entfernt damit beim Neustart die Waves eifacher neu erstellt werden können
            WaveAdder();

            // Startscreen und IsPaused werden auf true gesetzt damit das Spiel nicht von anfang an läuft
            IsStartScreen = true;
            IsPaused = true;
            IsEndScreen = false;

            // GameLoop erstellen: ruft untenstehendes Update(deltaTickTime) auf
            _loop = new GameLoop(Update, fps: 30);
            _loop.Start();

            // Hinweis: Test-Enemy entfernt, damit das Wave-System korrekt startet.
        }

        // Diese Methode wird vom GameLoop aufgerufen (deltaTickTime = vergangene Sekunden seit letztem Tick)
        private void Update(double deltaTickTime)
        {
            // Update der beschriftung des Continue Button
            if(_isStartScreen) // Wenn der Start Screen zu sehen ist
            {
                ContinueLabel = "Spiel Starten"; // Dann Label = Spiel Starten
            }
            // Wenn es noch wavesgibt dir waves nicht mehr läuft und keine Gegner mehr leben 
            else if(_waves != null && !_waves.IsRunning && Enemies.Count == 0) 
            {
                ContinueLabel = "Nächste Welle starten"; // Dann Label = Nächste Welle starten
            }
            else if(_isPaused) // Wenn das Spiel Pausiert ist
            {
                ContinueLabel = "Weiter"; // Dann Label = Weiter
            }
            else if(_isEndScreen) // Wenn der EndScreen zu sehen ist
            {
                ContinueLabel = "Neustart"; // Dann Label = Neustart
            }
            else
            {
                ContinueLabel = "Weiter"; // Dann Label = Weiter
            }
            // Wenn pausiert ist oder der Startscreen noch da ist wird nicht geubdated also das spiel läuft nicht

            if (IsPaused || IsStartScreen || IsEndScreen)
            {
                return;
            }
            // Beweis dass es tickt: Zeit und Framezahl hochzählen
            ElapsedSeconds += deltaTickTime;
            FrameCount++;

            // Update in waves wird aufgerufen (? wenn _waves null ist)
            _waves?.Update(deltaTickTime);

            // - Gegner updaten
            // .ToList() damit nicht ObservableCollection<Enemy> direkt verändert wird somit crasht es nicht.
            foreach (var enemy in Enemies.ToList())
            {
                // Update Methode in Enemy.cs wird ausgeführt und somit die neue position von Enemy gesetzt
                enemy.Update(deltaTickTime);

                // Wenn der Gegner das ende erreicht dann wird ein Leben abgezogen und der Enemy verschwindet
                if (enemy.ReachedEnd)
                {
                    Lives--;
                    Enemies.Remove(enemy);
                }

            }
            // Es gab Probleme mit mehreren Gegner und ich weis nicht ob das wirklich was verändert hat
            // Jedoch funktioniert es jetzt
            // Wenn ein Gegner stirbt wird Bounty dem Gold hinzugefügt und enemy Verschwindet
            foreach (var enemy in Enemies.Where(x => x.IsDead).ToList())
            {
                Gold += enemy.Bounty;
                Enemies.Remove(enemy);
            }
            // Wenn dein leben 0 oder weniger ist bist du tot und gehst zum EndScreen
            if(Lives <= 0)
            {
                IsEndScreen = true;
            }

            // Wenn wave nicht läuft und keine enemies mehr leben wird pausiert
            if (!_waves.IsRunning && Enemies.Count == 0)
            {
                IsPaused = true;
            }
            foreach (var tower in Towers)
            {
                tower.Update(deltaTickTime); // Turm-Internes Update, z.B. Schuss-Timer hochzählen

                if (!tower.IsPlaced)
                    continue; // Nur platzierte Türme dürfen schießen

                var target = tower.FindTarget(Enemies);

                // rotate always when seeing an enemy
                tower.RotateTowards(target);

                if (target != null)
                    tower.Shoot(target);
            }


            foreach (var tower in Towers)
            {
                tower.Update(deltaTickTime); // Turm-Internes Update, z.B. Schuss-Timer hochzählen

                if (!tower.IsPlaced)
                {
                    continue; // Nur platzierte Türme dürfen schießen
                }
                       



            // - Türme updaten



            // - Projektile updaten
            // - Kollisions- / Treffer-Checks

                var target = tower.FindTarget(Enemies); // Sucht Gegner im Radius
                if (target != null)
                {
                    if(tower.Type == Tower.TowerType.Type2) // Wenn der aktuelle Tower typ 2 ist
                    {
                        switch(tower.Level)
                        {
                            case 1:
                                target.Slow(1);
                                break;
                            case 2:
                                target.Slow(2);
                                break;
                            case 3:
                                target.Slow(3);
                                break;
                            default:
                                break;
                        }
                         // Wird das getroffene ziel verlangsamt
                    }
                    tower.Shoot(target); // Gegner Schaden zufügen
                }
                    
            }

            OnPropertyChanged(nameof(WaveRunning));
            OnPropertyChanged(nameof(CurrentWave));

            
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