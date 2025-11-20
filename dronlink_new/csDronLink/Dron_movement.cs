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
        // Movement
        private void _Move(string direction, int distance, bool heading = true, Action<byte, object> f = null, object param = null)
        {
            // Stop the navigation loop if necessary
            // Movement operations specify distance. It's not necessary to keep sending the command
            // periodically to the autopilot.

            // ATTENTION: IN DIAGONAL MOVES, THE DISTANCE SHOULD BE CORRECTED BECAUSE OTHERWISE
            // THE DISTANCE TRAVELED IS GREATER THAN THE INDICATED, ACCORDING TO THE PYTHAGOREAN THEOREM

            if (this.navigating)
                this.navigating = false;

            int dx = 0, dy = 0, dz = 0;

            if (direction == "ForwardLeft")
            {
                dx = distance;
                dy = -distance;
            }

            if (direction == "Forward")
            {
                dx = distance;
                dy = 0;
            }

            if (direction == "ForwardRight")
            {
                dx = distance;
                dy = distance;
            }

            if (direction == "Left")
            {
                dx = 0;
                dy = -distance;
            }

            if (direction == "Stop")
            {
                dx = 0;
                dy = 0;
            }

            if (direction == "Right")
            {
                dx = 0;
                dy = distance;
            }
            if (direction == "BackLeft")
            {
                dx = -distance;
                dy = -distance;
            }

            if (direction == "Back")
            {
                dx = -distance;
                dy = 0;
            }

            if (direction == "BackRight")
            {
                dx = -distance;
                dy = distance;
            }
            if (direction == "Up")
            {
                dz = -distance; // Z is negative for Up in NED
            }
            if (direction == "Down")
            {
                dz = distance; // Z is positive for Down in NED
            }

            // Create the movement message in NED coordinates (North-East-Down)

            MAVLink.mavlink_set_position_target_local_ned_t moveCmd = new MAVLink.mavlink_set_position_target_local_ned_t
            {
                target_system = this.id,
                target_component = 1,
                coordinate_frame = (byte)MAVLink.MAV_FRAME.BODY_OFFSET_NED, // Body-fixed local NED coordinate system
                // Mask: Only use position components (x, y, z), ignore velocity and acceleration.
                type_mask = 0b_0000110111111000,
                x = dx, // Distance in X (North)
                y = dy, // Distance in Y (East)
                z = dz, // Distance in Z (Down, so -distance for Up, +distance for Down)
                vx = 0,
                vy = 0,
                vz = 0,
                afx = 0,
                afy = 0,
                afz = 0,
                yaw = 0,
                yaw_rate = 0
            };

            if (!heading)
            {
                // Remove YAW_IGNORE if we do not want to maintain heading
                moveCmd.type_mask = 0b_0000010111111000;
            }

            // Generate MAVLink packet
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_POSITION_TARGET_LOCAL_NED, moveCmd);

            // Send command to the drone
            SendMessage(packet);
            // Wait 2 seconds for the drone to start moving
            // Because immediately after I will wait for the speed to return to 0
            Thread.Sleep(2000);


            // Wait here for the message indicating it has reached the destination
            // I will know this because the velocity will be zero
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            MAVLink.MAVLinkMessage message = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT (We check velocity from here)
                condition: CheckStopped, // Assuming ComprobarParado checks if speed is zero
                timeout: -1
            );

            if (f != null)
                f(this.id, param);
        }

        public void Move(string direction, int distance, Boolean heading = true, Boolean blocking = true, Action<byte, object> f = null, object param = null)
        {
            if (blocking)
            {
                this._Move(direction, distance);
            }
            else
            {
                Thread t = new Thread(() => _Move(direction, distance, heading, f, param));
                t.Start();
            }
        }

        private void _GoToWaypoint(float lat, float lon, float alt, Action<byte, object> f = null, object param = null)
        {
            // Stop the navigation loop (in case it was running)
            this.navigating = false;
            var msg = new MAVLink.mavlink_set_position_target_global_int_t
            {
                target_system = this.id, // Drone ID
                target_component = 1, // Component (usually 1)
                coordinate_frame = (byte)MAV_FRAME.GLOBAL_RELATIVE_ALT_INT,
                type_mask = (ushort)(
                POSITION_TARGET_TYPEMASK.VX_IGNORE |
                POSITION_TARGET_TYPEMASK.VY_IGNORE |
                POSITION_TARGET_TYPEMASK.VZ_IGNORE |
                POSITION_TARGET_TYPEMASK.AX_IGNORE |
                POSITION_TARGET_TYPEMASK.AY_IGNORE |
                POSITION_TARGET_TYPEMASK.AZ_IGNORE |
                POSITION_TARGET_TYPEMASK.YAW_IGNORE |
                POSITION_TARGET_TYPEMASK.YAW_RATE_IGNORE
                ), // Only use lat, lon, and alt
                lat_int = (int)(lat * 1e7), // Convert to integer format
                lon_int = (int)(lon * 1e7),
                alt = alt,
                yaw = 0, // Maintain current heading
                yaw_rate = 0
            };

            // Generate the MAVLink packet
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLINK_MSG_ID.SET_POSITION_TARGET_GLOBAL_INT, msg);

            // Send the packet to the drone
            SendMessage(packet);

            // wait 2 seconds for the drone to start moving
            Thread.Sleep(2000);

            // Wait here for the message indicating it has reached the destination
            // I will know this because the velocity will be zero
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            MAVLink.MAVLinkMessage message = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: CheckStopped, // Assuming ComprobarParado checks if speed is zero
                timeout: -1
            );

            if (f != null)
                f(this.id, param);
        }

        public void GoToWaypoint(float lat, float lon, float alt, Boolean blocking = true, Action<byte, object> f = null, object param = null)
        {
            if (blocking)
            {
                this._GoToWaypoint(lat, lon, alt);
            }
            else
            {
                Thread t = new Thread(() => _GoToWaypoint(lat, lon, alt, f, param));
                t.Start();
            }
        }
    }
}