using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Invaders.Model
{
    using Windows.Foundation;

    class InvadersModel
    {
        public readonly static Size PlayAreaSize = new Size(400, 300);
        public const int MaximumPlayerShots = 3;
        public const int MaximumLives = 5;
        public const int InitialStarCount = 50;

        private readonly Random _random = new Random();

        public int Score { get; private set; }
        public int Wave { get; private set; }
        public int Lives { get; private set; }
        public bool GameOver { get; private set; }

        private DateTime? _playerDied = null;
        public bool PlayerDying { get { return _playerDied.HasValue; } }

        private Player _player;

        private readonly List<Invader> _invaders = new List<Invader>();
        private readonly List<Shot> _playerShots = new List<Shot>();
        private readonly List<Shot> _invaderShots = new List<Shot>();
        private readonly List<Point> _stars = new List<Point>();

        private readonly Dictionary<InvaderType, int> _invaderScores = new Dictionary<InvaderType, int>()
        {
            { InvaderType.Star, 10 },
            { InvaderType.Satellite, 20 },
            { InvaderType.Saucer, 30 },
            { InvaderType.Bug, 40 },
            { InvaderType.Spaceship, 50 },
            { InvaderType.Mothership, 250 },
        };

        private Mothership _mothership = null;

        private Direction _invaderDirection = Direction.Left;
        private bool _justMovedDown = false;

        private DateTime _lastUpdated = DateTime.MinValue;
        private DateTime _lastMothershipUpdate = DateTime.MinValue;
        
        public InvadersModel()
        {
            EndGame();
        }

        public void EndGame()
        {
            GameOver = true;
        }

        public void StartGame()
        {
            // Set the GameOver property to false
            GameOver = false;
            
            // Clear all invaders, shots, and stars
            foreach (Invader invader in _invaders)
                OnShipChanged(invader as Ship, true);
            _invaders.Clear();
            
            foreach (Shot shot in _playerShots)
                OnShotMoved(shot, true);
            _playerShots.Clear();

            foreach (Shot shot in _invaderShots)
                OnShotMoved(shot, true);
            _invaderShots.Clear();

            foreach (Point star in _stars)
                OnStarChanged(star, true);
            _stars.Clear();

            // Create new stars
            for (int i = 0; i < InitialStarCount; i++)
                AddStar();

            // Add the player
            _player = new Player();
            OnShipChanged(_player, false);

            // Initialize game statistics
            Lives = 2;
            Wave = 0;
            Score = 0;
            
            // Add the first wave of invaders
            nextWave();
        }

        private void nextWave()
        {
            // Increment the Wave property, clear the private _invaders collection, and then
            // create all of the Invader objects, giving each of them a Location field with
            // the correct coordinates. Try spacing them out so that they’re spaced 1.4 invader
            // lengths apart horizontally, and 1.4 invader heights vertically.
            Wave++;
            _invaders.Clear();
            if (_mothership != null)
            {
                _mothership.Escape();
                OnShipChanged(_mothership, false);
                _mothership = null;
            }
            RemoveAllShots();

            _invaderDirection = Direction.Left;

            InvaderType invaderType;
            for (int row = 0; row < 6; row++)
            {
                switch (row)
                {
                    case 0:
                        invaderType = InvaderType.Spaceship;
                        break;
                    case 1:
                        invaderType = InvaderType.Bug;
                        break;
                    case 2:
                        invaderType = InvaderType.Saucer;
                        break;
                    case 3:
                        invaderType = InvaderType.Satellite;
                        break;
                    default:
                        invaderType = InvaderType.Star;
                        break;
                }
                for (int column = 0; column < 11; column++)
                {
                    Point location = new Point(column * Invader.InvaderSize.Width * 1.4, 
                        row * Invader.InvaderSize.Height * 1.4);
                    _invaders.Add(new Invader(invaderType, location, _invaderScores[invaderType]));
                    OnShipChanged(_invaders[_invaders.Count - 1], false);
                }
            }
        }

        private void MoveInvaders()
        {
            TimeSpan timeSinceLastMove = DateTime.Now - _lastUpdated;
            double msBetweenMoves = Math.Max(7 - Wave, 1) * (2 * _invaders.Count());

            if (timeSinceLastMove >= TimeSpan.FromMilliseconds(msBetweenMoves))
            {
                _lastUpdated = DateTime.Now;
                if (_invaderDirection == Direction.Right)
                {
                    var invadersTouchingRightBoundary =
                    from invader in _invaders
                    where invader.Area.Right > PlayAreaSize.Width - Invader.HorizontalPixelsPerMove * 2
                    select invader;

                    if (invadersTouchingRightBoundary.Count() > 0)
                    {
                        _invaderDirection = Direction.Down;
                        foreach (Invader invader in _invaders)
                        {
                            invader.Move(_invaderDirection);
                            OnShipChanged(invader, false);
                        }
                        _justMovedDown = true;
                        _invaderDirection = Direction.Left;
                    }
                    else
                    {
                        _justMovedDown = false;
                        foreach (Invader invader in _invaders)
                        {
                            invader.Move(_invaderDirection);
                            OnShipChanged(invader, false);
                        }
                    }

                }
                else if (_invaderDirection == Direction.Left)
                {
                    var invadersTouchingLeftBoundary =
                        from invader in _invaders
                        where invader.Area.Left < Invader.HorizontalPixelsPerMove * 2
                        select invader;

                    if (invadersTouchingLeftBoundary.Count() > 0)
                    {
                        _invaderDirection = Direction.Down;
                        foreach (Invader invader in _invaders)
                        {
                            invader.Move(_invaderDirection);
                            OnShipChanged(invader, false);
                        }
                        _justMovedDown = true;
                        _invaderDirection = Direction.Right;
                    }
                    else
                    {
                        _justMovedDown = false;
                        foreach (Invader invader in _invaders)
                        {
                            invader.Move(_invaderDirection);
                            OnShipChanged(invader, false);
                        }
                    }

                }
            }

            TimeSpan timeSinceLastMothershipMove = DateTime.Now - _lastMothershipUpdate;
            double msBetweenMothershipMoves = Math.Max(7 - Wave, 1) * (_invaders.Count() / 2);

            if (_mothership != null && timeSinceLastMothershipMove > 
                TimeSpan.FromMilliseconds(msBetweenMothershipMoves))
            {
                _lastMothershipUpdate = DateTime.Now;
                if (_mothership.Area.Right > PlayAreaSize.Width - Mothership.HorizontalPixelsPerMove * 2)
                {
                    _mothership.Escape();
                    OnShipChanged(_mothership, false);
                    _mothership = null;
                }
                else
                {
                    _mothership.Move(Direction.Right);
                    OnShipChanged(_mothership, false);
                }
            }
        }

        private void ReturnFire()
        {
            if (_invaderShots.Count() > Wave + 1 || _random.Next(10) < 10 - Wave)
                return;

            var invaderColumns =
                from invader in _invaders
                group invader by invader.Location.X
                    into invaderGroup
                    orderby invaderGroup.Key descending
                    select invaderGroup;

            var randomGroup = invaderColumns.ElementAt(_random.Next(invaderColumns.Count()));
            var shooter = randomGroup.Last();

            Point shotLocation = new Point(shooter.Area.X + (shooter.Size.Width / 2) - 1, shooter.Area.Bottom);
            Shot invaderShot = new Shot(shotLocation, Direction.Down);
            _invaderShots.Add(invaderShot);

            OnShotMoved(invaderShot, false);
        }

        public void FireShot()
        {
            // This method checks the number of player shots on screen to make sure there aren’t too many,
            // then it adds a new Shot to the _playerShots collection and fires the ShotMoved event.
            if (GameOver || PlayerDying || _lastUpdated == DateTime.MinValue)
                return;

            if (_playerShots.Count < MaximumPlayerShots)
            {
                Shot shotFired = new Shot(new Point(_player.Location.X + (_player.Size.Width / 2) - 1, _player.Location.Y),
                    Direction.Up);
                _playerShots.Add(shotFired);
                OnShotMoved(shotFired, false);
            }
        }

        public void MovePlayer(Direction direction)
        {
            if (!_playerDied.HasValue)
            {
                _player.Move(direction);
                OnShipChanged(_player, false);
            }
        }

        public void Twinkle()
        {
            if (_random.Next(2) == 0)
                if(_stars.Count < (1.5 * InitialStarCount) - 1)
                    AddStar();
            else
                if(_stars.Count > (0.85 * InitialStarCount) + 1)
                    RemoveStar();
        }

        public void AddStar()
        {
            Point newStar = new Point((double)_random.Next((int)PlayAreaSize.Width - 10), (double)_random.Next((int)PlayAreaSize.Height - 10));
            _stars.Add(newStar);
            OnStarChanged(newStar, false);
        }

        public void RemoveStar()
        {
            Point starToRemove = _stars[_random.Next(_stars.Count)];
            _stars.Remove(starToRemove);
            OnStarChanged(starToRemove, true);
        }

        public void Update(bool paused)
        {
            if (!paused)
            {
                if (_invaders.Count == 0)
                    nextWave();
                
                if (!PlayerDying)
                {
                    MoveInvaders();
                    MoveShots();
                    ReturnFire();
                    CheckForInvaderCollisions();
                    CheckForMothershipCollisions();
                    CheckForPlayerCollisions();

                    // The chance of a mothership appearing is inversely proportional to number of Lives, enemies,
                    // and the wave you're on.  For example, if you're down to your last life on the first wave
                    // with one enemy left in the wave, there's a good chance a mothership will appear (1/151).
                    int mothershipChance = Math.Min(4000, 150 + Wave * Math.Max(Lives, 1) * 
                        _invaders.Count);

                    if (_mothership == null && _random.Next(mothershipChance) == 1)
                         AddMothership();
                }
                if (PlayerDying && TimeSpan.FromSeconds(2.5) < DateTime.Now - _playerDied)
                {
                    _playerDied = null;
                    OnShipChanged(_player, false);
                }
            }
            Twinkle();
        }

        private void MoveShots()
        {
            List<Shot> playerShots = _playerShots.ToList();
            foreach (Shot shot in playerShots)
            {
                shot.Move();
                OnShotMoved(shot, false);
                if (shot.Location.Y < 0)
                {
                    _playerShots.Remove(shot);
                    OnShotMoved(shot, true);
                }
            }

            List<Shot> invaderShots = _invaderShots.ToList();
            foreach (Shot shot in invaderShots)
            {
                shot.Move();
                OnShotMoved(shot, false);
                if (shot.Location.Y > PlayAreaSize.Height)
                {
                    _invaderShots.Remove(shot);
                    OnShotMoved(shot, true);
                }
            }
        }

        private void AddMothership()
        {
            _mothership = new Mothership(new Point(0, 0), _invaderScores[InvaderType.Mothership]);
            OnShipChanged(_mothership, false);
        }

        private static bool RectsOverlap(Rect r1, Rect r2)
        {
            r1.Intersect(r2);
            if (r1.Width > 0 || r1.Height > 0)
                return true;
            return false;
        }

        private void CheckForInvaderCollisions()
        {   
            List<Shot> playerShots = _playerShots.ToList();
            List<Invader> invaders = _invaders.ToList();

            foreach (Shot shot in playerShots)
            {
                Rect shotRect = new Rect(shot.Location.X, shot.Location.Y, Shot.ShotSize.Width, 
                    Shot.ShotSize.Height);

                var invadersHit =
                    from invader in invaders
                    where RectsOverlap(invader.Area, shotRect)
                    select invader;

                foreach (Invader deadInvader in invadersHit)
                {
                    _invaders.Remove(deadInvader);
                    OnShipChanged(deadInvader, true);
                    _playerShots.Remove(shot);
                    OnShotMoved(shot, true);
                    Score += deadInvader.Score;
                }
            }
        }

        private void CheckForMothershipCollisions()
        {
            // We check to see if the mothership is null because it could have escaped off the edge
            // of the screen
            if (_mothership == null)
                return;

            List<Shot> playerShots = _playerShots.ToList();

            foreach (Shot shot in playerShots)
            {
                Rect shotRect = new Rect(shot.Location.X, shot.Location.Y, Shot.ShotSize.Width,
                    Shot.ShotSize.Height);
                if (RectsOverlap(_mothership.Area, shotRect))
                {
                    Mothership deadMothership = _mothership;
                    _mothership = null;
                    _playerShots.Remove(shot);
                    OnShotMoved(shot, true);
                    OnShipChanged(deadMothership, true);
                    Score += deadMothership.Score;
                    // An extra life is awarded for destroying a mothership
                    if (Lives + 1 <= MaximumLives)
                        Lives++;
                    break;
                }
            }
        }

        private void CheckForPlayerCollisions()
        {
            List<Shot> invaderShots = _invaderShots.ToList();

            foreach (Shot shot in invaderShots)
            {
                Rect shotRect = new Rect(shot.Location.X, shot.Location.Y, Shot.ShotSize.Width, 
                    Shot.ShotSize.Height);
                if (RectsOverlap(_player.Area, shotRect))
                {
                    if (Lives == 0)
                        EndGame();
                    else
                    {
                        _invaderShots.Remove(shot);
                        OnShotMoved(shot, true);
                        _playerDied = DateTime.Now;
                        OnShipChanged(_player, true);
                        RemoveAllShots();
                        Lives--;
                    }
                }
            }

            var invadersReachedBottom =
            from invader in _invaders
            where invader.Area.Bottom > _player.Area.Top
            select invader;

            if (invadersReachedBottom.Count() > 0)
                EndGame();
        }

        private void RemoveAllShots()
        {
            List<Shot> invaderShots = _invaderShots.ToList();
            List<Shot> playerShots = _playerShots.ToList();

            foreach (Shot shot in invaderShots)
                OnShotMoved(shot, true);

            foreach (Shot shot in playerShots)
                OnShotMoved(shot, true);

            _invaderShots.Clear();
            _playerShots.Clear();
        }

        public void UpdateAllShipsAndStars()
        {
            foreach (Shot shot in _playerShots)
                OnShotMoved(shot, false);
            foreach (Invader ship in _invaders)
                OnShipChanged(ship, false);
            OnShipChanged(_player, false);
            foreach(Point point in _stars)
                OnStarChanged(point, false);
        }

        public event EventHandler<ShipChangedEventArgs> ShipChanged;

        public void OnShipChanged(Ship shipUpdated, bool killed)
        {
            EventHandler<ShipChangedEventArgs> shipChanged = ShipChanged;
            if (shipChanged != null)
                shipChanged(this, new ShipChangedEventArgs(shipUpdated, killed));
        }

        public event EventHandler<ShotMovedEventArgs> ShotMoved;

        public void OnShotMoved(Shot shot, bool disappeared)
        {
            EventHandler<ShotMovedEventArgs> shotMoved = ShotMoved;
            if (shotMoved != null)
                shotMoved(this, new ShotMovedEventArgs(shot, disappeared));
        }

        public event EventHandler<StarChangedEventArgs> StarChanged;

        public void OnStarChanged(Point point, bool disappeared)
        {
            EventHandler<StarChangedEventArgs> starChanged = StarChanged;
            if (starChanged != null)
                starChanged(this, new StarChangedEventArgs(point, disappeared));
        }
    }
}
