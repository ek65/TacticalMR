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
            using (ResponseSocket server = new ResponseSocket())
            {
                //client.Connect("tcp://"+ ip +":" + port);
                server.Bind("tcp://127.0.0.1:5555");
                string message = null;
                string outMessage = null;
                int outNum = 0;
                bool gotMessage = false;
                //understand what is going on here and try to terminate socket yet still keep the same thread running 
                while (true)
                {
                    //Debug.Log(outData == null);
                    if (outNum != null){
                        while (Running)
                        {
                            //Debug.Log("I am receiving");
                            gotMessage = server.TryReceiveFrameString(out message);
                            if (gotMessage)
                            {
                                Debug.Log(gotMessage);
                                data = message;
                                break;
                            }
                        }

                        outMessage = outNum.ToString();
                        if (!readyToCommunicate)
                        {
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
                            server.TrySendFrame(outMessage);
                            //Debug.Log(outMessage);
                            
                            outNum++;
                            Thread.Sleep(100);
                        }
                        
                    }
                    else
                    {
                        Debug.LogError("Outdata is null. Zmq cannot load");
                    }
                }
            }
        }
        else {
            using (RequestSocket client = new RequestSocket())
            {
                //client.Connect("tcp://"+ ip +":" + port);
                client.Connect("tcp://127.0.0.1:5555");
                string message = null;
                string outMessage = null;
                bool gotMessage = false;
                while (true)
                {
                    if (outData != null){
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