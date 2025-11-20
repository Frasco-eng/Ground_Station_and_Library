using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System;

namespace GndStation
{
    public partial class Form1
    {
        // ========== CALLBACKS  ==============
        private void InAir(byte id, object param)
        {
            Takeoff.BackColor = Color.Green;
            Takeoff.ForeColor = Color.White;
            MessageBox.Show((string)param);
            Takeoff.BackColor = Color.Orange;
            Takeoff.ForeColor = Color.Black;
        }

        private void OnGround(byte id, object param)
        {
            if ((string)param == "Landed")
            {
                Land.BackColor = Color.Green;
                Land.ForeColor = Color.White;
                MessageBox.Show((string)param);
                Land.BackColor = Color.Orange;
                Land.ForeColor = Color.Black;
            }
            else
            {
                RTL.BackColor = Color.Green;
                RTL.ForeColor = Color.White;
                MessageBox.Show((string)param);
                RTL.BackColor = Color.Orange;
                RTL.ForeColor = Color.Black;
            }

        }

        private void MissionEnd(byte id, object param = null)
        {
            Mission.BackColor = Color.Green;
            Mission.ForeColor = Color.White;
            MessageBox.Show("Mission Ended");
            Mission.BackColor = Color.Orange;
            Mission.ForeColor = Color.Black;
            ClearMission();
            drone.SetGuidedMode();
        }
        private void Finished(byte id, object param)
        {
            Move.BackColor = Color.Green;
            Move.ForeColor = Color.White;
            MessageBox.Show((string)param);
            Move.BackColor = Color.Orange;
            Move.ForeColor = Color.Black;
        }

        private void EndFailSafe(byte id, object param)
        {
            ClearMission();
            Mission.BackColor = Color.Orange;
            Mission.ForeColor = Color.Black;
            Move.BackColor = Color.Orange;
            Move.ForeColor = Color.Black;
            violation = false;
            Console.WriteLine((string)param);
        }
    }
}
