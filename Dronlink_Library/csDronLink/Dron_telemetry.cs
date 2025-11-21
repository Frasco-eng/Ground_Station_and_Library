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
        // Telemetry

        /// <summary>
        /// Registers a callback function to be executed every time a new telemetry message is received and processed.
        /// </summary>
        /// <param name="f">The function to execute: Action<System ID, Message ID, List<(string name, float value)>> </param>
        /// 
        public void RegisterTelemetry(MAVLinkMessage msg)
        {
            switch ((MAVLINK_MSG_ID)msg.msgid)
            {
                case MAVLINK_MSG_ID.GLOBAL_POSITION_INT:
                    Telemetry_Outdoor(msg);
                    break;

                case MAVLINK_MSG_ID.LOCAL_POSITION_NED:
                    Telemetry_Indoor(msg);
                    break;

                case MAVLINK_MSG_ID.ATTITUDE:
                    Telemetry_Attitude(msg);
                    break;

                case MAVLINK_MSG_ID.BATTERY_STATUS:
                    Telemetry_Battery(msg);
                    break;

                case MAVLINK_MSG_ID.SCALED_IMU:
                    Telemetry_IMU(msg);
                    break;

                case MAVLINK_MSG_ID.HEARTBEAT:
                    Telemetry_Heartbeat(msg);
                    break;

                default:
                    // Unhandled message type
                    break;
            }
        }

        private void Telemetry_RequestList()
        {
            // I make an asynchronous request to the handler to send me all messages
            // of the indicated type, which are the ones containing the telemetry data that interests me
            // I indicate that when a message of that type arrives, execute the RegisterTelemetry function

            // =========== GLOBAL POSITION =============
            string msgType_global = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            messageHandler.RegisterHandler(msgType_global, RegisterTelemetry);

            // Now I ask the autopilot to send me messages of the indicated type (those containing
            // the telemetry data, every 2 seconds)
            MAVLink.mavlink_command_long_t req_global = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT, // ID of the message we want to receive
                param2 = 100000, // Interval in microseconds (1 Hz = 1,000,000 µs)
            };

            byte[] packet_global = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req_global);
            SendMessage(packet_global);

            // =========== LOCAL POSITION =============
            string msgType_local = ((int)MAVLink.MAVLINK_MSG_ID.LOCAL_POSITION_NED).ToString();
            messageHandler.RegisterHandler(msgType_local, RegisterTelemetry);

           
            MAVLink.mavlink_command_long_t req_local = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.LOCAL_POSITION_NED, 
                param2 = 100000, 
            };

            byte[] packet_local = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req_local);
            SendMessage(packet_local);

            // =========== ATTITUDE =============
            string msgType_attitude = ((int)MAVLink.MAVLINK_MSG_ID.ATTITUDE).ToString();
            messageHandler.RegisterHandler(msgType_attitude, RegisterTelemetry);

            
            MAVLink.mavlink_command_long_t req_attitude = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.ATTITUDE, 
                param2 = 1000000, 
            };

            byte[] packet_attitude = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req_attitude);
            SendMessage(packet_attitude);

            // =========== BATTERY STATUS =============
            string msgType_battery = ((int)MAVLink.MAVLINK_MSG_ID.BATTERY_STATUS).ToString();
            messageHandler.RegisterHandler(msgType_battery, RegisterTelemetry);

            
            MAVLink.mavlink_command_long_t req_battery = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.BATTERY_STATUS, 
                param2 = 2000000, 
            };

            byte[] packet_battery = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req_battery);
            SendMessage(packet_battery);

            // =========== SCALED IMU =============
            string msgType_IMU = ((int)MAVLink.MAVLINK_MSG_ID.SCALED_IMU).ToString();
            messageHandler.RegisterHandler(msgType_IMU, RegisterTelemetry);

            
            MAVLink.mavlink_command_long_t req_IMU = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.SCALED_IMU, 
                param2 = 1000000, 
            };

            byte[] packet_IMU = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req_IMU);
            SendMessage(packet_IMU);

            // =========== HEARTBEAT =============
            string msgType_heartbeat = ((int)MAVLink.MAVLINK_MSG_ID.HEARTBEAT).ToString();
            messageHandler.RegisterHandler(msgType_heartbeat, RegisterTelemetry);

            
            MAVLink.mavlink_command_long_t req_heartbeat = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.HEARTBEAT, 
                param2 = 500000, 
            };

            byte[] packet_heartbeat = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req_heartbeat);
            SendMessage(packet_heartbeat);
        }

        public void SendTelemetryDataToClient(Action<byte, uint, List<(string name, float value)>> f)
        {
            // The client asks me to execute function 'f' every time a message
            // with telemetry data is received.
            this.ProcessTelemetry = f; // Assuming ProcesarTelemetria is a delegate/Action field in the Dron class
        }

        /// <summary>
        /// Stops sending telemetry data to the client by clearing the callback function.
        /// </summary>
        public void StopTelemetryData()
        {
            // The client no longer wants telemetry data.
            this.ProcessTelemetry = null;
        }

        public void Telemetry_Indoor(MAVLinkMessage msg)
        {
            var pos = (MAVLink.mavlink_local_position_ned_t)msg.data;

            // NAME COMPATIBILITY: use simple and consistent keys (NED frame)
            var data = new List<(string name, float value)>
            {
                ("x", pos.x),
                ("y", pos.y),
                ("z", pos.z),
                ("vx", pos.vx),
                ("vy", pos.vy),
                ("vz", pos.vz)
            };

            this.ProcessTelemetry?.Invoke(this.id, msg.msgid, data);
        }

        public void Telemetry_Outdoor(MAVLinkMessage msg)
        {
            var pos = (MAVLink.mavlink_global_position_int_t)msg.data;
            // NAME COMPATIBILITY: use simple and consistent keys (WGS84)
            var data = new List<(string name, float value)>
            {
                ("lat", pos.lat / 1e7f), // deg * 10^7 -> deg
                ("lon", pos.lon / 1e7f), // deg * 10^7 -> deg
                ("alt", pos.alt / 1000f),    // alt in mm -> m
                ("relative_alt", pos.relative_alt / 1000f), // relative_alt in mm -> m
                ("vx", pos.vx / 100f),       // vx in cm/s -> m/s
                ("vy", pos.vy / 100f),       // vy in cm/s -> m/s
                ("vz", pos.vz / 100f),       // vz in cm/s -> m/s
                ("hdg", pos.hdg / 100f)      // hdg in cdeg -> deg
            };
            this.ProcessTelemetry?.Invoke(this.id, msg.msgid, data);
        }

        public void Telemetry_Attitude(MAVLinkMessage msg)
        {
            var att = (MAVLink.mavlink_attitude_t)msg.data;
            // NAME COMPATIBILITY: use simple and consistent keys
            var data = new List<(string name, float value)>
            {
                ("time_boot_ms", att.time_boot_ms), // time since boot in ms
                ("roll", att.roll),       // roll in rad
                ("pitch", att.pitch),     // pitch in rad
                ("yaw", att.yaw),         // yaw in rad
                ("rollspeed", att.rollspeed),   // roll speed in rad/s
                ("pitchspeed", att.pitchspeed), // pitch speed in rad/s
                ("yawspeed", att.yawspeed)      // yaw speed in rad/s
            };
            this.ProcessTelemetry?.Invoke(this.id, msg.msgid, data);
        }

        private void Telemetry_Battery(MAVLinkMessage msg)
        {
            var bat = (MAVLink.mavlink_battery_status_t)msg.data;
            // NAME COMPATIBILITY: use simple and consistent keys
            var data = new List<(string name, float value)>
            {
                ("current_battery", bat.current_battery / 100f), // current in cA -> A
                ("battery_remaining", bat.battery_remaining) // remaining in %
            };

            for (int i = 0; i < bat.voltages.Length; i++)
            {
                data.Add(($"voltage_cell_{i + 1}", bat.voltages[i] / 1000f)); // voltage in mV -> V
            }

            this.ProcessTelemetry?.Invoke(this.id, msg.msgid, data);
        }

        private void Telemetry_Heartbeat(MAVLinkMessage msg)
        {
            var hb = (MAVLink.mavlink_heartbeat_t)msg.data;
            // NAME COMPATIBILITY: use simple and consistent keys
            var data = new List<(string name, float value)>
            {
                ("custom_mode", hb.custom_mode),
                ("type", hb.type),
                ("autopilot", hb.autopilot),
                ("base_mode", hb.base_mode),
                ("system_status", hb.system_status),
                ("mavlink_version", hb.mavlink_version)
            };
            this.ProcessTelemetry?.Invoke(this.id, msg.msgid, data);
        }

        private void Telemetry_IMU(MAVLinkMessage msg)
        {
            var imu = (MAVLink.mavlink_scaled_imu_t)msg.data;
            // NAME COMPATIBILITY: use simple and consistent keys
            var data = new List<(string name, float value)>
            {
                ("xacc", imu.xacc / 1000f), // acceleration in mg -> g
                ("yacc", imu.yacc / 1000f), // acceleration in mg -> g
                ("zacc", imu.zacc / 1000f), // acceleration in mg -> g
                ("xgyro", imu.xgyro / 1000f), // angular speed in millirad/s -> rad/s
                ("ygyro", imu.ygyro / 1000f), // angular speed in millirad/s -> rad/s
                ("zgyro", imu.zgyro / 1000f), // angular speed in millirad/s -> rad/s
                ("xmag", imu.xmag), // magnetic field in mGa
                ("ymag", imu.ymag), // magnetic field in mGa
                ("zmag", imu.zmag)  // magnetic field in mGa
            };
            this.ProcessTelemetry?.Invoke(this.id, msg.msgid, data);
        }
    }
}
