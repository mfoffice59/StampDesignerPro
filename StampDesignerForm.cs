using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace StampDesigner;

public sealed class StampDesignerForm : Form
{
    const int BaseSize = 900;
    const int ExportSize = 3000;

    readonly PictureBox preview = new();
    readonly Panel left = new();

    readonly Dictionary<string, Control> controls = new();

    Bitmap? logo;
    string? logoBase64;

    float logoX = 0;
    float logoY = 25;
    bool draggingLogo;
    PointF dragStart;
    PointF logoStart;

    string tool = "logo";

    readonly List<EraserStroke> eraserStrokes = new();
    EraserStroke? currentStroke;
    PointF? lastMouse;

    readonly Stack<ProjectState> undo = new();
    readonly Stack<ProjectState> redo = new();
    bool lockHistory;

    public StampDesignerForm()
    {
        Text = "Stamp Designer Windows 1.0";
        Width = 1320;
        Height = 900;
        MinimumSize = new Size(1100, 740);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        ApplyTemplate("uz_mchj");
        PushHistory();
        RenderPreview();
    }

    void BuildUi()
    {
        var root = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 420,
            FixedPanel = FixedPanel.Panel1
        };
        Controls.Add(root);

        left.Dock = DockStyle.Fill;
        left.AutoScroll = true;
        left.Padding = new Padding(12);
        root.Panel1.Controls.Add(left);

