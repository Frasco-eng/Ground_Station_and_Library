using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GndStation
{
    public partial class SetupForm
    {
        private void LoadParamsBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialogFile = new OpenFileDialog();
            dialogFile.Title = "Select a file";
            dialogFile.Filter = "Text files (*.txt)|*.txt"; 

            if (dialogFile.ShowDialog() == DialogResult.OK)
            {
                string filepath = dialogFile.FileName;
                MessageBox.Show("You selected: " + filepath);

                string[] content = File.ReadAllLines(filepath);

                foreach (string line in content)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        string param_name = parts[0].Trim();
                        float param_value = float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
                        parameters_read.Add((param_name, param_value));
                    }
                }

                drone.WriteParameters(parameters_read);

            }
            MessageBox.Show("Parameters successfully loaded.");
        }

        private void SaveParamsBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialogFile = new OpenFileDialog();
            dialogFile.Title = "Select a file";
            dialogFile.Filter = "Text files (*.txt)|*.txt";

            if (dialogFile.ShowDialog() == DialogResult.OK)
            {
                string filepath = dialogFile.FileName;
                MessageBox.Show("You selected: " + filepath);

                File.WriteAllText(filepath, string.Empty);

                for (int i = 0; i < parameters_value.Count; i++)
                {
                    string line = parameters_value[i].name + ", " + parameters_value[i].value.ToString(CultureInfo.InvariantCulture);
                    File.AppendAllText(filepath, line + Environment.NewLine);
                }

            }
            MessageBox.Show("Parameters successfully saved in: {percorsoFile}.");
        }

        private void ReadParamsBtn_Click(object sender, EventArgs e)
        {
            parameters_value = drone.ReadParametersViaListStreamed(parameters_list);
        }

        private void CloseBtn_Click(object sender, EventArgs e)
        {
            SetupForm.ActiveForm.Close();
        }

        private void Set_param_Click(object sender, EventArgs e)
        {
            try
            {
                drone.Set_Parameter(Param_name.Text, float.Parse(Param_value.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting parameter: " + ex.Message);
            }

        }

        private void GeoFence_Enable_CheckedChanged(object sender, EventArgs e)
        {
            if (mainForm.MaxAltitude2.Text == "")
            {
                mainForm.MaxAltitude2.Text = "10";
            }
            if (mainForm.InOutDoor2.Text == "Outdoor")
            {
                mainForm.InOutDoor_Choice(sender, e);
            }

            if (GeoFence_Enable.Checked)
            {
                Thread t1 = new Thread(() => mainForm.CheckForViolation(GeoFence_Enable, SafetyMode.Value, mainForm.geofences, double.Parse(mainForm.MaxAltitude2.Text)));
                // Il thread t2 (LastGoodPoint(inside)) è gestito all'interno di CheckForViolation nel codice originale
                // Manteniamo la logica originale per non introdurre bugs, ma idealmente andrebbe rifattorizzata.
                // Thread t2 = new Thread(() => LastGoodPoint(inside)); 
                t1.IsBackground = true;
                t1.Start();
                // t2.IsBackground = true;
                // t2.Start();
            }
        }

        private void SceneFence_Click(object sender, EventArgs e)
        {
            List<List<(float lat, float lon)>> geof_points = new List<List<(float lat, float lon)>>();

            foreach (var list in mainForm.geofences)
            {
                var coords = new List<(float lat, float lon)>();
                // Note the inversion of coordinates here.


                foreach (var item in list.Item1)
                {
                    coords.Add((item.X, item.Y)); // (lon, lat) -> (item.Y, item.X)
                }

                geof_points.Add(coords);
            }

            drone.SetScenario(geof_points);
        }

        private void Read_SceneFence_Click(object sender, EventArgs e)
        {
            var geofencesRead = drone.GetScenario(); // Renamed to avoid conflict
            MessageBox.Show($"Geofences obtained: {geofencesRead.Count}");

            // snapshot per evitare "collection modified"
            var snapshot = geofencesRead.ToList();

            foreach (var fence in snapshot)
            {
                //We need to adapt to the new method signature
                mainForm.CreateFence(fence.type, fence.Item1);   // adapt tuple naming
            }
        }

    }
}
