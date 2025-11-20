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
        private const byte GCS_SYSID = 255;    // sender sysid (GCS)
        private const byte GCS_COMPID = 190;   // sender compid (MAV_COMP_ID_MISSIONPLANNER=190)

        private void _MoveIndoorInternal(float x, float y, float z, bool heading = true, Action<byte, object> f = null, object param = null)
        {
            Console.WriteLine($"[MoveIndoor] x={x} y={y} z={z} heading={heading}");

            // 1) Get Heartbeat for vehicle's actual sys/comp id
            string HB = ((int)MAVLink.MAVLINK_MSG_ID.HEARTBEAT).ToString();
            var hb = messageHandler.WaitForMessageBlock(HB, null, null, 1000);
            if (hb == null) { Console.WriteLine("[MoveIndoor] No HEARTBEAT received."); return; }
            byte vehicleSysId = hb.sysid;

            // 2) Get initial position (also serves to verify EKF status)
            string LPN = ((int)MAVLink.MAVLINK_MSG_ID.LOCAL_POSITION_NED).ToString();
            var startMsg = messageHandler.WaitForMessageBlock(LPN, null, null, 1500);
            if (startMsg == null) { Console.WriteLine("[MoveIndoor] No LOCAL_POSITION_NED."); return; }
            var start = (MAVLink.mavlink_local_position_ned_t)startMsg.data;

            // 3) Mask for "position only"
            MAVLink.POSITION_TARGET_TYPEMASK posMask =
                  MAVLink.POSITION_TARGET_TYPEMASK.VX_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.VY_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.VZ_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.AX_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.AY_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.AZ_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.YAW_RATE_IGNORE;
            if (heading) posMask |= MAVLink.POSITION_TARGET_TYPEMASK.YAW_IGNORE;

            // 4) Command: OFFSET in LOCAL_OFFSET_NED (z up in ENU => -z in NED)
            var cmd = new MAVLink.mavlink_set_position_target_local_ned_t()
            {
                time_boot_ms = (uint)Environment.TickCount,
                target_system = vehicleSysId,
                target_component = (byte)MAV_COMPONENT.MAV_COMP_ID_AUTOPILOT1,
                coordinate_frame = (byte)MAVLink.MAV_FRAME.LOCAL_OFFSET_NED,
                type_mask = (ushort)posMask,
                x = x,
                y = y,
                z = -z, // Z-axis is typically Down in NED, so we negate the Z-up input.
                vx = 0,
                vy = 0,
                vz = 0,
                afx = 0,
                afy = 0,
                afz = 0,
                yaw = 0,
                yaw_rate = 0
            };

            // Header = GCS
            byte[] pkt = mavlink.GenerateMAVLinkPacket10(
                MAVLINK_MSG_ID.SET_POSITION_TARGET_LOCAL_NED, cmd, GCS_SYSID, GCS_COMPID);
            // 

            // 5) Burst 10 Hz × 2 s
            for (int i = 0; i < 3; i++) { SendMessage(pkt); Thread.Sleep(50); }

            // 6) Wait for convergence within tolerance (10 cm or 10% of offset)
            float tx = x, ty = y, tz = -z;
            float normT = (float)Math.Sqrt(tx * tx + ty * ty + tz * tz);
            float tol = Math.Max(0.10f, 0.10f * normT);
            DateTime waitEnd = DateTime.UtcNow.AddSeconds(3);

            while (DateTime.UtcNow < waitEnd)
            {
                var m = messageHandler.WaitForMessageBlock(LPN, null, null, 200);
                if (m == null) continue;
                var p = (MAVLink.mavlink_local_position_ned_t)m.data;

                float dx = p.x - start.x, dy = p.y - start.y, dz = p.z - start.z;
                float err2 = (dx - tx) * (dx - tx) + (dy - ty) * (dy - ty) + (dz - tz) * (dz - tz);
                if (err2 <= tol * tol)
                {
                    Console.WriteLine($"[MoveIndoor] OK: Δx={dx:F2} Δy={dy:F2} Δz={dz:F2}");
                    f?.Invoke(vehicleSysId, param);
                    return;
                }
            }

            Console.WriteLine("[MoveIndoor] Target reach not confirmed (tolerance).");
            f?.Invoke(vehicleSysId, param);
        }



        public void MoveIndoor(float x, float y, float z, bool heading = true, bool blocking = true, Action<byte, object> f = null, object param = null)
        {
            if (blocking)
            {
                this._MoveIndoorInternal(x, y, z, heading, f, param);
            }
            else
            {
                Thread t = new Thread(() => _MoveIndoorInternal(x, y, z, heading, f, param));
                t.Start();
            }
        }

        private void _MoveIndoorInternal_Abs(float x, float y, float z, float vx, float vy, float vz, float ax, float ay, float az, bool mask = true, bool heading = true, Action<byte, object> f = null, object param = null)
        {
            Console.WriteLine($"[MoveIndoor] x={x} y={y} z={z} heading={heading}");

            // 1) Get Heartbeat for vehicle's actual sys/comp id
            string HB = ((int)MAVLink.MAVLINK_MSG_ID.HEARTBEAT).ToString();
            var hb = messageHandler.WaitForMessageBlock(HB, null, null, 1000);
            if (hb == null) { Console.WriteLine("[MoveIndoor] No HEARTBEAT received."); return; }
            byte vehicleSysId = hb.sysid;

            // 2) Get initial position (also serves to verify EKF status)
            string LPN = ((int)MAVLink.MAVLINK_MSG_ID.LOCAL_POSITION_NED).ToString();
            var startMsg = messageHandler.WaitForMessageBlock(LPN, null, null, 1500);
            if (startMsg == null) { Console.WriteLine("[MoveIndoor] No LOCAL_POSITION_NED."); return; }
            var start = (MAVLink.mavlink_local_position_ned_t)startMsg.data;


            MAVLink.POSITION_TARGET_TYPEMASK posMask = 0;
            // 3) Mask for "position only"
            if (mask)
            {
                posMask =
                  MAVLink.POSITION_TARGET_TYPEMASK.VX_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.VY_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.VZ_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.AX_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.AY_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.AZ_IGNORE
                | MAVLink.POSITION_TARGET_TYPEMASK.YAW_RATE_IGNORE;
            }

            if (heading) posMask |= MAVLink.POSITION_TARGET_TYPEMASK.YAW_IGNORE;

            // 4) Command: OFFSET in LOCAL_OFFSET_NED (z up in ENU => -z in NED)
            var cmd = new MAVLink.mavlink_set_position_target_local_ned_t()
            {
                time_boot_ms = (uint)Environment.TickCount,
                target_system = vehicleSysId,
                target_component = (byte)MAV_COMPONENT.MAV_COMP_ID_AUTOPILOT1,
                coordinate_frame = (byte)MAVLink.MAV_FRAME.LOCAL_NED,
                type_mask = (ushort)posMask,
                x = x,
                y = y,
                z = -z, // Z-axis is typically Down in NED, so we negate the Z-up input.
                vx = 0,
                vy = 0,
                vz = 0,
                afx = 0,
                afy = 0,
                afz = 0,
                yaw = 0,
                yaw_rate = 0
            };

            // Header = GCS
            byte[] pkt = mavlink.GenerateMAVLinkPacket10(
                MAVLINK_MSG_ID.SET_POSITION_TARGET_LOCAL_NED, cmd, GCS_SYSID, GCS_COMPID);
            // 

            // 5) Burst 10 Hz × 2 s
            for (int i = 0; i < 3; i++) { SendMessage(pkt); Thread.Sleep(50); }

            // 6) Wait for convergence within tolerance (10 cm or 10% of offset)
            float tx = x, ty = y, tz = -z;
            float normT = (float)Math.Sqrt(tx * tx + ty * ty + tz * tz);
            float tol = Math.Max(0.10f, 0.10f * normT);
            DateTime waitEnd = DateTime.UtcNow.AddSeconds(3);

            while (DateTime.UtcNow < waitEnd)
            {
                var m = messageHandler.WaitForMessageBlock(LPN, null, null, 200);
                if (m == null) continue;
                var p = (MAVLink.mavlink_local_position_ned_t)m.data;

                float dx = p.x - start.x, dy = p.y - start.y, dz = p.z - start.z;
                float err2 = (dx - tx) * (dx - tx) + (dy - ty) * (dy - ty) + (dz - tz) * (dz - tz);
                if (err2 <= tol * tol)
                {
                    Console.WriteLine($"[MoveIndoor] OK: Δx={dx:F2} Δy={dy:F2} Δz={dz:F2}");
                    f?.Invoke(vehicleSysId, param);
                    return;
                }
            }

            Console.WriteLine("[MoveIndoor] Target reach not confirmed (tolerance).");
            f?.Invoke(vehicleSysId, param);
        }



        public void MoveIndoor_Abs(float x, float y, float z, float vx = 0, float vy = 0, float vz = 0, float ax = 0, float ay = 0, float az = 0, bool mask = true, bool heading = true, bool blocking = true, Action<byte, object> f = null, object param = null)
        {
            if (blocking)
            {
                this._MoveIndoorInternal_Abs(x, y, z, vx, vy, vz, ax, ay, az, mask, heading, f, param);
            }
            else
            {
                Thread t = new Thread(() => _MoveIndoorInternal_Abs(x, y, z, vx, vy, vz, ax, ay, ax, mask, heading, f, param));
                t.Start();
            }

        }
    }
}
