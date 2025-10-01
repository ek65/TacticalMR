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

    public ZMQRequester(string ip, string port, bool isServer)
    {
        this.ip = ip;
        this.port = port;
        this.isInit = false;
        this.isServer = isServer;
        data = null;
        readyToCommunicate = true;
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
                                // Debug.Log(gotMessage);
                                data = message;
                                break;
                            }
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
                                    Thread.Sleep(100);
                                    humanReady = true;
                                }
                            }
                            
                        }
                        else
                        {
                            // Debug.LogError("Already ready to communicate");
                            server.TrySendFrame(timeout, outMessage);
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