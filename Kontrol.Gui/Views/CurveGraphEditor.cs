using Kontrol.Fan;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.UI;
using ShapesPath = Microsoft.UI.Xaml.Shapes.Path;

namespace Kontrol.Views;

public class CurveGraphEditor : Canvas
{
    private const double PointRadius = 7;
    private const double Padding = 40;

    private int _dragIndex = -1;
    private Pointer? _capturedPointer;

    public static readonly DependencyProperty CurveProperty =
        DependencyProperty.Register(nameof(Curve), typeof(FanCurve), typeof(CurveGraphEditor),
            new PropertyMetadata(null, OnCurveChanged));

    public FanCurve? Curve
    {
        get => (FanCurve?)GetValue(CurveProperty);
        set => SetValue(CurveProperty, value);
    }

    public CurveGraphEditor()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        MinHeight = 200;
        SizeChanged += (_, _) => Redraw();
        PointerMoved += OnCanvasPointerMoved;
        PointerReleased += OnCanvasPointerReleased;
    }

    private static void OnCurveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CurveGraphEditor editor) return;
        if (e.OldValue is FanCurve old)
            old.Points.CollectionChanged -= editor.OnPointsCollectionChanged;
        if (e.NewValue is FanCurve newCurve)
            newCurve.Points.CollectionChanged += editor.OnPointsCollectionChanged;
        editor.Redraw();
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Redraw();

    private Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        try
        {
            if (Resources.TryGetValue(resourceKey, out var r) && r is Brush b1) return b1;
            if (Application.Current?.Resources?.TryGetValue(resourceKey, out var ar) == true && ar is Brush b2) return b2;
        }
        catch { }
        return fallback;
    }

    private void Redraw()
    {
        Children.Clear();
        if (Curve is null || ActualWidth < 1 || ActualHeight < 1) return;

        double w = ActualWidth, h = ActualHeight;
        double left = Padding, right = w - 10;
        double top = 10, bottom = h - Padding;

        var gridBrush = ResolveBrush("ControlStrokeColorDefaultBrush",
            new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)));
        var textBrush = ResolveBrush("TextFillColorSecondaryBrush",
            new SolidColorBrush(Color.FromArgb(150, 128, 128, 128)));
        var accentBrush = ResolveBrush("AccentFillColorDefaultBrush",
            new SolidColorBrush(Color.FromArgb(255, 103, 58, 183)));

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
            Children.Add(new Line { X1 = x, Y1 = top, X2 = x, Y2 = bottom, Stroke = gridBrush, StrokeThickness = 1 });
        }
        for (int p = 0; p <= 100; p += 20)
        {
            double y = bottom - (bottom - top) * p / 100.0;
            Children.Add(new Line { X1 = left, Y1 = y, X2 = right, Y2 = y, Stroke = gridBrush, StrokeThickness = 1 });
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
                pathFigure.Segments.Add(new LineSegment { Point = new Point(x, y) });
            }
        }

        var lineGeo = new PathGeometry();
        lineGeo.Figures.Add(pathFigure);

        var lineGradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        lineGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 76, 175, 80), Offset = 0 });
        lineGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 244, 67, 54), Offset = 1 });

        Children.Add(new ShapesPath
        {
            Data = lineGeo,
            Stroke = lineGradient,
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round
        });

        var fillFigure = new PathFigure { StartPoint = pathFigure.StartPoint };
        foreach (var seg in pathFigure.Segments)
        {
            if (seg is LineSegment ls)
                fillFigure.Segments.Add(new LineSegment { Point = ls.Point });
        }
        fillFigure.Segments.Add(new LineSegment { Point = new Point(right, bottom) });
        fillFigure.Segments.Add(new LineSegment { Point = new Point(left, bottom) });
        fillFigure.IsClosed = true;

        var fillGeo = new PathGeometry();
        fillGeo.Figures.Add(fillFigure);

        var fillGradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        fillGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(40, 76, 175, 80), Offset = 0 });
        fillGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(40, 244, 67, 54), Offset = 1 });

        Children.Add(new ShapesPath { Data = fillGeo, Fill = fillGradient });
    }

    private void DrawPoints(double left, double right, double top, double bottom, Brush textBrush, Brush accentBrush)
    {
        var points = Curve!.Points.OrderBy(p => p.TempC).ToList();
        float minT = 0, maxT = 100;
        var strokeBrush = ResolveBrush("TextFillColorPrimaryBrush", new SolidColorBrush(Colors.White));

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
                Tag = i
            };

            SetLeft(ellipse, x - PointRadius);
            SetTop(ellipse, y - PointRadius);

            ellipse.PointerPressed += OnPointPointerPressed;
            Children.Add(ellipse);

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

    private void OnPointPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Ellipse el && el.Tag is int idx)
        {
            _dragIndex = idx;
            _capturedPointer = e.Pointer;
            CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_capturedPointer is not null)
        {
            ReleasePointerCapture(_capturedPointer);
            _capturedPointer = null;
        }
        _dragIndex = -1;
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_dragIndex < 0 || Curve is null) return;
        var pp = e.GetCurrentPoint(this);
        if (!pp.Properties.IsLeftButtonPressed) { _dragIndex = -1; return; }

        var points = Curve.Points.OrderBy(p => p.TempC).ToList();
        if (_dragIndex >= points.Count) return;

        double w = ActualWidth, h = ActualHeight;
        double left = Padding, right = w - 10;
        double top = 10, bottom = h - Padding;

        float tempC = (float)((pp.Position.X - left) / (right - left) * 100.0);
        float percent = (float)((bottom - pp.Position.Y) / (bottom - top) * 100.0);

        tempC = Math.Clamp(tempC, 0, 100);
        percent = Math.Clamp(percent, 0, 100);

        var point = points[_dragIndex];
        point.TempC = MathF.Round(tempC);
        point.Percent = MathF.Round(percent);

        Redraw();
        e.Handled = true;
    }
}
