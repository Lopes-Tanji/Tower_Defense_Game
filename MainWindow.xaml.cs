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
    public partial class MainWindow : Window
    {
        private GameStateModel _gameState;
        private Tower _draggedTower = null;
        private bool _isDragging = false;
        private MediaPlayer _backgroundMusic;
        public Tower SelectedTower { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _gameState = new GameStateModel();
            DataContext = _gameState;
            _backgroundMusic = new MediaPlayer();
            _backgroundMusic.Open(new Uri("images/CATHARSISTD.m4a", UriKind.Relative));
            _backgroundMusic.MediaEnded += (s, e) => _backgroundMusic.Position = TimeSpan.Zero;
            _backgroundMusic.Play();

            GameCanvas.MouseMove += GameCanvas_MouseMove;
            GameCanvas.MouseLeftButtonDown += GameCanvas_MouseLeftButtonDown;
            GameCanvas.MouseLeftButtonUp += GameCanvas_MouseLeftButtonUp;
        }

        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedTower != null && !_draggedTower.IsPlaced && _isDragging)
            {
                Point mousePos = e.GetPosition(GameCanvas);
                _draggedTower.CurrentX = mousePos.X - _draggedTower.Width / 2;
                _draggedTower.CurrentY = mousePos.Y - _draggedTower.Height / 2;
                _draggedTower.UpdatePosition();
            }
        }

        private void GameCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_draggedTower != null && !_draggedTower.IsPlaced)
                _isDragging = true;
        }

        private void GameCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void SpawnTower(Tower.TowerType type)
        {
            if (_draggedTower != null)
            {
                MessageBox.Show("Platziere den Turm bevor du einen neuen kaufst.");
                return;
            }
            // Größe hier anpassen (z. B. 72 statt 50)
            _draggedTower = new Tower(50, 50, type);
            GameCanvas.Children.Add(_draggedTower.Container);
            _isDragging = true;
        }

        private const int TowerCost = 50;

        private void PlaceTower_Click(object sender, RoutedEventArgs e)
        {
            if (_draggedTower != null)
            {
                if (_draggedTower.CheckCollision(_gameState.Towers, _gameState.PathPoints.ToList()))
                {
                    MessageBox.Show("Ungültige Platzierung! Turm überlappt andere Türme oder den Pfad.");
                    return;
                }

                if (_gameState.Gold < TowerCost)
                {
                    MessageBox.Show("Nicht genug Gold!");
                    return;
                }

                _gameState.Gold -= TowerCost;

                _draggedTower.IsPlaced = true;
                _gameState.Towers.Add(_draggedTower);
                _draggedTower = null;
                _isDragging = false;
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

        private void SpawnTower1_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.DroneOfDoom);
        private void SpawnTower2_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.DroneOfDemise);
        private void SpawnTower3_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.DroneOfDownfall);

        private void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GameStateModel model)
            {
                model.UpgradeSelectedTower();
            }
        }

        private void DeleteTower_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not GameStateModel model)
                return;

            var tower = model.SelectedTower;
            if (tower == null)
            {
                MessageBox.Show("Wähle zuerst einen Turm in der Liste aus.");
                return;
            }

            int refund = tower.Level switch
            {
                1 => 30,
                2 => 300,
                3 => 3000,
                _ => 0
            };

            TowerDeleter(tower); // gibt den Tower über SelectedTower an die Funktion TowerDeleter weiter und führt sie aus

            model.Gold += refund;
            model.SelectedTower = null;
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
                if (GameCanvas.Children.Contains(delTower.Container))
                    GameCanvas.Children.Remove(delTower.Container);
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



