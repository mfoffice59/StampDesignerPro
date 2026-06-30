namespace StampDesignerPro.Models;

public sealed class StampProject
{
    public string Version { get; set; } = "2.0-alpha1";
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
}
