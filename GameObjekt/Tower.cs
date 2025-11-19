using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Tower_Defense_Game.GameObjekt;

namespace Tower_Defense_Game.GameObjekt
{
    public class Tower : INotifyPropertyChanged
    {
        public enum TowerType { DroneOfDoom, DroneOfDemise, DroneOfDownfall }

        public TowerType Type { get; }

        public int Level { get; private set; } = 1; // NEW: tower level, starts at 1

        public double CurrentX { get; set; }
        public double CurrentY { get; set; }
        public double Width { get; }
        public double Height { get; }
        public string Name { get; set; }
        public double Range { get; private set; }
        public double FireRate { get; private set; }
        public int Damage { get; private set; }

        private double _fireCooldown;
        public bool IsPlaced { get; set; }

        private Image _sprite;
        private System.Threading.CancellationTokenSource? _flashCts;

        // Transform für kurzen visuellen Effekt, falls kein Flash-Bild vorhanden
        private readonly RotateTransform _rotateTransform = new RotateTransform();
        private readonly ScaleTransform _flashScaleTransform = new ScaleTransform(1, 1);

        public Image Sprite
        {
            get => _sprite;
            set
            {
                _sprite = value; OnPropertyChanged();
            }
        }
        public Border Container { get; }
        public ImageSource OriginalSource { get; private set; }
        public ImageSource HighlightSource { get; private set; }
        public ImageSource FlashSource { get; private set; }

        // NEW: tower images per type & level
        private static readonly Dictionary<TowerType, string[]> TowerImages = new()
        {
            
            { TowerType.DroneOfDoom, new[] { "images/Green1.png", "images/Green2.png", "images/Green3.png" } },
            { TowerType.DroneOfDemise, new[] { "images/Blue1.png", "images/Blue2.png", "images/Blue3.png" } },
            { TowerType.DroneOfDownfall, new[] { "images/Red1.png", "images/Red2.png", "images/Red3.png" } } 
        };

        // NEW: tower stats per type & level
        private static readonly Dictionary<TowerType, (int damage, double range, double fireRate)[]> TowerStats = new()
        {
            { TowerType.DroneOfDoom, new (int, double, double)[] { (40, 100, 1.0), (90, 120, 1.5), (140, 150, 2) } },
            { TowerType.DroneOfDemise, new (int, double, double)[] { (20, 100, 1.0), (70, 130, 1.2), (120, 200, 1.4) } },
            { TowerType.DroneOfDownfall, new (int, double, double)[] { (20, 100, 1.0), (50, 120, 1.75), (80, 140, 2.5) } },
        };

        // image cache to avoid reload problems
        private static readonly Dictionary<string, BitmapImage> _imageCache = new();

        private static BitmapImage LoadBitmap(string relativePath)
        {
            if (_imageCache.TryGetValue(relativePath, out var cached))
                return cached;

            // Versuche zuerst Pack-URI für WPF-Resource
            var packUri = new Uri($"pack://application:,,,/{relativePath}", UriKind.Absolute);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = packUri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.EndInit();
            bmp.Freeze();
            _imageCache[relativePath] = bmp;
            return bmp;
        }

        // Random zahl für istakill chance
        Random random = new Random();

        public Tower(double width, double height, TowerType type)
        {
            Width = width;
            Height = height;
            Type = type;
            Name = type.ToString();

            Level = 1;

            // Set initial stats
            (Damage, Range, FireRate) = TowerStats[type][0];

            _fireCooldown = 0;
            IsPlaced = false;

            var initialSource = LoadBitmap(TowerImages[type][0]);

            Sprite = new Image
            {
                Width = width,
                Height = height,
                Source = initialSource
            };

            // store references
            OriginalSource = initialSource;
            FlashSource = null; // kein Flash-Bild standardmäßig — verwenden wir stattdessen eine kurze Scale-Animation

            // Kombiniere Rotate + Scale als RenderTransform (Scale für Flash-Effekt)
            var tg = new TransformGroup();
            tg.Children.Add(_rotateTransform);
            tg.Children.Add(_flashScaleTransform);
            Sprite.RenderTransform = tg;
            Sprite.RenderTransformOrigin = new Point(0.5, 0.5);

            Container = new Border
            {
                Width = width,
                Height = height,
                BorderThickness = new Thickness(0),   // hidden default
                BorderBrush = Brushes.Yellow,         // highlight color
                Child = Sprite                         // IMPORTANT: sprite inside border
            };
        }

        // NEW: Upgrade ersetzt Bild vollständig, bricht laufende Flash-Effekte
        public bool Upgrade()
        {
            if (Level >= 3) return false; // already max level

            Level++;
            (Damage, Range, FireRate) = TowerStats[Type][Level - 1];

            // cancel running flash (falls vorhanden)
            if (_flashCts != null)
            {
                try { _flashCts.Cancel(); } catch { }
                _flashCts.Dispose();
                _flashCts = null;
            }

            // load cached, frozen bitmap (robust)
            var newSource = LoadBitmap(TowerImages[Type][Level - 1]);

            // set the image source used in the canvas — vollständiger Ersatz
            Sprite.Source = newSource;

            // update stored original source and notify bindings
            OriginalSource = newSource;

            // reset any visual flash transform
            _flashScaleTransform.ScaleX = 1;
            _flashScaleTransform.ScaleY = 1;

            // Decide: keep FlashSource null (use animation) or set to a dedicated flash image if available
            FlashSource = null;

            // Ensure UI bindings are informed
            OnPropertyChanged(nameof(Sprite));
            OnPropertyChanged(nameof(OriginalSource));

            return true; // success
        }

