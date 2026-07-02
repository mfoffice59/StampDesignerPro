using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using StampDesignerPro.Models;
using StampDesignerPro.Rendering;

namespace StampDesignerPro.Views;

public sealed partial class MainWindow : Window
{
    readonly PreviewPanel preview = new();
    readonly StampProject project = new();
    Bitmap? logoBitmap;
    bool updatingUi;

    public MainWindow()
    {
        InitializeComponent();

        CanvasHost.Children.Add(preview);
        preview.Project = project;
        preview.GetEraserMode = () => EraserModeBox.IsChecked ?? false;
        preview.ProjectChanged = SyncLogoUiFromProject;

        RegisterEvents();
        ApplyFromUi();
    }

    void RegisterEvents()
    {
        OuterStyleBox.SelectionChanged += (_, _) => ApplyFromUi();
        TransparentBox.IsCheckedChanged += (_, _) => ApplyFromUi();

        foreach (var box in new[]
        {
            ColorBox, BandTextBox, BandWidthBox, BandRadiusBox, BandFontSizeBox,
            TopTextBox, BottomTextBox, InnerTextBox, FontSizeBox, LetterSpacingBox,
            TextRadiusBox, InnerFontSizeBox, InnerTextRadiusBox, OuterRadiusBox,
            SecondRadiusBox, InnerRadiusBox, LineWidthBox,
            LogoXBox, LogoYBox, LogoSizeBox, LogoOpacityBox, EraserSizeBox
        })
        {
            box.TextChanged += (_, _) => ApplyFromUi();
        }

        foreach (var box in new[]
        {
            BandWidthBox, BandRadiusBox, BandFontSizeBox, FontSizeBox, LetterSpacingBox,
            TextRadiusBox, InnerFontSizeBox, InnerTextRadiusBox, OuterRadiusBox,
            SecondRadiusBox, InnerRadiusBox, LineWidthBox,
            LogoXBox, LogoYBox, LogoSizeBox, LogoOpacityBox, EraserSizeBox
        })
        {
            RegisterNumericWheel(box, 1);
        }

        LoadLogoButton.Click += async (_, _) => await LoadLogoAsync();
        DeleteLogoButton.Click += (_, _) => DeleteLogo();
        CenterLogoButton.Click += (_, _) => CenterLogo();
        ClearEraserButton.Click += (_, _) => ClearEraser();
        EraserModeBox.IsCheckedChanged += (_, _) => preview.InvalidateVisual();
    }

