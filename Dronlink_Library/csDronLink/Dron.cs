using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MAVLink;

using System.Threading;
using System.Collections.Concurrent;
using System.Net.Sockets;

// LOOK AT THIS. IF I REMOVE IT, THE WaitingRequest CLASS IS NOT RECOGNIZED
using static csDronLink.MessageHandler;
using System.IO;
using System.Data.Entity.Core.Metadata.Edm;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Data;
using GMap.NET.MapProviders;



namespace csDronLink
{

    public partial class Dron
    {
        // Attributes
        byte id; // drone identifier (a number starting from 1)
                 // The drone identifier is necessary when working with a simulated drone swarm

        MAVLink.MavlinkParse mavlink = new MAVLink.MavlinkParse();
        // The mode can be "simulation" or "production"
        string mode;

        // For production communication
        SerialPort serialPort = new SerialPort();
        // For simulation communication
        NetworkStream tcpPort;

        // Last navigation command, which the navigation loop will use to remind
        // the drone where it should navigate
        byte[] navPacket;


        // The telemetry data that interests me
        float relative_alt;
        float lat;
        float lon;
        float heading;


        // Here I will store the reference to the function I need to execute
        // if the client asks me to send the telemetry data.
        Action<byte, uint, List<(string name, float value)>> ProcessTelemetry = null;

        // When I receive a navigation command, I must start the
        // navigation loop to remind the autopilot to maintain
        // the course. With this variable, we control the navigation loop
        Boolean navigating = false;

        // Speed for navigation operations
        int speed = 1;

        Thread t;
        MessageHandler messageHandler;

        // Constructor, connection, telemetry registration and message sending
        public Dron(byte id = 1)
        {
            // Upon creating the drone, its identifier is set (1 by default)
            this.id = id;
        }

        private void SendMessage(byte[] packet)
        {
            if (mode == "production")
                serialPort.Write(packet, 0, packet.Length);
            else
                tcpPort.Write(packet, 0, packet.Length);
        }

        

        public void Connect(string mode, string connector = null)
        {
            this.mode = mode;
            if (mode == "production")
            {
                Console.WriteLine("Connecting to drone via serial port " + connector);
                // Configure the serial port
                serialPort.PortName = connector;
                serialPort.BaudRate = 57600;
                serialPort.Open();
                // Start the message handler
                messageHandler = new MessageHandler(mode, serialPort);
            }
            else
            {
                Console.WriteLine("Connecting to drone simulator via TCP");
                // Configure the connection with the simulator
                string ip = "127.0.0.1";
                // The port depends on the identifier:
                // 1 -> 5763
                // 2 -> 5773
                // id -> 5763 + (id-1)*10
                int port = 5763 + (this.id - 1) * 10;
                TcpClient client = new TcpClient(ip, port);
                tcpPort = client.GetStream();
                messageHandler = new MessageHandler(mode, tcpPort);

            }


            Telemetry_RequestList();


            Console.WriteLine("Connection message sent");
        }


    }
}