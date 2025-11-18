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

namespace Tower_Defense_Game
{
    public partial class MainWindow : Window
    {
        private GameStateModel _gameState;
        private Tower _draggedTower = null;
        private bool _isDragging = false;
        private MediaPlayer _backgroundMusic;
        public int Coins { get; set; } = 10000;      // Example starting coins
        public Tower SelectedTower { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // GameStateModel instanziieren
            _gameState = new GameStateModel();
            DataContext = _gameState;
            _backgroundMusic = new MediaPlayer();
            _backgroundMusic.Open(new Uri("images/TDMusic.mp3", UriKind.Relative));
            _backgroundMusic.MediaEnded += (s, e) => _backgroundMusic.Position = TimeSpan.Zero; // Loop
            _backgroundMusic.Play();


            // Events für Drag & Drop
            GameCanvas.MouseMove += GameCanvas_MouseMove;
            GameCanvas.MouseLeftButtonDown += GameCanvas_MouseLeftButtonDown;
            GameCanvas.MouseLeftButtonUp += GameCanvas_MouseLeftButtonUp;

            // Gegner müssen nicht mehr als Sprite hinzugefügt werden,
            // weil sie bereits über E_x_pos / E_y_pos gezeichnet werden.
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
            // Startposition oben links, kann mit Maus verschoben werden
            _draggedTower = new Tower(50, 50, type);
            GameCanvas.Children.Add(_draggedTower.Container);
            _isDragging = true;
        }

        private const int TowerCost = 50; // Kosten pro Turm

        private void PlaceTower_Click(object sender, RoutedEventArgs e)
        {
            if (_draggedTower != null)
            {
                // Prüfen, ob der Turm auf anderen Türmen oder auf dem Pfad liegt
                if (_draggedTower.CheckCollision(_gameState.Towers, _gameState.PathPoints.ToList()))
                {
                    MessageBox.Show("Ungültige Platzierung! Turm überlappt andere Türme oder den Pfad.");
                    return;
                }

                // Prüfen, ob genug Gold vorhanden ist
                if (_gameState.Gold < TowerCost)
                {
                    MessageBox.Show("Nicht genug Gold!");
                    return;
                }

                // Gold abziehen
                _gameState.Gold -= TowerCost;

                // Turm platzieren
                _draggedTower.IsPlaced = true;
                _gameState.Towers.Add(_draggedTower);
                _draggedTower = null;
                _isDragging = false;
            }
        }

        // Wenn der Button geklickt wird wird ContinueButton() ausgelöst
        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as GameStateModel)?.ContinueButton();
        }

        // Button-Events für Turmspawns
        private void SpawnTower1_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.Type1);
        private void SpawnTower2_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.Type2);
        private void SpawnTower3_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.Type3);

        // Make sure this is set via your tower selection

        private void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GameStateModel model)
            {
                model.UpgradeSelectedTower();
            }
        }


    }
}



