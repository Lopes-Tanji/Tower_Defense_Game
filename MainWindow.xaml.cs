using System.Collections.Generic;
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
        private List<Tower> _towers = new();
        private Tower _draggedTower = null;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new GameStateModel();

            // Events für Drag & Drop
            GameCanvas.MouseMove += GameCanvas_MouseMove;
            GameCanvas.MouseLeftButtonDown += GameCanvas_MouseLeftButtonDown;
            GameCanvas.MouseLeftButtonUp += GameCanvas_MouseLeftButtonUp;
        }

        private bool _isDragging = false;

        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedTower != null && !_draggedTower.IsPlaced && _isDragging)
            {
                Point mousePos = e.GetPosition(GameCanvas);
                _draggedTower.CurrentX = mousePos.X;
                _draggedTower.CurrentY = mousePos.Y;
                _draggedTower.UpdatePosition();
            }
        }

        private void GameCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_draggedTower != null && !_draggedTower.IsPlaced)
            {
                _isDragging = true;
            }
        }

        private void GameCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void SpawnTower(Tower.TowerType type)
        {
            Tower tower = new Tower(100, 100, type); // initiale Position
            GameCanvas.Children.Add(tower.Sprite);
            _draggedTower = tower;
        }

        private void SpawnTower1_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.Type1);
        private void SpawnTower2_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.Type2);
        private void SpawnTower3_Click(object sender, RoutedEventArgs e) => SpawnTower(Tower.TowerType.Type3);

        private void PlaceTower_Click(object sender, RoutedEventArgs e)
        {
            if (_draggedTower != null)
            {
                _draggedTower.IsPlaced = true;
                _towers.Add(_draggedTower);
                _draggedTower = null;
                _isDragging = false;
            }
        }

        // Wenn der Button geklickt wird wird ContinueButton() ausgelöst
        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as GameStateModel)?.ContinueButton();
        }
    }
}


