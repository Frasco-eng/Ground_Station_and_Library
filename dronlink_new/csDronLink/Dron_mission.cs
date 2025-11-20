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
        // Mission
        public void UploadMission(List<((float lat, float lon), float alt)> mission)
        {
            // First, clear the autopilot's current mission
            // Send command to clear all missions from the autopilot
            MAVLink.mavlink_mission_clear_all_t clearMission = new MAVLink.mavlink_mission_clear_all_t
            {
                target_system = this.id,
                target_component = 1
            };
            // NOTE: The message ID for clearing is MISSION_CLEAR_ALL, not MISSION_COUNT.
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_CLEAR_ALL, clearMission);
            SendMessage(packet);

            // FOR SOME REASON that I don't control, I have to send the first waypoint of the mission twice
            ((float lat, float lon), float alt) first = mission[0];
            List<((float lat, float lon), float alt)> wpsToUpload = new List<((float lat, float lon), float alt)>(mission);
            wpsToUpload.Insert(0, first);

            // List with the MAVLink commands
            List<MAVLink.mavlink_mission_item_int_t> wploader = new List<MAVLink.mavlink_mission_item_int_t>();
            int seq = 0;
            foreach (var wp in wpsToUpload)
            {
                wploader.Add(new MAVLink.mavlink_mission_item_int_t()
                {
                    target_system = this.id,
                    target_component = 1,
                    seq = (ushort)seq,
                    frame = (byte)MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT,
                    command = (ushort)MAVLink.MAV_CMD.WAYPOINT,
                    current = (byte)(seq == 0 ? 1 : 0), // The first waypoint is the current one
                    autocontinue = 1,
                    //param1 = mission.Count,
                    x = (int)(wp.Item1.lat * 1e7),  // Latitude in integer format (deg * 10^7)
                    y = (int)(wp.Item1.lon * 1e7),  // Longitude in integer format (deg * 10^7)
                    z = wp.alt, // Altitude in meters (float). NOTE: mavlink_mission_item_int_t uses float for z, but the source was casting to int. Keeping the source type 'int' for the field 'z' here, assuming 'wp.alt' is a float that needs to be cast.
                    mission_type = (byte)MAVLink.MAV_MISSION_TYPE.MISSION
                });
                seq++;
            }

            // Send the total number of waypoints
            var countMsg = new MAVLink.mavlink_mission_count_t
            {
                target_system = this.id,
                target_component = 1,
                count = (ushort)wploader.Count, // Use wploader.Count since it contains the duplicated first point
                mission_type = (byte)MAVLink.MAV_MISSION_TYPE.MISSION
            };

            packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_COUNT, countMsg);
            SendMessage(packet);
            string msgType;

            // Now wait for the autopilot to request waypoints one by one
            while (true)
            {
                msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST).ToString();
                MAVLink.MAVLinkMessage request = messageHandler.WaitForMessageBlock(
                    msgType,
                    timeout: -1
                );
                int next = ((MAVLink.mavlink_mission_request_t)request.data).seq;

                // Send the requested waypoint
                MAVLink.mavlink_mission_item_int_t msg1 = wploader[next];
                packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT, msg1);
                SendMessage(packet);

                if (next == wploader.Count - 1) break; // All waypoints have been sent
            }

            // Wait for the final confirmation
            msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ACK).ToString();
            MAVLink.MAVLinkMessage response2 = messageHandler.WaitForMessageBlock(
                msgType,
                timeout: -1
            );
            // NOTE: response2.data should be checked for the acceptance status (MAV_RESULT.ACCEPTED)
        }

        private void _ExecuteMissionInternal(Action<byte, object> OnWaypointReached = null, Action<byte, object> f = null, object param = null)
        {
            // Send request for the number of waypoints
            MAVLink.mavlink_mission_request_list_t requestList = new MAVLink.mavlink_mission_request_list_t
            {
                target_system = this.id,
                target_component = 1
            };
            // Send the command to the autopilot
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST_LIST, requestList);
            SendMessage(packet);

            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_COUNT).ToString();

            var message = messageHandler.WaitForMessageBlock(
                msgType, // Expecting MISSION_COUNT
                timeout: -1 // indefinite time
            );
            // Retrieve the number of waypoints in the mission
            int numWaypoints = ((MAVLink.mavlink_mission_count_t)message.data).count;

            // Issue the command to start the mission
            MAVLink.mavlink_command_long_t cmd = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.MISSION_START,
                confirmation = 0,
            };

            // Send the command to the autopilot
            packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd);
            SendMessage(packet);

            // Now wait for each waypoint to be reached
            // Loop from 1 to numWaypoints - 1, skipping the first (index 0) which is usually the takeoff or home point
            for (int i = 1; i < numWaypoints; i++)
            {
                msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_REACHED).ToString();
                message = messageHandler.WaitForMessageBlock(
                    msgType,
                    condition: CheckAtWaypoint, // Assuming 'ComprobarEnWaypoint' is a local condition checking function
                    parameters: i,
                    timeout: -1
                );
                // Call the requested function, passing the index of the reached waypoint
                if (OnWaypointReached != null)
                    OnWaypointReached(this.id, i);
            }

            // Assuming PonModoGuiado is defined elsewhere and sets Guided mode
            // This is typically done after a mission finishes so the drone doesn't loiter indefinitely.
            SetGuidedMode();

            if (f != null)
                f(this.id, param);
        }

        public void ExecuteMission(bool blocking = true, Action<byte, object> OnWaypointReached = null, Action<byte, object> f = null, object param = null)
        {
            // OnWaypointReached is the function that will be activated each time a waypoint is reached,
            // passing the index of the reached waypoint as a parameter.
            if (blocking)
            {
                this._ExecuteMissionInternal(OnWaypointReached, f, param);
            }
            else
            {
                if (this.t != null)
                {
                    this.t.Abort();
                    messageHandler.Clear();
                }
                this.t = new Thread(() => _ExecuteMissionInternal(OnWaypointReached, f, param));
                this.t.Start();
            }
        }

        
        public void CancelMission()
        {
            // 1. Clear all missions
            MAVLink.mavlink_mission_clear_all_t clearAll = new MAVLink.mavlink_mission_clear_all_t
            {
                target_system = this.id,
                target_component = 1
            };
            SendMessage(mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_CLEAR_ALL, clearAll));

            // 2. Wait for MISSION_ACK to confirm clear
            var ack = messageHandler.WaitForMessageBlock(
                ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ACK).ToString(),
                timeout: 2000
            );

            // 3. Force reset of mission index
            MAVLink.mavlink_mission_set_current_t setCurrent = new MAVLink.mavlink_mission_set_current_t
            {
                target_system = this.id,
                target_component = 1,
                seq = 0
            };
            SendMessage(mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_SET_CURRENT, setCurrent));

            // 4. (Opzionale) Imposta modalità GUIDED o HOLD per uscire da AUTO
            SetGuidedMode();
        }


    }
}
