using System.Collections.Generic;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Drawing;
using System.Linq;
using System;
using System.Threading;

namespace GndStation
{
    public partial class Form1
    {
        // ========== GEOFENCES, CHECKS AND PROTOCOLS ==============
        public void CreateFence(string type, List<(float lat, float lon)> mission)
        {
            if (mission == null || mission.Count < 3)
            {
                MessageBox.Show("Define at least 3 waypoints for the fence.");
                return;
            }
            // Draw the geofence based on the selected type
            List<PointLatLng> fencePoints = new List<PointLatLng>();
            foreach (var wp in mission)
            {
                var p = new PointLatLng(wp.lat, wp.lon);
                fencePoints.Add(new PointLatLng(wp.lat, wp.lon));
                var marker = new GMarkerGoogle(p, GMarkerGoogleType.blue_dot);
                fenceOverlay.Markers.Add(marker);
            }

            geofences.Add((mission, type));

            // Close the polygon by adding the first point at the end
            fencePoints.Add(fencePoints[0]);
            GMapRoute fenceRoute = new GMapRoute(fencePoints, "Geofence")
            {
                Stroke = new Pen(type == "Inclusion" ? Color.Green : Color.Red, 2)
            };
            fenceOverlay.Routes.Add(fenceRoute);
            missionOverlay.Markers.Clear();
            missionOverlay.Routes.Clear();
            gmap.Refresh();
        }

        public void CheckForViolation(CheckBox geoFenceEnable, int safetymode, List<(List<(float X, float Y)> fence, string type)> geofences, double MaxAlt)
        {
            if (!geoFenceEnable.Checked || geofences.Count == 0)
            {
                MessageBox.Show("[CheckForViolation] GeoFence disabled or no fence.");
                return;
            }

            while (geoFenceEnable.Checked)
            {
                double Xval = 0;
                double Yval = 0;
                double Zval = 0;

                if (InOutDoor.Text == "Outdoor")
                {
                    Xval = lat;
                    Yval = lon;
                    Zval = alt;
                }
                else
                {
                    Xval = x;
                    Yval = y;
                    Zval = z;
                }

                var currentPosition = (Xval, Yval);

                //Console.WriteLine($"[CheckForViolation] Drone Position: X={lat}, Y={lon}, Z={alt}, MaxAlt={MaxAlt}");

                foreach (var (fence, type) in geofences)
                {
                    /*Console.WriteLine($"[CheckForViolation] Fence Type={type}, Punti={fence.Count}");
                    for (int i = 0; i < fence.Count; i++)
                    {
                        Console.WriteLine($"   Punto {i}: lat={fence[i].lat}, lon={fence[i].lon}");
                    }*/

                    inside = IsPointInPolygon(currentPosition.Xval, currentPosition.Yval, fence);
                    //Console.WriteLine($"[CheckForViolation] Risultato IsPointInPolygon={inside} per fence Type={type}");

                    if ((type == "Inclusion" && !inside) || (type == "Exclusion" && inside) || Zval > MaxAlt)
                    {
                        //Console.WriteLine($"[CheckForViolation] VIOLAZIONE! Type={type}, Inside={inside}, Alt={alt}");
                        if (!violation)
                        {
                            violation = true;
                            //MessageBox.Show("ora"); almost instantaneous
                            //Console.WriteLine("[CheckForViolation] Chiamo SafetyCall...");
                            SafetyCall(safetymode);
                        }

                        break;
                    }
                    else
                    {
                        LastGoodPoint(true);
                    }
                }
                Thread.Sleep(500); // Rallentiamo il check
            }
        }

        private void SafetyCall(int safetymode)
        {
            //Console.WriteLine($"[SafetyCall] Avviato, safetymode={safetymode}");

            switch (safetymode)
            {
                case 1:
                    //Console.WriteLine("[SafetyCall] Mode 1: Land");
                    drone.Land(block: false, EndFailSafe, "Landed due to Geofence Violation");
                    while (violation)
                    {
                        Thread.Sleep(1000);
                    }
                    break;
                case 2:
                    //Console.WriteLine("[SafetyCall] Mode 2: RTL");
                    drone.RTL(block: false, EndFailSafe, "RTL due to Geofence Violation");
                    while (violation)
                    {
                        Thread.Sleep(1000);
                    }
                    break;
                case 3:
                    //Console.WriteLine("[SafetyCall] Modalità 3: Push Back");
                    PushBackViolation(vx, vy, 10, radiocommand);
                    break;
            }

            //Console.WriteLine("[SafetyCall] End, flag violation reset.");
        }

        private bool IsPointInPolygon(double Xval, double Yval, List<(float X, float Y)> polygon)
        {
            int n = polygon.Count;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((polygon[i].X > Xval) != (polygon[j].X > Xval) &&
                    (Yval < (polygon[j].Y - polygon[i].Y) * (Xval - polygon[i].X) / (polygon[j].X - polygon[i].X) + polygon[i].Y))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private void PushBackViolation(float vx, float vy, float distanceMeters, bool radiocommand)
        {
            //float headingrad = heading * (float)Math.PI / 180;
            Console.WriteLine($"The last good point was {lastX}x, {lastY}y, {lastZ}z");
            float v = (float)Math.Sqrt(vx * vx + vy * vy);
            float direction = (float)Math.Atan2(vy, vx);
            Console.WriteLine($"Pushing back drone by {distanceMeters} meters in direction {direction * 180 / Math.PI:F1}° (velocity {v:F2} m/s)");
            drone.SetFlightMode("BRAKE");
            Thread.Sleep(1000);
            drone.SetFlightMode("GUIDED");


            // Ritorno al punto buono
            drone.MoveIndoor_Abs(lastX - (float)(1.2 * vx), lastY - (float)(1.2 * vy), lastZ - (float)(1.2* vz), heading: false, blocking: true); // Minus velocities for some margin on the last point

            if (radiocommand)
            {
                drone.SetFlightMode("BRAKE");
            }

            drone.CancelMission();
            EndFailSafe(1, "FailSafe due to Geofence Violation Braking");
        }
    }
}