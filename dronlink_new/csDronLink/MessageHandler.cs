using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MAVLink;

namespace csDronLink
{

    public class MessageHandler
    {
        // This is the class to manage messages received from the autopilot.
        // The idea is to have a single reception point for messages (in the MessageLoop method)
        // and from there, forward them to whoever requested that type of message.
        // Two types of requests are considered: Synchronous requests are for a specific message
        // that the requester needs instantly. Therefore, the requester will be blocked until
        // the expected message is received.
        // Asynchronous requests do not block the requester. The requester is simply registered
        // so that every time a message of the indicated type arrives, it is sent to them.

        private MavlinkParse mavlink;
        string mode;
        SerialPort serialPort;
        NetworkStream tcpPort;

        private bool running = true;
        Thread messageThread;
        private readonly object lockObj = new object();

        // List for asynchronous requests
        private ConcurrentDictionary<string, List<Action<MAVLinkMessage>>> handlers = new ConcurrentDictionary<string, List<Action<MAVLinkMessage>>>();

        // This is the structure that represents a synchronous request
        public class WaitingRequest
        {
            public string MsgType { get; }
            public Func<MAVLinkMessage, object, bool> Condition { get; }
            public object Params { get; }
            public BlockingCollection<MAVLinkMessage> Queue { get; }

            public WaitingRequest(string msgType, Func<MAVLinkMessage, object, bool> condition, object parameters, BlockingCollection<MAVLinkMessage> queue)
            {
                // When registering a synchronous request we save:
                // the type of message being requested
                MsgType = msgType;
                // the function that will verify if the message meets a certain condition
                Condition = condition;
                // parameters that the condition verification function might need
                Params = parameters;
                // the queue in which the requester will be blocked until the manager
                // enqueues the message that meets the established conditions
                Queue = queue;

                /// THE 3 TYPICAL CASES:
                /// 1. Message request that does not require a condition: requesting a parameter's value.
                /// 2. Request with condition: requesting the drone's altitude, but conditioned on that
                /// altitude being less than 0.5 meters. We would do this when we want to know if the drone
                /// has just landed.
                /// 3. Request with condition and parameter: requesting the altitude but conditioned on it
                /// being greater than the value received as a parameter. We would use this when we want to
                /// know if the drone has reached the established takeoff altitude.
            }
        }

        // List for synchronous requests
        private List<WaitingRequest> waitingThreads = new List<WaitingRequest>();

        private MAVLinkMessage ReadMessage()
        {
            if (mode == "production")
                return mavlink.ReadPacket(serialPort.BaseStream);
            else
                return mavlink.ReadPacket(tcpPort);
        }

        public MessageHandler(string mode, object channel)
        {
            this.mode = mode;
            if (mode == "production")
                this.serialPort = (SerialPort)channel;
            else
                this.tcpPort = (NetworkStream)channel;

            this.mavlink = new MavlinkParse();

            // Start the thread that will continuously receive and distribute messages
            this.messageThread = new Thread(MessageLoop) { IsBackground = true };
            this.messageThread.Start();
        }

        private void MessageLoop()
        {
            // 
            while (running)
            {

                var packet = ReadMessage();
                if (packet == null) continue;

                // Get the message ID
                string msgType = packet.msgid.ToString();

                lock (lockObj)
                {
                    // Process synchronous requests
                    // Use ToList() to iterate over a copy while modifying the original list
                    foreach (var waiting in waitingThreads.ToList())
                    {
                        // Iterate through the list of requests to see which ones are waiting
                        // for a message of that type

                        // Check if message type matches AND (no condition OR condition is met)
                        if (waiting.MsgType == msgType && (waiting.Condition == null || waiting.Condition(packet, waiting.Params)))
                        {
                            // We've checked that the message is of the correct type and, if a condition
                            // was set, we called the corresponding function to verify that the
                            // condition is met by passing the corresponding parameters.

                            // Since the conditions have been satisfied, we put the message in the queue
                            // so that it reaches the requester, who will be unblocked and can continue.

                            waiting.Queue.Add(packet);

                            // Remove the request from the list because it has been satisfied
                            waitingThreads.Remove(waiting);

                            // Break immediately because this message satisfies one synchronous request
                            // and synchronous requests are typically exclusive.
                            break;
                        }
                    }
                }

                // Process asynchronous handlers
                // Look in the handlers list for all callbacks for the received message type
                if (handlers.TryGetValue(msgType, out var callbacks))
                {
                    // Now call each of the callbacks, passing the received message.
                    // Run each callback in a separate Task to avoid blocking the main MessageLoop thread.
                    foreach (var callback in callbacks)
                    {
                        Task.Run(() => callback(packet));
                    }
                }
            }
        }

