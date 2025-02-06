using System;
using System.Drawing;
using System.Windows.Forms;

namespace DesktopAnalogClock
{
    public partial class TransparentClockForm : Form
    {
        private System.Windows.Forms.Timer timer = new();
        private Point mouseOffset;
        private bool isDragging = false;
        private ContextMenuStrip contextMenu;

        public TransparentClockForm()
        {
            ConfigureForm();
            InitializeTimer();
            ConfigureMouseEvents();
            ConfigureContextMenu();
        }

        private void ConfigureForm()
        {
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.AllowTransparency = true;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(300, 300);
            this.TopMost = true;
            this.MinimumSize = new Size(150, 150);
            this.MaximumSize = new Size(600, 600);
        }

        private void ConfigureContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Close", null, (s, e) => Application.Exit());
            this.ContextMenuStrip = contextMenu;
        }

        private void InitializeTimer()
        {
            timer.Interval = 1000;
            timer.Tick += (s, e) => this.Invalidate();
            timer.Start();
        }

        private void ConfigureMouseEvents()
        {
            bool isResizing = false;
            ResizeDirection resizeDir = ResizeDirection.None;

            this.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    var dir = GetResizeDirection(e.Location);
                    if (dir != ResizeDirection.None)
                    {
                        isResizing = true;
                        isDragging = false;
                        resizeDir = dir;
                    }
                    else
                    {
                        mouseOffset = e.Location;
                        isDragging = true;
                        isResizing = false;
                    }
                }
            };

            this.MouseMove += (s, e) =>
            {
                // Update cursor based on position
                var dir = GetResizeDirection(e.Location);
                this.Cursor = GetResizeCursor(dir);

                if (isResizing)
                {
                    int newSize;
                    Point newLocation = this.Location;

                    switch (resizeDir)
                    {
                        case ResizeDirection.TopLeft:
                            newSize = Math.Min(
                                this.Right - PointToScreen(e.Location).X,
                                this.Bottom - PointToScreen(e.Location).Y
                            );
                            newLocation = new Point(
                                this.Right - newSize,
                                this.Bottom - newSize
                            );
                            break;
                        case ResizeDirection.TopRight:
                            newSize = Math.Min(
                                PointToScreen(e.Location).X - this.Left,
                                this.Bottom - PointToScreen(e.Location).Y
                            );
                            newLocation = new Point(
                                this.Left,
                                this.Bottom - newSize
                            );
                            break;
                        case ResizeDirection.BottomLeft:
                            newSize = Math.Min(
                                this.Right - PointToScreen(e.Location).X,
                                PointToScreen(e.Location).Y - this.Top
                            );
                            newLocation = new Point(
                                this.Right - newSize,
                                this.Top
                            );
                            break;
                        case ResizeDirection.BottomRight:
                            newSize = Math.Min(e.X, e.Y);
                            break;
                        default:
                            return;
                    }

                    newSize = Math.Min(newSize, MaximumSize.Width);
                    newSize = Math.Max(newSize, MinimumSize.Width);

                    if (resizeDir != ResizeDirection.BottomRight)
                    {
                        this.Location = newLocation;
                    }
                    this.ClientSize = new Size(newSize, newSize);
                    this.Invalidate();
                }
                else if (isDragging)
                {
                    var newLocation = this.PointToScreen(e.Location);
                    newLocation.Offset(-mouseOffset.X, -mouseOffset.Y);
                    this.Location = newLocation;
                }
            };

            this.MouseUp += (s, e) =>
            {
                isDragging = false;
                isResizing = false;
                resizeDir = ResizeDirection.None;
            };
        }

        private enum ResizeDirection
        {
            None,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private ResizeDirection GetResizeDirection(Point p)
        {
            const int RESIZE_BORDER = 10;
            bool near_left = p.X <= RESIZE_BORDER;
            bool near_right = p.X >= ClientSize.Width - RESIZE_BORDER;
            bool near_top = p.Y <= RESIZE_BORDER;
            bool near_bottom = p.Y >= ClientSize.Height - RESIZE_BORDER;

            if (near_left && near_top) return ResizeDirection.TopLeft;
            if (near_right && near_top) return ResizeDirection.TopRight;
            if (near_left && near_bottom) return ResizeDirection.BottomLeft;
            if (near_right && near_bottom) return ResizeDirection.BottomRight;
            return ResizeDirection.None;
        }

        private Cursor GetResizeCursor(ResizeDirection dir)
        {
            return dir switch
            {
                ResizeDirection.TopLeft or ResizeDirection.BottomRight => Cursors.SizeNWSE,
                ResizeDirection.TopRight or ResizeDirection.BottomLeft => Cursors.SizeNESW,
                _ => Cursors.Default
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var center = new PointF(ClientSize.Width / 2f, ClientSize.Height / 2f);
            var radius = Math.Min(ClientSize.Width, ClientSize.Height) / 2f - 10;

            DrawClockFace(g, center, radius);

            var now = DateTime.Now;
            DrawHand(g, center, radius * 0.5f, now.Hour % 12 * 30 + now.Minute * 0.5f, 8, Color.Black);
            DrawHand(g, center, radius * 0.7f, now.Minute * 6f, 6, Color.Black);
            DrawHand(g, center, radius * 0.85f, now.Second * 6f, 2, Color.Red);

            using var centerBrush = new SolidBrush(Color.FromArgb(180, 40, 40, 40));
            g.FillEllipse(centerBrush, center.X - 5, center.Y - 5, 10, 10);
        }

        private void DrawClockFace(Graphics g, PointF center, float radius)
        {
            using var bgBrush = new SolidBrush(Color.FromArgb(255, 245, 245, 220));

            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            g.FillPath(bgBrush, path);

            using var rimPen = new Pen(Color.FromArgb(160, 160, 160), 8);
            g.DrawEllipse(rimPen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            using var innerRimPen = new Pen(Color.FromArgb(192, 192, 192), 3);
            g.DrawEllipse(innerRimPen, center.X - radius + 4, center.Y - radius + 4, (radius - 4) * 2, (radius - 4) * 2);

            // Draw hour markers with shadow effect
            for (int i = 0; i < 12; i++)
            {
                var angle = i * 30;
                var markerLength = (i % 3 == 0) ? 20 : 10; // Longer markers for 3, 6, 9, 12
                var innerPoint = GetPointOnCircle(center, radius - markerLength, angle);
                var outerPoint = GetPointOnCircle(center, radius - 5, angle);

                // Draw shadow
                using var shadowPen = new Pen(Color.FromArgb(100, 0, 0, 0), i % 3 == 0 ? 4 : 2);
                g.DrawLine(shadowPen,
                    new PointF(innerPoint.X + 1, innerPoint.Y + 1),
                    new PointF(outerPoint.X + 1, outerPoint.Y + 1));

                // Draw marker
                using var markerPen = new Pen(Color.FromArgb(60, 60, 60), i % 3 == 0 ? 4 : 2);
                g.DrawLine(markerPen, innerPoint, outerPoint);
            }

            // Draw numbers with vintage font and shadow
            using var font = new Font("Times New Roman", 16, FontStyle.Bold);
            for (int i = 1; i <= 12; i++)
            {
                var angle = i * 30 - 60;
                var textPoint = GetPointOnCircle(center, radius - 35, angle);
                var text = i.ToString();
                var textSize = g.MeasureString(text, font);

                // Draw shadow
                g.DrawString(text, font, new SolidBrush(Color.FromArgb(100, 0, 0, 0)),
                    textPoint.X - textSize.Width / 2 + 1,
                    textPoint.Y - textSize.Height / 2 + 1);

                // Draw number
                g.DrawString(text, font, new SolidBrush(Color.FromArgb(40, 40, 40)),
                    textPoint.X - textSize.Width / 2,
                    textPoint.Y - textSize.Height / 2);
            }
        }

        private void DrawHand(Graphics g, PointF center, float length, float angle, float width, Color color)
        {
            var endPoint = GetPointOnCircle(center, length, angle - 90);

            // Draw shadow
            using var shadowPen = new Pen(Color.FromArgb(100, 0, 0, 0), width)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            g.DrawLine(shadowPen,
                new PointF(center.X + 2, center.Y + 2),
                new PointF(endPoint.X + 2, endPoint.Y + 2));

            // Draw hand
            using var pen = new Pen(color, width)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            g.DrawLine(pen, center, endPoint);
        }

        private PointF GetPointOnCircle(PointF center, float radius, float angle)
        {
            var radians = angle * Math.PI / 180;
            return new PointF(
                center.X + radius * (float)Math.Cos(radians),
                center.Y + radius * (float)Math.Sin(radians)
            );
        }
    }
}