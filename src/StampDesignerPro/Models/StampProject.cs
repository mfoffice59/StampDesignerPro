using System.Collections.Generic;

namespace StampDesignerPro.Models;

public sealed class StampProject
{
    public string Version { get; set; } = "2.0-full-logo-eraser-ui-v2";

    public string TopText { get; set; } = "«Sizning nomingiz» mas’uliyati cheklangan jamiyati";
    public string BottomText { get; set; } = "O‘zbekiston Respublikasi - Toshkent shahri";
    public string InnerText { get; set; } = "STIR 000000000";

    public string StampColor { get; set; } = "#003f9e";
    public string FontFamily { get; set; } = "Arial";

    public string OuterStyle { get; set; } = "double";
    public string BandText { get; set; } = "O‘ZBEKISTON RESPUBLIKASI - MChJ «SIZNING NOMINGIZ»";
    public double BandRadius { get; set; } = 333;
    public double BandWidth { get; set; } = 54;
    public double BandFontSize { get; set; } = 25;

    public double OuterRadius { get; set; } = 345;
    public double SecondRadius { get; set; } = 323;
    public double InnerRadius { get; set; } = 232;
    public double TextRadius { get; set; } = 285;
    public double InnerTextRadius { get; set; } = 202;

    public double FontSize { get; set; } = 27;
    public double InnerFontSize { get; set; } = 25;
    public double LetterSpacing { get; set; } = 2;
    public double LineWidth { get; set; } = 5;

    public bool TransparentBackground { get; set; } = true;

    public LogoLayer Logo { get; set; } = new();
    public EraserLayer Eraser { get; set; } = new();
}

public sealed class LogoLayer
{
    public string? FilePath { get; set; }
    public double X { get; set; } = 0;
    public double Y { get; set; } = 25;
    public double Size { get; set; } = 170;
    public double Opacity { get; set; } = 100;
    public bool Visible { get; set; } = true;
}

public sealed class EraserLayer
{
    public bool Visible { get; set; } = true;
    public double Size { get; set; } = 28;
    public List<EraserPoint> Points { get; set; } = new();
}

public sealed class EraserPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Size { get; set; }
}
