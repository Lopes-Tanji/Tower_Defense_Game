using System.Windows.Media;
using System.Windows.Shapes;

namespace Tower_Defense_Game.GameObjekt
{
    internal class Tower
    {
        // --- Basic Properties ---
        public double X { get; set; }
        public double Y { get; set; }

        public double Range { get; set; } = 120; // Range in pixels
        public double FireRate { get; set; } = 1.0; // shots per second
        public int Damage { get; set; } = 10; // base damage per shot
        public int Cost { get; } = 30; // cost to build the tower with gold coins

        // --- Internal cooldown timer ---
        private double _cooldown = 0;

        // --- Visual representation ---
        public Rectangle Sprite { get; }

        // --- Drag/Placement ---
        public bool IsPlaced { get; set; } = false;
        public double CurrentX { get; set; }
        public double CurrentY { get; set; }

        // --- Tower type enum ---
        public enum TowerType { Type1, Type2, Type3 }
        public TowerType Type { get; }

        public Tower(double x, double y, TowerType type)
        {
            X = x;
            Y = y;
            CurrentX = x;
            CurrentY = y;
            Type = type;

            Sprite = new Rectangle
            {
                Width = 30,
                Height = 30,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = type switch
                {
                    TowerType.Type1 => Brushes.DarkCyan,
                    TowerType.Type2 => Brushes.Orange,
                    TowerType.Type3 => Brushes.MediumPurple,
                    _ => Brushes.Gray
                }
            };

            UpdatePosition();
        }

        public void Update(double deltaTime)
        {
            if (_cooldown > 0)
                _cooldown -= deltaTime;

            // TODO: enemy targeting & shooting
        }

        public bool CanFire => _cooldown <= 0;

        public void ResetCooldown() => _cooldown = 1.0 / FireRate;

        public void UpdatePosition()
        {
            System.Windows.Controls.Canvas.SetLeft(Sprite, CurrentX - Sprite.Width / 2);
            System.Windows.Controls.Canvas.SetTop(Sprite, CurrentY - Sprite.Height / 2);
        }
    }
}
