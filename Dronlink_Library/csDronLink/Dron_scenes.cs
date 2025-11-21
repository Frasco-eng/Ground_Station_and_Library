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
        // Scenario
        public void SetScenario(List<List<(float lat, float lon)>> scenario)
        {
            /* A scenario is a list of fences. The first one is always the inclusion fence that defines
             * the limits of the flight area. The remaining ones are exclusion fences, which represent obstacles
             * in the flight area.
             * Each fence is a list of PointLatLng.
             * If the list has 3 or more PointLatLng, it's a polygon type fence. If it only has two, then
             * it is a circle type fence. In that case, the first PointLatLng is the center of the circle and the latitude
             * of the second PointLatLng is actually the radius of the circle.
             * The first fence (the inclusion one) must be of polygon type
             * */

            // Take the inclusion fence
            List<(float lat, float lon)> waypoints = scenario[0];
            // In this list, I will prepare the commands to set the waypoints
            List<MAVLink.mavlink_mission_item_int_t> wploader = new List<MAVLink.mavlink_mission_item_int_t>();
            int seq = 0;

            // Prepare the commands
            foreach (var wp in waypoints)
            {
                wploader.Add(new MAVLink.mavlink_mission_item_int_t()
                {
                    target_system = this.id,
                    target_component = 1,
                    seq = (ushort)seq,
                    frame = (byte)MAVLink.MAV_FRAME.GLOBAL,
                    command = (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_INCLUSION,
                    param1 = waypoints.Count,
                    x = (int)(wp.lat * 1e7),
                    y = (int)(wp.lon * 1e7),
                    mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                });
                seq++;
            }


            // now prepare the obstacles
            for (int i = 1; i < scenario.Count; i++)
            {
                waypoints = scenario[i];
                if (waypoints.Count == 2)
                // it's a circle
                {
                    wploader.Add(new MAVLink.mavlink_mission_item_int_t()
                    {
                        target_system = this.id,
                        target_component = 1,
                        seq = (ushort)seq,
                        frame = (byte)MAVLink.MAV_FRAME.GLOBAL,
                        command = (ushort)MAVLink.MAV_CMD.FENCE_CIRCLE_EXCLUSION,
                        param1 = Convert.ToSingle(waypoints[1].lat), // actually is the radius
                        x = (int)(waypoints[0].lat * 1e7),
                        y = (int)(waypoints[0].lon * 1e7),
                        mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                    });
                    seq++;
                }
                else
                {
                    // it's a polygon

                    foreach (var wp in waypoints)
                    {
                        wploader.Add(new MAVLink.mavlink_mission_item_int_t()
                        {
                            target_system = this.id,
                            target_component = 1,
                            seq = (ushort)seq,
                            frame = (byte)MAVLink.MAV_FRAME.GLOBAL,
                            command = (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION,
                            param1 = waypoints.Count,
                            x = (int)(wp.lat * 1e7),
                            y = (int)(wp.lon * 1e7),
                            mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                        });
                        seq++;
                    }
                }
            }
            // Send the number of waypoints
            var msg = new MAVLink.mavlink_mission_count_t
            {
                target_system = this.id,
                target_component = 1,
                count = (ushort)wploader.Count,
                mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
            };

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_COUNT, msg);
            SendMessage(packet);


            // Now I wait for the autopilot to ask for the waypoints one by one
            string msgType;
            while (true)
            {
                msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST).ToString();
                MAVLink.MAVLinkMessage request = messageHandler.WaitForMessageBlock(
                    msgType,
                    timeout: -1
                );
                // The message contains the waypoint number requested by the autopilot
                int next = ((MAVLink.mavlink_mission_request_t)request.data).seq;
                // I send the requested command
                MAVLink.mavlink_mission_item_int_t msg2 = wploader[next];
                packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT, msg2);
                //serialPort1.Write(packet, 0, packet.Length);
                SendMessage(packet);
                if (next == wploader.Count - 1) break; // I have already sent them all
            }
            // Wait for confirmation that everything was received correctly
            msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ACK).ToString();
            MAVLink.MAVLinkMessage response = messageHandler.WaitForMessageBlock(
                msgType,
                timeout: -1
            );

        }

        public void SetScenario2(List<List<(float lat, float lon)>> scenario)
        {
            if (scenario == null || scenario.Count == 0 || scenario[0].Count < 3)
                throw new ArgumentException("The first fence (inclusion) must be a polygon with $\\ge$3 vertices.");

            // Fence commands (ardupilotmega)
            const ushort CMD_POLY_INCL = 5001;  // MAV_CMD_NAV_FENCE_POLYGON_VERTEX_INCLUSION
            const ushort CMD_POLY_EXCL = 5002;  // MAV_CMD_NAV_FENCE_POLYGON_VERTEX_EXCLUSION
            const ushort CMD_CIRC_INCL = 5003;  // MAV_CMD_NAV_FENCE_CIRCLE_INCLUSION
            const ushort CMD_CIRC_EXCL = 5004;  // MAV_CMD_NAV_FENCE_CIRCLE_EXCLUSION

            // Local helpers
            Func<string, int, MAVLink.MAVLinkMessage> Wait = (msgIdStr, toMs)
                => messageHandler.WaitForMessageBlock(msgIdStr, timeout: toMs);

            Action<object, MAVLink.MAVLINK_MSG_ID> Send = (payload, id) =>
            {
                var pkt = mavlink.GenerateMAVLinkPacket10(id, payload);
                SendMessage(pkt);
            };

            // ========== Build ITEM_INT list ==========
            var wploader = new List<MAVLink.mavlink_mission_item_int_t>();
            int seq = 0;

            // Inclusion (main polygon)
            var incl = scenario[0];
            foreach (var wp in incl)
            {
                wploader.Add(new MAVLink.mavlink_mission_item_int_t
                {
                    target_system = this.id,
                    target_component = 1,
                    seq = (ushort)seq++,
                    frame = (byte)MAVLink.MAV_FRAME.GLOBAL,
                    command = CMD_POLY_INCL,
                    param1 = incl.Count, // n. vertices (ArduPilot accepts if repeated)
                    x = (int)Math.Round(wp.lat * 1e7),
                    y = (int)Math.Round(wp.lon * 1e7),
                    z = 0,
                    mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                });
            }

            // Exclusions (polygons or circles)
            for (int i = 1; i < scenario.Count; i++)
            {
                var waypoints = scenario[i];
                if (waypoints == null || waypoints.Count == 0) continue;

                if (waypoints.Count == 2)
                {
                    // Circle exclusion: [0]=center, [1].lat=radius (m)
                    wploader.Add(new MAVLink.mavlink_mission_item_int_t
                    {
                        target_system = this.id,
                        target_component = 1,
                        seq = (ushort)seq++,
                        frame = (byte)MAVLink.MAV_FRAME.GLOBAL,
                        command = CMD_CIRC_EXCL,
                        param1 = waypoints[1].lat, // radius in meters
                        x = (int)Math.Round(waypoints[0].lat * 1e7),
                        y = (int)Math.Round(waypoints[0].lon * 1e7),
                        z = 0,
                        mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                    });
                }
                else
                {
                    foreach (var wp in waypoints)
                    {
                        wploader.Add(new MAVLink.mavlink_mission_item_int_t
                        {
                            target_system = this.id,
                            target_component = 1,
                            seq = (ushort)seq++,
                            frame = (byte)MAVLink.MAV_FRAME.GLOBAL,
                            command = CMD_POLY_EXCL,
                            param1 = waypoints.Count,
                            x = (int)Math.Round(wp.lat * 1e7),
                            y = (int)Math.Round(wp.lon * 1e7),
                            z = 0,
                            mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                        });
                    }
                }
            }

            // ========== CLEAR (optional) + COUNT ==========
            var clear = new MAVLink.mavlink_mission_clear_all_t
            {
                target_system = this.id,
                target_component = 1,
                mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
            };
            Send(clear, MAVLink.MAVLINK_MSG_ID.MISSION_CLEAR_ALL);
            Wait(((int)MAVLink.MAVLINK_MSG_ID.MISSION_ACK).ToString(), 500); // optional

            var countMsg = new MAVLink.mavlink_mission_count_t
            {
                target_system = this.id,
                target_component = 1,
                count = (ushort)wploader.Count,
                mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
            };
            Send(countMsg, MAVLink.MAVLINK_MSG_ID.MISSION_COUNT);

            // ========== Handshake REQUEST/REQUEST_INT -> ITEM/ITEM_INT ==========
            int timeoutReqMs = 3000;
            int sent = 0;

            // Some firmwares need a small pause after COUNT
            System.Threading.Thread.Sleep(50);

            while (sent < wploader.Count)
            {
                // wait for REQUEST_INT or REQUEST
                var reqIntMsgId = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST_INT).ToString();
                var reqMsgId = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST).ToString();

                MAVLink.MAVLinkMessage req = messageHandler.WaitForMessageBlock(reqIntMsgId, timeout: timeoutReqMs);
                bool requestIsInt = true;

                if (req == null)
                {
                    // fallback: older ArduPilot/SITL use MISSION_REQUEST not -INT
                    req = messageHandler.WaitForMessageBlock(reqMsgId, timeout: timeoutReqMs);
                    requestIsInt = false;
                }

                if (req == null)
                    throw new TimeoutException("Timeout waiting for MISSION_REQUEST(_INT).");

                int next;
                if (requestIsInt)
                {
                    var r = (MAVLink.mavlink_mission_request_int_t)req.data;
                    next = r.seq;
                    // (optional) check r.mission_type == FENCE
                }
                else
                {
                    var r = (MAVLink.mavlink_mission_request_t)req.data;
                    next = r.seq;
                    // (optional) check r.mission_type == FENCE if present in your struct
                }

                if (next < 0 || next >= wploader.Count)
                    throw new InvalidOperationException($"Seq out of range: {next}/{wploader.Count}");

                var itemInt = wploader[next];

                if (requestIsInt)
                {
                    // consistent response: ITEM_INT
                    var pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT, itemInt);
                    SendMessage(pkt);
                }
                else
                {
                    // down-convert: ITEM (x,y in degrees, not *1e7)
                    var item = new MAVLink.mavlink_mission_item_t
                    {
                        target_system = itemInt.target_system,
                        target_component = itemInt.target_component,
                        seq = itemInt.seq,
                        frame = itemInt.frame,
                        command = itemInt.command,
                        current = itemInt.current,
                        autocontinue = itemInt.autocontinue,
                        param1 = itemInt.param1,
                        param2 = itemInt.param2,
                        param3 = itemInt.param3,
                        param4 = itemInt.param4,
                        x = itemInt.x / 1e7f,  // INT -> float degrees
                        y = itemInt.y / 1e7f,
                        z = itemInt.z,
                        mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                    };
                    var pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ITEM, item);
                    SendMessage(pkt);
                }

                sent++;
            }

            // Final ACK of the upload (many SITL don't send it: short timeout)
            Wait(((int)MAVLink.MAVLINK_MSG_ID.MISSION_ACK).ToString(), 800);

            // ========== AUTO-APPLY: enable fence ==========
            // 1) COMMAND_LONG: MAV_CMD_DO_FENCE_ENABLE (param1=1)
            var cmdEnable = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.DO_FENCE_ENABLE,
                confirmation = 0,
                param1 = 1f
            };
            Send(cmdEnable, MAVLink.MAVLINK_MSG_ID.COMMAND_LONG);
            Wait(((int)MAVLink.MAVLINK_MSG_ID.COMMAND_ACK).ToString(), 500); // optional

            // 2) Toggle PARAM_SET FENCE_ENABLE 0→1 to force internal reload
            var set0 = new MAVLink.mavlink_param_set_t
            {
                target_system = this.id,
                target_component = 1,
                param_id = new byte[16],
                param_type = (byte)MAVLink.MAV_PARAM_TYPE.UINT8,
                param_value = 0f
            };
            var name = System.Text.Encoding.ASCII.GetBytes("FENCE_ENABLE");
            Array.Copy(name, set0.param_id, Math.Min(16, name.Length));
            Send(set0, MAVLink.MAVLINK_MSG_ID.PARAM_SET);
            Wait(((int)MAVLink.MAVLINK_MSG_ID.PARAM_VALUE).ToString(), 500); // optional

            var set1 = set0; set1.param_value = 1f;
            Send(set1, MAVLink.MAVLINK_MSG_ID.PARAM_SET);
            Wait(((int)MAVLink.MAVLINK_MSG_ID.PARAM_VALUE).ToString(), 500); // optional

            // (optional) If you use circles/polygons, check/force FENCE_TYPE coherent via PARAM_SET.
        }


        public List<(List<(float lat, float lon)> vertices, string type)> GetScenario()
        {

            // 1) Request FENCE item count
            var msg = new MAVLink.mavlink_mission_request_list_t
            {
                target_system = this.id,
                target_component = 1,
                mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
            };
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST_LIST, msg);
            SendMessage(packet);

            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_COUNT).ToString();
            MAVLink.MAVLinkMessage response = messageHandler.WaitForMessageBlock(msgType, timeout: -1);
            int count = ((MAVLink.mavlink_mission_count_t)response.data).count;

            Console.WriteLine($"Drone {this.id}: The scenario has {count} items");

            // 2) Download all items
            var wploader = new List<MAVLink.mavlink_mission_item_int_t>();

            for (int i = 0; i < count; i++)
            {
                // 1) Request with INT variant (consistent with ITEM_INT)
                var reqInt = new MAVLink.mavlink_mission_request_int_t
                {
                    target_system = this.id,
                    target_component = 1,
                    seq = (ushort)i,
                    mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                };

                // If your library only has GenerateMAVLinkPacket10, use that; otherwise 20 is fine
                byte[] packetReq = mavlink.GenerateMAVLinkPacket10(
                    MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST_INT, reqInt);
                SendMessage(packetReq);

                // 2) Wait for MISSION_ITEM_INT (use a FINITE timeout, e.g., 1500 ms)
                string idItemInt = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT).ToString();
                MAVLink.MAVLinkMessage resp = messageHandler.WaitForMessageBlock(idItemInt, timeout: 1500);

                // 3) Retry/fallback if needed
                if (resp == null)
                {
                    // Some firmwares still reply with MISSION_ITEM (non-INT).
                    // Try the non-INT request and wait for ITEM (non-INT).
                    var req = new MAVLink.mavlink_mission_request_t
                    {
                        target_system = this.id,
                        target_component = 1,
                        seq = (ushort)i,
                        mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                    };
                    byte[] packetReqOld = mavlink.GenerateMAVLinkPacket10(
                        MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST, req);
                    SendMessage(packetReqOld);

                    string idItem = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ITEM).ToString();
                    resp = messageHandler.WaitForMessageBlock(idItem, timeout: 1500);

                    if (resp == null)
                        throw new TimeoutException($"No response for fence seq={i}.");

                    // Convert ITEM -> ITEM_INT to unify downstream parsing
                    var wpOld = (MAVLink.mavlink_mission_item_t)resp.data;
                    var wpInt = new MAVLink.mavlink_mission_item_int_t
                    {
                        target_system = wpOld.target_system,
                        target_component = wpOld.target_component,
                        seq = wpOld.seq,
                        frame = wpOld.frame,
                        command = wpOld.command,
                        current = wpOld.current,
                        autocontinue = wpOld.autocontinue,
                        param1 = wpOld.param1,
                        param2 = wpOld.param2,
                        param3 = wpOld.param3,
                        param4 = wpOld.param4,
                        // x,y in degrees -> *1e7 to simulate INT
                        x = (int)Math.Round(wpOld.x * 1e7),
                        y = (int)Math.Round(wpOld.y * 1e7),
                        z = wpOld.z,
                        mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                    };
                    wploader.Add(wpInt);
                    continue;
                }

                // Here I received ITEM_INT
                var wp = (MAVLink.mavlink_mission_item_int_t)resp.data;

                // (optional) check that it is indeed the expected seq and mission_type FENCE
                if (wp.seq != (ushort)i || wp.mission_type != (byte)MAVLink.MAV_MISSION_TYPE.FENCE)
                {
                    // If it doesn't match, you can repeat the request or handle as an error
                    // For simplicity, I still add it and continue
                }

                wploader.Add(wp);
            }

            // 4) Final ACK: DO NOT block infinitely; some firmwares do not send ACK for download
            string idAck = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ACK).ToString();
            var ack = messageHandler.WaitForMessageBlock(idAck, timeout: 500); // optional

            Console.WriteLine($"Drone {this.id}: Downloaded {wploader.Count} items");


            // 4) Parsing into polygons + type
            var scenarioResult = new List<(List<(float lat, float lon)>, string)>();

            List<(float lat, float lon)> currentFence = null;
            int expectedVertices = 0;
            string currentType = "";

            foreach (var item in wploader.OrderBy(x => x.seq))
            {
                bool isInclusion = item.command == (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_INCLUSION;
                bool isExclusion = item.command == (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION;
                if (!isInclusion && !isExclusion)
                    continue;

                // If a new polygon starts
                if (expectedVertices == 0)
                {
                    expectedVertices = (int)Math.Round(item.param1); // number of vertices declared in the first polygon item
                    currentFence = new List<(float lat, float lon)>(capacity: Math.Max(3, expectedVertices));
                    currentType = isInclusion ? "Inclusion" : "Exclusion";
                }

                // Add vertex (lat/lon in 1e7)
                float lat = item.x / 1e7f;
                float lon = item.y / 1e7f;
                currentFence.Add((lat, lon));
                expectedVertices--;

                // If polygon is complete -> push and clear
                if (expectedVertices == 0)
                {
                    scenarioResult.Add((currentFence, currentType));
                    currentFence = null;
                    currentType = "";
                }
            }

            Console.WriteLine($"Drone {this.id}: Parsed {scenarioResult.Count} fences");

            // If pending vertices remain (incomplete data), return what we have
            if (expectedVertices > 0 && currentFence != null && currentFence.Count > 0)
            {
                scenarioResult.Add((currentFence, string.IsNullOrEmpty(currentType) ? "unknown" : currentType));
            }

            Console.WriteLine($"Drone {this.id}: Final scenario has {scenarioResult.Count} fences");
            return scenarioResult;
        }

    }
}
