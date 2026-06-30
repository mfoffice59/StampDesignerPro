using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using StampDesignerPro.Models;

namespace StampDesignerPro.Rendering;

public static class StampRenderer
{
    public const double BaseSize = 800;
    public static void Render(DrawingContext context, StampProject p, Rect bounds)
    {
        var scale = Math.Min(bounds.Width, bounds.Height) / BaseSize;
        var cx = bounds.X + bounds.Width / 2; var cy = bounds.Y + bounds.Height / 2;
        if (!p.TransparentBackground) context.FillRectangle(Brushes.White, bounds);
        var color = ParseColor(p.StampColor); var brush = new SolidColorBrush(color); var typeface = new Typeface(p.FontFamily);
        if (p.OuterStyle == "band")
        {
            DrawCircle(context, cx, cy, p.BandRadius * scale, new Pen(brush, p.BandWidth * scale));
            DrawCircle(context, cx, cy, (p.BandRadius - p.BandWidth / 2 - 10) * scale, new Pen(brush, p.LineWidth * scale / 1.5));
            DrawCircle(context, cx, cy, p.InnerRadius * scale, new Pen(brush, p.LineWidth * scale / 1.5));
            DrawTextOnCircleTop(context, p.BandText, cx, cy, p.BandRadius * scale, p.BandFontSize * scale, Colors.White, typeface, p.LetterSpacing * scale);
        }
        else
        {
            DrawCircle(context, cx, cy, p.OuterRadius * scale, new Pen(brush, p.LineWidth * scale));
            DrawCircle(context, cx, cy, p.SecondRadius * scale, new Pen(brush, p.LineWidth * scale / 1.5));
            DrawCircle(context, cx, cy, p.InnerRadius * scale, new Pen(brush, p.LineWidth * scale / 1.5));
            DrawTextOnCircleTop(context, p.TopText, cx, cy, p.TextRadius * scale, p.FontSize * scale, color, typeface, p.LetterSpacing * scale);
        }
        DrawTextOnCircleBottom(context, p.BottomText, cx, cy, p.TextRadius * scale, p.FontSize * scale, color, typeface, p.LetterSpacing * scale);
        DrawTextOnCircleTop(context, p.InnerText, cx, cy, p.InnerTextRadius * scale, p.InnerFontSize * scale, color, typeface, p.LetterSpacing * scale);
    }
    static void DrawCircle(DrawingContext c, double cx, double cy, double r, Pen pen){ if(r>0)c.DrawEllipse(null, pen, new Point(cx, cy), r, r); }
    static void DrawTextOnCircleTop(DrawingContext c,string text,double cx,double cy,double radius,double size,Color color,Typeface typeface,double spacing){ if(string.IsNullOrWhiteSpace(text)||radius<=1||size<=1)return; var b=new SolidColorBrush(color); var widths=Measure(text,typeface,size,spacing); double total=0; foreach(var w in widths) total+=w; var angle=-Math.PI/2-total/radius/2; for(int i=0;i<text.Length;i++){ var w=widths[i]; var a=w/radius; angle+=a/2; var x=cx+radius*Math.Cos(angle); var y=cy+radius*Math.Sin(angle); DrawChar(c,text[i].ToString(),x,y,angle+Math.PI/2,typeface,size,b); angle+=a/2; } }
    static void DrawTextOnCircleBottom(DrawingContext c,string text,double cx,double cy,double radius,double size,Color color,Typeface typeface,double spacing){ if(string.IsNullOrWhiteSpace(text)||radius<=1||size<=1)return; var b=new SolidColorBrush(color); var widths=Measure(text,typeface,size,spacing); double total=0; foreach(var w in widths) total+=w; var angle=Math.PI/2+total/radius/2; for(int i=0;i<text.Length;i++){ var w=widths[i]; var a=w/radius; angle-=a/2; var x=cx+radius*Math.Cos(angle); var y=cy+radius*Math.Sin(angle); DrawChar(c,text[i].ToString(),x,y,angle-Math.PI/2,typeface,size,b); angle-=a/2; } }
    static List<double> Measure(string text, Typeface tf, double size, double spacing){ var list=new List<double>(); foreach(var ch in text){ var ft=FT(ch.ToString(),tf,size,Brushes.Black); list.Add(Math.Max(1,ft.WidthIncludingTrailingWhitespace+spacing)); } return list; }
    static void DrawChar(DrawingContext c,string ch,double x,double y,double rot,Typeface tf,double size,IBrush brush){ var ft=FT(ch,tf,size,brush); var m=Matrix.CreateTranslation(-ft.Width/2,-ft.Height/2)*Matrix.CreateRotation(rot)*Matrix.CreateTranslation(x,y); using(c.PushTransform(m)){ c.DrawText(ft,new Point(0,0)); } }
    static FormattedText FT(string text, Typeface tf, double size, IBrush brush)=>new(text,System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,tf,size,brush);
    static Color ParseColor(string hex){ try{return Color.Parse(hex);}catch{return Color.FromRgb(0,63,158);} }
}