        public void RegisterHandler(string msgType, Action<MAVLinkMessage> callback)
        {
            // Register an asynchronous request by indicating the message type of interest and the
            // callback to call (passing the message) every time a message of that type is received.

            // This ConcurrentDictionary method handles adding a new type (key) with a new list
            // or updating an existing list by adding the new callback.
            handlers.AddOrUpdate(msgType, new List<Action<MAVLinkMessage>> { callback }, (key, list) =>
            {
                // The update function runs if the key already exists
                list.Add(callback);
                return list;
            });
        }

        public void UnregisterHandler(string msgType, Action<MAVLinkMessage> callback)
        {
            if (handlers.ContainsKey(msgType))
            {
                // Remove the specific callback
                handlers[msgType].Remove(callback);

                // If the list for this message type is now empty, try to remove the key from the dictionary
                if (!handlers[msgType].Any())
                    handlers.TryRemove(msgType, out _);
            }
        }

        public WaitingRequest WaitForMessageNoBlock(string msgType, Func<MAVLinkMessage, object, bool> condition = null, object parameters = null)
        {
            // This is a synchronous but NON-BLOCKING request registration.
            // The requester will have to block itself later by calling WaitNow.

            // Prepare the queue where the requester will wait
            var msgQueue = new BlockingCollection<MAVLinkMessage>();

            // Create a request object with the message type, condition, parameters for the condition, and the queue.
            var waiting = new WaitingRequest(msgType, condition, parameters, msgQueue);

            // Add the request to the list of waiting threads
            lock (lockObj) waitingThreads.Add(waiting);

            // Return the object so that WaitNow can be called later
            return waiting;
        }

        public MAVLinkMessage WaitForMessageBlock(string msgType, Func<MAVLinkMessage, object, bool> condition = null, object parameters = null, int timeout = 3000)
        {
            // This is a synchronous BLOCKING request.

            // Prepare the queue where the requester will wait
            var msgQueue = new BlockingCollection<MAVLinkMessage>();

            // Create a request object
            var waiting = new WaitingRequest(msgType, condition, parameters, msgQueue);

            // Place the object in the request queue
            lock (lockObj) waitingThreads.Add(waiting);

            // Make the requester wait and return the result
            return WaitNow(waiting, timeout);
        }

        public MAVLinkMessage WaitNow(WaitingRequest waiting, int timeout)
        {
            // The timeout is in MILLISECONDS.
            // A timeout value of -1 indicates to wait indefinitely.

            // Wait for the message using the blocking queue
            if (waiting.Queue.TryTake(out var msg, timeout))
            {
                // Message received (it was already removed from waitingThreads in MessageLoop, 
                // but for safety, ensure it's removed on a timeout path as well, 
                // though TryTake succeeded here)
                // The original code was designed to be removed *inside* MessageLoop on success.
                // The logic here is safe but slightly redundant if MessageLoop handles successful removal correctly.
                // However, to cover the scenario where a request registered via WaitForMessageNoBlock 
                // is NOT satisfied within the timeout, the removal logic should live here or in both places.
                return msg;
            }
            else
            {
                // Timeout occurred or the queue was marked complete.
                // Since this path is hit on timeout, we must clean up the waitingThreads list.
                lock (lockObj) waitingThreads.Remove(waiting);
                return null;
            }

        }

        public void Stop()
        {
            running = false;
            // The MessageLoop thread is blocked on ReadMessage(), which depends on the underlying stream.
            // A proper stop often requires closing the SerialPort/NetworkStream to unblock the thread,
            // but the current implementation only calls Join().
            messageThread.Join();
        }

        public void Clear()
        {
            lock (lockObj) waitingThreads.Clear();
        }

    }

}
