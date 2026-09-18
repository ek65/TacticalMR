using System;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using System.Threading;


public class ZMQRequester : RunAbleThread
{
    private string ip;
    private string port;
    private bool isInit;
    private bool isServer;
    public string data;
    private string outData;
    private bool readyToCommunicate;

    public ResponseSocket server;
    TimeSpan timeout = new TimeSpan(0, 0, 0, 3, 0);

    // --- Throttled traffic logging (for debugging Scenic <-> Unity communication) ---
    //
    // Messages flow every ~0.1s and always differ slightly (positions, tick numbers, float
    // jitter), so comparing raw strings would treat every message as unique. Instead, each
    // message is reduced to its "shape": the same text with every number replaced by '#'.
    // A message is logged immediately when its shape differs from the last logged shape for
    // that direction (a new object, a new action/behavior name, a control flag flipping, a
    // field appearing or disappearing). Messages whose only differences are numeric are
    // suppressed. A separate heartbeat line, at most once per heartbeatSeconds, reports how
    // many messages went through so you can still see that traffic is flowing.
    private bool logTraffic = false;
    private double heartbeatSeconds = 10.0;
    private int logPreviewChars = 200;
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

    private static readonly System.Text.RegularExpressions.Regex NumberPattern =
        new System.Text.RegularExpressions.Regex(@"-?\d+(\.\d+)?([eE][-+]?\d+)?",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private class DirectionLog
    {
        public string label;
        public string lastShape = null;
        public double lastLogTime = double.NegativeInfinity;
        public int total = 0;
        public int sinceLastLog = 0;
    }

    private readonly DirectionLog receivedLog = new DirectionLog { label = "Scenic -> Unity" };
    private readonly DirectionLog sentLog = new DirectionLog { label = "Unity -> Scenic" };
    private double lastWaitingLogTime = double.NegativeInfinity;

    public ZMQRequester(string ip, string port, bool isServer)
    {
        this.ip = ip;
        this.port = port;
        this.isInit = false;
        this.isServer = isServer;
        data = null;
        readyToCommunicate = true;
    }

    public void SetLogging(bool enabled, float heartbeatIntervalSeconds, int previewChars)
    {
        logTraffic = enabled;
        heartbeatSeconds = Math.Max(0f, heartbeatIntervalSeconds);
        logPreviewChars = Math.Max(0, previewChars);
    }

    private static string Shape(string msg)
    {
        return msg == null ? null : NumberPattern.Replace(msg, "#");
    }

    private string Preview(string msg)
    {
        if (msg == null) return "<null>";
        if (logPreviewChars == 0 || msg.Length <= logPreviewChars) return msg;
        return msg.Substring(0, logPreviewChars) + "... (" + msg.Length + " chars)";
    }

    private void LogMessage(DirectionLog dir, string msg)
    {
        dir.total++;
        dir.sinceLastLog++;
        if (!logTraffic) return;

        double now = clock.Elapsed.TotalSeconds;
        string shape = Shape(msg);
        bool first = dir.total == 1;
        bool shapeChanged = !string.Equals(shape, dir.lastShape);
        bool heartbeatDue = heartbeatSeconds > 0 && now - dir.lastLogTime >= heartbeatSeconds;

        string reason;
        if (first) reason = "first message";
        else if (shapeChanged) reason = "content changed";
        else if (heartbeatDue) reason = "heartbeat";
        else return;

        Debug.Log("[ZMQ] " + dir.label + " #" + dir.total + " [" + reason + ", "
                  + (dir.sinceLastLog - 1) + " similar suppressed]: " + Preview(msg));
        dir.lastShape = shape;
        dir.lastLogTime = now;
        dir.sinceLastLog = 0;
    }

    private void LogReceived(string msg) { LogMessage(receivedLog, msg); }
    private void LogSent(string msg) { LogMessage(sentLog, msg); }

    // Called each time a receive attempt times out with nothing from Scenic.
    private void LogWaiting()
    {
        if (!logTraffic) return;
        double now = clock.Elapsed.TotalSeconds;
        if (now - lastWaitingLogTime < heartbeatSeconds) return;
        lastWaitingLogTime = now;
        Debug.LogWarning("[ZMQ] Waiting for Scenic on tcp://" + ip + ":" + port
                         + " (nothing received in the last " + timeout.TotalSeconds + "s; "
                         + receivedLog.total + " received, " + sentLog.total + " sent so far)");
    }

    protected override void Run()
    {
        ForceDotNet.Force(); //this prevents unity freezing idk why 
        if (isServer)
        {
            Debug.Log("Starting Scenic/Unity Server");
            using (server = new ResponseSocket())
            {
                server.Bind("tcp://"+ ip +":" + port);
                string message = null;
                string outMessage = null;
                //int outNum = 0;
                bool gotMessage = false;
                //understand what is going on here and try to terminate socket yet still keep the same thread running 
                // Debug.LogError("IN SERVER");
                while (true)
                {
                    // Debug.LogError(outData == null);
                    // Debug.LogError("IN TRUE");
                    data = null;
                    if (outData != null){
                        while (Running)
                        {
                            // Debug.LogError("I am receiving");
                            gotMessage = server.TryReceiveFrameString(timeout, out message);
                            if (gotMessage)
                            {
                                LogReceived(message);
                                data = message;
                                break;
                            }
                            LogWaiting();
                        }
                        if (message != null)
                        {
                            data = message;
                        } 
                        else
                        {
                            // Debug.LogError("Received scenic data is NULL");
                        }
                        outMessage = outData;
                        // Debug.LogError("OUT MSG: " + outMessage);
                        if (!readyToCommunicate) // we dont go into this
                        {
                            bool humanReady = false;
                            while (!humanReady)
                            {
                                if (readyToCommunicate)
                                {
                                    // Debug.Log("Ready to communicate");
                                    server.TrySendFrame(outMessage);
                                    LogSent(outMessage);
                                    Thread.Sleep(100);
                                    humanReady = true;
                                }
                            }
                            
                        }
                        else
                        {
                            // Debug.LogError("Already ready to communicate");
                            if (server.TrySendFrame(timeout, outMessage))
                            {
                                LogSent(outMessage);
                            }
                            else if (logTraffic)
                            {
                                Debug.LogWarning("[ZMQ] Unity -> Scenic send timed out after " + timeout.TotalSeconds + "s");
                            }
                            Thread.Sleep(100);
                            // outNum++;
                        }
                        
                    }
                    else
                    {
                        // Debug.LogError("Outdata is null. Zmq cannot load");
                    }
                }
            }
        }
        else // Should never enter here since Unity should always be server
        {
            using (RequestSocket client = new RequestSocket())
            {
                Debug.Log("Starting Unity/Scenic Client");
                //client.Connect("tcp://"+ ip +":" + port);
                client.Connect("tcp://127.0.0.1:5555");
                string message = null;
                string outMessage = null;
                bool gotMessage = false;
                while (true)
                {
                    Debug.Log(outData == null);
                    if (outData != null)
                    {
                        outMessage = outData;
                        client.TrySendFrame(outMessage);
                        while (Running)
                        {
                            gotMessage = client.TryReceiveFrameString(out message);
                            if (gotMessage) break;
                        }

                        if (message != null)
                        {
                            data = message;
                        }
                    }
                }
            }
        }

    }
    
    public string GetData()
    {
        return data;
    }
    public void SetSendData(string message)
    {
        outData = message;
    }
    public void SetReady(bool b)
    {
        readyToCommunicate = b;
    }
}