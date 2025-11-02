using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tower_Defense_Game.GameObjekt
{
    // Ein Wegpunkt beschreibt einen Punkt auf dem Spielfeld,
    // den der Gegner später entlangläuft.
    public readonly struct Waypoint
    {
        public Waypoint(double x, double y)
        {
            X = x; // X-Koordinate auf dem Canvas
            Y = y; // Y-Koordinate auf dem Canvas
        }

        public double X { get; } // Nur lesbar, da readonly struct
        public double Y { get; }
    }

    // Die Path-Klasse beschreibt den gesamten Weg,
    // bestehend aus mehreren Wegpunkten.
    // Sie kann außerdem eine Position zwischen den Punkten berechnen
    // (für flüssige Bewegung).
    public class GOPath
    {
        // Alle Wegpunkte des Pfads
        public IReadOnlyList<Waypoint> Points { get; }

        // Länge jedes einzelnen Liniensegments zwischen zwei Wegpunkten
        private readonly double[] _segLen;

        // Gesamte Pfadlänge (Summe aller Segmentlängen)
        public double TotalLength { get; }

        // Konstruktor: nimmt eine Liste von Wegpunkten entgegen
        public GOPath(IReadOnlyList<Waypoint> points)
        {
            if (points == null || points.Count < 2)
                throw new ArgumentException("Ein Pfad braucht mindestens 2 Wegpunkte.");

            Points = points;

            // Array vorbereiten, in dem wir die Streckenlänge je Segment speichern
            _segLen = new double[points.Count - 1];

            double lengthSum = 0;

            // Berechne Länge zwischen jedem Punktpaar
            for (int i = 0; i < _segLen.Length; i++)
            {
                _segLen[i] = Dist(points[i], points[i + 1]);
                lengthSum += _segLen[i];
            }

            TotalLength = lengthSum; // Speichere Gesamtlänge
        }

        // Berechnet die exakte Position auf dem Pfad
        // abhängig davon, wie weit man bereits gelaufen ist (Distance).
        public (double x, double y) PositionAt(double distance)
        {
            // Stelle sicher, dass wir nicht über das Ende hinausschießen
            distance = Math.Clamp(distance, 0, TotalLength);

            int seg = 0;

            // Finde das Segment, auf dem die Position liegt
            while (seg < _segLen.Length && distance > _segLen[seg])
                distance -= _segLen[seg++];

            // Falls wir am letzten Segment sind → Korrektur
            if (seg >= _segLen.Length)
                seg = _segLen.Length - 1;

            var a = Points[seg];
            var b = Points[seg + 1];

            double segmentLength = _segLen[seg];

            // Berechne "t" = wie weit wir zwischen Punkt A und B sind (0..1)
            double t = segmentLength == 0 ? 1 : distance / segmentLength;

            // Interpolierte Position
            return (
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t
            );
        }

        // Hilfsfunktion zur Distanz zwischen zwei Punkten
        private static double Dist(Waypoint a, Waypoint b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }
}
