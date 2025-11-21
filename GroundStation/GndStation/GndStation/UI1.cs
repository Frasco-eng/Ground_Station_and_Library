using csDronLink;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static GMap.NET.Entity.OpenStreetMapGraphHopperGeocodeEntity;

namespace GndStation
{
    public partial class Form1
    {
        // ========== BUTTONS ==============

        private void Simulation_CheckedChanged(object sender, EventArgs e)
        {
            CMB_comport.Visible = false;
        }

        private void Production_CheckedChanged(object sender, EventArgs e)
        {
            CMB_comport.Visible = true;
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            if (Simulation.Checked)
            {
                drone.Connect("simulation");
                Connect.BackColor = Color.Green;
            }

            else if (Production.Checked)
            {
                drone.Connect("production", CMB_comport.Text);
                //drone.Connect("production", "COM10");
                Connect.BackColor = Color.Green;
            }

            else
            {
                MessageBox.Show("Choose Connection mode");
                return;
            }

            drone.StartHeartbeatLoop();

            // Set up the map
            gmap.MapProvider = GMapProviders.GoogleSatelliteMap;
            GMaps.Instance.Mode = AccessMode.ServerOnly;


            // I put the map center to home position
            gmap.Position = home;

            gmap.Visible = true; // Mostrar el mapa
            Thread tmap = new Thread(() => MapRefresherLoop());
            tmap.IsBackground=true;
            tmap.Start();
                                            

        }

        private void Value_TextChanged(object sender, EventArgs e)
        {
            value = Value.Text;
        }

        private void Direction_TextChanged(object sender, EventArgs e)
        {
            direction = Direction.Text;
        }


        private void Takeoff_Click(object sender, EventArgs e)
        {
            if (int.Parse(Value.Text) <= 0)
            {
                MessageBox.Show("Insert a valid altitude (>0)");
                return;
            }
            else if (int.Parse(Value.Text) > 0)
            {
                drone.Takeoff(int.Parse(value), block: false, InAir, "Flying");
                Takeoff.BackColor = Color.Yellow;
            }
            else
            {
                MessageBox.Show("Invalid");
                return;
            }
        }

        private void Land_Click(object sender, EventArgs e)
        {
            drone.Land(block: false, OnGround, "Landed");
            Land.BackColor = Color.Yellow;
        }

        private void Mission_Click(object sender, EventArgs e)
        {   
            if (mission == null || mission.Count < 2)
            {
                MessageBox.Show("Define at least 2 waypoints for the mission.");
                return;
            }
            else
            {
                Mission.BackColor = Color.Yellow;

                var missionCopy = new List<((float lat, float lon) point, float alt)>(mission);

                drone.UploadMission(missionCopy);
                drone.ExecuteMission(blocking: false, OnWaypointReached: null, MissionEnd);
            }
            
            // Reset for next input
            // mission = new List<((float lat, float lon) point, float alt)>(); 
        }

        private void Move_Click(object sender, EventArgs e)
        {
            drone.Move(direction, int.Parse(value), heading: !_fixHead, blocking: false, Finished, "Finished Moving");
            Move.BackColor = Color.Yellow;
        }

        private void RTL_Click(object sender, EventArgs e)
        {
            drone.RTL(block: false, OnGround, "Landed");
            RTL.BackColor = Color.Yellow;
        }

        private void Enable_telemetry_CheckedChanged(object sender, EventArgs e)
        {
            if (Enable_telemetry.Checked)
            {
                drone.SendTelemetryDataToClient(ProcessTelemetry);
                Thread map_t = new Thread(() => Discover_Map());
                map_t.IsBackground = true;
                map_t.Start();
                
            }
            else
            {
                drone.StopTelemetryData();
            }
            
            
        }

        private void Velocity_Scroll(object sender, EventArgs e)
        {
            int n = Velocidad.Value;
            VelLbl.Text = Velocidad.Value.ToString();
        }

        private void Heading_Scroll(object sender, EventArgs e)
        {
            int n = Heading.Value;
            HdgLbl.Text = Heading.Value.ToString();
        }

        private void Velocity_MouseUp(object sender, MouseEventArgs e)
        {

            int valorSeleccionado = Velocidad.Value;
            drone.ChangeSpeed(valorSeleccionado);
        }