        public void Highlight() => Container.BorderThickness = new Thickness(1);
        public void ResetHighlight() => Container.BorderThickness = new Thickness(0);

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
                .OrderByDescending(e => e.GetProgress())
                .FirstOrDefault();
        }

        // Shoot: robust against stacked flashes.
        // If a level-specific FlashSource exists we swap images; otherwise we use a short scale animation.
        public async void Shoot(Enemy target)
        {
            
            if (_fireCooldown > 0) return; // Noch nicht bereit
            if (target == null) return;

            if(Type == TowerType.DroneOfDownfall) // Wenn der dritte Typ Tower schiesst dann
            {
                int randomInstaKill; //int für random.Next() wird erstellt
                switch (Level) // Je nach dem welches Level der turm hat hat er eine andere chance für den InstaKill
                {
                    case 1: // wenn Level 1
                        randomInstaKill = random.Next(1, 60); // zufällige zahl wird generiert. 1 zu 60 Chance das InstaKill ausgelöst wird
                            break;
                    case 2: // wenn Level 2
                        randomInstaKill = random.Next(1, 40); // zufällige zahl wird generiert. 1 zu 40 Chance das InstaKill ausgelöst wird
                        break;
                    case 3: // wenn Level 3
                        randomInstaKill = random.Next(1, 10); // zufällige zahl wird generiert. 1 zu 10 Chance das InstaKill ausgelöst wird
                        break;
                    default:
                        randomInstaKill = 0;
                        break;
                }
                // Wird eine random zahl 
                if(randomInstaKill == 5) // random zahl im zahlenbereich
                {
                    target.TakeDamage(Damage * 999); // Damage des Turms mal 999 damit es wie ein instakill ist
                    
                }
                else
                {
                    target.TakeDamage(Damage); // damage an enemy
                }
            }
            else
            {
                target.TakeDamage(Damage);// damage an enemy
            }
            
            _fireCooldown = 1.0 / FireRate; // Timer zurücksetzen

            // Cancel and dispose previous CTS
            if (_flashCts != null)
            {
                try { _flashCts.Cancel(); } catch { }
                _flashCts.Dispose();
                _flashCts = null;
            }

            _flashCts = new CancellationTokenSource();
            var token = _flashCts.Token;

            if (FlashSource != null)
            {
                // Bildwechsel-Flash (nur, wenn explizites FlashSource gesetzt)
                var currentOriginal = OriginalSource; // nicht das beim Aufruf gecapturete alte Bild
                Sprite.Source = FlashSource;

                try
                {
                    await Task.Delay(100, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested) return;

                // Setze immer auf das aktuell gültige OriginalSource zurück
                Sprite.Source = currentOriginal;
            }
            else
            {
                // Visueller Effekt: kurze Scale-Animation (kein Bildtausch -> kein "Stacking" alter Bilder)
                // Animation auf UI-Thread starten
                var dur = TimeSpan.FromMilliseconds(120);
                var anim = new DoubleAnimation(1.15, dur) { AutoReverse = true, EasingFunction = new QuadraticEase() };

                // Start animation
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                    _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                });

                try
                {
                    await Task.Delay(dur + dur, token); // warten bis anim fertig (hin+zurück)
                }
                catch (TaskCanceledException)
                {
                    // abbrechen: Animation entfernen und reset
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                        _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                        _flashScaleTransform.ScaleX = 1;
                        _flashScaleTransform.ScaleY = 1;
                    });
                    return;
                }

                if (token.IsCancellationRequested) return;

                // sicherstellen, dass Scale auf 1 zurückgesetzt ist
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    _flashScaleTransform.ScaleX = 1;
                    _flashScaleTransform.ScaleY = 1;
                });
            }

            // cleanup
            _flashCts.Dispose();
            _flashCts = null;
        }

        public double DistanceTo(Enemy enemy)
        {
            double dx = (CurrentX + Width / 2) - enemy.E_x_pos;
            double dy = (CurrentY + Height / 2) - enemy.E_y_pos;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public void UpdatePosition()
        {
            Canvas.SetLeft(Container, CurrentX);
            Canvas.SetTop(Container, CurrentY);
        }

        public void RotateTowards(Enemy target)
        {
            if (target == null) return;

            double dx = target.E_x_pos - (CurrentX + Width / 2);
            double dy = target.E_y_pos - (CurrentY + Height / 2);

            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            angle += 90; // adjust for north-facing sprite
            _rotateTransform.Angle = angle;
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

        public void Dispose()
        {
            try
            {
                if (_flashCts != null)
                {
                    _flashCts.Cancel();
                    _flashCts.Dispose();
                    _flashCts = null;
                }
            }
            catch { /* ignore */ }
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



