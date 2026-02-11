namespace BusinessManager.App.Graphics;

public sealed class KpiTrendDrawable(IReadOnlyList<float> values, string title) : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.FillColor = Color.FromArgb("#0f172a");
        canvas.FillRectangle(dirtyRect);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 16;
        canvas.DrawString(title, 12, 10, dirtyRect.Width, 24, HorizontalAlignment.Left, VerticalAlignment.Center);

        if (values.Count < 2)
        {
            canvas.RestoreState();
            return;
        }

        var min = values.Min();
        var max = values.Max();
        var spread = Math.Max(1f, max - min);

        var left = 16f;
        var right = dirtyRect.Width - 16f;
        var top = 44f;
        var bottom = dirtyRect.Height - 16f;

        canvas.StrokeColor = Color.FromArgb("#334155");
        canvas.StrokeSize = 1;
        canvas.DrawLine(left, bottom, right, bottom);
        canvas.DrawLine(left, top, left, bottom);

        canvas.StrokeColor = Color.FromArgb("#22d3ee");
        canvas.StrokeSize = 3;

        for (int i = 1; i < values.Count; i++)
        {
            var x1 = left + (i - 1) * ((right - left) / (values.Count - 1));
            var x2 = left + i * ((right - left) / (values.Count - 1));
            var y1 = bottom - ((values[i - 1] - min) / spread) * (bottom - top);
            var y2 = bottom - ((values[i] - min) / spread) * (bottom - top);
            canvas.DrawLine(x1, y1, x2, y2);
        }

        canvas.RestoreState();
    }
}
