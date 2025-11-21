using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GndStation
{
    // EventArgs for cursor position changes: gives X and Y in meters
    public class CursorPositionEventArgs : EventArgs
    {
        public float XMeters { get; }
        public float YMeters { get; }
        public CursorPositionEventArgs(float xMeters, float yMeters)
        {
            XMeters = xMeters; YMeters = yMeters; 
        }
    }

    /// Control for displaying a Cartesian plane with points and lines in meters.
    public class CartesianPlaneControl : Control
    {
        public double PixelsPerMeterX { get; private set; } = 50.0;
        public double PixelsPerMeterY { get; private set; } = 50.0;
        public bool ShowGrid { get; set; } = true;
        public double GridStepMeters { get; set; } = 1.0;

        // Points and lines to draw
        public PointF? dronePoint = null;
        private readonly List<PointF> _points = new List<PointF>();
        private readonly List<List<PointF>> _fences = new List<List<PointF>>();
        private readonly List<Tuple<PointF, PointF>> _lines = new List<Tuple<PointF, PointF>>();

        int index = 0;

        // Cursor position in meters
        public float CursorXMeters { get; private set; }
        public float CursorYMeters { get; private set; }
        public event EventHandler<CursorPositionEventArgs> CursorPositionChanged;

        private ContextMenuStrip _menu;
        private PointF _lastClickMeters;

        public event Action<PointF> DroneSetRequested;
        public event Action<(List<PointF>, string type)> SetFence;
        public event Action<List<PointF>> SetMission;
        public event Action<List<PointF>> ResetFence;

        public CartesianPlaneControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                        ControlStyles.OptimizedDoubleBuffer |
                        ControlStyles.UserPaint, true);
            BackColor = Color.White;

            InitContextMenu();
            this.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                    _lastClickMeters = ScreenToWorld(e.X, e.Y); 
            };
            
            this.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    _menu.Show(this, e.Location);
            };
            this.MouseMove += CartesianPlaneControl_MouseMove;
        }

        private void InitContextMenu()
        {
            _menu = new ContextMenuStrip();

            var addPointItem = new ToolStripMenuItem("Add Point Here");
            var setDroneItem = new ToolStripMenuItem("Fly Here");
            var drawFenceItemIncl = new ToolStripMenuItem("Draw Fence Inclusion");
            var drawFenceItemExcl = new ToolStripMenuItem("Draw Fence Exclusion");
            var missionItem = new ToolStripMenuItem("Mission");
            var clearItem = new ToolStripMenuItem("Clear All");

            addPointItem.Click += (s, e) =>
            {
                string msg = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Coordinates:\nX = {0:F3} m\nY = {1:F3} m",
                        _lastClickMeters.X, _lastClickMeters.Y);

                //MessageBox.Show(msg, "Points Coordinates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AddPoint(_lastClickMeters.X, _lastClickMeters.Y);
            };

            setDroneItem.Click += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[Plane #{GetHashCode()}] SetDrone click at {_lastClickMeters}");
                var subs = DroneSetRequested?.GetInvocationList()?.Length ?? 0;
                System.Diagnostics.Debug.WriteLine($"[Plane] Subscribers: {subs}");
                DroneSetRequested?.Invoke(_lastClickMeters);
            };


            drawFenceItemExcl.Click += (s, e) =>
            {
                string type = "Exclusion";
                SetFence?.Invoke((_points, type));
                _points.Clear();
            };

            drawFenceItemIncl.Click += (s, e) =>
            {
                string type = "Inclusion";
                SetFence?.Invoke((_points, type));
                _points.Clear();
            };

            missionItem.Click += (s, e) =>
            {
                SetMission?.Invoke(_points);
                //_points.Clear();
            };

            clearItem.Click += (s, e) =>
            {
                ResetFence?.Invoke(_points);
                ClearDrawings();
 
            };


            _menu.Items.Add(addPointItem);
            _menu.Items.Add(setDroneItem);
            _menu.Items.Add(drawFenceItemIncl);
            _menu.Items.Add(drawFenceItemExcl);
            _menu.Items.Add(missionItem);
            _menu.Items.Add(clearItem);

            this.ContextMenuStrip = _menu;
        }
        public void SetScale(int pxX, int pxY, double mX, double mY)
        {
            if (pxX <= 0 || pxY <= 0 || mX <= 0 || mY <= 0) throw new ArgumentOutOfRangeException();
            PixelsPerMeterX = pxX / mX;
            PixelsPerMeterY = pxY / mY;
            Invalidate();
        }

        public void AddPoint(float xMeters, float yMeters)
        {
            _points.Add(new PointF(xMeters, yMeters)); 
            Invalidate();
        }

        public void AddFencePoint(float xMeters, float yMeters)
        {
            _fences[_fences.Count - 1].Add(new PointF(xMeters, yMeters)); 
            Invalidate();
        }
        public void AddFirstFencePoint(float xMeters, float yMeters)
        {
            _fences.Add(new List<PointF>());
            _fences[_fences.Count - 1].Add(new PointF(xMeters, yMeters)); 
            Invalidate();
        }

        public void DroneUpdate(float xMeters, float yMeters)
        {
            dronePoint = new PointF(xMeters, yMeters); 
            Invalidate();
        }

        public void AddLine(PointF startMeters, PointF endMeters)
        {
            _lines.Add(Tuple.Create(startMeters, endMeters));
            Invalidate();
        }

        public void ClearDrawings()
        {
            _points.Clear();
            _lines.Clear();
            _fences.Clear();
            index = 0;
            Invalidate();
        }

        public void ClearDroneRelated()
        {
            dronePoint = null;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cx = Width / 2;
            int cy = Height / 2;

            using (SolidBrush bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, ClientRectangle);

            if (ShowGrid && PixelsPerMeterX > 0 && PixelsPerMeterY > 0)
            {
                double stepX = PixelsPerMeterX * GridStepMeters;
                double stepY = PixelsPerMeterY * GridStepMeters;

                using (Pen pGrid = new Pen(Color.Gainsboro, 1))
                {
                    for (double x = cx; x <= Width; x += stepX) g.DrawLine(pGrid, (float)x, 0, (float)x, Height);
                    for (double x = cx; x >= 0; x -= stepX) g.DrawLine(pGrid, (float)x, 0, (float)x, Height);
                    for (double y = cy; y <= Height; y += stepY) g.DrawLine(pGrid, 0, (float)y, Width, (float)y);
                    for (double y = cy; y >= 0; y -= stepY) g.DrawLine(pGrid, 0, (float)y, Width, (float)y);
                }
            }

            using (Pen pAxis = new Pen(Color.Black, 2))
            {
                g.DrawLine(pAxis, 0, cy, Width, cy);
                g.DrawLine(pAxis, cx, 0, cx, Height);
            }

            using (Pen pLine = new Pen(Color.Blue, 4))
            {
                foreach (var line in _lines)
                {
                    PointF p1 = WorldToScreen(line.Item1.X, line.Item1.Y);
                    PointF p2 = WorldToScreen(line.Item2.X, line.Item2.Y);
                    g.DrawLine(pLine, p1, p2);
                }
            }

            using (Brush pBrush = new SolidBrush(Color.Red))
            {
                foreach (PointF p in _points)
                {
                    PointF s = WorldToScreen(p.X, p.Y);
                    g.FillEllipse(pBrush, s.X - 8, s.Y - 8, 16, 16);
                }
            }

            using (Brush pBrush = new SolidBrush(Color.Green))
            {
                if (dronePoint.HasValue)
                {
                    PointF p = dronePoint.Value;
                    PointF s = WorldToScreen(p.X, p.Y);
                    g.FillEllipse(pBrush, s.X - 8, s.Y - 8, 16, 16);
                }

            }

            using (Font f = new Font(Font.FontFamily, 12f))
            using (SolidBrush textBrush = new SolidBrush(Color.Black))
                g.DrawString("(0,0)", f, textBrush, cx + 4, cy + 4);
        }

        
        // HERE X AND Y ROLES ARE SWAPPED TO GET CONSISTENCY WITH DRONE'S FRAME OF REFERENCE
        public PointF WorldToScreen(float xMeters, float yMeters)
        {
            // swap roles: X drives vertical, Y drives horizontal
            float x = (float)(Width / 2 + yMeters * PixelsPerMeterX);
            float y = (float)(Height / 2 - xMeters * PixelsPerMeterY);
            return new PointF(x, y);
        }
  
        public PointF ScreenToWorld(float xPixels, float yPixels)
        {
            // invert the above swap
            float yMeters = (float)((xPixels - (Width / 2.0)) / PixelsPerMeterX);
            float xMeters = (float)(((Height / 2.0) - yPixels) / PixelsPerMeterY);
            return new PointF(xMeters, yMeters);
        }

        // --- Tracking cursore ---
        private void CartesianPlaneControl_MouseMove(object sender, MouseEventArgs e)
        {
            PointF w = ScreenToWorld(e.X, e.Y);
            if (w.X != CursorXMeters || w.Y != CursorYMeters)
            {
                CursorXMeters = w.X;
                CursorYMeters = w.Y;
                var handler = CursorPositionChanged;
                if (handler != null) handler(this, new CursorPositionEventArgs(CursorXMeters, CursorYMeters));
            }
        }
    }
}