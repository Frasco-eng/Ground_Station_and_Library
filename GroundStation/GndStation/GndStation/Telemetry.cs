using System.Collections.Generic;
using System.Linq;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Drawing;
using System;
using System.Threading;

namespace GndStation
{
    public partial class Form1
    {
        // ========== TELEMETRÍA ==============
        
        private void ProcessTelemetry_Outdoor(byte id, uint msgid, (string name, float value)[] snap)
        {
            if (msgid != MSG_GLOBAL_POSITION_INT) return;

            var telemetry = snap.ToDictionary(p => p.name, p => p.value);
            float Get(string k) => telemetry.TryGetValue(k, out var v) ? v : 0f;

            lat = (double)Get("lat");
            lon = (double)Get("lon");
            heading = (double)Get("hdg");
            alt = (double)Get("alt") - 3.450;

            Alt.Text = alt.ToString("F2");
            Hdg.Text = heading.ToString("F2");
            Lat.Text = lat.ToString("F2");
            Lon.Text = lon.ToString("F2");

            PointLatLng point = new PointLatLng(lat, lon);
            int angle = Convert.ToInt16(heading);
            double angleRad = angle * Math.PI / 180.0;
            double distDeg = 50 / 111139.0;
            double lat2 = lat + (distDeg * Math.Cos(angleRad));
            double lon2 = lon + (distDeg * Math.Sin(angleRad) / Math.Cos(lat * Math.PI / 180));

            double latvel, lonvel;

            const double M = 50.0;
            const double M_PER_DEG_LAT = 111139.0;
            double mPerDegLon = M_PER_DEG_LAT * Math.Cos(lat * Math.PI / 180.0);

            double dLat = lat - prevlat;
            double dLon = lon - prevlon;
            double vx_m = dLat * M_PER_DEG_LAT;
            double vy_m = dLon * mPerDegLon;

            const double alpha = 0.25; // lower = more stable


            if (!_filtInit)
            {
                _vxFilt = vx_m;
                _vyFilt = vy_m;
                _filtInit = true;
            }
            else
            {
                _vxFilt = (1 - alpha) * _vxFilt + alpha * vx_m;
                _vyFilt = (1 - alpha) * _vyFilt + alpha * vy_m;
            }

            double norm = Math.Sqrt(_vxFilt * _vxFilt + _vyFilt * _vyFilt);

            // EMA filter on the angle of the velocity vector
            double angoloVel = Math.Atan2(_vyFilt, _vxFilt) * 180.0 / Math.PI;
            if (!_angleFiltInit)
            {
                _angleFilt = angoloVel;
                _angleFiltInit = true;
            }
            else
            {
                _angleFilt = (1 - alpha) * _angleFilt + alpha * angoloVel;
            }
            double angleVelRad = _angleFilt * Math.PI / 180.0;

            if (norm < 0.01) // Speed threshold to avoid noise
            {
                double dx_m = M * Math.Cos(angleRad);
                double dy_m = M * Math.Sin(angleRad);
                latvel = lat + dx_m / M_PER_DEG_LAT;
                lonvel = lon + dy_m / mPerDegLon;
            }
            else
            {
                double dx_m = M * Math.Cos(angleVelRad);
                double dy_m = M * Math.Sin(angleVelRad);
                latvel = lat + dx_m / M_PER_DEG_LAT;
                lonvel = lon + dy_m / mPerDegLon;
            }

            if (droneIcon == null)
            {
                droneIcon = new GMarkerGoogle(point, customIcon);
                droneIcon.Offset = new Point(-customIcon.Width / 2, -customIcon.Height / 2);
                trackOverlay.Markers.Add(droneIcon);
            }
            if (droneHeading == null)
            {
                droneHeading = new GMapRoute(new List<PointLatLng>
                {
                    new PointLatLng(lat, lon),
                    new PointLatLng(lat2, lon2)
                }, "myline")
                { Stroke = new Pen(Color.Red, 2) };
                trackOverlay.Routes.Add(droneHeading);
            }
            if (velHeading == null)
            {
                velHeading = new GMapRoute(new List<PointLatLng>
                {
                    new PointLatLng(lat, lon),
                    new PointLatLng(latvel, lonvel)
                }, "myline2")
                { Stroke = new Pen(Color.Black, 2) };
                trackOverlay.Routes.Add(velHeading);
            }

            droneIcon.Position = point;

            droneHeading.Points[0] = new PointLatLng(lat, lon);
            droneHeading.Points[1] = new PointLatLng(lat2, lon2);

            velHeading.Points[0] = new PointLatLng(lat, lon);
            velHeading.Points[1] = new PointLatLng(latvel, lonvel);

            gmap.UpdateRouteLocalPosition(droneHeading);
            gmap.UpdateRouteLocalPosition(velHeading);
            gmap.Refresh();

            prevlat = lat;
            prevlon = lon;
        }