    async System.Threading.Tasks.Task LoadLogoAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите логотип",
            AllowMultiple = false,
            Filters =
            {
                new FileDialogFilter { Name = "Images", Extensions = { "png", "jpg", "jpeg", "bmp" } }
            }
        };

        var result = await dialog.ShowAsync(this);
        if (result == null || result.Length == 0)
            return;

        try
        {
            logoBitmap?.Dispose();
            logoBitmap = new Bitmap(result[0]);
            project.Logo.FilePath = result[0];
            project.Logo.Visible = true;
            preview.LogoBitmap = logoBitmap;
            ApplyFromUi();
        }
        catch (Exception ex)
        {
            await new Window
            {
                Title = "Ошибка",
                Width = 440,
                Height = 170,
                Content = new TextBlock
                {
                    Text = "Не удалось загрузить логотип:\n" + ex.Message,
                    Margin = new Thickness(20)
                }
            }.ShowDialog(this);
        }
    }

    void DeleteLogo()
    {
        logoBitmap?.Dispose();
        logoBitmap = null;
        project.Logo.FilePath = null;
        project.Logo.Visible = false;
        preview.LogoBitmap = null;
        preview.InvalidateVisual();
    }

    void CenterLogo()
    {
        project.Logo.X = 0;
        project.Logo.Y = 25;
        SyncLogoUiFromProject();
        preview.InvalidateVisual();
    }

    void ClearEraser()
    {
        project.Eraser.Points.Clear();
        preview.InvalidateVisual();
    }

    void SyncLogoUiFromProject()
    {
        updatingUi = true;
        LogoXBox.Text = project.Logo.X.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        LogoYBox.Text = project.Logo.Y.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        LogoSizeBox.Text = project.Logo.Size.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        updatingUi = false;
        preview.InvalidateVisual();
    }

    void ApplyFromUi()
    {
        if (updatingUi)
            return;

        project.OuterStyle = OuterStyleBox.SelectedIndex == 1 ? "band" : "double";
        project.StampColor = ColorBox.Text ?? "#003f9e";
        project.TransparentBackground = TransparentBox.IsChecked ?? true;

        project.BandText = BandTextBox.Text ?? "";
        project.BandWidth = ReadDouble(BandWidthBox, 54);
        project.BandRadius = ReadDouble(BandRadiusBox, 333);
        project.BandFontSize = ReadDouble(BandFontSizeBox, 25);

        project.TopText = TopTextBox.Text ?? "";
        project.BottomText = BottomTextBox.Text ?? "";
        project.InnerText = InnerTextBox.Text ?? "";

        project.FontSize = ReadDouble(FontSizeBox, 27);
        project.LetterSpacing = ReadDouble(LetterSpacingBox, 2);
        project.TextRadius = ReadDouble(TextRadiusBox, 285);
        project.InnerFontSize = ReadDouble(InnerFontSizeBox, 25);
        project.InnerTextRadius = ReadDouble(InnerTextRadiusBox, 202);

        project.OuterRadius = ReadDouble(OuterRadiusBox, 345);
        project.SecondRadius = ReadDouble(SecondRadiusBox, 323);
        project.InnerRadius = ReadDouble(InnerRadiusBox, 232);
        project.LineWidth = ReadDouble(LineWidthBox, 5);

        project.Logo.X = ReadDouble(LogoXBox, project.Logo.X);
        project.Logo.Y = ReadDouble(LogoYBox, project.Logo.Y);
        project.Logo.Size = ReadDouble(LogoSizeBox, 170);
        project.Logo.Opacity = ReadDouble(LogoOpacityBox, 100);

        project.Eraser.Size = ReadDouble(EraserSizeBox, 28);

        preview.InvalidateVisual();
    }

    static double ReadDouble(TextBox box, double fallback)
    {
        if (double.TryParse((box.Text ?? "").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        return fallback;
    }

    static void RegisterNumericWheel(TextBox box, double step)
    {
        box.PointerWheelChanged += (_, e) =>
        {
            var current = ReadDouble(box, 0);
            var delta = e.Delta.Y > 0 ? step : -step;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                delta *= 10;

            box.Text = Math.Max(0, current + delta).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            e.Handled = true;
        };
    }
}

public sealed class PreviewPanel : Control
{
    public StampProject Project { get; set; } = new();
    public Bitmap? LogoBitmap { get; set; }
    public Func<bool>? GetEraserMode { get; set; }
    public Action? ProjectChanged { get; set; }

    bool draggingLogo;
    bool erasing;
    Point lastPoint;
    Point? eraserCursor;

    public PreviewPanel()
    {
        Focusable = true;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        PointerExited += (_, _) =>
        {
            eraserCursor = null;
            InvalidateVisual();
        };
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        StampRenderer.Render(context, Project, Bounds, LogoBitmap, GetEraserMode?.Invoke() == true, eraserCursor);
    }

    void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        var p = e.GetPosition(this);
        lastPoint = p;

        if (GetEraserMode?.Invoke() == true)
        {
            erasing = true;
            eraserCursor = p;
            AddEraserPoint(p);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (LogoBitmap != null && HitLogo(p))
        {
            draggingLogo = true;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var p = e.GetPosition(this);

        if (GetEraserMode?.Invoke() == true)
        {
            eraserCursor = p;

            if (erasing || e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                AddEraserPoint(p);
                e.Handled = true;
            }

            InvalidateVisual();
            return;
        }

        if (draggingLogo)
        {
            var scale = GetScale();
            if (scale <= 0) return;

            Project.Logo.X += (p.X - lastPoint.X) / scale;
            Project.Logo.Y += (p.Y - lastPoint.Y) / scale;
            lastPoint = p;

            ProjectChanged?.Invoke();
            e.Handled = true;
        }
    }

    void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        draggingLogo = false;
        erasing = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (GetEraserMode?.Invoke() == true)
        {
            var deltaE = e.Delta.Y > 0 ? 2 : -2;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                deltaE *= 5;

            Project.Eraser.Size = Math.Max(2, Project.Eraser.Size + deltaE);
            ProjectChanged?.Invoke();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (LogoBitmap == null)
            return;

        var delta = e.Delta.Y > 0 ? 6 : -6;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            delta *= 3;

        Project.Logo.Size = Math.Max(10, Project.Logo.Size + delta);
        ProjectChanged?.Invoke();
        e.Handled = true;
    }

    void AddEraserPoint(Point p)
    {
        var scale = GetScale();
        var center = GetCenter();
        if (scale <= 0) return;

        Project.Eraser.Points.Add(new EraserPoint
        {
            X = (p.X - center.X) / scale,
            Y = (p.Y - center.Y) / scale,
            Size = Project.Eraser.Size
        });

        InvalidateVisual();
    }

    bool HitLogo(Point p)
    {
        var scale = GetScale();
        var center = GetCenter();
        var size = Project.Logo.Size * scale;
        var x = center.X + Project.Logo.X * scale - size / 2;
        var y = center.Y + Project.Logo.Y * scale - size / 2;
        return new Rect(x, y, size, size).Contains(p);
    }

    double GetScale()
    {
        return Math.Min(Bounds.Width, Bounds.Height) / StampRenderer.BaseSize;
    }

    Point GetCenter()
    {
        return new Point(Bounds.Width / 2, Bounds.Height / 2);
    }
}
