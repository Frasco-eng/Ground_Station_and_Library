using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace csDronLink
{
    public partial class Dron
    {
        // Miscellaneous
        public void SetGuidedMode()
        {
            // First, set the drone to GUIDED mode
            MAVLink.mavlink_set_mode_t setMode = new MAVLink.mavlink_set_mode_t
            {
                target_system = this.id,
                base_mode = (byte)MAVLink.MAV_MODE_FLAG.CUSTOM_MODE_ENABLED,
                custom_mode = 4 // GUIDED Mode in ArduPilot
            };

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_MODE, setMode);
            SendMessage(packet);
        }
        public void ChangeSpeed(int speed)
        {
            this.speed = speed; // Assuming 'speed' is a field in the Dron class to store the desired speed
            // Create the COMMAND_LONG message
            MAVLink.mavlink_command_long_t speedCommand = new MAVLink.mavlink_command_long_t();
            speedCommand.target_system = this.id;     // System ID (drone)
            speedCommand.target_component = 1; // Component ID
            speedCommand.command = (ushort)MAVLink.MAV_CMD.DO_CHANGE_SPEED;
            speedCommand.param1 = 1;   // Speed Type (1 = Airspeed, 0 = Groundspeed, 2 = Vertical speed)
            speedCommand.param2 = speed;     // Speed in m/s
            speedCommand.param3 = -1;  // No specific acceleration (-1 uses default)
            speedCommand.param4 = 0;   // Not used
            speedCommand.param5 = 0;   // Not used
            speedCommand.param6 = 0;   // Not used
            speedCommand.param7 = 0;   // Not used

            // Generate the MAVLink packet
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, speedCommand);

            // Send the packet to the drone
            SendMessage(packet);

        }
        public void _ChangeHeadingInternal(float newHeading, Action<byte, object> f = null, object param = null)
        {
            // stop the navigation loop, if it is active
            this.navigating = false; // Assuming 'navigating' is a flag in the Dron class
            var req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,        // Drone ID
                target_component = 1, // Component ID (usually autopilot)
                command = (ushort)MAVLink.MAV_CMD.CONDITION_YAW,
                param1 = newHeading,    // Heading angle (in degrees)
                param2 = 30,      // Rotation speed (degrees per second)
                param3 = 1,              // Clockwise direction (1 = Clockwise, -1 = Counter-clockwise)
                param4 = 0,      // Absolute heading (0 = absolute, 1 = relative) **Note: This should likely be 0 for absolute or 1 for relative yaw**
                param5 = 0,              // Not used
                param6 = 0,              // Not used
                param7 = 0     // **Note: MAV_FRAME or similar for heading reference is usually here in MAVLink2, but COMMAND_LONG uses parameters 1-7. For CONDITION_YAW, param4 usually determines absolute/relative, but it is often misused/ignored depending on firmware.**
            };
            // Generate the MAVLink packet
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);

            // Send the packet to the drone
            SendMessage(packet);

            // Wait for the operation to complete
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            var message = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: CheckOrientation, // Assuming 'ComprobarOrientacion' is a local condition checking function
                parameters: newHeading,
                timeout: -1
            );

            if (f != null)
                f(this.id, param);
        }

        public void ChangeHeading(float newHeading, bool blocking = true, Action<byte, object> f = null, object param = null)
        {
            if (blocking)
            {
                this._ChangeHeadingInternal(newHeading);
            }
            else
            {
                Thread t = new Thread(() => _ChangeHeadingInternal(newHeading, f, param));
                t.Start();
            }
        }

    }
}