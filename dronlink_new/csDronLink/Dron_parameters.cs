using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static csDronLink.MessageHandler;

namespace csDronLink
{
    public partial class Dron
    {
        // Parameters

        public Dictionary<string, (float value, MAVLink.MAV_PARAM_TYPE type)> RequestAllParameters()
        {
            var all = new Dictionary<string, (float, MAVLink.MAV_PARAM_TYPE)>(StringComparer.Ordinal);

            // 1) Registrati a PARAM_VALUE PRIMA di inviare la richiesta
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.PARAM_VALUE).ToString();
            WaitingRequest waiting = messageHandler.WaitForMessageNoBlock(msgType);

            // 2) Invia PARAM_REQUEST_LIST
            var req = new MAVLink.mavlink_param_request_list_t
            {
                target_system = this.id,
                target_component = 1
            };
            byte[] pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.PARAM_REQUEST_LIST, req);
            SendMessage(pkt);

            // 3) Ricevi lo stream di PARAM_VALUE fino a completare param_count
            int expectedCount = -1;
            var deadline = Environment.TickCount + 60000; // timeout totale 15s
            while (Environment.TickCount < deadline)
            {
                // attesa a piccoli step per non bloccare troppo
                var resp = messageHandler.WaitNow(waiting, timeout: 500);

                if (resp == null)
                {
                    Console.WriteLine("Timeout attesa PARAM_VALUE...");
                    // se abbiamo già tutti i parametri, usciamo
                    if (expectedCount > 0 && all.Count >= expectedCount) break;
                    // ri-arma il “waiting” e continua
                    waiting = messageHandler.WaitForMessageNoBlock(msgType);
                    continue;
                }

                var pv = (MAVLink.mavlink_param_value_t)resp.data;

                // aggiorna il totale atteso
                if (pv.param_count > 0) expectedCount = pv.param_count;

                // estrai nome (trim della C-string 16 byte)
                string name = Encoding.ASCII.GetString(pv.param_id).TrimEnd('\0').Trim();
                Console.WriteLine($"Received parameter: {name} = {pv.param_value} (type {pv.param_type})");

                // salva (evita duplicati: tieni l’ultimo)
                all[name] = (pv.param_value, (MAVLink.MAV_PARAM_TYPE)pv.param_type);
                Console.WriteLine($"Stored parameter: {name} = {pv.param_value} (type {pv.param_type})");

                // condizione di uscita naturale
                if (expectedCount > 0 && all.Count >= expectedCount) break;

                // IMPORTANTISSIMO: dopo ogni ricezione, ri-arma l’attesa per il prossimo PARAM_VALUE
                waiting = messageHandler.WaitForMessageNoBlock(msgType);
            }

            if (expectedCount > 0 && all.Count < expectedCount)
                Console.WriteLine($"Warning: ricevuti {all.Count}/{expectedCount} parametri (timeout).");