        private void Heading_MouseUp(object sender, MouseEventArgs e)
        {

            int valorSeleccionado = Heading.Value;
            drone.ChangeHeading(valorSeleccionado, blocking: false);
        }

        private void CMB_comport_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void CMB_comport_Click(object sender, EventArgs e)
        {
            CMB_comport.DataSource = SerialPort.GetPortNames();
        }

        private void Geof_Inclusion_CheckedChanged(object sender, EventArgs e)
        {
            GFence_type = Geof_Inclusion.Text;
        }

        private void Geof_Exclusion_CheckedChanged(object sender, EventArgs e)
        {
            GFence_type = Geof_Exclusion.Text;
        }

        private void checkRCcommand_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                radiocommand = checkRCcommand.Checked;
                if (radiocommand)
                {
                    // Executes joystick function in the beginning
                    Joystick_fcn(channels);
                }
                else
                {
                    // Stops joystick function
                    //Still to be implemented !!!!!!!!!!!!!!!!!!!!!!!!!!!
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ShowError(ex);
            }

        }

        private void Draw_Fence_Click(object sender, EventArgs e)
        {
            CreateFence(GFence_type, MissionPoints);
            mission = null;
        }

        public void InOutDoor_Choice(object sender, EventArgs e)
        {
            List<string> options = new List<string> { "Outdoor", "Indoor" };
            InOutDoor.DataSource = options;
        }

        private void Flight_ModeSelect(object sender, EventArgs e)
        {
            List<string> options = new List<string> { "STABILIZE", "ALT_HOLD", "LOITER", "AUTO", "RTL", "LAND", "BRAKE", "GUIDED" };
            Flight_mode.DataSource = options;
        }

        private void Send_mode_Click(object sender, EventArgs e)
        {
            drone.SetFlightMode(Flight_mode.Text);
        }

        private void Move_Indoor_Click(object sender, EventArgs e)
        {
            //Console.WriteLine(X_box.Text, Y_box.Text, Z_box.Text);
            if (X_box.Text=="" || Y_box.Text=="" || Z_box.Text=="")
            {                 
                MessageBox.Show("Insert valid coordinates");
                return;
            }
            drone.MoveIndoor(float.Parse(X_box.Text), float.Parse(Y_box.Text), float.Parse(Z_box.Text), heading: true, blocking: false, Finished, "Finished Moving Indoor");
        }

        private void Fix_Head_CheckedChanged(object sender, EventArgs e)
        {
            _fixHead = Fix_Head.Checked;

        }

        private void BtnEnable_Click(object sender, EventArgs e)
        {
            if (BtnEnable.Text == "Enable")
            {
                // dynamic plane creation setup
                _plane = new CartesianPlaneControl();
                _plane.Dock = DockStyle.Fill;
                panelMapa.Controls.Add(_plane);
                _plane.BringToFront();
                _plane.Visible = true;

                _plane.DroneSetRequested += Plane_DroneSetRequested;
                _plane.SetFence += Plane_SetRequested;
                _plane.SetMission += Plane_MissionRequested;
                _plane.ResetFence += Plane_ResetFence;

                _plane.SetScale(pxX: pixelx, pxY: pixely, mX: 20.0, mY: 12.0);

                _plane.CursorPositionChanged += (s, ev) =>
                {
                    // Coordinates in meters of cursor position
                    float x = ev.XMeters;
                    float y = ev.YMeters;

                    
                };

                // optional: change grid step
                _plane.GridStepMeters = 1.0;

                BtnEnable.Text = "Disable";
            }
            else
            {
                // Logica di pulizia
                if (_plane != null)
                {
                    panelMapa.Controls.Remove(_plane);
                    _plane.Dispose();
                    _plane = null;
                }
                BtnEnable.Text = "Enable";
            }
        }

        private void cameraBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                _pb.Visible = cameraBox.Checked;
            }
            catch (Exception ex)
            {
                ErrorHandler.ShowError(ex);
            }
        }

        private void SetupBtn_Click(object sender, EventArgs e)
        {
            SetupForm f = new SetupForm(drone, this);
            f.ShowDialog();
        }

    }
}
