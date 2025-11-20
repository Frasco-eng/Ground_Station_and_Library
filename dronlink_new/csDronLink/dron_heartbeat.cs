using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MAVLink;

namespace csDronLink
{
    public partial class Dron
    {
        // Start this timer at the beginning of the application (after opening the link)
        private System.Timers.Timer _hbTimer;

        public void StartHeartbeatLoop(byte gcsSysId = 255)
        {
            // 1 Hz timer
            _hbTimer = new System.Timers.Timer(1000);
            _hbTimer.AutoReset = true;
            _hbTimer.Elapsed += (s, e) => SendGcsHeartbeat(gcsSysId);
            _hbTimer.Start();
        }

        public void StopHeartbeatLoop()
        {
            _hbTimer?.Stop();
            _hbTimer?.Dispose();
            _hbTimer = null;
        }

        public void SendGcsHeartbeat(byte gcsSysId = 255)
        {
            var hb = new MAVLink.mavlink_heartbeat_t
            {
                // enums not present in your dll -> using numerical values
                type = (byte)6,        // MAV_TYPE_GCS
                autopilot = (byte)8,   // MAV_AUTOPILOT_INVALID
                base_mode = 0,
                system_status = (byte)4, // MAV_STATE_ACTIVE
                mavlink_version = 3
            };

            // ATTENTION: gcsSysId is the **sender** (your GCS), not the drone
            var pkt = mavlink.GenerateMAVLinkPacket10(
                MAVLINK_MSG_ID.HEARTBEAT,
                hb,
                gcsSysId,
                (byte)190 // MAV_COMP_ID_MISSIONPLANNER
            );

            SendMessage(pkt);
        }

    }
}
