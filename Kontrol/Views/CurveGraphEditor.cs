using Kontrol.Models.Fan;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Kontrol.Views;

public class CurveGraphEditor : Canvas
{
    private const double PointRadius = 7;
    private const double Padding = 40;

    private int _dragIndex = -1;
    private readonly List<Ellipse> _pointVisuals = new();

    public static readonly DependencyProperty CurveProperty =
        DependencyProperty.Register(nameof(Curve), typeof(FanCurve), typeof(CurveGraphEditor),
            new FrameworkPropertyMetadata(null, OnCurveChanged));

    public FanCurve? Curve
    {
        get => (FanCurve?)GetValue(CurveProperty);
        set => SetValue(CurveProperty, value);
    }

    public CurveGraphEditor()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;
        MinHeight = 200;
    }

    private static void OnCurveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CurveGraphEditor editor) editor.Redraw();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        Redraw();
    }

    private Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        try { return (Brush)FindResource(resourceKey); }
        catch { return fallback; }
    }

    private void Redraw()
    {
        Children.Clear();
        _pointVisuals.Clear();

        if (Curve is null || ActualWidth < 1 || ActualHeight < 1) return;

        double w = ActualWidth;
        double h = ActualHeight;
        double left = Padding, right = w - 10;
        double top = 10, bottom = h - Padding;

        var gridBrush = ResolveBrush("ControlStrokeColorDefaultBrush", new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)));
        var textBrush = ResolveBrush("TextFillColorSecondaryBrush", new SolidColorBrush(Color.FromArgb(150, 128, 128, 128)));
        var accentBrush = ResolveBrush("AccentFillColorDefaultBrush", new SolidColorBrush(Color.FromRgb(103, 58, 183)));

        DrawGrid(left, right, top, bottom, gridBrush);
        DrawCurveLine(left, right, top, bottom);
        DrawPoints(left, right, top, bottom, textBrush, accentBrush);
        DrawAxisLabels(left, right, top, bottom, textBrush);
    }

    private void DrawGrid(double left, double right, double top, double bottom, Brush gridBrush)
    {
        for (int t = 0; t <= 100; t += 20)
        {
            double x = left + (right - left) * t / 100.0;
            Children.Add(new Line { X1 = x, Y1 = top, X2 = x, Y2 = bottom, Stroke = gridBrush, StrokeThickness = 1, Opacity = 0.4 });
        }

        for (int p = 0; p <= 100; p += 20)
        {
            double y = bottom - (bottom - top) * p / 100.0;
            Children.Add(new Line { X1 = left, Y1 = y, X2 = right, Y2 = y, Stroke = gridBrush, StrokeThickness = 1, Opacity = 0.4 });
        }
    }

    private void DrawCurveLine(double left, double right, double top, double bottom)
    {
        var points = Curve!.Points.OrderBy(p => p.TempC).ToList();
        if (points.Count < 2) return;

        float minT = 0, maxT = 100;
        var pathFigure = new PathFigure();
        bool first = true;

        for (float t = minT; t <= maxT; t += 0.5f)
        {
            float pct = CurveInterpolator.Interpolate(points, t);
            double x = left + (right - left) * (t - minT) / (maxT - minT);
            double y = bottom - (bottom - top) * pct / 100.0;

            if (first)
            {
                pathFigure.StartPoint = new Point(x, y);
                first = false;
            }
            else
            {
                pathFigure.Segments.Add(new LineSegment(new Point(x, y), true));
            }
        }

        var gradient = new LinearGradientBrush(
            Color.FromRgb(76, 175, 80), Color.FromRgb(244, 67, 54),
            new Point(0, 0), new Point(1, 0));

        Children.Add(new Path
        {
            Data = new PathGeometry(new[] { pathFigure }),
            Stroke = gradient,
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round
        });

        var fillFigure = new PathFigure { StartPoint = pathFigure.StartPoint };
        foreach (var seg in pathFigure.Segments) fillFigure.Segments.Add(seg.Clone());

        fillFigure.Segments.Add(new LineSegment(new Point(left + (right - left), bottom), true));
        fillFigure.Segments.Add(new LineSegment(new Point(left, bottom), true));
        fillFigure.IsClosed = true;

        var fillGradient = new LinearGradientBrush(
            Color.FromArgb(40, 76, 175, 80), Color.FromArgb(40, 244, 67, 54),
            new Point(0, 0), new Point(1, 0));

        Children.Add(new Path { Data = new PathGeometry(new[] { fillFigure }), Fill = fillGradient });
    }

    private void DrawPoints(double left, double right, double top, double bottom, Brush textBrush, Brush accentBrush)
    {
        var points = Curve!.Points.OrderBy(p => p.TempC).ToList();
        float minT = 0, maxT = 100;
        var strokeBrush = ResolveBrush("TextFillColorPrimaryBrush", Brushes.White);

        for (int i = 0; i < points.Count; i++)
        {
            var pt = points[i];
            double x = left + (right - left) * (pt.TempC - minT) / (maxT - minT);
            double y = bottom - (bottom - top) * pt.Percent / 100.0;

            var ellipse = new Ellipse
            {
                Width = PointRadius * 2,
                Height = PointRadius * 2,
                Fill = accentBrush,
                Stroke = strokeBrush,
                StrokeThickness = 2,
                Cursor = Cursors.Hand,
                Tag = i
            };

            SetLeft(ellipse, x - PointRadius);
            SetTop(ellipse, y - PointRadius);

            ellipse.MouseLeftButtonDown += OnPointMouseDown;
            ellipse.MouseLeftButtonUp += OnPointMouseUp;
            ellipse.MouseMove += OnPointMouseMove;

            Children.Add(ellipse);
            _pointVisuals.Add(ellipse);

            var label = new TextBlock
            {
                Text = $"{pt.TempC:F0}°C, {pt.Percent:F0}%",
                FontSize = 10,
                Foreground = textBrush
            };
            SetLeft(label, x - 20);
            SetTop(label, y - 20);
            Children.Add(label);
        }
    }

    private void DrawAxisLabels(double left, double right, double top, double bottom, Brush textBrush)
    {
        for (int t = 0; t <= 100; t += 20)
        {
            double x = left + (right - left) * t / 100.0;
            var tb = new TextBlock { Text = $"{t}°", FontSize = 10, Foreground = textBrush };
            SetLeft(tb, x - 10);
            SetTop(tb, bottom + 4);
            Children.Add(tb);
        }

        for (int p = 0; p <= 100; p += 20)
        {
            double y = bottom - (bottom - top) * p / 100.0;
            var tb = new TextBlock { Text = $"{p}%", FontSize = 10, Foreground = textBrush };
            SetLeft(tb, 2);
            SetTop(tb, y - 8);
            Children.Add(tb);
        }
    }

    private void OnPointMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse el && el.Tag is int idx)
        {
            _dragIndex = idx;
            el.CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnPointMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse el)
        {
            _dragIndex = -1;
            el.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void OnPointMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragIndex < 0 || Curve is null) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var points = Curve.Points.OrderBy(p => p.TempC).ToList();
        if (_dragIndex >= points.Count) return;

        double w = ActualWidth, h = ActualHeight;
        double left = Padding, right = w - 10;
        double top = 10, bottom = h - Padding;

        var pos = e.GetPosition(this);
        float tempC = (float)((pos.X - left) / (right - left) * 100.0);
        float percent = (float)((bottom - pos.Y) / (bottom - top) * 100.0);

        tempC = Math.Clamp(tempC, 0, 100);
        percent = Math.Clamp(percent, 0, 100);

        var point = points[_dragIndex];
        point.TempC = MathF.Round(tempC);
        point.Percent = MathF.Round(percent);

        Redraw();
    }
}
