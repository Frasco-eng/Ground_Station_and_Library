using GMap.NET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace GndStation
{
    public partial class Form1
    {
        // ========== EVENT HANDLERS (Cartesian Plane) ==============

        private void Plane_DroneSetRequested(PointF p) // handles the request to set the drone position in the cartesian plane
        {
            float dx = p.X - x;
            float dy = p.Y - y;
            MessageBox.Show($"Moving drone to ({p.X:F2}, {p.Y:F2}) meters in Cartesian Plane.\nDelta: ({dx:F2}, {dy:F2}) meters.", "Set Drone Position", MessageBoxButtons.OK, MessageBoxIcon.Information);
            drone.MoveIndoor(dx, dy, 0, heading: false, blocking: false);
        }

        private void Plane_SetRequested((List<PointF> points, string type) data) // handles the request to create a geofence
        {
            if ((data.points == null || data.points.Count < 3) && data.type != "Delete")
            {
                MessageBox.Show("Define at least 3 waypoints for the fence.");
                return;
            }
            else if (data.type == "Delete")
            {
                ClearMission();
                ClearFence();
                MessageBox.Show("All fences cleared.");
                return;
            }


            List<(float x, float y)> fencePoints = data.points.Select(p => ((float)p.X, (float)p.Y)).ToList();
            geofences.Add((fencePoints, data.type));
            int cntr = fencePoints.Count;

            for (int i = 0; i < cntr; i++)
            {
                if (i == 0)
                {
                    _plane.AddFirstFencePoint(fencePoints[i].x, fencePoints[i].y);
                }
                else if (i == cntr - 1)
                {
                    _plane.AddFencePoint(fencePoints[i].x, fencePoints[i].y);
                    PointF point1 = new PointF(fencePoints[i].x, fencePoints[i].y);
                    PointF point2 = new PointF(fencePoints[0].x, fencePoints[0].y);
                    PointF point3 = new PointF(fencePoints[i - 1].x, fencePoints[i - 1].y);
                    _plane.AddLine(point3, point1);
                    _plane.AddLine(point1, point2);
                }
                else
                {
                    _plane.AddFencePoint(fencePoints[i].x, fencePoints[i].y);
                    PointF point1 = new PointF(fencePoints[i].x, fencePoints[i].y);
                    PointF point2 = new PointF(fencePoints[i - 1].x, fencePoints[i - 1].y);
                    _plane.AddLine(point1, point2);
                }
            }
        }

        private void _Plane_MissionRequested(List<PointF> points) // handles the request to create a mission
        {
            Console.WriteLine("Creating mission from Cartesian Plane.");
            List<(float dx, float dy, float dz)> delta = new List<(float dx, float dy, float dz)>();
            float dx, dy, dz;
            for (int i = 0; i < points.Count; i++)
            {
                /*
                if (i == 0)
                {
                    dx = points[i].X - x;
                    dy = points[i].Y - y;
                    dz = 0;
                    delta.Add((dx, dy, dz));
                }
                else
                {
                    dx = points[i].X - points[i - 1].X;
                    dy = points[i].Y - points[i - 1].Y;
                    dz = 0;
                    delta.Add((dx, dy, dz));
                }
                */
                dx = points[i].X;
                dy = points[i].Y;
                dz = 0;
                delta.Add((dx, dy, dz));
            }

            foreach (var p in delta)
            {
                drone.MoveIndoor_Abs(p.dx, p.dy, p.dz, heading: false, blocking: true);
            }

            points.Clear();
        }
        
        private void Plane_MissionRequested(List<PointF> points)
        {
            Thread t = new Thread(() => _Plane_MissionRequested(points));
            t.IsBackground = true;
            t.Start();
        }

        private void Plane_ResetFence(List<PointF> points)
        {
            Console.WriteLine("Resetting fences from Cartesian Plane.");
            try
            {
                geofences.Clear();
                MessageBox.Show("All fences cleared.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel reset: {ex.Message}");
            }
        }

        private void GMapControl1_MouseDown(object sender, MouseEventArgs e) // handles mouse clicks on the map
        {
            if (e.Button == MouseButtons.Right)
            {
                // when right-clicking, get the latitude and longitude of the clicked point

                PointLatLng point = gmap.FromLocalToLatLng(e.X, e.Y);

                // strip the context menu
                ContextMenuStrip contextMenu = new ContextMenuStrip();

                ToolStripMenuItem option1 = new ToolStripMenuItem("Fly Here");
                ToolStripMenuItem option2 = new ToolStripMenuItem("Put Waypoint");
                ToolStripMenuItem option3 = new ToolStripMenuItem("Clear All Points and Fences");
                ToolStripMenuItem option4 = new ToolStripMenuItem("Reset Buttons");


                // match the options to their event handlers
                option1.Click += (s, ev) => drone.GoToWaypoint((float)point.Lat, (float)point.Lng, 5, blocking: false);
                option2.Click += (s, ev) =>
                {
                    float alt = float.TryParse(Value.Text, out var parsed) ? parsed : 10f;
                    AddWaypoint(((float)point.Lat, (float)point.Lng), alt);
                };
                option3.Click += (s, ev) =>
                {
                    ClearFence();
                    ClearMission();
                };
                option4.Click += (s, ev) =>
                {
                    Buttons_Reset();
                };

                contextMenu.Items.Add(option1);
                contextMenu.Items.Add(option2);
                contextMenu.Items.Add(option3);
                contextMenu.Items.Add(option4);

                contextMenu.Show(gmap, e.Location);
            }
        }
    }
}
