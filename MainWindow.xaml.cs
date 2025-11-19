using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tower_Defense_Game.GameObjekt;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tower_Defense_Game.GameLogic;
using Tower_Defense_Game.GameState;
using System.Collections.ObjectModel;

namespace Tower_Defense_Game
{
    public partial class MainWindow : Window // Hauptfenster der Anwendung
    {
        private GameStateModel _gameState; // Das Spielzustandsmodell
        private Tower _draggedTower = null; // Der aktuell gezogene Turm
        private bool _isDragging = false; // Ob ein Turm gezogen wird
        private MediaPlayer _backgroundMusic; // Hintergrundmusik-Player
        public Tower SelectedTower { get; set; } // Der aktuell ausgewählte Turm

        public MainWindow() // Konstruktor der MainWindow Klasse
        {
            InitializeComponent(); // Initialisiert die UI-Komponenten

            _gameState = new GameStateModel(); // Erstellt ein neues Spielzustandsmodell
            DataContext = _gameState; // Setzt den DataContext für Datenbindung
            _backgroundMusic = new MediaPlayer(); // Erstellt einen neuen MediaPlayer für die Hintergrundmusik
            _backgroundMusic.Open(new Uri("images/CATHARSISTD.m4a", UriKind.Relative)); // Lädt die Musikdatei
            _backgroundMusic.MediaEnded += (s, e) => _backgroundMusic.Position = TimeSpan.Zero; // Schleife der Musik
            _backgroundMusic.Play(); // Startet die Wiedergabe der Musik

            GameCanvas.MouseMove += GameCanvas_MouseMove; // Ereignishandler für Mausbewegungen
            GameCanvas.MouseLeftButtonDown += GameCanvas_MouseLeftButtonDown; // Ereignishandler für linke Maustaste gedrückt
            GameCanvas.MouseLeftButtonUp += GameCanvas_MouseLeftButtonUp; // Ereignishandler für linke Maustaste losgelassen
        }

        private void GameCanvas_MouseMove(object sender, MouseEventArgs e) // Ereignishandler für Mausbewegungen
        {
            if (_draggedTower != null && !_draggedTower.IsPlaced && _isDragging) // Wenn ein Turm gezogen wird
            {
                Point mousePos = e.GetPosition(GameCanvas); // Position der Maus auf dem Canvas
                _draggedTower.CurrentX = mousePos.X - _draggedTower.Width / 2; // Zentriert den Turm auf die Mausposition in X-Richtung
                _draggedTower.CurrentY = mousePos.Y - _draggedTower.Height / 2; // Zentriert den Turm auf die Mausposition in Y-Richtung
                _draggedTower.UpdatePosition(); // Aktualisiert die Position des Turms
            }
        }

        private void GameCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) // Ereignishandler für linke Maustaste gedrückt
        {
            if (_draggedTower != null && !_draggedTower.IsPlaced) // Wenn ein Turm gezogen wird
                _isDragging = true; // Setzt den Ziehstatus auf wahr    
        }

