using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Invaders.View
{
    using Model;
    using Windows.Foundation;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Media.Imaging;
    using Windows.UI.Xaml.Shapes;

    static class InvadersHelper
    {
        private static Random _random = new Random();

        // Rather than have separate factory methods for players and invaders, I combined the code into
        // a ShipControlFactory method to reduce duplicate code and make future changes easier.
        public static FrameworkElement ShipControlFactory(Ship ship, double scale)
        {
            AnimatedImage shipImage = new AnimatedImage();
            if (ship is Invader)
            {
                Invader invader = ship as Invader;
                shipImage = new AnimatedImage(GenerateImageList(invader.InvaderType),
                    TimeSpan.FromMilliseconds(500));
            }
            else if (ship is Player)
            {
                Player player = ship as Player;
                shipImage = new AnimatedImage(new List<string>() { "player.png", "player.png" }, 
                    TimeSpan.FromSeconds(1));
            }
            shipImage.Width = ship.Size.Width * scale;
            shipImage.Height = ship.Size.Height * scale;
            SetCanvasLocation(shipImage, ship.Location.X * scale, ship.Location.Y * scale);
            return shipImage;
        }

        public static void ResizeElement(FrameworkElement control, double width, double height)
        {
            control.Width = width;
            control.Height = height;
        }
        
        public static IEnumerable<string> GenerateImageList(InvaderType invaderType)
        {
            string fileName = "";
            List<string> imageNames = new List<string>();

            switch (invaderType)
            {
                case InvaderType.Bug:
                    fileName = "bug";
                    break;
                case InvaderType.Mothership:
                    fileName = "Mothership";
                    break;
                case InvaderType.Saucer:
                    fileName = "flyingsaucer";
                    break;
                case InvaderType.Satellite:
                    fileName = "satellite";
                    break;
                case InvaderType.Spaceship:
                    fileName = "spaceship";
                    break;
                case InvaderType.Star:
                    fileName = "star";
                    break;
                default:
                    break;
            };

            if (!string.IsNullOrEmpty(fileName))
            {
                int numImages = 4;
                if (invaderType == InvaderType.Mothership)
                    numImages = 3;

                for (int i = 1; i <= numImages; i++)
                {
                    imageNames.Add(fileName + i + ".png");
                }
            }

            return imageNames;
        }

        public static FrameworkElement ShotControlFactory(Shot shot, double scale)
        {
            FrameworkElement shotRectangle;

            shotRectangle = new Rectangle()
            {
                Width = Shot.ShotSize.Width * scale,
                Height = Shot.ShotSize.Height * scale,
                Fill = new SolidColorBrush(Colors.Yellow),
            };
            SetCanvasLocation(shotRectangle, shot.Location.X * scale, shot.Location.Y * scale);
            return shotRectangle;
        }

        public static FrameworkElement StarControlFactory(Point point, double scale)
        {
            FrameworkElement star;

            switch (_random.Next(3))
            {
                case 0:
                    star = new Rectangle()
                    {
                        Width = 2,
                        Height = 2,
                        Fill = new SolidColorBrush(GetRandomStarColor()),
                    };
                    break;
                case 1:
                    star = new Ellipse()
                    {
                        Width = 2,
                        Height = 2,
                        Fill = new SolidColorBrush(GetRandomStarColor()),
                    };
                    break;
                default:
                    star = new StarControl();
                    ((StarControl)star).SetFill(new SolidColorBrush(GetRandomStarColor()));
                    break;
            };

            SetCanvasLocation(star, point.X * scale, point.Y * scale);
            SendToBack(star);

            return star;
        }

        public static FrameworkElement ScanLineFactory(int y, int width, double scale)
        {
            Rectangle scanLine = new Rectangle()
            {
                Height = 2,
                Width = width * scale,
                Opacity = .1,
                Fill = new SolidColorBrush(Colors.White),
            };
            SetCanvasLocation(scanLine, 0, y * scale);

            return scanLine as FrameworkElement;
        }

        public static void SetCanvasLocation(UIElement control, double x, double y)
        {
            Canvas.SetLeft(control, x);
            Canvas.SetTop(control, y);
        }

        public static void MoveElementOnCanvas(UIElement control, double toX, double toY)
        {
            double fromX = Canvas.GetLeft(control);
            double fromY = Canvas.GetTop(control);
            
            Storyboard storyboard = new Storyboard();

            DoubleAnimation animationX = CreateDoubleAnimation(control, fromX, toX, "(Canvas.Left)");
            DoubleAnimation animationY = CreateDoubleAnimation(control, fromY, toY, "(Canvas.Top)");
            storyboard.Children.Add(animationX);
            storyboard.Children.Add(animationY);

            storyboard.Begin();
        }

        public static DoubleAnimation CreateDoubleAnimation(UIElement control, double from, double to,
            string property)
        {
            return CreateDoubleAnimation(control, from, to, property, TimeSpan.FromMilliseconds(25));
        }

        public static DoubleAnimation CreateDoubleAnimation(UIElement control, double from, double to,
            string property, TimeSpan duration)
        {
            DoubleAnimation animation = new DoubleAnimation();
            Storyboard.SetTarget(animation, control);
            Storyboard.SetTargetProperty(animation, property);
            animation.From = from;
            animation.To = to;
            animation.Duration = duration;

            return animation;
        }

        public static BitmapImage CreateImageFromAssets(string imageFilename)
        {
            return new BitmapImage(new Uri("ms-appx:///Assets/" + imageFilename));
        }

        public static Color GetRandomStarColor()
        {
            switch(_random.Next(9))
            {
                case 0:
                    return Colors.WhiteSmoke;
                case 1:
                    return Colors.Turquoise;
                case 2:
                    return Colors.Silver;
                case 3:
                    return Colors.PaleGoldenrod;
                case 4:
                    return Colors.OrangeRed;
                case 5:
                    return Colors.LightCyan;
                case 6:
                    return Colors.Fuchsia;
                case 7:
                    return Colors.AntiqueWhite;
                case 8:
                    return Colors.LightSkyBlue;
                default:
                    return Colors.Blue;
            };
        }

        public static void SendToBack(UIElement control)
        {
            Canvas.SetZIndex(control, -1000);
        }
    }
}