        var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        root.Panel2.Controls.Add(right);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            FlowDirection = FlowDirection.LeftToRight
        };
        right.Controls.Add(toolbar);

        toolbar.Controls.Add(Button("Логотип", () => SetTool("logo"), 90));
        toolbar.Controls.Add(Button("Ластик", () => SetTool("eraser"), 90));
        toolbar.Controls.Add(Button("Отмена", Undo, 90));
        toolbar.Controls.Add(Button("Повтор", Redo, 90));
        toolbar.Controls.Add(Button("PNG", ExportPng, 90));

        preview.Dock = DockStyle.Fill;
        preview.BackColor = Color.FromArgb(241, 245, 249);
        preview.SizeMode = PictureBoxSizeMode.Zoom;
        preview.MouseDown += PreviewMouseDown;
        preview.MouseMove += PreviewMouseMove;
        preview.MouseUp += PreviewMouseUp;
        preview.MouseWheel += PreviewMouseWheel;
        preview.DoubleClick += (_, _) => ResetLogo();
        right.Controls.Add(preview);
        preview.BringToFront();

        int y = 10;

        AddTitle("Stamp Designer Windows", ref y);

        AddSection("Файлы", ref y);
        AddButton("Загрузить логотип", LoadLogo, ref y);
        AddButton("Экспорт PNG", ExportPng, ref y);
        AddButton("Сохранить проект .stamp", SaveProject, ref y);
        AddButton("Открыть проект .stamp", OpenProject, ref y);

        AddSection("Шаблоны", ref y);
        AddButton("ООО RU", () => ApplyTemplate("ooo_ru"), ref y);
        AddButton("MChJ UZ", () => ApplyTemplate("uz_mchj"), ref y);
        AddButton("YATT", () => ApplyTemplate("yatt"), ref y);
        AddButton("Синяя полоса", () => ApplyTemplate("band"), ref y);

        AddSection("Стиль", ref y);
        AddCombo("outerStyle", "Тип внешнего кольца", new[] { "double", "band" }, "double", ref y);
        AddText("bandText", "Текст в синей полосе", "O‘ZBEKISTON RESPUBLIKASI - MChJ «SIZNING NOMINGIZ»", ref y, true);
        AddNumber("bandWidth", "Ширина синей полосы", 54, 10, 120, ref y);
        AddNumber("bandRadius", "Радиус текста полосы", 333, 240, 390, ref y);
        AddNumber("bandFontSize", "Размер текста полосы", 25, 8, 90, ref y);

        AddSection("Текст", ref y);
        AddText("topText", "Верхний текст", "«Sizning nomingiz» mas’uliyati cheklangan jamiyati", ref y, true);
        AddText("bottomText", "Нижний текст", "O‘zbekiston Respublikasi - Toshkent shahri", ref y, true);
        AddText("innerText", "Внутренний текст", "STIR 000000000", ref y, false);
        AddCombo("fontFamily", "Шрифт", new[] { "Arial", "Times New Roman", "Georgia", "Verdana", "Tahoma", "Calibri", "Segoe UI" }, "Arial", ref y);
        AddNumber("fontSize", "Размер текста", 28, 8, 90, ref y);
        AddNumber("letterSpacing", "Интервал букв", 2, -4, 30, ref y);
        AddNumber("textRadius", "Радиус текста", 285, 120, 380, ref y);
        AddNumber("innerFontSize", "Размер внутреннего текста", 25, 8, 70, ref y);
        AddNumber("innerTextRadius", "Радиус внутреннего текста", 202, 80, 300, ref y);
        AddCheck("showBottom", "Показывать нижний текст", true, ref y);
        AddCheck("showInner", "Показывать внутренний текст", true, ref y);

        AddSection("Круги", ref y);
        AddText("stampColor", "Цвет HEX", "#003f9e", ref y, false);
        AddNumber("outerRadius", "Внешний радиус", 345, 200, 410, ref y);
        AddNumber("secondRadius", "Второй радиус", 323, 180, 390, ref y);
        AddNumber("innerRadius", "Внутренний радиус", 232, 60, 330, ref y);
        AddNumber("lineWidth", "Толщина линий", 5, 1, 32, ref y);
        AddCheck("transparentBg", "Прозрачный фон", true, ref y);

        AddSection("Логотип", ref y);
        AddNumber("logoSize", "Размер логотипа", 170, 20, 550, ref y);
        AddNumber("logoOpacity", "Прозрачность логотипа", 100, 5, 100, ref y);
        AddButton("Логотип в центр", ResetLogo, ref y);

        AddSection("Ластик", ref y);
        AddNumber("eraserSize", "Размер ластика", 42, 2, 320, ref y);
        AddNumber("eraserSoftness", "Мягкость ластика", 70, 0, 100, ref y);
        AddNumber("eraserOpacity", "Сила стирания", 100, 1, 100, ref y);
        AddButton("Очистить ручные правки", ClearEraser, ref y);
    }

    Button Button(string text, Action action, int width = 220)
    {
        var b = new Button { Text = text, Width = width, Height = 30, Margin = new Padding(4) };
        b.Click += (_, _) => action();
        return b;
    }

    void AddTitle(string text, ref int y)
    {
        var lbl = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            Location = new Point(12, y),
            AutoSize = true
        };
        left.Controls.Add(lbl);
        y += 38;
    }

    void AddSection(string text, ref int y)
    {
        var line = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(12, y),
            Width = 360,
            Height = 2
        };
        left.Controls.Add(line);
        y += 12;

        var lbl = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(12, y),
            Width = 360,
            Height = 24
        };
        left.Controls.Add(lbl);
        y += 28;
    }

    void AddButton(string text, Action action, ref int y)
    {
        var b = Button(text, action, 360);
        b.Location = new Point(12, y);
        left.Controls.Add(b);
        y += 36;
    }

    void AddText(string key, string label, string value, ref int y, bool multiline)
    {
        left.Controls.Add(new Label { Text = label, Location = new Point(12, y), Width = 360, Height = 20 });
        y += 20;

        TextBox tb = new()
        {
            Text = value,
            Location = new Point(12, y),
            Width = 360,
            Height = multiline ? 54 : 26,
            Multiline = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
        };
        tb.TextChanged += (_, _) => Changed();
        controls[key] = tb;
        left.Controls.Add(tb);
        y += tb.Height + 8;
    }

    void AddNumber(string key, string label, decimal value, decimal min, decimal max, ref int y)
    {
        left.Controls.Add(new Label { Text = label, Location = new Point(12, y), Width = 360, Height = 20 });
        y += 20;

        NumericUpDown n = new()
        {
            Location = new Point(12, y),
            Width = 360,
            Minimum = min,
            Maximum = max,
            DecimalPlaces = 0,
            Increment = 1,
            Value = value
        };
        n.ValueChanged += (_, _) => Changed();
        n.MouseWheel += NumericMouseWheel;
        controls[key] = n;
        left.Controls.Add(n);
        y += 34;
    }

    void AddCheck(string key, string label, bool value, ref int y)
    {
        CheckBox cb = new()
        {
            Text = label,
            Checked = value,
            Location = new Point(12, y),
            Width = 360,
            Height = 26
        };
        cb.CheckedChanged += (_, _) => Changed();
        controls[key] = cb;
        left.Controls.Add(cb);
        y += 30;
    }

    void AddCombo(string key, string label, string[] values, string value, ref int y)
    {
        left.Controls.Add(new Label { Text = label, Location = new Point(12, y), Width = 360, Height = 20 });
        y += 20;

        ComboBox cb = new()
        {
            Location = new Point(12, y),
            Width = 360,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cb.Items.AddRange(values);
        cb.SelectedItem = value;
        cb.SelectedIndexChanged += (_, _) => Changed();
        controls[key] = cb;
        left.Controls.Add(cb);
        y += 34;
    }

    void NumericMouseWheel(object? sender, MouseEventArgs e)
    {
        if (sender is not NumericUpDown n) return;
        var step = ModifierKeys.HasFlag(Keys.Shift) ? 10 : 1;
        var next = n.Value + (e.Delta > 0 ? step : -step);
        if (next < n.Minimum) next = n.Minimum;
        if (next > n.Maximum) next = n.Maximum;
        n.Value = next;
    }

    void Changed()
    {
        if (!lockHistory)
            RenderPreview();
    }

    string S(string key) => controls[key] switch
    {
        TextBox tb => tb.Text,
        ComboBox cb => cb.SelectedItem?.ToString() ?? "",
        NumericUpDown n => n.Value.ToString(),
        CheckBox cb => cb.Checked.ToString(),
        _ => ""
    };

    float F(string key)
    {
        if (controls[key] is NumericUpDown n) return (float)n.Value;
        float.TryParse(S(key), out var v);
        return v;
    }

    bool B(string key) => controls[key] is CheckBox cb && cb.Checked;

    void Set(string key, object value)
    {
        if (!controls.TryGetValue(key, out var c)) return;
        switch (c)
        {
            case TextBox tb:
                tb.Text = Convert.ToString(value) ?? "";
                break;
            case ComboBox cb:
                cb.SelectedItem = Convert.ToString(value);
                break;
            case NumericUpDown n:
                var dec = Convert.ToDecimal(value);
                if (dec < n.Minimum) dec = n.Minimum;
                if (dec > n.Maximum) dec = n.Maximum;
                n.Value = dec;
                break;
            case CheckBox ch:
                ch.Checked = Convert.ToBoolean(value);
                break;
        }
    }

    Color StampColor()
    {
        try { return ColorTranslator.FromHtml(S("stampColor")); }
        catch { return Color.FromArgb(0, 63, 158); }
    }

    void SetTool(string selected)
    {
        tool = selected;
        preview.Cursor = selected == "eraser" ? Cursors.Cross : Cursors.SizeAll;
    }

    void ApplyTemplate(string name)
    {
        lockHistory = true;

        if (name == "ooo_ru")
        {
            Set("outerStyle", "double");
            Set("topText", "Общество с ограниченной ответственностью «Ваше название»");
            Set("bottomText", "Республика Узбекистан - г. Ташкент");
            Set("innerText", "ИНН 000000000");
            Set("fontFamily", "Arial");
            Set("fontSize", 28);
            Set("textRadius", 285);
            Set("innerFontSize", 25);
            Set("innerTextRadius", 202);
            Set("outerRadius", 345);
            Set("secondRadius", 323);
            Set("innerRadius", 232);
            Set("lineWidth", 5);
            Set("stampColor", "#003f9e");
            Set("showBottom", true);
            Set("showInner", true);
        }
        else if (name == "yatt")
        {
            Set("outerStyle", "double");
            Set("topText", "Yakka tartibdagi tadbirkor");
            Set("bottomText", "Familiya Ism Otasining ismi");
            Set("innerText", "STIR 000000000");
            Set("fontFamily", "Times New Roman");
            Set("fontSize", 32);
            Set("textRadius", 285);
            Set("innerFontSize", 26);
            Set("innerTextRadius", 200);
            Set("outerRadius", 345);
            Set("secondRadius", 323);
            Set("innerRadius", 230);
            Set("lineWidth", 5);
            Set("stampColor", "#003f9e");
            Set("showBottom", true);
            Set("showInner", true);
        }
        else if (name == "band")
        {
            Set("outerStyle", "band");
            Set("bandText", "O‘ZBEKISTON RESPUBLIKASI - MChJ «SIZNING NOMINGIZ» - STIR 000000000");
            Set("bandWidth", 54);
            Set("bandRadius", 333);
            Set("bandFontSize", 25);
            Set("topText", "Mas’uliyati cheklangan jamiyat");
            Set("bottomText", "Toshkent shahri");
            Set("innerText", "Rasmiy muhr");
            Set("fontFamily", "Arial");
            Set("fontSize", 27);
            Set("textRadius", 275);
            Set("innerFontSize", 24);
            Set("innerTextRadius", 200);
            Set("outerRadius", 345);
            Set("secondRadius", 300);
            Set("innerRadius", 225);
            Set("lineWidth", 4);
            Set("stampColor", "#003f9e");
            Set("showBottom", true);
            Set("showInner", true);
        }
        else
        {
            Set("outerStyle", "double");
            Set("topText", "«Sizning nomingiz» mas’uliyati cheklangan jamiyati");
            Set("bottomText", "O‘zbekiston Respublikasi - Toshkent shahri");
            Set("innerText", "STIR 000000000");
            Set("fontFamily", "Arial");
            Set("fontSize", 27);
            Set("textRadius", 285);
            Set("innerFontSize", 25);
            Set("innerTextRadius", 202);
            Set("outerRadius", 345);
            Set("secondRadius", 323);
            Set("innerRadius", 232);
            Set("lineWidth", 5);
            Set("stampColor", "#003f9e");
            Set("showBottom", true);
            Set("showInner", true);
        }

        lockHistory = false;
        PushHistory();
        RenderPreview();
    }

    Font MakeFont(string sizeKey, float scale, bool bold = true)
    {
        var style = bold ? FontStyle.Bold : FontStyle.Regular;
        return new Font(S("fontFamily"), F(sizeKey) * scale, style, GraphicsUnit.Pixel);
    }

    Bitmap RenderStamp(int size, bool helpers)
    {
        float scale = size / (float)BaseSize;
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);

        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        if (!B("transparentBg"))
            g.Clear(Color.White);

        var color = StampColor();
        var cx = size / 2f;
        var cy = size / 2f;
        var line = F("lineWidth") * scale;

        using var pen = new Pen(color, line);

        if (S("outerStyle") == "band")
        {
            DrawRing(g, cx, cy, F("outerRadius") * scale, (F("outerRadius") - F("bandWidth")) * scale, color);
            using var whiteBrush = new SolidBrush(Color.White);
            using var bandFont = MakeFont("bandFontSize", scale, true);
            DrawTextTop(g, S("bandText"), cx, cy, F("bandRadius") * scale, bandFont, whiteBrush, F("letterSpacing") * scale);
        }
        else
        {
            g.DrawEllipse(pen, CircleRect(cx, cy, F("outerRadius") * scale));
            using var pen2 = new Pen(color, line / 1.5f);
            g.DrawEllipse(pen2, CircleRect(cx, cy, F("secondRadius") * scale));
        }

        using (var penInner = new Pen(color, line / 1.5f))
            g.DrawEllipse(penInner, CircleRect(cx, cy, F("innerRadius") * scale));

        using var brush = new SolidBrush(color);
        using var font = MakeFont("fontSize", scale, true);
        DrawTextTop(g, S("topText"), cx, cy, F("textRadius") * scale, font, brush, F("letterSpacing") * scale);

        if (B("showBottom"))
            DrawTextBottom(g, S("bottomText"), cx, cy, F("textRadius") * scale, font, brush, F("letterSpacing") * scale);

        if (B("showInner"))
        {
            using var innerFont = MakeFont("innerFontSize", scale, true);
            DrawTextTop(g, S("innerText"), cx, cy, F("innerTextRadius") * scale, innerFont, brush, F("letterSpacing") * scale);
        }

        DrawLogo(g, cx, cy, scale, color);
        ApplyEraser(bmp, scale);

        if (helpers && tool == "eraser" && lastMouse.HasValue)
        {
            using var hp = new Pen(Color.Red, 2);
            var r = F("eraserSize") * scale / 2f;
            var p = lastMouse.Value;
            g.DrawEllipse(hp, p.X * scale - r, p.Y * scale - r, r * 2, r * 2);
        }

        return bmp;
    }

    RectangleF CircleRect(float cx, float cy, float r) => new(cx - r, cy - r, r * 2, r * 2);

    void DrawRing(Graphics g, float cx, float cy, float outer, float inner, Color color)
    {
        using var path = new GraphicsPath();
        path.AddEllipse(CircleRect(cx, cy, outer));
        path.AddEllipse(CircleRect(cx, cy, inner));
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    void DrawLogo(Graphics g, float cx, float cy, float scale, Color color)
    {
        var size = F("logoSize") * scale;
        var x = cx + logoX * scale;
        var y = cy + logoY * scale;
        var opacity = Math.Clamp(F("logoOpacity") / 100f, 0.05f, 1f);

        if (logo != null)
        {
            var ratio = Math.Min(size / logo.Width, size / logo.Height);
            var w = logo.Width * ratio;
            var h = logo.Height * ratio;
            var dest = new RectangleF(x - w / 2, y - h / 2, w, h);

            using var ia = new ImageAttributes();
            var cm = new ColorMatrix { Matrix33 = opacity };
            ia.SetColorMatrix(cm);
            g.DrawImage(logo, Rectangle.Round(dest), 0, 0, logo.Width, logo.Height, GraphicsUnit.Pixel, ia);
        }
        else
        {
            using var f = new Font("Arial", 42 * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            using var b = new SolidBrush(color);
            var text = "ВАШ ЛОГОТИП";
            var s = g.MeasureString(text, f);
            g.DrawString(text, f, b, x - s.Width / 2, y - s.Height / 2);
        }
    }

    void DrawTextTop(Graphics g, string text, float cx, float cy, float radius, Font font, Brush brush, float spacing)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var widths = new List<float>();
        float total = 0;
        foreach (var ch in text)
        {
            var w = g.MeasureString(ch.ToString(), font).Width + spacing;
            widths.Add(w);
            total += w;
        }

        var angle = -MathF.PI / 2 - total / radius / 2;

        for (int i = 0; i < text.Length; i++)
        {
            var w = widths[i];
            var a = w / radius;
            angle += a / 2;

            var x = cx + radius * MathF.Cos(angle);
            var y = cy + radius * MathF.Sin(angle);

            var state = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform((angle + MathF.PI / 2) * 180 / MathF.PI);
            var s = g.MeasureString(text[i].ToString(), font);
            g.DrawString(text[i].ToString(), font, brush, -s.Width / 2, -s.Height / 2);
            g.Restore(state);

            angle += a / 2;
        }
    }

    void DrawTextBottom(Graphics g, string text, float cx, float cy, float radius, Font font, Brush brush, float spacing)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var widths = new List<float>();
        float total = 0;
        foreach (var ch in text)
        {
            var w = g.MeasureString(ch.ToString(), font).Width + spacing;
            widths.Add(w);
            total += w;
        }

        var angle = MathF.PI / 2 + total / radius / 2;

        for (int i = 0; i < text.Length; i++)
        {
            var w = widths[i];
            var a = w / radius;
            angle -= a / 2;

            var x = cx + radius * MathF.Cos(angle);
            var y = cy + radius * MathF.Sin(angle);

            var state = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform((angle - MathF.PI / 2) * 180 / MathF.PI);
            var s = g.MeasureString(text[i].ToString(), font);
            g.DrawString(text[i].ToString(), font, brush, -s.Width / 2, -s.Height / 2);
            g.Restore(state);

            angle -= a / 2;
        }
    }

    void ApplyEraser(Bitmap bmp, float scale)
    {
        var all = new List<EraserStroke>(eraserStrokes);
        if (currentStroke != null) all.Add(currentStroke);
        if (all.Count == 0) return;

        using var mask = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
        using var mg = Graphics.FromImage(mask);
        mg.SmoothingMode = SmoothingMode.AntiAlias;

        foreach (var stroke in all)
            DrawEraserStroke(mg, stroke, scale);

        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        var maskData = mask.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        unsafe
        {
            byte* p = (byte*)data.Scan0;
            byte* m = (byte*)maskData.Scan0;

            for (int y = 0; y < bmp.Height; y++)
            {
                byte* row = p + y * data.Stride;
                byte* mr = m + y * maskData.Stride;

                for (int x = 0; x < bmp.Width; x++)
                {
                    var erase = mr[x * 4 + 3] / 255f;
                    if (erase > 0)
                    {
                        var alpha = row[x * 4 + 3];
                        row[x * 4 + 3] = (byte)Math.Clamp(alpha * (1f - erase), 0, 255);
                    }
                }
            }
        }

        bmp.UnlockBits(data);
        mask.UnlockBits(maskData);
    }

    void DrawEraserStroke(Graphics g, EraserStroke stroke, float scale)
    {
        if (stroke.Points.Count == 0) return;

        var size = stroke.Size * scale;
        var radius = size / 2f;
        var opacity = Math.Clamp(stroke.Opacity / 100f, 0.01f, 1f);
        var softness = Math.Clamp(stroke.Softness / 100f, 0f, 1f);

        void Dot(PointF p)
        {
            var x = p.X * scale;
            var y = p.Y * scale;

            if (softness <= 0.01f)
            {
                using var b = new SolidBrush(Color.FromArgb((int)(255 * opacity), Color.Black));
                g.FillEllipse(b, x - radius, y - radius, size, size);
            }
            else
            {
                using var path = new GraphicsPath();
                path.AddEllipse(x - radius, y - radius, size, size);
                using var pgb = new PathGradientBrush(path);
                pgb.CenterColor = Color.FromArgb((int)(255 * opacity), Color.Black);
                pgb.SurroundColors = new[] { Color.FromArgb(0, Color.Black) };
                g.FillPath(pgb, path);
            }
        }

        if (stroke.Points.Count == 1)
        {
            Dot(stroke.Points[0]);
            return;
        }

        for (int i = 1; i < stroke.Points.Count; i++)
        {
            var a = stroke.Points[i - 1];
            var b = stroke.Points[i];
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            var steps = Math.Max(1, (int)(dist / Math.Max(1, stroke.Size / 5f)));

            for (int s = 0; s <= steps; s++)
            {
                var t = s / (float)steps;
                Dot(new PointF(a.X + dx * t, a.Y + dy * t));
            }
        }
    }

    void RenderPreview()
    {
        preview.Image?.Dispose();
        preview.Image = RenderStamp(BaseSize, true);
    }

    PointF PreviewToBase(MouseEventArgs e)
    {
        if (preview.Image == null) return new PointF(0, 0);

        var imgW = preview.Image.Width;
        var imgH = preview.Image.Height;
        var boxW = preview.ClientSize.Width;
        var boxH = preview.ClientSize.Height;

        var ratio = Math.Min(boxW / (float)imgW, boxH / (float)imgH);
        var shownW = imgW * ratio;
        var shownH = imgH * ratio;
        var offX = (boxW - shownW) / 2f;
        var offY = (boxH - shownH) / 2f;

        var x = (e.X - offX) / ratio;
        var y = (e.Y - offY) / ratio;

        return new PointF(x, y);
    }

    void PreviewMouseDown(object? sender, MouseEventArgs e)
    {
        var p = PreviewToBase(e);
        lastMouse = p;

        if (tool == "eraser")
        {
            currentStroke = new EraserStroke(F("eraserSize"), F("eraserSoftness"), F("eraserOpacity"));
            currentStroke.Points.Add(p);
            RenderPreview();
        }
        else
        {
            draggingLogo = true;
            dragStart = p;
            logoStart = new PointF(logoX, logoY);
        }
    }

    void PreviewMouseMove(object? sender, MouseEventArgs e)
    {
        var p = PreviewToBase(e);
        lastMouse = p;

        if (tool == "eraser" && currentStroke != null)
        {
            currentStroke.Points.Add(p);
            RenderPreview();
        }
        else if (draggingLogo)
        {
            logoX = logoStart.X + (p.X - dragStart.X);
            logoY = logoStart.Y + (p.Y - dragStart.Y);
            RenderPreview();
        }
    }

    void PreviewMouseUp(object? sender, MouseEventArgs e)
    {
        if (currentStroke != null)
        {
            eraserStrokes.Add(currentStroke);
            currentStroke = null;
            PushHistory();
        }

        if (draggingLogo)
        {
            draggingLogo = false;
            PushHistory();
        }

        RenderPreview();
    }

    void PreviewMouseWheel(object? sender, MouseEventArgs e)
    {
        if (tool == "eraser")
        {
            var n = (NumericUpDown)controls["eraserSize"];
            var next = n.Value + (e.Delta > 0 ? 5 : -5);
            n.Value = Math.Clamp(next, n.Minimum, n.Maximum);
        }
        else
        {
            var n = (NumericUpDown)controls["logoSize"];
            var next = n.Value + (e.Delta > 0 ? 8 : -8);
            n.Value = Math.Clamp(next, n.Minimum, n.Maximum);
        }
        RenderPreview();
    }

    void ResetLogo()
    {
        logoX = 0;
        logoY = 25;
        PushHistory();
        RenderPreview();
    }

    void ClearEraser()
    {
        eraserStrokes.Clear();
        currentStroke = null;
        PushHistory();
        RenderPreview();
    }

    void LoadLogo()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };
        if (ofd.ShowDialog() != DialogResult.OK) return;

        logo?.Dispose();
        logo = new Bitmap(ofd.FileName);
        logoBase64 = Convert.ToBase64String(File.ReadAllBytes(ofd.FileName));
        PushHistory();
        RenderPreview();
    }

    void ExportPng()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "PNG|*.png",
            FileName = "round-stamp.png"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        using var bmp = RenderStamp(ExportSize, false);
        bmp.Save(sfd.FileName, ImageFormat.Png);
        MessageBox.Show("PNG сохранен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    ProjectState Snapshot()
    {
        var fields = new Dictionary<string, string>();
        foreach (var kv in controls)
        {
            fields[kv.Key] = kv.Value switch
            {
                TextBox tb => tb.Text,
                ComboBox cb => cb.SelectedItem?.ToString() ?? "",
                NumericUpDown n => n.Value.ToString(),
                CheckBox ch => ch.Checked.ToString(),
                _ => ""
            };
        }

        return new ProjectState
        {
            Fields = fields,
            LogoX = logoX,
            LogoY = logoY,
            LogoBase64 = logoBase64,
            EraserStrokes = eraserStrokes
        };
    }

    void PushHistory()
    {
        if (lockHistory) return;
        undo.Push(Snapshot());
        redo.Clear();
        while (undo.Count > 60)
        {
            var arr = undo.ToArray();
            undo.Clear();
            for (int i = arr.Length - 2; i >= 0; i--) undo.Push(arr[i]);
        }
    }

    void Restore(ProjectState st)
    {
        lockHistory = true;

        foreach (var kv in st.Fields)
            Set(kv.Key, kv.Value);

        logoX = st.LogoX;
        logoY = st.LogoY;
        eraserStrokes.Clear();
        eraserStrokes.AddRange(st.EraserStrokes ?? new List<EraserStroke>());
        currentStroke = null;

        logoBase64 = st.LogoBase64;
        logo?.Dispose();
        logo = null;
        if (!string.IsNullOrWhiteSpace(logoBase64))
        {
            var bytes = Convert.FromBase64String(logoBase64);
            using var ms = new MemoryStream(bytes);
            logo = new Bitmap(ms);
        }

        lockHistory = false;
        RenderPreview();
    }

    void Undo()
    {
        if (undo.Count <= 1) return;
        redo.Push(undo.Pop());
        Restore(undo.Peek());
    }

    void Redo()
    {
        if (redo.Count == 0) return;
        var st = redo.Pop();
        undo.Push(st);
        Restore(st);
    }

    void SaveProject()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Stamp project|*.stamp|JSON|*.json",
            FileName = "stamp-project.stamp"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        var json = JsonSerializer.Serialize(Snapshot(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(sfd.FileName, json);
    }

    void OpenProject()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Stamp project|*.stamp|JSON|*.json|All files|*.*"
        };
        if (ofd.ShowDialog() != DialogResult.OK) return;

        var json = File.ReadAllText(ofd.FileName);
        var st = JsonSerializer.Deserialize<ProjectState>(json);
        if (st == null)
        {
            MessageBox.Show("Не удалось открыть проект.");
            return;
        }

        Restore(st);
        PushHistory();
    }
}

public sealed class EraserStroke
{
    public float Size { get; set; }
    public float Softness { get; set; }
    public float Opacity { get; set; }
    public List<PointF> Points { get; set; } = new();

    public EraserStroke() { }

    public EraserStroke(float size, float softness, float opacity)
    {
        Size = size;
        Softness = softness;
        Opacity = opacity;
    }
}

public sealed class ProjectState
{
    public Dictionary<string, string> Fields { get; set; } = new();
    public float LogoX { get; set; }
    public float LogoY { get; set; }
    public string? LogoBase64 { get; set; }
    public List<EraserStroke> EraserStrokes { get; set; } = new();
}
