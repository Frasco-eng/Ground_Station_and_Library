using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MAVLink;

namespace csDronLink
{
    public partial class Dron
    {
        // Navigation
        private void _NavigationLoop()
        {
            // In this loop, we repeat the last navigation command every second
            // because the drone only navigates for a few seconds if not periodically insisted upon.
            this.navigating = true; // Assuming 'navegando' is a boolean flag in the Dron class
            while (this.navigating)
            {
                // Resend the last navigation command packet
                // serialPort1.Write(this.navPacket, 0, this.navPacket.Length); // Original code comment
                SendMessage(this.navPacket); // Assuming 'navPacket' is a byte[] field holding the last command
                Thread.Sleep(1000);
            }
        }

        public void Navigate(string direction)
        {
            int vx = 0, vy = 0, vz = 0;
            int speed = this.speed; // Assuming 'velocidad' is an int field for speed in m/s

            // Map direction string to NED (North-East-Down) velocity components

            // NORTH = +X (vx)
            // EAST = +Y (vy)
            // DOWN = +Z (vz)

            if (direction == "NorthWest")
            {
                vx = speed;
                vy = -speed;
            }

            if (direction == "North")
            {
                vx = speed;
                vy = 0;
            }

            if (direction == "NorthEast")
            {
                vx = speed;
                vy = speed;
            }

            if (direction == "West")
            {
                vx = 0;
                vy = -speed;
            }

            if (direction == "Stop")
            {
                vx = 0;
                vy = 0;
                // Vertical speed remains 0 unless explicitly commanded
            }

            if (direction == "East")
            {
                vx = 0;
                vy = speed;
            }
            if (direction == "SouthWest")
            {
                vx = -speed;
                vy = -speed;
            }

            if (direction == "South")
            {
                vx = -speed;
                vy = 0;
            }

            if (direction == "SouthEast")
            {
                vx = -speed;
                vy = speed;
            }
            if (direction == "Up")
            {
                vx = 0;
                vy = 0;
                vz = -speed; // Z is Down in NED, so negative speed means Up
            }
            if (direction == "Down")
            {
                vx = 0;
                vy = 0;
                vz = speed; // Z is Down in NED, so positive speed means Down
            }

            // If the direction is "Stop", we should also stop the periodic loop
            if (direction == "Stop")
            {
                this.navigating = false;
            }

            // Create the SET_POSITION_TARGET_LOCAL_NED message to navigate in the indicated direction
            MAVLink.mavlink_set_position_target_local_ned_t moveCmd = new MAVLink.mavlink_set_position_target_local_ned_t
            {
                target_system = this.id,        // System ID (autopilot)
                target_component = 1,           // Component ID (flight controller)
                coordinate_frame = (byte)MAVLink.MAV_FRAME.LOCAL_NED,  // Local NED coordinate system
                // Mask: Ignore Position (X,Y,Z), Ignore Acceleration (AFX,AFY,AFZ), Ignore Yaw, Ignore Yaw Rate.
                // Only use Velocity (VX,VY,VZ).
                type_mask = 0b_0000111111000111,
                x = 0,                          // Ignored (Position)
                y = 0,                          // Ignored (Position)
                z = 0,                          // Ignored (Position)
                vx = vx,                        // Velocity North (m/s)
                vy = vy,                        // Velocity East (m/s)
                vz = vz,                        // Velocity Down (m/s)
                afx = 0,                        // Ignored (Acceleration)
                afy = 0,                        // Ignored (Acceleration)
                afz = 0,                        // Ignored (Acceleration)
                yaw = float.NaN,                // Ignored (Yaw)
                yaw_rate = 0                    // Ignored (Yaw Rate)
            };

            // Generate the MAVLink packet and store it for the loop
            this.navPacket = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_POSITION_TARGET_LOCAL_NED, moveCmd);
            SendMessage(this.navPacket);

            // Start the navigation loop if it's not already running
            if (!this.navigating && direction != "Stop")
            {
                Thread t = new Thread(() => _NavigationLoop());
                t.Start();
            }
            // If it was "Stop", the while loop check (this.navegando) will terminate the thread
        }

    }
}
