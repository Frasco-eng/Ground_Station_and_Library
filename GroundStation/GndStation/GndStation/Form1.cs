using csDronLink;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Microsoft.Ajax.Utilities;
using SharpDX.DirectInput;
using SharpGL.SceneGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static GndStation.Form1;
using static MAVLink;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace GndStation
{
    // Rendi la classe partial per poterla estendere in altri file
    public partial class Form1 : Form
    {

        // ========== INICIO ============== 
        public Form1()
        {

            InitializeComponent();// Initialize all components

            CheckForIllegalCrossThreadCalls = false; // I turn off cross-thread checking (going to handle threads myself)

            this.WindowState = FormWindowState.Maximized; // we maximize the window

            _pb = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Black, // opzionale
                Visible = false
            };
            panelMapa.Controls.Add(_pb);

            this.DoubleBuffered = true;
            //pictureBox1.Dock = DockStyle.Fill;
            //pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            // We load the custom drone icon
            customIcon = new Bitmap("dron.png");
            // We resize the icon to 20x20 pixels
            customIcon = RescaleImage(customIcon, 30, 30);


            pixelx = panelMapa.Width;
            pixely = panelMapa.Height;

            // Cartesian Plane Control for Indoor Mapping
            _plane = new CartesianPlaneControl
            {
                Dock = DockStyle.Fill,
                Visible = false, // Initially hidden
            };
            panelMapa.Controls.Add(_plane);

            pictureBox = new PictureBox();
            pictureBox.Image = Image.FromFile("NOE.png");
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage; // o Zoom, CenterImage, ecc.
            pictureBox.Dock = DockStyle.Fill;

            panelMapa.Controls.Add(pictureBox);


            // Subscribe to events from the Indoor Cartesian Plane Control
            _plane.DroneSetRequested += Plane_DroneSetRequested;
            _plane.SetFence += Plane_SetRequested;
            _plane.SetMission += Plane_MissionRequested;
            _plane.ResetFence += Plane_ResetFence;


            // Outdoor Map Control for Outdoor Mapping
            gmap = new GMapControl
            {
                Dock = DockStyle.Fill,
                Visible = false, // Initially hidden
                CanDragMap = true, // we can drag the map
                DragButton = MouseButtons.Left,
                MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter,
                IgnoreMarkerOnMouseWheel = true,
                MinZoom = 5,
                MaxZoom = 20,
                Zoom = 18,
                ShowCenter = false
            };
            missionOverlay = new GMapOverlay("mission"); // overlay for the mission
            fenceOverlay = new GMapOverlay("fences"); // overlay for geofences
            trackOverlay = new GMapOverlay("track"); // overlay for the drone track

            gmap.MouseDown += GMapControl1_MouseDown; // Handle mouse down event

            panelMapa.Controls.Add(gmap);
            gmap.Overlays.Add(missionOverlay);
            gmap.Overlays.Add(fenceOverlay);
            gmap.Overlays.Add(trackOverlay);

        }

        // ========== GLOBAL VARIABLES ==============

        //Drone Object
        public Dron drone = new Dron();

        // for Outdoor Map
        private GMapControl gmap;
        private GMapOverlay missionOverlay;
        private GMapOverlay fenceOverlay;
        private GMapOverlay trackOverlay;
        PointLatLng Refr = new PointLatLng(0, 0);

        // for Indoor Map
        private CartesianPlaneControl _plane;

        // For Drone Representation
        private Bitmap customIcon; //dron icon image
        GMarkerGoogle droneIcon; // marker for the drone
        GMapRoute droneHeading; // heading line
        GMapRoute velHeading; // velocity line
        PictureBox pictureBox;

        // for Home Position
        PointLatLng home;
        string nameHome;

        // for Mission Management
        List<((float X, float Y), float Z)> mission; // here we store the mission waypoints with altitude
        public List<(float X, float Y)> MissionPoints => mission?.Select(m => m.Item1).ToList() ?? new List<(float X, float Y)>(); // property to get mission waypoints without altitude

        // for Geofences Management
        public List<(List<(float X, float Y)> fence, string type)> geofences = new List<(List<(float lat, float lon)>, string)>();
        string GFence_type;
        double prevlat;
        double prevlon;
        bool violation = false;
        bool inside = true;
        float lastX, lastY, lastZ;

        // for Movement Commands
        string direction;
        string value = "0";
        private bool _fixHead = false;

        // for Telemetry Data
        float x, y, z, vx, vy, vz;
        double lat, lon, alt, heading;
        float roll, pitch, yaw, wroll, wpitch, wyaw, time;
        float battery_current, battery_remaining;
        float[] battery_cell;
        float accx, accy, accz, gyrox, gyroy, gyroz;


        uint base_mode;
        uint custom_mode;
        // for Telemetry Representation
        private double _vxFilt = 0, _vyFilt = 0;

        private bool _filtInit = false;
        private double _angleFilt;
        private bool _angleFiltInit = false;
        int pixelx, pixely;

        // Find a Joystick Guid
        Guid joystickGuid = Guid.Empty;

        List<ushort> channels = new List<ushort>(10) { 1500, 1500, 1500, 1500, 1000, 1000, 1000, 1000, 1000, 1000 };       // Initialize DirectInput


        SharpDX.DirectInput.DirectInput directInput = new DirectInput();
        bool radiocommand = false;

        // MAVLink Message IDs
        private const int MSG_GLOBAL_POSITION_INT = 33;
        private const int MSG_LOCAL_POSITION_NED = 32;
        private const int MSG_ATTITUDE = 30;
        private const int MSG_BATTERY_STATUS = 147;
        private const int MSG_HEARTBEAT = 0;
        private const int MSG_SCALED_IMU = 26;

        public TextBox MaxAltitude2 => this.MaxAltitude;
        public ComboBox InOutDoor2 => this.InOutDoor;

        float radtodeg = 180/(float)Math.PI;

        List<(string nombre, float valor)> telemetria_copy;
    }
}