        private void GameCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) // Ereignishandler für linke Maustaste losgelassen
        {
            _isDragging = false; // Setzt den Ziehstatus auf falsch
        }

        private void SpawnTower(Tower.TowerType type) // Methode zum Erzeugen eines neuen Turms
        {
            if (_draggedTower != null) // Wenn bereits ein Turm gezogen wird
            {
                MessageBox.Show("Platziere den Turm bevor du einen neuen kaufst."); // Zeigt eine Nachricht an
                return; // Beendet die Methode
            }
            _draggedTower = new Tower(50, 50, type); // Erstellt einen neuen Turm des angegebenen Typs
            GameCanvas.Children.Add(_draggedTower.Container); // Fügt den Turm dem Canvas hinzu
            _isDragging = true; // Setzt den Ziehstatus auf wahr
        }

        private const int TowerCost = 50; // Kosten für das Platzieren eines Turms

        private void PlaceTower_Click(object sender, RoutedEventArgs e) // Ereignishandler für das Platzieren eines Turms
        {
            if (_draggedTower != null) // Wenn ein Turm gezogen wird
            {
                if (_draggedTower.CheckCollision(_gameState.Towers, _gameState.PathPoints.ToList())) // Überprüft Kollisionen mit anderen Türmen und dem Pfad
                {
                    MessageBox.Show("Ungültige Platzierung! Turm überlappt andere Türme oder den Pfad."); // Zeigt eine Nachricht an
                    return; // Beendet die Methode
                }

                if (_gameState.Gold < TowerCost) // Überprüft, ob genug Gold vorhanden ist
                {
                    MessageBox.Show("Nicht genug Gold!"); // Zeigt eine Nachricht an
                    return; // Beendet die Methode
                }

                _gameState.Gold -= TowerCost; // Zieht die Kosten vom Gold ab

                _draggedTower.IsPlaced = true; // Markiert den Turm als platziert
                _gameState.Towers.Add(_draggedTower); // Fügt den Turm der Liste der Türme im Spielzustandsmodell hinzu
                _draggedTower = null; // Setzt den gezogenen Turm auf null
                _isDragging = false; // Setzt den Ziehstatus auf falsch
            }
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            if(_gameState.IsEndScreen) // Wenn der endScreen angezeigt wird
            {
                foreach (var delTower in _gameState.Towers) // wird jeder Tower in ObservableCollection<Tower> Towers Visuell gelöscht
                {
                    TowerDeleter(delTower); // gibt den tower von Tower an die Funktion TowerDeleter weiter und führt sie aus
                }
            }
            // ContinueButton ist unterhalb der Schleife damit die Towers zuerst visuel gelöscht werden können
            (DataContext as GameStateModel)?.ContinueButton();
        }

        private void SpawnTower1_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.DroneOfDoom); // Ereignishandler für das Erzeugen eines Turms des Typs DroneOfDoom
        private void SpawnTower2_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.DroneOfDemise); // Ereignishandler für das Erzeugen eines Turms des Typs DroneOfDemise
        private void SpawnTower3_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.DroneOfDownfall); // Ereignishandler für das Erzeugen eines Turms des Typs DroneOfDownfall

        private void UpgradeButton_Click(object sender, RoutedEventArgs e) // Ereignishandler für das Upgraden eines Turms
        {
            if (DataContext is GameStateModel model) // Überprüft, ob der DataContext ein GameStateModel ist
            {
                model.UpgradeSelectedTower(); // Ruft die UpgradeSelectedTower-Methode im Spielzustandsmodell auf
            }
        }

        private void DeleteTower_Click(object sender, RoutedEventArgs e) // Ereignishandler für das Löschen eines Turms
        {
            if (DataContext is not GameStateModel model) // Überprüft, ob der DataContext ein GameStateModel ist
                return; // Beendet die Methode, wenn nicht dann weiter

            var tower = model.SelectedTower; // Holt den ausgewählten Turm aus dem Spielzustandsmodell
            if (tower == null) // Überprüft, ob kein Turm ausgewählt ist
            {
                MessageBox.Show("Wähle zuerst einen Turm in der Liste aus."); // Zeigt eine Nachricht an
                return; // Beendet die Methode
            }

            int refund = tower.Level switch // Bestimmt die Rückerstattung basierend auf dem Turmlevel
            {
                1 => 30,
                2 => 300,
                3 => 3000,
                _ => 0
            };

            TowerDeleter(tower); // gibt den Tower über SelectedTower an die Funktion TowerDeleter weiter und führt sie aus

            model.Gold += refund; // Fügt das Gold basierend auf der Rückerstattung hinzu
            model.SelectedTower = null; // Setzt den ausgewählten Turm auf null
        }

        // gleiche Funktioen zum Visuelen löschen der Türme Wie in Delete ausser der ursprung von wo der Tower geholt wird 
        public void TowerDeleter(Tower delTower) // Funktion zum visuellen löschen des Towers welcher den Tower der klasse Tower benötigt
        {
            // Aufräumen im Tower (CTS stoppen)
            // Es wird versucht auf die Dispose Funktion in Tower zuzugreiffen 
            try { delTower.Dispose(); } catch { }

            // UI-Entfernung auf Dispatcher ausführen (sicher)
            Dispatcher.Invoke(() =>
            {
                if (GameCanvas.Children.Contains(delTower.Container)) // Überprüfung ob der Tower Container noch im Canvas ist
                    GameCanvas.Children.Remove(delTower.Container); // Entfernen des Tower Containers aus dem Canvas
            });

            // Es wird überprüft ob der selected Tower null ist
            // Für den restart delete wird das nicht benötigt da es im GameStateModel gelöscht wird wenn die Schleife beendet ist
            if (_gameState.SelectedTower != null) 
            {
                _gameState.Towers.Remove(delTower); // Tower wird aus ObservableCollection<Tower> Towers gelöscht
            }
        }
    }
}



