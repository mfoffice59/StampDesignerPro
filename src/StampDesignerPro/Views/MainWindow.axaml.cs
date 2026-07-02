using System;
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

    public MainWindow()
    {
        InitializeComponent();
        CanvasHost.Children.Add(preview);
        preview.Project = project;

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
            SecondRadiusBox, InnerRadiusBox, LineWidthBox, LogoSizeBox, LogoOpacityBox
        })
        {
            box.TextChanged += (_, _) => ApplyFromUi();
        }

        foreach (var box in new[]
        {
            BandWidthBox, BandRadiusBox, BandFontSizeBox, FontSizeBox, LetterSpacingBox,
            TextRadiusBox, InnerFontSizeBox, InnerTextRadiusBox, OuterRadiusBox,
            SecondRadiusBox, InnerRadiusBox, LineWidthBox, LogoSizeBox, LogoOpacityBox
        })
        {
            RegisterNumericWheel(box, 1);
        }

        LoadLogoButton.Click += async (_, _) => await LoadLogoAsync();
        DeleteLogoButton.Click += (_, _) => DeleteLogo();
        CenterLogoButton.Click += (_, _) => CenterLogo();
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
                Width = 420,
                Height = 160,
                Content = new TextBlock
                {
                    Text = "Не удалось загрузить логотип:\n" + ex.Message,
                    Margin = new Avalonia.Thickness(20)
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
        preview.InvalidateVisual();
    }

    void ApplyFromUi()
    {
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

        project.Logo.Size = ReadDouble(LogoSizeBox, 170);
        project.Logo.Opacity = ReadDouble(LogoOpacityBox, 100);

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

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        StampRenderer.Render(context, Project, Bounds, LogoBitmap);
    }
}
