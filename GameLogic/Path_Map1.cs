using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tower_Defense_Game.GameObjekt;

namespace Tower_Defense_Game.GameLogic
{
    internal class Path_Map1
    {
        // Erzeugt einen einfachen Beispielpfad,
        // der in Zick-Zack-Linien über das Spielfeld läuft.
        public static GOPath SimplePath()
        {
            var pts = new List<Waypoint>
            {
                new(  0,  60),
                new(240,  60),
                new(240, 180),
                new( 80, 180),
                new( 80, 320),
                new(300, 320),
                new(300, 460),
                new(120, 460),
                new(120, 600),
                new(520, 600),
                new(520, 420),
                new(760, 420),
                new(760, 200),
                new(450, 200),
                new(450,  60),
                new(820,  60)
            };

            // Wir geben einen fertigen Path zurück
            return new GOPath(pts);
        }
    }
}
