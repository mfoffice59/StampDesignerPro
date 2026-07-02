using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using StampDesignerPro.Models;

namespace StampDesignerPro.Rendering;

public static class StampRenderer
{
    public const double BaseSize = 800;

    public static void Render(DrawingContext context, StampProject project, Rect bounds, Bitmap? logoBitmap = null)
    {
        var scale = Math.Min(bounds.Width, bounds.Height) / BaseSize;
        var cx = bounds.X + bounds.Width / 2;
        var cy = bounds.Y + bounds.Height / 2;

        if (!project.TransparentBackground)
            context.FillRectangle(Brushes.White, bounds);

        var color = ParseColor(project.StampColor);
        var brush = new SolidColorBrush(color);
        var typeface = new Typeface(project.FontFamily);

        if (project.OuterStyle == "band")
        {
            DrawCircle(context, cx, cy, project.BandRadius * scale, new Pen(brush, project.BandWidth * scale));
            DrawCircle(context, cx, cy, (project.BandRadius - project.BandWidth / 2 - 10) * scale, new Pen(brush, project.LineWidth * scale / 1.5));
            DrawCircle(context, cx, cy, project.InnerRadius * scale, new Pen(brush, project.LineWidth * scale / 1.5));
            DrawTextOnCircleTop(context, project.BandText, cx, cy, project.BandRadius * scale, project.BandFontSize * scale, Colors.White, typeface, project.LetterSpacing * scale);
        }
        else
        {
            DrawCircle(context, cx, cy, project.OuterRadius * scale, new Pen(brush, project.LineWidth * scale));
            DrawCircle(context, cx, cy, project.SecondRadius * scale, new Pen(brush, project.LineWidth * scale / 1.5));
            DrawCircle(context, cx, cy, project.InnerRadius * scale, new Pen(brush, project.LineWidth * scale / 1.5));
            DrawTextOnCircleTop(context, project.TopText, cx, cy, project.TextRadius * scale, project.FontSize * scale, color, typeface, project.LetterSpacing * scale);
        }

        DrawLogo(context, project, logoBitmap, cx, cy, scale);

        DrawTextOnCircleBottom(context, project.BottomText, cx, cy, project.TextRadius * scale, project.FontSize * scale, color, typeface, project.LetterSpacing * scale);
        DrawTextOnCircleTop(context, project.InnerText, cx, cy, project.InnerTextRadius * scale, project.InnerFontSize * scale, color, typeface, project.LetterSpacing * scale);
    }

    static void DrawLogo(DrawingContext context, StampProject project, Bitmap? logoBitmap, double cx, double cy, double scale)
    {
        if (logoBitmap == null || !project.Logo.Visible)
            return;

        var size = Math.Max(1, project.Logo.Size * scale);
        var x = cx + project.Logo.X * scale - size / 2;
        var y = cy + project.Logo.Y * scale - size / 2;
        var rect = new Rect(x, y, size, size);
        var opacity = Math.Clamp(project.Logo.Opacity / 100.0, 0, 1);

        using (context.PushOpacity(opacity))
            context.DrawImage(logoBitmap, rect);
    }

    static void DrawCircle(DrawingContext context, double cx, double cy, double r, Pen pen)
    {
        if (r > 0)
            context.DrawEllipse(null, pen, new Point(cx, cy), r, r);
    }

    static void DrawTextOnCircleTop(DrawingContext context, string text, double cx, double cy, double radius, double size, Color color, Typeface typeface, double letterSpacing)
    {
        if (string.IsNullOrWhiteSpace(text) || radius <= 1 || size <= 1)
            return;

        var brush = new SolidColorBrush(color);
        var widths = MeasureChars(text, typeface, size, letterSpacing);
        var total = 0.0;
        foreach (var w in widths) total += w;

        var angle = -Math.PI / 2 - total / radius / 2;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i].ToString();
            var a = widths[i] / radius;
            angle += a / 2;
            DrawRotatedChar(context, ch, cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle), angle + Math.PI / 2, typeface, size, brush);
            angle += a / 2;
        }
    }

    static void DrawTextOnCircleBottom(DrawingContext context, string text, double cx, double cy, double radius, double size, Color color, Typeface typeface, double letterSpacing)
    {
        if (string.IsNullOrWhiteSpace(text) || radius <= 1 || size <= 1)
            return;

        var brush = new SolidColorBrush(color);
        var widths = MeasureChars(text, typeface, size, letterSpacing);
        var total = 0.0;
        foreach (var w in widths) total += w;

        var angle = Math.PI / 2 + total / radius / 2;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i].ToString();
            var a = widths[i] / radius;
            angle -= a / 2;
            DrawRotatedChar(context, ch, cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle), angle - Math.PI / 2, typeface, size, brush);
            angle -= a / 2;
        }
    }

    static List<double> MeasureChars(string text, Typeface typeface, double size, double letterSpacing)
    {
        var result = new List<double>();
        foreach (var ch in text)
        {
            var ft = CreateFormattedText(ch.ToString(), typeface, size, Brushes.Black);
            result.Add(Math.Max(1, ft.WidthIncludingTrailingWhitespace + letterSpacing));
        }
        return result;
    }

    static void DrawRotatedChar(DrawingContext context, string ch, double x, double y, double rotateRadians, Typeface typeface, double size, IBrush brush)
    {
        var ft = CreateFormattedText(ch, typeface, size, brush);
        var transform =
            Matrix.CreateTranslation(-ft.Width / 2, -ft.Height / 2) *
            Matrix.CreateRotation(rotateRadians) *
            Matrix.CreateTranslation(x, y);

        using (context.PushTransform(transform))
            context.DrawText(ft, new Point(0, 0));
    }

    static FormattedText CreateFormattedText(string text, Typeface typeface, double size, IBrush brush)
    {
        return new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush);
    }

    static Color ParseColor(string hex)
    {
        try { return Color.Parse(hex); }
        catch { return Color.FromRgb(0, 63, 158); }
    }
}
