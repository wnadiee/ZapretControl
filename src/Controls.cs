using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZapretControl
{
    internal static class Ui
    {
        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;

            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void RoundRegion(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            using (var path = RoundRect(new Rectangle(0, 0, control.Width, control.Height), radius))
                control.Region = new Region(path);
        }
    }

    public class PowerButton : Control
    {
        private bool _on;
        private bool _hover;

        public bool On
        {
            get { return _on; }
            set
            {
                if (_on == value) return;
                _on = value;
                Invalidate();
            }
        }

        public PowerButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(72, 72);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color baseColor = _on
                ? Color.FromArgb(46, 204, 113)
                : Color.FromArgb(62, 66, 78);
            if (_hover) baseColor = _on
                ? Color.FromArgb(58, 220, 128)
                : Color.FromArgb(78, 82, 96);

            using (var brush = new SolidBrush(Color.FromArgb(24, baseColor)))
                g.FillEllipse(brush, 0, 0, Width - 1, Height - 1);

            using (var brush = new SolidBrush(baseColor))
                g.FillEllipse(brush, 5, 5, Width - 11, Height - 11);

            using (var pen = new Pen(Color.FromArgb(120, baseColor), 1f))
                g.DrawEllipse(pen, 0.5f, 0.5f, Width - 2, Height - 2);

            int cx = Width / 2;
            int cy = Height / 2;
            var arc = new Rectangle(cx - 13, cy - 13, 26, 26);

            using (var pen = new Pen(Color.White, 4f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawArc(pen, arc, -55, 290);
                g.DrawLine(pen, cx, cy - 19, cx, cy - 2);
            }
        }
    }

    public class Dot : Control
    {
        private Color _color;

        public Color Color
        {
            get { return _color; }
            set { _color = value; Invalidate(); }
        }

        public Dot()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(12, 12);
            _color = Color.FromArgb(120, 124, 136);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var glow = new SolidBrush(Color.FromArgb(45, _color)))
                g.FillEllipse(glow, 0, 0, Width - 1, Height - 1);
            using (var brush = new SolidBrush(_color))
                g.FillEllipse(brush, 2, 2, Width - 5, Height - 5);
        }
    }

    public class ToggleSwitch : Control
    {
        private bool _checked;
        private bool _hover;

        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(56, 28);
            Cursor = Cursors.Hand;
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                var handler = CheckedChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var track = new Rectangle(0, 1, Width - 1, Height - 3);
            Color onColor = Color.FromArgb(46, 204, 113);
            Color offColor = _hover ? Color.FromArgb(82, 86, 100) : Color.FromArgb(60, 63, 76);

            using (var path = Ui.RoundRect(track, track.Height / 2))
            using (var brush = new SolidBrush(_checked ? onColor : offColor))
                g.FillPath(brush, path);

            int knob = track.Height - 6;
            int kx = _checked ? track.Right - knob - 3 : track.Left + 3;
            int ky = track.Top + 3;

            using (var brush = new SolidBrush(Color.White))
                g.FillEllipse(brush, kx, ky, knob, knob);
        }
    }

    public class StatusPill : Control
    {
        private bool _active;

        public bool Active
        {
            get { return _active; }
            set
            {
                if (_active == value) return;
                _active = value;
                Invalidate();
            }
        }

        public StatusPill()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(182, 44);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color green = Color.FromArgb(46, 204, 113);
            Color fill = _active ? Color.FromArgb(32, 60, 45) : Color.FromArgb(32, 34, 42);
            Color border = _active ? Color.FromArgb(46, 204, 113) : Color.FromArgb(58, 61, 72);
            Color dot = _active ? green : Color.FromArgb(120, 124, 136);
            Color textColor = _active ? Color.FromArgb(126, 232, 166) : Color.FromArgb(150, 154, 165);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Ui.RoundRect(rect, Height / 2))
            {
                using (var brush = new SolidBrush(fill))
                    g.FillPath(brush, path);
                using (var pen = new Pen(border, 1f))
                    g.DrawPath(pen, path);
            }

            using (var brush = new SolidBrush(dot))
                g.FillEllipse(brush, 16, Height / 2 - 5, 10, 10);

            string text = _active ? "АКТИВЕН" : "ВЫКЛЮЧЕН";
            using (var font = new Font("Segoe UI Semibold", 8.5f))
            using (var brush = new SolidBrush(textColor))
                g.DrawString(text, font, brush, new PointF(36, Height / 2 - 9));
        }
    }

    public class StrategySelector : Control
    {
        private static readonly Color Fill = Color.FromArgb(34, 36, 45);
        private static readonly Color Border = Color.FromArgb(52, 55, 66);
        private static readonly Color BorderHover = Color.FromArgb(92, 78, 160);
        private static readonly Color TextColor = Color.FromArgb(236, 237, 241);
        private static readonly Color Dim = Color.FromArgb(120, 124, 136);

        private readonly List<string> _items = new List<string>();
        private int _selectedIndex = -1;
        private bool _hover;
        private Form _popup;
        private ListBox _list;

        public event EventHandler SelectionChanged;

        public StrategySelector()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 34;
            Cursor = Cursors.Hand;
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
        }

        public string SelectedItem
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _items.Count) return null;
                return _items[_selectedIndex];
            }
        }

        public void SetItems(List<string> items, int selectedIndex)
        {
            ClosePopup();
            _items.Clear();
            if (items != null) _items.AddRange(items);

            if (selectedIndex < 0 || selectedIndex >= _items.Count) selectedIndex = -1;
            _selectedIndex = selectedIndex;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Ui.RoundRect(rect, 10))
            {
                using (var brush = new SolidBrush(Enabled ? Fill : Color.FromArgb(28, 30, 37)))
                    g.FillPath(brush, path);
                using (var pen = new Pen(Enabled ? (_hover ? BorderHover : Border) : Color.FromArgb(42, 45, 54), 1f))
                    g.DrawPath(pen, path);
            }

            string text = SelectedItem ?? (_items.Count == 0 ? "Стратегии не найдены" : "Выберите стратегию");
            using (var font = new Font("Segoe UI", 9.5f))
            using (var brush = new SolidBrush(Enabled ? TextColor : Dim))
            using (var format = new StringFormat())
            {
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                format.FormatFlags = StringFormatFlags.NoWrap;
                g.DrawString(text, font, brush,
                    new RectangleF(12, 0, Width - 42, Height), format);
            }

            using (var pen = new Pen(Enabled ? Color.FromArgb(150, 155, 170) : Dim, 1.8f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                int cx = Width - 20;
                int cy = Height / 2;
                g.DrawLine(pen, cx - 4, cy - 2, cx, cy + 2);
                g.DrawLine(pen, cx, cy + 2, cx + 4, cy - 2);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!Enabled || _items.Count == 0) return;
            ShowMenu();
        }

        private void ShowMenu()
        {
            if (_popup != null)
            {
                ClosePopup();
                return;
            }

            var screen = Screen.FromControl(this);
            var area = screen.WorkingArea;
            var host = FindForm();

            var popup = new Form();
            popup.FormBorderStyle = FormBorderStyle.None;
            popup.ShowInTaskbar = false;
            popup.StartPosition = FormStartPosition.Manual;
            popup.BackColor = Color.FromArgb(52, 55, 66);
            popup.Padding = new Padding(1);

            var list = new ListBox();
            list.BorderStyle = BorderStyle.None;
            list.BackColor = Color.FromArgb(30, 32, 40);
            list.ForeColor = TextColor;
            list.Font = new Font("Segoe UI", 9.5f);
            list.DrawMode = DrawMode.OwnerDrawFixed;
            list.ItemHeight = 28;
            list.IntegralHeight = false;
            list.Dock = DockStyle.Fill;

            foreach (var item in _items) list.Items.Add(item);
            if (_selectedIndex >= 0)
            {
                list.SelectedIndex = _selectedIndex;
                list.TopIndex = _selectedIndex;
            }

            list.DrawItem += delegate(object s, DrawItemEventArgs e)
            {
                if (e.Index < 0) return;
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (var brush = new SolidBrush(selected ? Color.FromArgb(58, 48, 96) : Color.FromArgb(30, 32, 40)))
                    e.Graphics.FillRectangle(brush, e.Bounds);
                using (var brush = new SolidBrush(e.Index == _selectedIndex ? Color.FromArgb(160, 140, 255) : TextColor))
                using (var format = new StringFormat())
                {
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;
                    format.FormatFlags = StringFormatFlags.NoWrap;
                    e.Graphics.DrawString(list.Items[e.Index].ToString(), list.Font, brush,
                        new RectangleF(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height), format);
                }
            };

            list.Click += delegate
            {
                if (list.SelectedIndex >= 0) Commit(list.SelectedIndex);
                else ClosePopup();
            };

            list.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    if (list.SelectedIndex >= 0) Commit(list.SelectedIndex);
                    else ClosePopup();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    ClosePopup();
                }
            };

            popup.Deactivate += delegate { ClosePopup(); };
            if (host != null)
            {
                host.Move += delegate { ClosePopup(); };
                host.Resize += delegate { ClosePopup(); };
            }
            popup.Controls.Add(list);

            int desired = _items.Count * 28 + 2;
            int maxHeight = Math.Max(120, area.Height - 40);
            if (desired > maxHeight) desired = maxHeight;

            var below = PointToScreen(new Point(0, Height + 2));
            int y = below.Y;
            if (y + desired > area.Bottom)
            {
                int aboveY = PointToScreen(new Point(0, 0)).Y - desired - 2;
                y = Math.Max(area.Top, aboveY);
            }

            popup.SetBounds(below.X, y, Math.Max(Width, 380), desired);

            _popup = popup;
            _list = list;
            popup.Show(host);
            list.Focus();
        }

        private void Commit(int index)
        {
            ClosePopup();
            if (index < 0 || index >= _items.Count || index == _selectedIndex) return;
            _selectedIndex = index;
            Invalidate();
            var handler = SelectionChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void ClosePopup()
        {
            var popup = _popup;
            _popup = null;
            _list = null;
            if (popup != null)
            {
                try { popup.Close(); } catch { }
                try { popup.Dispose(); } catch { }
            }
        }
    }
}
