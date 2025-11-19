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
        public enum TowerType { DroneOfDoom, DroneOfDemise, DroneOfDownfall } //Namengebung der 3 Turmtypen

        public TowerType Type { get; } // Turmtyp Property

        public int Level { get; private set; } = 1; // Level Property

        public double CurrentX { get; set; } // Aktuelle X-Position auf dem Canvas
        public double CurrentY { get; set; } // Aktuelle Y-Position auf dem Canvas
        public double Width { get; } // Breite des Turms
        public double Height { get; } // Höhe des Turms
        public double Range { get; private set; } // Angriffsreichweite des Turms (Radius)
        public string Name { get; set; } // Name des Turms für die UI
        public double FireRate { get; private set; } // Feuerrate
        public int Damage { get; private set; } // Schaden pro Schuss

        private double _fireCooldown; // Timer bis zum nächsten Schuss
        public bool IsPlaced { get; set; } // Ob der Turm platziert wurde

        private Image _sprite; // Bild des Turms
        private System.Threading.CancellationTokenSource? _flashCts; // CancellationTokenSource für Flash-Effekt

        // Transform für kurzen visuellen Effekt, falls kein Flash-Bild vorhanden
        private readonly RotateTransform _rotateTransform = new RotateTransform();
        private readonly ScaleTransform _flashScaleTransform = new ScaleTransform(1, 1);

        public Image Sprite // Property für das Bild des Turms
        {
            get => _sprite;
            set
            {
                _sprite = value; OnPropertyChanged();
            }
        }
        public Border Container { get; } // Border um den Turm für Hervorhebung
        public ImageSource OriginalSource { get; private set; } // Originalbild des Turms
        public ImageSource FlashSource { get; private set; } // Bild für Flash-Effekt

        // Liste der Bildpfade pro Turmtyp und Level
        private static readonly Dictionary<TowerType, string[]> TowerImages = new()
        {
            
            { TowerType.DroneOfDoom, new[] { "images/Green1.png", "images/Green2.png", "images/Green3.png" } },
            { TowerType.DroneOfDemise, new[] { "images/Blue1.png", "images/Blue2.png", "images/Blue3.png" } },
            { TowerType.DroneOfDownfall, new[] { "images/Red1.png", "images/Red2.png", "images/Red3.png" } } 
        };

        // Liste der Stats pro Turmtyp und Level
        private static readonly Dictionary<TowerType, (int damage, double range, double fireRate)[]> TowerStats = new()
        {
            { TowerType.DroneOfDoom, new (int, double, double)[] { (40, 100, 1.0), (90, 120, 1.5), (140, 150, 2) } },
            { TowerType.DroneOfDemise, new (int, double, double)[] { (20, 100, 1.0), (70, 130, 1.2), (120, 200, 1.4) } },
            { TowerType.DroneOfDownfall, new (int, double, double)[] { (20, 100, 1.0), (50, 120, 1.75), (80, 140, 2.5) } },
        };

        // Bild-Cache für geladene BitmapImages
        private static readonly Dictionary<string, BitmapImage> _imageCache = new();

        // Lädt ein BitmapImage aus dem angegebenen relativen Pfad (Resource-Pfad)

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


        // Konstruktor für den Turm auf dem Canvas
        public Tower(double width, double height, TowerType type)
        {
            Width = width; // Setze Breite
            Height = height; // Setze Höhe
            Type = type; // Setze Turmtyp
            Name = type.ToString(); // Setze Name basierend auf Turmtyp

            Level = 1; // Startlevel ist 1

            // Setze Anfangsstats basierend auf Turmtyp und Level
            (Damage, Range, FireRate) = TowerStats[type][0];

            _fireCooldown = 0; // Timer default auf 0
            IsPlaced = false; // Turm ist anfangs nicht platziert

            var initialSource = LoadBitmap(TowerImages[type][0]); // Lade Anfangsbild

            Sprite = new Image // Image-Element für den Turm
            {
                Width = width, 
                Height = height,
                Source = initialSource
            };

            // speichere Original- und Flash-Bilder
            OriginalSource = initialSource; // ursprüngliches Bild
            FlashSource = null; // kein Flash-Bild standardmäßig — verwenden wir stattdessen eine kurze Scale-Animation

            // Kombiniere Rotate + Scale als RenderTransform (Scale für Flash-Effekt)
            var tg = new TransformGroup(); // TransformGroup für mehrere Transformationen
            tg.Children.Add(_rotateTransform); // Rotation
            tg.Children.Add(_flashScaleTransform); // Scale für Flash-Effekt
            Sprite.RenderTransform = tg; // Setze RenderTransform
            Sprite.RenderTransformOrigin = new Point(0.5, 0.5); // Transformationsursprung in der Mitte

            Container = new Border // Border um den Turm für Hervorhebung
            {
                Width = width, // Setze Breite
                Height = height, // Setze Höhe
                BorderThickness = new Thickness(0),   // versteckt standardmäßig
                BorderBrush = Brushes.Yellow,         // gelbe Hervorhebung
                Child = Sprite                         // Sprite/Bild im Container
            };
        }

        // Upgrade-Methode für den Turm
        public bool Upgrade()
        {
            if (Level >= 3) return false; // Max Level erreicht

            Level++; // Level erhöhen
            (Damage, Range, FireRate) = TowerStats[Type][Level - 1]; // Neue Stats setzen

            // Lösche und storniere vorherigen Flash-Effekt, falls aktiv
            if (_flashCts != null)
            {
                try { _flashCts.Cancel(); } catch { }
                _flashCts.Dispose();
                _flashCts = null;
            }

            // Lade neues Bild für das aktuelle Level
            var newSource = LoadBitmap(TowerImages[Type][Level - 1]);

            // Aktualisiere das Sprite-Bild
            Sprite.Source = newSource;

            // Aktualisiere das OriginalSource-Bild
            OriginalSource = newSource;

            // Setze Scale-Transform zurück
            _flashScaleTransform.ScaleX = 1;
            _flashScaleTransform.ScaleY = 1;

            // Kein Flash-Bild für Upgrades
            FlashSource = null;

            // Benachrichtige UI über Änderungen
            OnPropertyChanged(nameof(Sprite));
            OnPropertyChanged(nameof(OriginalSource));

            return true; // erfolgreich
        }

        public void Highlight() => Container.BorderThickness = new Thickness(1); // Hervorhebung aktivieren
        public void ResetHighlight() => Container.BorderThickness = new Thickness(0); // Hervorhebung deaktivieren

        public void Update(double deltaTime) // Update-Methode für den Turm
        {
            if (!IsPlaced) return; // Nur aktualisieren, wenn platziert
            _fireCooldown -= deltaTime; // Feuercooldown verringern
        }

        public Enemy FindTarget(IEnumerable<Enemy> enemies) // Zielsuche-Methode
        {
            if (!IsPlaced) return null; // Nur suchen, wenn platziert

            return enemies // Ziel finden
                .Where(e => DistanceTo(e) <= Range) // Innerhalb der Reichweite
                .OrderByDescending(e => e.GetProgress()) // Priorisiere nach Fortschritt auf dem Pfad
                .FirstOrDefault(); // Nimm das erste Ziel
        }

        // Shoot: robust against stacked flashes.
        // If a level-specific FlashSource exists we swap images; otherwise we use a short scale animation.
        public async void Shoot(Enemy target)
        {
            
            if (_fireCooldown > 0) return; // Noch nicht bereit
            if (target == null) return;

            if(Type == TowerType.DroneOfDownfall) // Wenn der dritte Typ Tower schiesst 
            {
                int randomInstaKill;
                switch (Level)
                {
                    case 1:
                        randomInstaKill = random.Next(1, 60);
                            break;
                    case 2:
                        randomInstaKill = random.Next(1, 40);
                        break;
                    case 3:
                        randomInstaKill = random.Next(1, 10);
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

            // Flash-Effekt starten
            if (_flashCts != null)
            {
                try { _flashCts.Cancel(); } catch { } // vorherigen Flash abbrechen
                _flashCts.Dispose();
                _flashCts = null;  // säubern
            }

            _flashCts = new CancellationTokenSource(); // neue CancellationTokenSource
            var token = _flashCts.Token; // token für die Methode

            if (FlashSource != null) // wenn ein Flash-Bild definiert ist
            {
                // Bildwechsel-Flash (nur, wenn explizites FlashSource gesetzt)
                var currentOriginal = OriginalSource; // nicht das beim Aufruf gecapturete alte Bild
                Sprite.Source = FlashSource; // setze auf Flash-Bild

                try // kurze Wartezeit
                {
                    await Task.Delay(100, token); // 100ms warten
                }
                catch (TaskCanceledException) // falls abgebrochen
                {
                    return; // einfach zurückkehren
                }

                if (token.IsCancellationRequested) return; // Abbruch prüfen

                // Setze immer auf das aktuell gültige OriginalSource zurück
                Sprite.Source = currentOriginal; // zurück zum Original-Bild
            }
            else // kein Flash-Bild definiert
            {
                // Visueller Effekt: kurze Scale-Animation (kein Bildtausch -> kein "Stacking" alter Bilder)
                // Animation auf UI-Thread starten
                var dur = TimeSpan.FromMilliseconds(120); // Dauer der Animation
                var anim = new DoubleAnimation(1.15, dur) { AutoReverse = true, EasingFunction = new QuadraticEase() }; // Animation definieren

                // Start animation
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim); // X skalieren
                    _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim); // Y skalieren
                });

                try // warten bis die Animation fertig ist
                {
                    await Task.Delay(dur + dur, token); // warten bis anim fertig (hin+zurück)
                }
                catch (TaskCanceledException) // falls abgebrochen
                {
                    // abbrechen: Animation entfernen und reset
                    Application.Current.Dispatcher.Invoke(() => // auf UI-Thread
                    {
                        _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null); // Animation abbrechen für X
                        _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null); // Animation abbrechen für Y
                        _flashScaleTransform.ScaleX = 1; // reset X
                        _flashScaleTransform.ScaleY = 1; // reset Y
                    });
                    return; // zurückkehren
                }

                if (token.IsCancellationRequested) return; // Abbruch prüfen

                // sicherstellen, dass Scale auf 1 zurückgesetzt ist
                Application.Current.Dispatcher.Invoke(() => // auf UI-Thread
                {
                    _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null); // Animation abbrechen für X
                    _flashScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null); // Animation abbrechen für Y
                    _flashScaleTransform.ScaleX = 1; // reset X
                    _flashScaleTransform.ScaleY = 1; // reset Y
                });
            }

            // cleanup
            _flashCts.Dispose();
            _flashCts = null;
        }

        public double DistanceTo(Enemy enemy) // Berechnet die Entfernung zu einem Gegner
        {
            double dx = (CurrentX + Width / 2) - enemy.E_x_pos; // Mittelpunkt des Turms in X-Richtung
            double dy = (CurrentY + Height / 2) - enemy.E_y_pos; // Mittelpunkt des Turms in Y-Richtung
            return Math.Sqrt(dx * dx + dy * dy); // euklidische Distanz
        }

        public void UpdatePosition() // Aktualisiert die Position des Turms auf dem Canvas
        {
            Canvas.SetLeft(Container, CurrentX); // Setze linke Position
            Canvas.SetTop(Container, CurrentY); // Setze obere Position
        }

        public void RotateTowards(Enemy target) // Dreht den Turm in Richtung des Ziels
        {
            if (target == null) return; // Kein Ziel

            double dx = target.E_x_pos - (CurrentX + Width / 2); // Differenz in X-Richtung
            double dy = target.E_y_pos - (CurrentY + Height / 2); // Differenz in Y-Richtung

            double angle = Math.Atan2(dy, dx) * 180 / Math.PI; // Winkel in Grad
            angle += 90; // Anpassung, da das Bild nach oben zeigt
            _rotateTransform.Angle = angle; // Setze Winkel
        }

        public bool CheckCollision(IEnumerable<Tower> otherTowers, List<Point> pathPoints) // Kollisionserkennung
        {
            Rect myRect = new Rect(CurrentX, CurrentY, Width, Height); // Rechteck des aktuellen Turms (BoxCollider)

            foreach (var tower in otherTowers) // Überprüfe Kollision mit anderen Türmen
            {
                if (tower == this) continue; // Überspringe sich selbst
                Rect tRect = new Rect(tower.CurrentX, tower.CurrentY, tower.Width, tower.Height); // Rechteck des anderen Turms
                if (myRect.IntersectsWith(tRect)) return true; // Kollision erkannt
            }

            double padding = 10; // Padding um den Pfad für Kollisionserkennung
            foreach (var pt in pathPoints) // Überprüfe Kollision mit Pfadpunkten
            {
                Rect pathRect = new Rect(pt.X - padding, pt.Y - padding, padding * 2, padding * 2); // Rechteck um Pfadpunkt
                if (myRect.IntersectsWith(pathRect)) return true; // Kollision erkannt
            }

            return false; // Keine Kollision
        }

        public void Dispose() // Aufräummethode für den Turm
        {
            try // versuche den Flash-Effekt zu stoppen
            {
                if (_flashCts != null) // wenn aktiv
                {
                    _flashCts.Cancel(); // abbrechen
                    _flashCts.Dispose(); // Ressourcen freigeben
                    _flashCts = null; // säubern
                }
            }
            catch { /* ignore */ } // Fehler ignorieren
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



