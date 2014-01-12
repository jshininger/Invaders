using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Invaders.ViewModel
{
    using View;
    using Model;
    using System.ComponentModel;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using Windows.Foundation;
    using DispatcherTimer = Windows.UI.Xaml.DispatcherTimer;
    using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
    using VirtualKey = Windows.System.VirtualKey;
    using Windows.UI.Xaml.Controls;

    class InvadersViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<FrameworkElement> _sprites = 
            new ObservableCollection<FrameworkElement>();
        public INotifyCollectionChanged Sprites { get { return _sprites; } }

        public bool GameOver { get { return _model.GameOver; } }
        
        private readonly ObservableCollection<object> _lives = new ObservableCollection<object>();
        public INotifyCollectionChanged Lives { get { return _lives; } }

        public bool Paused { get; set; }
        private bool _lastPaused = true;

        public static double Scale { get; private set; }

        public int Score { get; private set; }

        public Size PlayAreaSize 
        {
            set
            {
                Scale = value.Width / 405;
                _model.UpdateAllShipsAndStars();
                RecreateScanLines();
            }
        }

        private readonly InvadersModel _model = new InvadersModel();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private FrameworkElement _playerControl = null;
        private bool _playerFlashing = false;
        private FrameworkElement _mothershipControl = null;
        private readonly Dictionary<Invader, FrameworkElement> _invaders =
            new Dictionary<Invader, FrameworkElement>();
        private readonly Dictionary<FrameworkElement, DateTime> _shotInvaders =
            new Dictionary<FrameworkElement, DateTime>();
        private readonly Dictionary<Shot, FrameworkElement> _shots =
            new Dictionary<Shot, FrameworkElement>();
        private readonly Dictionary<Point, FrameworkElement> _stars =
            new Dictionary<Point, FrameworkElement>();
        private readonly List<FrameworkElement> _scanLines = new List<FrameworkElement>();
        private DateTime? _leftAction = null;
        private DateTime? _rightAction = null;

        public InvadersViewModel()
        {
            Scale = 1;

            _model.ShipChanged += ModelShipChangedEventHandler;
            _model.ShotMoved += ModelShotMovedEventHandler;
            _model.StarChanged += ModelStarChangedEventHandler;

            _timer.Interval = TimeSpan.FromMilliseconds(75);
            _timer.Tick += TimerTickEventHandler;

            EndGame();
        }

        public void StartGame()
        {
            Paused = false;
            foreach (var invader in _invaders.Values) _sprites.Remove(invader);
            foreach (var shot in _shots.Values) _sprites.Remove(shot);

            _model.StartGame();
            OnPropertyChanged("GameOver");

            _timer.Start();
        }

        private void RecreateScanLines()
        {
            foreach (FrameworkElement scanLine in _scanLines)
                if (_sprites.Contains(scanLine))
                    _sprites.Remove(scanLine);
            _scanLines.Clear();
            for (int y = 0; y < 300; y += 2)
            {
                FrameworkElement scanLine = InvadersHelper.ScanLineFactory(y, 400, Scale);
                _scanLines.Add(scanLine);
                _sprites.Add(scanLine);
            }
        }

        public void EndGame()
        {
            _model.EndGame();
        }

        private void ModelShipChangedEventHandler(object sender, ShipChangedEventArgs e)
        {
            if (!e.Killed)
            {
                if (e.ShipUpdated is Mothership)
                {
                    Mothership mothership = e.ShipUpdated as Mothership;
                    if (_mothershipControl == null)
                    {
                        _mothershipControl = InvadersHelper.ShipControlFactory(mothership, Scale);
                        _sprites.Add(_mothershipControl);
                    }
                    else if (mothership.Escaped == true)
                    {
                        _sprites.Remove(_mothershipControl);
                        _mothershipControl = null;
                    }
                    else
                    {
                        InvadersHelper.MoveElementOnCanvas(_mothershipControl, mothership.Location.X * Scale,
                            mothership.Location.Y * Scale);
                        InvadersHelper.ResizeElement(_mothershipControl, mothership.Size.Width * Scale,
                            mothership.Size.Height * Scale);
                    }
                }
                else if (e.ShipUpdated is Invader)
                {
                    Invader invader = e.ShipUpdated as Invader;
                    if (!_invaders.ContainsKey(invader))
                    {
                        FrameworkElement invaderControl = InvadersHelper.ShipControlFactory(invader, Scale);
                        _invaders[invader] = invaderControl;
                        _sprites.Add(invaderControl);
                    }
                    else
                    {
                        FrameworkElement invaderControl = _invaders[invader];
                        InvadersHelper.MoveElementOnCanvas(invaderControl, invader.Location.X * Scale, 
                            invader.Location.Y * Scale);
                        InvadersHelper.ResizeElement(invaderControl, invader.Size.Width * Scale,
                            invader.Size.Height * Scale);
                    }
                }
                else if (e.ShipUpdated is Player)
                {
                    if (_playerFlashing)
                    {
                        AnimatedImage playerImage = _playerControl as AnimatedImage;
                        playerImage.StopFlashing();
                        _playerFlashing = false;
                    }
                    if (_playerControl == null)
                    {
                        _playerControl = InvadersHelper.ShipControlFactory(e.ShipUpdated as Player, Scale);
                        _sprites.Add(_playerControl);
                    }
                    else
                    {
                        InvadersHelper.MoveElementOnCanvas(_playerControl, e.ShipUpdated.Location.X * Scale,
                            e.ShipUpdated.Location.Y * Scale);
                        InvadersHelper.ResizeElement(_playerControl, e.ShipUpdated.Size.Width * Scale,
                            e.ShipUpdated.Size.Height * Scale);
                    }
                }
            }
            else
            {
                if (e.ShipUpdated is Mothership)
                {
                    Mothership deadMothership = e.ShipUpdated as Mothership;

                    if (deadMothership == null)
                        return;

                    AnimatedImage deadMothershipControl = _mothershipControl as AnimatedImage;
                    if (deadMothershipControl != null)
                    {
                        deadMothershipControl.InvaderShot();
                        _shotInvaders[deadMothershipControl] = DateTime.Now;
                        _mothershipControl = null;
                    }
                }
                else if (e.ShipUpdated is Invader)
                {
                    Invader deadInvader = e.ShipUpdated as Invader;

                    if (!_invaders.ContainsKey(deadInvader))
                        return;

                    AnimatedImage deadInvaderControl = _invaders[deadInvader] as AnimatedImage;

                    if (deadInvaderControl != null)
                    {
                        deadInvaderControl.InvaderShot();
                        _shotInvaders[deadInvaderControl] = DateTime.Now;
                        _invaders.Remove(deadInvader);
                    }
                }
                else if (e.ShipUpdated is Player)
                {
                    AnimatedImage playerImage = _playerControl as AnimatedImage;
                    if (playerImage != null)
                        playerImage.StartFlashing();
                    _playerFlashing = true;
                }
            }
        }

        private void ModelShotMovedEventHandler(object sender, ShotMovedEventArgs e)
        {
            if (!e.Disappeared)
            {
                if (!_shots.ContainsKey(e.Shot))
                {
                    FrameworkElement shotControl = InvadersHelper.ShotControlFactory(e.Shot, Scale);
                    _shots[e.Shot] = shotControl;
                    _sprites.Add(shotControl);
                }
                else
                {
                    InvadersHelper.MoveElementOnCanvas(_shots[e.Shot], e.Shot.Location.X * Scale,
                        e.Shot.Location.Y * Scale);
                }
            }
            else
            {
                if (_shots.ContainsKey(e.Shot))
                {
                    _sprites.Remove(_shots[e.Shot]);
                    _shots.Remove(e.Shot);
                }
            }
        }

        private void ModelStarChangedEventHandler(object sender, StarChangedEventArgs e)
        {
            if (e.Disappeared && _stars.ContainsKey(e.Point))
            {
                FrameworkElement starToRemove = _stars[e.Point];
                _sprites.Remove(starToRemove);
            }
            else
            {
                if (!_stars.ContainsKey(e.Point))
                {
                    FrameworkElement newStar = InvadersHelper.StarControlFactory(e.Point, Scale);
                    _stars[e.Point] = newStar;
                    _sprites.Add(newStar);
                }
            }
        }

        private void TimerTickEventHandler(object sender, object e)
        {
            if (_lastPaused != Paused)
            {
                _lastPaused = Paused;
                OnPropertyChanged("Paused");
            }
            if (!Paused)
            {
                if (_leftAction != null && _rightAction != null)
                {
                    // Use the most recent action to decide which direction to move.
                    if (DateTime.Compare((DateTime)_leftAction, (DateTime)_rightAction) > 0)
                        _model.MovePlayer(Direction.Left);
                    else
                        _model.MovePlayer(Direction.Right);
                }
                else
                {
                    if (_leftAction.HasValue)
                        _model.MovePlayer(Direction.Left);
                    else if (_rightAction.HasValue)
                        _model.MovePlayer(Direction.Right);
                }
            }

            _model.Update(Paused);
            
            if (Score != _model.Score)
            {
                Score = _model.Score;
                OnPropertyChanged("Score");
            }

            if (_model.Lives >= 0)
            {
                while(_model.Lives > _lives.Count)
                    _lives.Add(new object());
                while(_model.Lives < _lives.Count)
                    _lives.RemoveAt(0);
            }  

            foreach (FrameworkElement control in _shotInvaders.Keys.ToList())
            {
                if (DateTime.Now - _shotInvaders[control] > TimeSpan.FromSeconds(0.3))
                {
                    _sprites.Remove(control);
                    _shotInvaders.Remove(control);
                }
            }

            if (_model.GameOver)
            {
                _timer.Stop();
                OnPropertyChanged("GameOver");
            }
        }

        internal void KeyDown(VirtualKey virtualKey)
        {
            if (virtualKey == VirtualKey.Space)
                _model.FireShot();
            if (virtualKey == VirtualKey.Left)
                _leftAction = DateTime.Now;
            if (virtualKey == VirtualKey.Right)
                _rightAction = DateTime.Now;
        }

        internal void KeyUp(VirtualKey virtualKey)
        {
            if (virtualKey == VirtualKey.Left)
                _leftAction = null;
            if (virtualKey == VirtualKey.Right)
                _rightAction = null;
        }

        internal void LeftGestureStarted()
        {
            _leftAction = DateTime.Now;
        }

        internal void LeftGestureCompleted()
        {
            _leftAction = null;
        }
        
        internal void RightGestureStarted()
        {
            _rightAction = DateTime.Now;
        }

        internal void RightGestureCompleted()
        {
            _rightAction = null;
        }

        internal void Tapped()
        {
            _model.FireShot();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if (propertyChanged != null)
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