            return all;
        }

        public List<(string name, float value)> ReadParametersViaListStreamed(List<string> parameters)
        {
            var all = RequestAllParameters();

            var result = new List<(string name, float value)>(parameters.Count);
            foreach (var raw in parameters)
            {
                string name = raw.Trim();
                if (name.Length > 16) name = name.Substring(0, 16); // compat con MAVLink C-string

                if (all.TryGetValue(name, out var tup))
                    result.Add((name, tup.value));
                else
                {
                    Console.WriteLine($"Parametro non trovato nello stream: {raw}");
                    // opzionale: result.Add(float.NaN);
                }
            }
            return result;
        }


        //======================================
        //======================================
        //======================================

        public List<(string name, float value)> ReadParameters(List<string> parameters)
        {
            // The resulting list will contain the parameter values in the same
            // order as they appear in the list of parameters.

            List<(string name, float value)> result = new List<(string name, float value)>();

            // Create a byte array of fixed size for the parameter name
            byte[] paramIdBytes = new byte[16];

            foreach (string param in parameters)
            {
                // First, register a synchronous but non-blocking request.
                // This is done because if the request is made after the message is sent,
                // the message might arrive before the request is registered.
                // So, first register, then request, then wait.
                string msgType = ((int)MAVLink.MAVLINK_MSG_ID.PARAM_VALUE).ToString();
                WaitingRequest waiting = messageHandler.WaitForMessageNoBlock(msgType);

                // Convert the parameter name
                // Clear the array and copy the new parameter name
                Array.Clear(paramIdBytes, 0, paramIdBytes.Length);
                Array.Copy(Encoding.ASCII.GetBytes(param), paramIdBytes, Math.Min(param.Length, paramIdBytes.Length));

                // Create the request to get the parameter value
                MAVLink.mavlink_param_request_read_t req = new MAVLink.mavlink_param_request_read_t
                {
                    target_system = this.id,  // System ID
                    target_component = 1,  // Component ID (autopilot)
                    param_index = -1,  // Parameter index (-1 is used to request by name)
                    param_id = paramIdBytes,  // The parameter name must be a fixed-length byte array
                };

                // Generate the MAVLink packet to request the parameter
                byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.PARAM_REQUEST_READ, req);

                // Send the request
                SendMessage(packet);

                // Now wait for the response (10 seconds timeout)
                var response = messageHandler.WaitNow(waiting, timeout: 10000);

                if (response != null)
                {
                    // Response received
                    var paramResponse = (MAVLink.mavlink_param_value_t)response.data;

                    // Decode and trim the parameter name from the response (MAVLink PARAM_VALUE)
                    string receivedParamName = Encoding.ASCII.GetString(paramResponse.param_id).TrimEnd('\0').Trim();

                    // Verify that the received parameter matches the requested one (Highly recommended in practice)
                    if (receivedParamName == param)
                    {
                        result.Add((param, paramResponse.param_value));
                    }
                    else
                    {
                        // Handle mismatch or log error if necessary
                        Console.WriteLine($"Parameter mismatch: Requested '{param}', received '{receivedParamName}'");
                    }
                }
                else
                {
                    // Parameter not received within the timeout
                    Console.WriteLine($"Timeout reading parameter: {param}");
                    // The original code did not add anything, maintaining that behavior (or you might add a default/NaN)
                }

            }
            return result;
        }

        public void WriteParameters(List<(string parameter, float value)> parameters)
        {
            byte[] paramIdBytes = new byte[16];

            for (int i = 0; i < parameters.Count; i++)
            {
                // Data for the next parameter to write
                string parameter = parameters[i].parameter;
                float value = parameters[i].value;

                // Convert the parameter name to a byte array
                Array.Clear(paramIdBytes, 0, paramIdBytes.Length);
                Array.Copy(Encoding.ASCII.GetBytes(parameter), paramIdBytes, Math.Min(parameter.Length, paramIdBytes.Length));

                // Create the request to set the parameter value
                MAVLink.mavlink_param_set_t request = new MAVLink.mavlink_param_set_t
                {
                    target_system = this.id,     // System ID
                    target_component = 1,   // Component ID (autopilot)
                    param_value = value,    // New parameter value
                    param_id = paramIdBytes,    // Parameter name in a byte array
                    param_type = (byte)MAVLink.MAV_PARAM_TYPE.REAL32 // Data type (32-bit float)
                };

                // 

                // Generate the MAVLink packet to send the new parameter value
                byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.PARAM_SET, request);

                // Send the parameter update request to the drone
                SendMessage(packet);

                // NOTE: The MAVLink protocol usually expects a brief delay between PARAM_SET messages
                // to give the autopilot time to process and save the value, but the original code 
                // did not include a delay. A small Thread.Sleep(100) might be advisable here.
            }
        }

        public void Set_Parameter(string name, float value)
        {
            // Create the PARAM_SET message
            MAVLink.mavlink_param_set_t paramSet = new MAVLink.mavlink_param_set_t
            {
                target_system = this.id,
                target_component = 1, // Autopilot component
                param_id = new byte[16],
                param_value = value,
                param_type = (byte)MAVLink.MAV_PARAM_TYPE.REAL32
            };
            // Copy the parameter name into the byte array, ensuring it doesn't exceed 16 bytes
            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
            Array.Copy(nameBytes, paramSet.param_id, Math.Min(nameBytes.Length, paramSet.param_id.Length));
            // Generate the MAVLink packet
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.PARAM_SET, paramSet);
            // Send the packet to the drone
            SendMessage(packet);
        }

    }
}
