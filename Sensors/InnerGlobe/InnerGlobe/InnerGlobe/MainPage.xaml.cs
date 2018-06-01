using System;
using System.Numerics;

using Xamarin.Forms;
using Xamarin.Essentials;

using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace InnerGlobe
{
    public partial class MainPage : ContentPage
    {
        double PIXELS_PER_DEGREE = 45;
     //   Motion motion;
        HorizontalCoordinateProjection coordinateProjection = new HorizontalCoordinateProjection();
        //   Brush lineBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 128, 128, 255));
        // Brush textBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 192, 192, 255));


        SKPaint linePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(0xFF, 0x80, 0x80),
            StrokeWidth = 3
        };


        SKPaint textPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(0xFF, 0xC0, 0xC0),
            TextSize = 48
        };

        double width, height;



        string[] compass = { "North", "NE", "East", "SE", "South", "SW", "West", "NW" };
        //   bool elementsCreated;

        Quaternion quaternion;
        Matrix4x4 rotationMatrix;




        public MainPage()
        {
            InitializeComponent();

            OrientationSensor.ReadingChanged += OnOrientationSensorReadingChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                OrientationSensor.Start(SensorSpeed.Normal);
            }
            catch
            {
                Content = new Label
                {
                    Text = "Sorry, an orientation sensor is not available on this device",
                    FontSize = 24,
                    TextColor = Color.White,
                    BackgroundColor = Color.DarkGray,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(48, 0)
                };
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            OrientationSensor.Stop();
        }

        void OnOrientationSensorReadingChanged(OrientationSensorChangedEventArgs args)
        {
            quaternion = args.Reading.Orientation;

            rotationMatrix = Matrix4x4.CreateFromQuaternion(quaternion);

            System.Diagnostics.Debug.WriteLine(args.Reading.Orientation);


            canvasView.InvalidateSurface();
        }

        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear(SKColors.Black); //  SKColors.CornflowerBlue);

            width = info.Width;
            height = info.Height;



            HorizontalCoordinate viewCenter = HorizontalCoordinate.FromRotationMatrix(rotationMatrix);
            coordinateProjection.SetViewCenter(viewCenter);

            // rotate.Angle = -viewCenter.Tilt;


            // Draw azimuth lines
            for (double azimuth = 0; azimuth < 360; azimuth += 15)
            {
                for (double altitude = -90; altitude < 90; altitude += 15)
                {
                    HorizontalCoordinate coord1 = new HorizontalCoordinate(azimuth, altitude);
                    HorizontalCoordinate coord2 = new HorizontalCoordinate(azimuth, altitude + 15);
                    DrawLine(canvas, coord1, coord2);
                }
            }

            // Draw altitude lines
            for (double altitude = -75; altitude < 90; altitude += 15)
            {
                for (double azimuth = 0; azimuth < 360; azimuth += 15)
                {
                    HorizontalCoordinate coord1 = new HorizontalCoordinate(azimuth, altitude);
                    HorizontalCoordinate coord2 = new HorizontalCoordinate(azimuth + 15, altitude);
                    DrawLine(canvas, coord1, coord2);
                }
            }

            // Draw text for altitude angles
            for (double altitude = -75; altitude < 90; altitude += 15)
            {
                HorizontalCoordinate coord = new HorizontalCoordinate(viewCenter.Azimuth, altitude);
                DrawText(canvas, altitude.ToString(), coord);
            }

            // Draw text for azimuth compass points
            for (double azimuth = 0; azimuth < 360; azimuth += 45)
            {
                double altitude = Math.Min(80, Math.Max(-80, viewCenter.Altitude));
                HorizontalCoordinate coord = new HorizontalCoordinate(azimuth, altitude);
                DrawText(canvas, compass[(int)(azimuth / 45)], coord);
            }
        }


        void DrawLine(SKCanvas canvas, HorizontalCoordinate coord1, HorizontalCoordinate coord2)
        {
            SKPoint point1 = CalculateScreenPoint(coord1);
            SKPoint point2 = CalculateScreenPoint(coord2);

            // If points are out of range, move them offscreen
            if (double.IsNaN(point1.X) || double.IsNaN(point2.X))
            {
                point1 = point2 = new SKPoint(-1000, -1000);
            }

            canvas.DrawLine(point1, point2, linePaint);
/*
            Line line = null;

            // Create Line first time through
            if (!elementsCreated)
            {
                line = new Line
                {
                    Stroke = lineBrush,
                    StrokeThickness = 3
                };
                canvas.Children.Add(line);
            }
            // Get created Line otherwise
            else
            {
                line = canvas.Children[childIndex] as Line;
                childIndex += 1;
            }

            line.X1 = point1.X;
            line.Y1 = point1.Y;
            line.X2 = point2.X;
            line.Y2 = point2.Y;

*/
        }


        void DrawText(SKCanvas canvas, string str, HorizontalCoordinate coord)
        {
            SKPoint point = CalculateScreenPoint(coord);

            // If point is out of range, move them offscreen
            if (double.IsNaN(point.X))
            {
                point = new SKPoint(-1000, -1000);
            }

            canvas.DrawText(str, point, textPaint);
/*
            TextBlock txtblk = null;

            // Create TextBlock first time through
            if (!elementsCreated)
            {
                txtblk = new TextBlock
                {
                    Text = str,
                    FontSize = 48,
                    Foreground = textBrush,
                };
                canvas.Children.Add(txtblk);
            }
            // Get created TextBlock otherwise
            else
            {
                txtblk = canvas.Children[childIndex] as TextBlock;
                childIndex += 1;
            }

            Canvas.SetLeft(txtblk, point.X - txtblk.ActualWidth / 2);
            Canvas.SetTop(txtblk, point.Y - txtblk.ActualHeight / 2);
*/
        }

        SKPoint CalculateScreenPoint(HorizontalCoordinate horizontalCoordinate)
        {
            double horzAngle, vertAngle;
            coordinateProjection.GetAngleOffsets(horizontalCoordinate, out horzAngle, out vertAngle);

            // Use NaN to indicate points clearly out of range of the screen
            float x = float.NaN;
            float y = float.NaN;

            if (horzAngle > -90 && horzAngle < 90 && vertAngle > -90 && vertAngle < 90)
            {
                x = (float)(width / 2 + PIXELS_PER_DEGREE * horzAngle);
                y = (float)(height / 2 + PIXELS_PER_DEGREE * vertAngle);
            }
            return new SKPoint(x, y);
        }
    }
}
