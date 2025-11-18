using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tower_Defense_Game.GameObjekt;

namespace Tower_Defense_Game.GameLogic
{
    public class WaveManager
    {
        // Die Komplette liste von Enemy
        private readonly ObservableCollection<Enemy> _enemies;
        // Der weg wo der Enemy entlang geht
        private readonly GOPath _gamePath;

        // Eine Queue der Waves Geplant war das es endlos ist und mit einer normalen liste ist das nicht einfach
        // so wird eine wave in die WarteSchlange generiert
        private readonly Queue<Wave> _queue = new();
        // aktuelle wave darf null sein
        private Wave? _current;
        // Wie viele gegner sind schon generiert worden
        private int _spawned;
        // gefäs der ticks wo wenn bei 0 ist ein gegner generiert wird
        private double _spawnTimer;

        // ist die Wave noch am lauffen
        public bool IsRunning { get; private set; }
        // Index der aktuellen Wave
        public int CurrentIndex { get; private set; }

        //Konstrucktor
        public WaveManager(ObservableCollection<Enemy> enemies, GOPath gamePath)
        {
            this._enemies = enemies;
            this._gamePath = gamePath;
        }

        // Funktion AddWave braucht eine Wave die dann in die Warteschlange kommt
        public void AddWave(Wave w) => _queue.Enqueue(w);

        // Bool zur überprüfung und vorbereitung neuer Waves 
        public bool StartNextWave()
        {
            // wenn die wave noch läuft oder es keine waves mehr gibt stoppen und false zurückgeben
            if(IsRunning || _queue.Count == 0)
            {   
                return false;
            }
            // Neue wave wird vorbereiten die aktuelle wave wird aus der queue genommen
            _current = _queue.Dequeue();
            // Gespawnter gegner wird auf 0 gesetzt
            _spawned = 0;
            // Spawntimer der gegner wird auf 0 gesetzt so wird dirkt bei start ein neuer gegner generiert
            _spawnTimer = 0;
            // der Index der aktuellen wave + 1
            CurrentIndex++;
            // Die wave wird als aktiv markiert
            IsRunning = true;
            return true;
        }

        public bool RestartWaves()
        {
            // Die que mit den waves wird gelöscht damit die neu erstellten wieder am anfang
            // sind sonst würde mann bei der runde anfangen wo man zuvor verlohren hat
            _queue.Clear();
            // Gespawnter gegner wird auf 0 gesetzt
            _spawned = 0;
            // Spawntimer der gegner wird auf 0 gesetzt so wird dirkt bei start ein neuer gegner generiert
            _spawnTimer = 0;
            // der Index der aktuellen wave wird auf 0 gesetzt also Wave 0
            CurrentIndex = 0;
            // Die wave wird wie beim start noch pausiert
            IsRunning = false;
            return true;

        }

        // Ubdate von Waves
        public void Update(double deltaTickTime)
        {
            // Hier gab es ein riesen Problem das ! vor IsRunning wurde vergessen und so wurden 2-3h verschwendet
            // den das heisst dann wenn die wave läuft dann aktualisiere die wave nicht
            if (!IsRunning || _current == null)
            {
                return;
            }

            // Tickrate wird vom Spawntimer abgezogen
            _spawnTimer -= deltaTickTime;

            // Wenn die anzahl gespawnter gegner kleiner ist als die maximale anzahl gegner der Wave und der Spawntimer 0 oder weniger ist
            while(_spawned < _current.Count && _spawnTimer <= 0)
            {
                // Wird ein neuer gegner erstellt die anzahl spawned +1 und der Spawntimer wird durch Interval zurückgesetzt
                _enemies.Add(new Enemy(_gamePath, _current.Hp, _current.Speed, bounty: 10));
                _spawned++;
                _spawnTimer += _current.Interval;

            }
        
            // Wenn alle gegner gespawned sind und keine mehr übrig sind wird die wave beendet
            if(_spawned >= _current.Count && _enemies.Count == 0)
            {
                IsRunning = false;
                _current = null;
            }
        }
    }
}
