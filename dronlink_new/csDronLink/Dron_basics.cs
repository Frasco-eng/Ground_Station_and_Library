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
        // Takeoff, landing, and RTL
        private void _Takeoff(int altitude, Action<byte, object> f = null, object param = null)
        {
            // First, we put the drone in GUIDED mode
            SetGuidedMode();

            // Then we arm
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();
            req.target_system = this.id;
            req.target_component = 1;
            req.command = (ushort)MAVLink.MAV_CMD.COMPONENT_ARM_DISARM;
            req.param1 = 1;
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            SendMessage(packet);

            // And now we takeoff
            req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.TAKEOFF,
                param7 = altitude // Desired altitude in meters
            };

            // Generate MAVLink packet
            packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            // Send command to the drone
            SendMessage(packet);

            // Here I wait for the message that indicates it has reached the takeoff altitude
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            MAVLink.MAVLinkMessage message = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: CheckInAir,
                parameters: altitude,
                timeout: -1
            );
            if (f != null)
                f(this.id, param);

        }
        public void Takeoff(int altitude, Boolean block = true, Action<byte, object> f = null, object param = null)
        {
            // If the call is blocking, I call the function
            if (block)
            {
                this._Takeoff(altitude);
            }
            // If it is not blocking, I start a thread to handle it and not block the client
            else
            {
                Thread t = new Thread(() => _Takeoff(altitude, f, param));
                t.Start();
            }

        }
        public void _RTL(Action<byte, object> f = null, object param = null)
        {
            // stop the navigation loop if it is active
            this.navigating = false;

            MAVLink.mavlink_set_mode_t setMode = new MAVLink.mavlink_set_mode_t
            {
                target_system = this.id,
                base_mode = (byte)MAVLink.MAV_MODE_FLAG.CUSTOM_MODE_ENABLED,
                custom_mode = 6 // RTL Mode in ArduPilot
            };

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_MODE, setMode);
            // Send command to the drone
            //serialPort1.Write(packet, 0, packet.Length);
            SendMessage(packet);

            // We request a telemetry message that indicates it is already on the ground
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            var message = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: CheckOnGround,
                timeout: -1
            );

            if (f != null)
                f(this.id, param);

        }
        public void _Land(Action<byte, object> f = null, object param = null)
        {
            // stop the navigation loop if it is active
            this.navigating = false;

            // Create the packet for the landing command
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,  // System ID (1 is the main system)
                target_component = 1,  // Component ID (1 is the autopilot)
                command = (ushort)MAVLink.MAV_CMD.LAND,  // Landing command
                param1 = 0,  // Not used (0 is default value)
                param2 = 0,  // Not used (0 is default value)
                param3 = 0,  // Not used (0 is default value)
                param4 = 0,  // Not used (0 is default value)
                param5 = 0,  // Not used (0 is default value)
                param6 = 0,  // Not used (0 is default value)
                param7 = 0   // Not used (0 is default value)
            };

            // Generate the MAVLink packet for the landing command
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            // Send command to the drone
            //serialPort1.Write(packet, 0, packet.Length);
            SendMessage(packet);

            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            var message = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: CheckOnGround,
                   timeout: -1
            );


            if (f != null)
                f(this.id, param);

        }
        public void RTL(Boolean block = true, Action<byte, object> f = null, object param = null)
        {
            if (block)
            {
                this._RTL();
            }
            else
            {
                Thread t = new Thread(() => _RTL(f, param));
                t.Start();
            }
        }
        public void Land(Boolean block = true, Action<byte, object> f = null, object param = null)
        {
            if (block)
            {
                this._Land();
            }
            else
            {
                Thread t = new Thread(() => _Land(f, param));
                t.Start();
            }
        }

        public void Disarm()
        {
            // MAV_CMD.COMPONENT_ARM_DISARM with param1 = 0 (Disarm)
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();
            req.target_system = this.id;
            req.target_component = 1;
            req.command = (ushort)MAVLink.MAV_CMD.COMPONENT_ARM_DISARM;
            req.param1 = 0; // 0 for Disarm
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            SendMessage(packet);
        }

        public void Arm()
        {
            // MAV_CMD.COMPONENT_ARM_DISARM with param1 = 1 (Arm)
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();
            req.target_system = this.id;
            req.target_component = 1;
            req.command = (ushort)MAVLink.MAV_CMD.COMPONENT_ARM_DISARM;
            req.param1 = 1; // 1 for Arm
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            SendMessage(packet);
        }

        public void Send_RC_Override(List<ushort> channels)
        {
            MAVLink.mavlink_rc_channels_override_t rcOverride = new MAVLink.mavlink_rc_channels_override_t
            {
                target_system = this.id,
                target_component = 1, // Autopilot component
                chan1_raw = channels[0],     // Roll
                chan2_raw = channels[1],     // Pitch
                chan3_raw = channels[2],     // Throttle
                chan4_raw = channels[3],     // Yaw
                chan5_raw = 0,               // Use default/release control (original had '0')
                chan6_raw = 65535,           // Ignore
                chan7_raw = 65535,           // Ignore
                chan8_raw = 65535            // Ignore
            };
            // 65535 means 'no change' or 'ignore' for the channel.
            // Generate the MAVLink packet
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.RC_CHANNELS_OVERRIDE, rcOverride, GCS_SYSID, GCS_COMPID);
            // Send the packet to the drone
            SendMessage(packet);
        }

    }
}