using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Tower_Defense_Game.GameObjekt;

namespace Tower_Defense_Game.GameObjekt
{
    public class Tower
    {
        public enum TowerType { Type1, Type2, Type3 }

        public TowerType Type { get; }

        public double CurrentX { get; set; }
        public double CurrentY { get; set; }
        public double Width { get; }
        public double Height { get; }

        public double Range { get; private set; }
        public double FireRate { get; private set; }
        public int Damage { get; private set; }

        private double _fireCooldown;
        public bool IsPlaced { get; set; }

        public Rectangle Sprite { get; }

        public Tower(double width, double height, TowerType type)
        {
            Width = width;
            Height = height;
            Type = type;

            Range = 100;
            FireRate = 1.0;
            Damage = 20;

            _fireCooldown = 0;
            IsPlaced = false;

            Sprite = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = type switch
                {
                    TowerType.Type1 => Brushes.Blue,
                    TowerType.Type2 => Brushes.Green,
                    TowerType.Type3 => Brushes.Red,
                    _ => Brushes.Gray
                }
            };
        }

        public void Update(double deltaTime)
        {
            if (!IsPlaced) return;
            _fireCooldown -= deltaTime;
        }

        public Enemy FindTarget(IEnumerable<Enemy> enemies)
        {
            if (!IsPlaced) return null;

            return enemies
                .Where(e => DistanceTo(e) <= Range)
                .OrderByDescending(e => e.GetProgress()) // <- hier richtig die Enemy-Methode nutzen
                .FirstOrDefault();
        }

        public void Shoot(Enemy target)
        {
            if (_fireCooldown > 0) return; // Noch nicht bereit
            if (target == null) return;

            target.TakeDamage(Damage);
            _fireCooldown = 1.0 / FireRate; // Timer zurücksetzen

            // --- VISUELLES FEEDBACK ---
            var originalBrush = Sprite.Fill;         // aktuelle Farbe merken
            Sprite.Fill = Brushes.Yellow;            // kurz gelb färben
            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 0,1 Sek sichtbar
            };
            dispatcherTimer.Tick += (s, e) =>
            {
                Sprite.Fill = originalBrush;           // Farbe zurücksetzen
                dispatcherTimer.Stop();
            };
            dispatcherTimer.Start();
        }

        public double DistanceTo(Enemy enemy)
        {
            double dx = (CurrentX + Width / 2) - enemy.E_x_pos; // <- Enemy X-Property
            double dy = (CurrentY + Height / 2) - enemy.E_y_pos; // <- Enemy Y-Property
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public void UpdatePosition()
        {
            Canvas.SetLeft(Sprite, CurrentX);
            Canvas.SetTop(Sprite, CurrentY);
        }

        public bool CheckCollision(IEnumerable<Tower> otherTowers, List<Point> pathPoints)
        {
            Rect myRect = new Rect(CurrentX, CurrentY, Width, Height);

            foreach (var tower in otherTowers)
            {
                if (tower == this) continue;
                Rect tRect = new Rect(tower.CurrentX, tower.CurrentY, tower.Width, tower.Height);
                if (myRect.IntersectsWith(tRect)) return true;
            }

            double padding = 10;
            foreach (var pt in pathPoints)
            {
                Rect pathRect = new Rect(pt.X - padding, pt.Y - padding, padding * 2, padding * 2);
                if (myRect.IntersectsWith(pathRect)) return true;
            }

            return false;
        }
    }
}



