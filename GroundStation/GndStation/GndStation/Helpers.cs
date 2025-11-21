using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Windows.Forms;
using System.Threading;
using SharpDX.DirectInput;
using System;
using System.Linq;

namespace GndStation
{
    public partial class Form1
    {
        // ========== HELPERS ==============
        private void Buttons_Reset()
        {
            RTL.BackColor = Color.Orange;
            RTL.ForeColor = Color.Black;
            Land.BackColor = Color.Orange;
            Land.ForeColor = Color.Black;
            Takeoff.BackColor = Color.Orange;
            Takeoff.ForeColor = Color.Black;
            Mission.BackColor = Color.Orange;
            Mission.ForeColor = Color.Black;
            Move.BackColor = Color.Orange;
            Move.ForeColor = Color.Black;

        }
        private void ApplyScale(int pxX, int pxY, double mX, double mY)
        {
            if (_plane == null) return;
            _plane.SetScale(pxX, pxY, mX, mY);
        }

        private Bitmap RescaleImage(Bitmap img, int ancho, int alto)
        {
            Bitmap nuevaImagen = new Bitmap(ancho, alto);
            using (Graphics g = Graphics.FromImage(nuevaImagen))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, ancho, alto);
            }
            return nuevaImagen;
        }

        private void ClearMission()
        {
            missionOverlay.Markers.Clear();
            missionOverlay.Routes.Clear();
            mission = null;
        }

        private void ClearFence()
        {
            fenceOverlay.Markers.Clear();
            fenceOverlay.Routes.Clear();
            geofences.Clear();
        }

        private void AddWaypoint((float lat, float lon) point, float alt)
        {
            if (alt == 0)
            {
                alt = 5;
            }

            if (this.mission == null)
                this.mission = new List<((float lat, float lon) point, float alt)>();

            // marker
            var p = new PointLatLng(point.lat, point.lon);
            var marker = new GMarkerGoogle(p, GMarkerGoogleType.blue_dot);
            missionOverlay.Markers.Add(marker);

            // saves the waypoint
            this.mission.Add((point, alt));

            if (this.mission.Count > 1)
            {
                // penultilmate point
                var (prevPos, _) = this.mission[this.mission.Count - 2];
                var (prevLat, prevLon) = prevPos;

                var previo = new PointLatLng(prevLat, prevLon);

                var points = new List<PointLatLng> { previo, p };
                var route = new GMapRoute(points, "Line")
                {
                    Stroke = new System.Drawing.Pen(System.Drawing.Color.Blue, 2)
                };
                missionOverlay.Routes.Add(route);
            }
        }

        private void LastGoodPoint(bool inside)
        {
            if (inside)
            {
                // saves last good point
                lastX = x;
                lastY = y;
                lastZ = z;
                return;
            }
            else
            {
                // I'm outside, I DO NOT update last good point
                return;
            }

            //Console.WriteLine($"Last good point: {lastX}, {lastY}, {lastZ}");
        }

        private void MapRefresher(double lon, double lat)
        {
            Refr.Lat = lat;
            Refr.Lng = lon;
            gmap.Position = Refr;
            gmap.Refresh();

        }

        private void MapRefresherLoop()
        {
            while (true)
            {
                MapRefresher(lon, lat);
                Thread.Sleep(5000);
            }
        }

        private void Discover_Map()
        {
            Thread.Sleep(5000);
            pictureBox.Visible = false;
        }

        public static class ErrorHandler
        {
           
            public static void ShowError(Exception ex)
            {
                MessageBox.Show(
                    $"Si è verificato un errore:\n{ex.Message}",
                    "Errore",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

    }
}
