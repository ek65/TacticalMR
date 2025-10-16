using System;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using System.Threading;

/// <summary>
/// Handles ZeroMQ network communication for the Scenic simulation system.
/// Manages bidirectional message exchange between Unity and external Scenic processes.
/// Runs in a background thread to avoid blocking Unity's main execution.
/// </summary>
public class ZMQRequester : RunAbleThread
{
    #region Private Fields
    private string ip;
    private string port;
    private bool isInit;
    private bool isServer;
    private string outData;
    private bool readyToCommunicate;

    public ResponseSocket server;
    TimeSpan timeout = new TimeSpan(0, 0, 0, 10, 0);
    #endregion

    #region Public Properties
    /// <summary>
    /// Data received from the remote Scenic process
    /// </summary>
    public string data;
    #endregion

    #region Constructor
    /// <summary>
    /// Initializes ZMQ communication with specified network parameters
    /// </summary>
    /// <param name="ip">IP address to bind/connect to</param>
    /// <param name="port">Port number for communication</param>
    /// <param name="isServer">Whether to act as server (bind) or client (connect)</param>
    public ZMQRequester(string ip, string port, bool isServer)
    {
        this.ip = ip;
        this.port = port;
        this.isInit = false;
        this.isServer = isServer;
        data = null;
        readyToCommunicate = true;
    }
    #endregion
    
    #region Thread Execution
    /// <summary>
    /// Main background thread execution method.
    /// Handles server/client communication loops with appropriate socket types.
    /// </summary>
    protected override void Run()
    {
        ForceDotNet.Force(); // Prevents Unity freezing with .NET threading
        
        if (isServer)
        {
            RunServerLoop();
        }
        else
        {
            RunClientLoop();
        }
    }

    /// <summary>
    /// Server communication loop - binds to port and waits for client connections
    /// </summary>
    private void RunServerLoop()
    {
        Debug.Log("Starting Scenic/Unity Server");
        using (server = new ResponseSocket())
        {
            server.Bind("tcp://" + ip + ":" + port);
            string message = null;
            string outMessage = null;
            bool gotMessage = false;

            while (true)
            {
                data = null;
                if (outData != null)
                {
                    // Wait for incoming message from Scenic
                    while (Running)
                    {
                        gotMessage = server.TryReceiveFrameString(timeout, out message);
                        if (gotMessage)
                        {
                            data = message;
                            break;
                        }
                    }
                    
                    if (message != null)
                    {
                        data = message;
                    }
                    
                    outMessage = outData;
                    
                    if (!readyToCommunicate)
                    {
                        // Wait for ready signal before sending
                        bool humanReady = false;
                        while (!humanReady)
                        {
                            if (readyToCommunicate)
                            {
                                server.TrySendFrame(outMessage);
                                Thread.Sleep(100);
                                humanReady = true;
                            }
                        }
                    }
                    else
                    {
                        // Send immediately if ready
                        server.TrySendFrame(timeout, outMessage);
                        Thread.Sleep(100);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Client communication loop - connects to server and exchanges messages
    /// Note: Currently unused as Unity typically acts as the server
    /// </summary>
    private void RunClientLoop()
    {
        using (RequestSocket client = new RequestSocket())
        {
            Debug.Log("Starting Unity/Scenic Client");
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
    #endregion
    
    #region Public Methods
    /// <summary>
    /// Gets the most recent data received from the remote process
    /// </summary>
    /// <returns>JSON string data from Scenic</returns>
    public string GetData()
    {
        return data;
    }

    /// <summary>
    /// Sets data to be sent to the remote process
    /// </summary>
    /// <param name="message">JSON string to send</param>
    public void SetSendData(string message)
    {
        outData = message;
    }

    /// <summary>
    /// Sets whether the system is ready to communicate
    /// </summary>
    /// <param name="b">Ready state</param>
    public void SetReady(bool b)
    {
        readyToCommunicate = b;
    }
    #endregion
}