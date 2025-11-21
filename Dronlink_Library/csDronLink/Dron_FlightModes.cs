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
        private static uint MapModeToCode(string mode)
        {
            if (mode == null) return 4u; // GUIDED default
                                         // normalize for safety
            var m = mode.Trim().ToUpperInvariant();

            switch (m)
            {
                case "STABILIZE": return 0u;
                case "ALT_HOLD": return 2u;
                case "AUTO": return 3u;
                case "GUIDED": return 4u;
                case "LOITER": return 5u;
                case "RTL": return 6u;
                case "LAND": return 9u;
                case "BRAKE": return 17u;
                default: return 4u; // GUIDED
            }
        }

        public void SetFlightMode(string mode, byte vehicleSysId = 1, byte gcsSysId = 255)
        {
            uint code = MapModeToCode(mode);

            var msg = new MAVLink.mavlink_set_mode_t
            {
                target_system = vehicleSysId,
                base_mode = (byte)MAVLink.MAV_MODE_FLAG.CUSTOM_MODE_ENABLED,
                custom_mode = code
            };

            // IMPORTANT: gcsSysId is the sysid of the SENDER (GCS), NOT the vehicle's
            byte[] packet = mavlink.GenerateMAVLinkPacket10(
                MAVLINK_MSG_ID.SET_MODE,
                msg,
                gcsSysId,
                (byte)MAV_COMPONENT.MAV_COMP_ID_AUTOPILOT1
            );

            SendMessage(packet);
        }

    }
}
