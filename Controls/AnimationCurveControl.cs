using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FreeDSender.Controls
{
    public class AnimationCurveControl : Canvas
    {   
        private readonly List<Point> _points = new List<Point>();
        private readonly Polyline _curve;
        private readonly int _maxPoints = 100;

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(AnimationCurveControl),
                new PropertyMetadata(Brushes.Blue, OnStrokeChanged));

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AnimationCurveControl)d;
            control._curve.Stroke = (Brush)e.NewValue;
        }

        public AnimationCurveControl()
        {   
            _curve = new Polyline
            {
                Stroke = Stroke,
                StrokeThickness = 2
            };
            Children.Add(_curve);

            // 添加网格线
            for (int i = 1; i < 4; i++)
            {
                var horizontalLine = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)),
                    StrokeThickness = 1,
                    X1 = 0,
                    Y1 = 0,
                    X2 = 0,
                    Y2 = 0
                };
                Children.Add(horizontalLine);

                var verticalLine = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)),
                    StrokeThickness = 1,
                    X1 = 0,
                    Y1 = 0,
                    X2 = 0,
                    Y2 = 0
                };
                Children.Add(verticalLine);
            }

            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {   
            UpdateGridLines();
            UpdateCurve();
        }

        private void UpdateGridLines()
        {   
            for (int i = 1; i < 4; i++)
            {
                var horizontalLine = Children[i] as Line;
                if (horizontalLine != null)
                {
                    horizontalLine.Y1 = horizontalLine.Y2 = ActualHeight * i / 4;
                    horizontalLine.X2 = ActualWidth;
                }

                var verticalLine = Children[i + 3] as Line;
                if (verticalLine != null)
                {
                    verticalLine.X1 = verticalLine.X2 = ActualWidth * i / 4;
                    verticalLine.Y2 = ActualHeight;
                }
            }
        }

        public void AddPoint(double normalizedValue)
        {   
            if (_points.Count >= _maxPoints)
            {
                _points.RemoveAt(0);
            }

            double x = _points.Count * (ActualWidth / (_maxPoints - 1));
            double y = ActualHeight - (normalizedValue * ActualHeight);
            _points.Add(new Point(x, y));

            UpdateCurve();
        }

        private void UpdateCurve()
        {   
            if (_points.Count < 2) return;

            var scaledPoints = new List<Point>();
            double xScale = ActualWidth / (_maxPoints - 1);

            for (int i = 0; i < _points.Count; i++)
            {
                var point = _points[i];
                scaledPoints.Add(new Point(i * xScale, point.Y));
            }

            _curve.Points = new PointCollection(scaledPoints);
        }

        public void Clear()
        {   
            _points.Clear();
            _curve.Points.Clear();
        }
    }
}