using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Tower_Defense_Game.GameLogic
{
    //Spiele-Loop
    //Ruft in einer festen Frequenz (z.B. 30 mal pro Sekunde) einen Update-Callback-Methode auf.
    public class GameLoop
    {
        private readonly DispatcherTimer _timer; //Sagt wann ein Update stattfinden soll (z.B. 30x/Sek.)

        private readonly Stopwatch _stopwatch = new(); //Misst wie viel Zeit seit dem letzten Update vergangen ist

        private readonly Action<double> _onUpdate; //Gibt welche Funktion pro Tick ausgeführt wird (Game-Logik)

        public GameLoop(Action<double> onUpdate, int fps = 30) //Konstrukor
        {
            this._onUpdate = onUpdate;

            // Intervall aus FPS berechnen (ms pro Tick)
            this._timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / fps)
            };

            _timer.Tick += (s, e) =>
            {
                // vergangene Zeit seit letztem Tick (in Sekunden)
                double deltaTickTime = _stopwatch.Elapsed.TotalSeconds;
                _stopwatch.Restart();

                // Update-Aufruf (hier passiert später: Bewegung, Schießen, Kollisionen usw.)
                _onUpdate(deltaTickTime);
            };
        }

        public void Start()
        {
            _stopwatch.Restart();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
