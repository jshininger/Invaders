using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Invaders.Model
{
    using Windows.Foundation;

    class Mothership : Invader
    {
        public static Size MothershipSize = new Size(45, 15);

        public bool Escaped { get; private set; }

        public Mothership(Point location, int score)
            : base(InvaderType.Mothership, location, score)
        {
            Size = MothershipSize;
            Escaped = false;
        }

        public override void Move(Direction direction)
        {
            if (direction == Direction.Right)
                Location = new Point(Location.X + HorizontalPixelsPerMove, Location.Y);
        }

        public void Escape()
        {
            Escaped = true;
        }
    }
}