        private void ProcessTelemetry_Indoor(byte id, uint msgid, (string name, float value)[] snap)
        {
            if (msgid != MSG_LOCAL_POSITION_NED) return;

            var telemetry = snap.ToDictionary(p => p.name, p => p.value);
            float Get(string k) => telemetry.TryGetValue(k, out var v) ? v : 0f;

            // Aqui vendre cada vez que llegue un paquete de telemetría
            x = Get("x");
            y = Get("y");
            z = -Get("z");
            vx = Get("vx");
            vy = Get("vy");
            vz = Get("vz");

            if (BtnEnable.Text != "Enable")
            {
                _plane.ClearDroneRelated();
                _plane.DroneUpdate(x, y);
            }


            // I show telemetry values
            Z.Text = z.ToString("F2");
            Y.Text = y.ToString("F2");
            X.Text = x.ToString("F2");
            VelX.Text = vx.ToString("F2");
            VelY.Text = vy.ToString("F2");
            VelZ.Text = vz.ToString("F2");
        }

        private void Process_Attitude(byte id, uint msgid, (string name, float value)[] snap)
        {
            if (msgid != MSG_ATTITUDE) return;
            var telemetry = snap.ToDictionary(p => p.name, p => p.value);
            float Get(string k) => telemetry.TryGetValue(k, out var v) ? v : 0f;

            roll = Get("roll") * radtodeg;
            pitch = Get("pitch") * radtodeg;
            yaw = Get("yaw") * radtodeg;
            wroll = Get("rollspeed") * radtodeg;
            wpitch = Get("pitchspeed") * radtodeg;
            wyaw = Get("yawspeed") * radtodeg;
            time = Get("time_boot_ms") / 1000;

            Roll_display.Text = roll.ToString("F2");
            Pitch_display.Text = pitch.ToString("F2");
            Yaw_display.Text = yaw.ToString("F2");
            WR_display.Text = wroll.ToString("F2");
            WP_display.Text = wpitch.ToString("F2");
            WY_display.Text = wyaw.ToString("F2");
            Time_display.Text = time.ToString("F2");

        }

        private void Process_Battery(byte id, uint msgid, (string name, float value)[] snap)
        {
            if (msgid != MSG_BATTERY_STATUS) return;
            var telemetry = snap.ToDictionary(p => p.name, p => p.value);
            float Get(string k) => telemetry.TryGetValue(k, out var v) ? v : 0f;

            battery_current = Get("current_battery");
            battery_remaining = Get("battery_remaining");
            for (int i = 0; i < telemetry.Count - 2; i++)
            {
                //battery_cell.Append(telemetry.FirstOrDefault(p => p.name == $"voltage_cell_{i+1}").value);
            }

            I.Text = battery_current.ToString("F2");
            V_perc.Text = battery_remaining.ToString("F2");
            if (battery_cell == null) return;
            V_1.Text = battery_cell.Length > 0 ? battery_cell[0].ToString("F2") : "N/A";
            V_2.Text = battery_cell.Length > 1 ? battery_cell[1].ToString("F2") : "N/A";
            V_3.Text = battery_cell.Length > 2 ? battery_cell[2].ToString("F2") : "N/A";
            V_4.Text = battery_cell.Length > 3 ? battery_cell[3].ToString("F2") : "N/A";
        }

        private void Process_Heartbeat(byte id, uint msgid, (string name, float value)[] snap)
        {
            if (msgid != MSG_HEARTBEAT) return;
            var telemetry = snap.ToDictionary(p => p.name, p => p.value);
            float Get(string k) => telemetry.TryGetValue(k, out var v) ? v : 0f;

            base_mode = (uint)Get("base_mode");
            custom_mode = (uint)Get("custom_mode");

            switch (base_mode)
            {
                case 0:
                    Base_display.Text = "Manual";
                    break;
                case 64:
                    Base_display.Text = "Stabilized";
                    break;
                case 88:
                    Base_display.Text = "Altitude Hold";
                    break;
                case 92:
                    Base_display.Text = "Position Hold";
                    break;
                case 216:
                    Base_display.Text = "Auto";
                    break;
                case 192:
                    Base_display.Text = "Guided";
                    break;
                case 208:
                    Base_display.Text = "Loiter";
                    break;
                default:
                    Base_display.Text = "Unknown";
                    break;
            }

            switch (custom_mode)
            {
                case 0:
                    Custom_display.Text = "Stabilize";
                    break;
                case 2:
                    Custom_display.Text = "Altitude Hold";
                    break;
                case 16:
                    Custom_display.Text = "Position Hold";
                    break;
                case 3:
                    Custom_display.Text = "Auto";
                    break;
                case 4:
                    Custom_display.Text = "Guided";
                    break;
                case 5:
                    Custom_display.Text = "Loiter";
                    break;
                case 6:
                    Custom_display.Text = "RTL";
                    break;
                case 9:
                    Custom_display.Text = "Land";
                    break;
                case 17:
                    Custom_display.Text = "Brake";
                    break;
                default:
                    Custom_display.Text = "Unknown";
                    break;
            }

        }

