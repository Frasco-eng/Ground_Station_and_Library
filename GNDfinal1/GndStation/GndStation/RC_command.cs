using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GndStation
{
    public partial class Form1
    {
        private void Joystick_fcn(List<ushort> channels)
        {
            // First look for a Gamepad
            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
                        DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
                            DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            // If Joystick not found, exit
            if (joystickGuid == Guid.Empty)
            {
                //Console.WriteLine("No joystick/Gamepad found.");
                MessageBox.Show("No joystick/Gamepad found.", "DirectInput Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Instantiate the joystick
            var joystick = new Joystick(directInput, joystickGuid);

            Console.WriteLine("Found Joystick/Gamepad with GUID: {0}", joystickGuid);

            // Query all supported ForceFeedback effects
            var allEffects = joystick.GetEffects();
            foreach (var effectInfo in allEffects)
                Console.WriteLine("Effect available: {0}", effectInfo.Name);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();

            Thread j = new Thread(() => JoystickPolling(joystick, channels));

            j.IsBackground = true;
            j.Start();

            Thread send = new Thread(() =>
            {
                while (true)
                {
                    drone.Send_RC_Override(channels);
                    //Console.WriteLine("Sent: " + string.Join(", ", channels));
                    Thread.Sleep(100); // I send every 100 ms
                }
            });
            send.IsBackground = true;
            send.Start();

            Thread modes = new Thread(() =>
            {
                modesetting(channels);
            });

            modes.IsBackground = true;
            modes.Start();

        }

        private void JoystickPolling(Joystick joystick, List<ushort> channels)
        {
            while (true)
            {
                joystick.Poll();
                var datas = joystick.GetBufferedData();
                foreach (var state in datas)
                {

                    switch (state.Offset)
                    {
                        case JoystickOffset.Z:
                            //Console.WriteLine("X changed");
                            channels[0] = (ushort)((state.Value / 32767.0 * 500) + 1000); // I use 32767.0 for double division
                            break;
                        case JoystickOffset.RotationZ:
                            //Console.WriteLine("Y changed");
                            channels[1] = (ushort)((state.Value / 32767.0 * (-500)) + 2000); // I use 32767.0 for double division
                            break;
                        case JoystickOffset.Y:
                            //Console.WriteLine("Z changed");
                            channels[2] = (ushort)((state.Value / 32767.0 * (-500)) + 2000); // I use 32767.0 for double division
                            break;
                        case JoystickOffset.X:
                            //Console.WriteLine("Rotation Z changed");
                            channels[3] = (ushort)((state.Value / 32767.0 * 500) + 1000); // I use 32767.0 for double division
                            break;
                        case JoystickOffset.Buttons0:
                            //Console.WriteLine("Button 0 pressed");
                            channels[4] = (ushort)((state.Value / 128.0 * 500) + 1000);
                            break;
                        case JoystickOffset.Buttons1:
                            //Console.WriteLine("Button 1 pressed");
                            channels[5] = (ushort)((state.Value / 128.0 * 500) + 1000);
                            break;
                        case JoystickOffset.Buttons2:
                            //Console.WriteLine("Button 2 pressed");
                            channels[6] = (ushort)((state.Value / 128.0 * 500) + 1000);
                            break;
                        case JoystickOffset.Buttons3:
                            //Console.WriteLine("Button 3 pressed");
                            channels[7] = (ushort)((state.Value / 128.0 * 500) + 1000);
                            break;
                        case JoystickOffset.Buttons4:
                            channels[8] = (ushort)((state.Value / 128.0 * 500) + 1000);
                            break;
                        case JoystickOffset.Buttons5:
                            channels[9] = (ushort)((state.Value / 128.0 * 500) + 1000);
                            break;
                        default:
                            //Console.WriteLine("Unknown offset: {0}", state.Offset);
                            break;
                    }
                    //Console.WriteLine(state);
                    Console.WriteLine(string.Join(", ", channels));
                }
            }
        }

        private void modesetting(List<ushort> channels)
        {
            while (true)
            {
                if (channels[4] > 1250)
                {
                    drone.SetFlightMode("LOITER");
                    Console.WriteLine("LOITER");
                }
                else if (channels[5] > 1250)
                {
                    drone.SetFlightMode("RTL");
                    Console.WriteLine("RTL");
                }
                else if (channels[6] > 1250)
                {
                    drone.SetFlightMode("LAND");
                    Console.WriteLine("LAND");
                }
                else if (channels[7] > 1250)
                {
                    drone.SetFlightMode("GUIDED");
                    Console.WriteLine("GUIDED");
                }
                else if (channels[8] > 1250)
                {
                    drone.Takeoff(1);
                    Console.WriteLine("Takeoff");
                }
                else if (channels[9] > 1250)
                {
                    drone.Disarm();
                    Console.WriteLine("STABILIZE");
                }
                else
                {
                    //NOTHING
                }
                Thread.Sleep(150); // I check every 150 ms
            }
        }
    }
}
