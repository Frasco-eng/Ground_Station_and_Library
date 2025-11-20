using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csDronLink
{
    public partial class Dron
    {
        private bool CheckInAir(MAVLink.MAVLinkMessage msg, object targetAlt)
        {
            // this is the function that will check that the drone's altitude is the one indicated as
            // parameter. It will be used to see if the drone has reached the height indicated in the takeoff
            // Retrieve the altitude from the received message
            var position = (MAVLink.mavlink_global_position_int_t)msg.data;
            float altitude = position.relative_alt / 1000.0f;
            // return the result of performing the check (with a 10% margin)
            return altitude > (int)targetAlt * 0.90;
        }


        private bool CheckIndoorFlightDelta(MAVLink.MAVLinkMessage msg, object state)
        {
            var position = (MAVLink.mavlink_local_position_ned_t)msg.data;
            var s = ((MAVLink.mavlink_local_position_ned_t start, (float x, float y, float z) target))state;

            // Accumulated Delta since the command was sent
            float dx = position.x - s.start.x;
            float dy = position.y - s.start.y;
            float dz = position.z - s.start.z;

            // Target vector (required offset)
            float tx = s.target.x;
            float ty = s.target.y;
            float tz = s.target.z;

            // Error relative to the required offset
            float ex = dx - tx;
            float ey = dy - ty;
            float ez = dz - tz;

            float err2 = ex * ex + ey * ey + ez * ez;
            float targetNorm = (float)Math.Sqrt(tx * tx + ty * ty + tz * tz);
            float tol = Math.Max(0.10f, 0.10f * targetNorm); // 10 cm or 10% of the offset

            return err2 <= tol * tol;
        }


        private bool CheckStopped(MAVLink.MAVLinkMessage msg, object param = null)
        {
            // checks if the message indicates that the drone's speed is zero
            // It will serve to detect that the drone has reached its destination
            var position = (MAVLink.mavlink_global_position_int_t)msg.data;
            float vx = position.vx;
            float vy = position.vy;
            float vz = position.vz;
            double speed = Math.Sqrt(vx * vx + vy * vy + vz * vz) / 100;
            return speed < 0.1;
        }
        private bool CheckOnGround(MAVLink.MAVLinkMessage msg, object param = null)
        {
            // this is the function that will check that the drone's altitude is less than 50 cm. It will be used to
            // detect the end of the landing or RTL operation
            var position = (MAVLink.mavlink_global_position_int_t)msg.data;
            float altitude = position.relative_alt / 1000.0f;
            return altitude < 0.50;
        }
        private bool CheckOrientation(MAVLink.MAVLinkMessage msg, object degrees)
        {
            // this is the function that will check that the orientation is the one indicated
            // It is used to determine when the heading change operation has finished
            var position = (MAVLink.mavlink_global_position_int_t)msg.data;
            float heading = position.hdg / 100.0f;
            return Math.Abs(heading - (float)degrees) < 5;
        }
        private bool CheckAtWaypoint(MAVLink.MAVLinkMessage msg, object n)
        // Used to check that the message indicates that the waypoint
        // with index n has been reached
        {

            var seq = ((MAVLink.mavlink_mission_item_reached_t)msg.data).seq;
            return seq == (int)n;
        }

    }
}