        private void Process_IMU(byte id, uint msgid, (string name, float value)[] snap)
        {
            if (msgid != MSG_SCALED_IMU) return;

            var telemetry = snap.ToDictionary(p => p.name, p => p.value);
            float Get(string k) => telemetry.TryGetValue(k, out var v) ? v : 0f;

            accx = Get("xacc");
            accy = Get("yacc");
            accz = Get("zacc");
            gyrox = Get("xgyro");
            gyroy = Get("ygyro");
            gyroz = Get("zgyro");

            AccX_display.Text = accx.ToString("F2");
            AccY_display.Text = accy.ToString("F2");
            AccZ_display.Text = accz.ToString("F2");

        }


        private void ProcessTelemetry(byte id, uint msgid, List<(string nombre, float valor)> telemetry)
        {

            if (msgid == MSG_LOCAL_POSITION_NED)
            {
                //ProcessTelemetry_Indoor(id, msgid, telemetry);
                var snap = telemetry.ToArray();
                if (this.InvokeRequired)
                {
                    BeginInvoke(new Action(() => ProcessTelemetry_Indoor(id, msgid, snap)));
                }
                else
                {
                    // Se siamo già sul thread UI, chiama direttamente.
                    ProcessTelemetry_Indoor(id, msgid, snap);
                }
                return;
            }
            else if (msgid == MSG_GLOBAL_POSITION_INT)
            {
                //ProcessTelemetry_Outdoor(id, msgid, telemetry);
                var snap = telemetry.ToArray();
                if (this.InvokeRequired)
                {
                    BeginInvoke(new Action(() => ProcessTelemetry_Outdoor(id, msgid, snap)));
                }
                else
                {
                    // Se siamo già sul thread UI, chiama direttamente.
                    ProcessTelemetry_Outdoor(id, msgid, snap);
                }
                return;
            }
            else if (msgid == MSG_ATTITUDE)
            {
                //Process_Attitude(id, msgid, telemetry);
                var snap = telemetry.ToArray();
                if (this.InvokeRequired)
                {
                    BeginInvoke(new Action(() => Process_Attitude(id, msgid, snap)));
                }
                else
                {
                    // Se siamo già sul thread UI, chiama direttamente.
                    Process_Attitude(id, msgid, snap);
                }
                return;
            }
            else if (msgid == MSG_BATTERY_STATUS)
            {
                //Process_Battery(id, msgid, telemetry);
                var snap = telemetry.ToArray();
                if (this.InvokeRequired)
                {
                    BeginInvoke(new Action(() => Process_Battery(id, msgid, snap)));
                }
                else
                {
                    // Se siamo già sul thread UI, chiama direttamente.
                    Process_Battery(id, msgid, snap);
                }
                return;
            }
            else if (msgid == MSG_HEARTBEAT)
            {
                //Process_Heartbeat(id, msgid, telemetry);
                var snap = telemetry.ToArray();
                if (this.InvokeRequired)
                {
                    BeginInvoke(new Action(() => Process_Heartbeat(id, msgid, snap)));
                }
                else
                {
                    // Se siamo già sul thread UI, chiama direttamente.
                    Process_Heartbeat(id, msgid, snap);
                }
                return;
            }
            else if (msgid == MSG_SCALED_IMU)
            {
                //Process_IMU(id, msgid, telemetry);
                var snap = telemetry.ToArray();
                if (this.InvokeRequired)
                {
                    BeginInvoke(new Action(() => Process_IMU(id, msgid, snap)));
                }
                else
                {
                    // Se siamo già sul thread UI, chiama direttamente.
                    Process_IMU(id, msgid, snap);
                }
                return;
            }
            else
            {
                return;
            }
        }
    }
}
