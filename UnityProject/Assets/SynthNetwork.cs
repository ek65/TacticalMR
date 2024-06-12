using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Firebase.Firestore;
using Firebase.Extensions;

public class SynthNetwork : MonoBehaviour
{
    [Header("Service")]
    [SerializeField] private string id;
    private FirebaseFirestore db;

    // Start is called before the first frame update
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        DocumentReference docRef = db.Collection("services").Document(id);
        docRef.Listen(snapshot => {
            if (snapshot.Exists)
            {
                Debug.Log("Callback received document snapshot.");
                Debug.Log(String.Format("Document data for {0} document:", snapshot.Id));
                Dictionary<string, object> data = snapshot.ToDictionary();

                // Convert Dictionary to JSON
                string json = JsonConvert.SerializeObject(data);

                // Deserialize JSON to Service object
                SynthService service = JsonConvert.DeserializeObject<SynthService>(json);

                // Print out the Service object properties
                Debug.Log($"ID: {service.Id}");
                Debug.Log("InterfaceIN: " + string.Join(", ", service.InterfaceIN));
                Debug.Log("InterfaceOUT: " + string.Join(", ", service.InterfaceOUT));
                Debug.Log("DeviceIN: " + string.Join(", ", service.DeviceIN));
                Debug.Log("DeviceOUT: " + string.Join(", ", service.DeviceOUT));
                Debug.Log($"LastUpdated: {service.LastUpdated}");

                var timestamp = (Timestamp)data["lastUpdated"];
                var myDateTime = timestamp.ToDateTime();

                Debug.Log(data["lastUpdated"]);
                Debug.Log(data["lastUpdated"].GetType());
                Debug.Log(myDateTime);
            }
            else
            {
                Debug.Log("No such document!");
            }
        });
    }

    void Upload() {
        DocumentReference docRef = db.Collection("cities").Document("LA");
        Dictionary<string, object> city = new Dictionary<string, object>
        {
                { "Name", "Los Angeles" },
                { "State", "CA" },
                { "Country", "USA" }
        };
        docRef.SetAsync(city).ContinueWithOnMainThread(task => {
                Debug.Log("Added data to the LA document in the cities collection.");
        });
    }

    // Update is called once per frame
    void UploadTask(type: string, content: string)
    {
        ServiceTask serviceTask = new ServiceTask(Guid.NewGuid().ToString(), type, content)
        DocumentReference docRef = db.Collection("services").Document(id).Collection("tasks").Document(serviceTask.id);
        docRef.SetAsync(serviceTask);
    }
}

public class ServiceTask {
    public string id { get; set;}
    public string type { get; set;}
    public string content { get; set;}
}

public class SynthService
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("interfaceIN")]
    public List<string> InterfaceIN { get; set; }

    [JsonProperty("interfaceOUT")]
    public List<string> InterfaceOUT { get; set; }

    [JsonProperty("deviceIN")]
    public List<string> DeviceIN { get; set; }

    [JsonProperty("deviceOUT")]
    public List<string> DeviceOUT { get; set; }

    [JsonProperty("lastUpdated")]
    public Timestamp LastUpdated { get; set; }

    public SynthService()
    {
        Id = Guid.NewGuid().ToString();
        InterfaceIN = new List<string>();
        InterfaceOUT = new List<string>();
        DeviceIN = new List<string>();
        DeviceOUT = new List<string>();
        LastUpdated = Timestamp.GetCurrentTimestamp();
    }
}

public class TestService
{
    public static void Main()
    {
        // Example JSON string
        string jsonString = "{\"id\":\"some-unique-id\",\"interfaceIN\":[\"input1\",\"input2\"],\"interfaceOUT\":[\"output1\"],\"deviceIN\":[\"device1\"],\"deviceOUT\":[\"device2\"],\"lastUpdated\":\"2024-06-12T10:00:00Z\"}";

        // Deserialize JSON to Service object
        SynthService service = JsonConvert.DeserializeObject<SynthService>(jsonString);

        // Print out the Service object properties
        Console.WriteLine($"ID: {service.Id}");
        Console.WriteLine("InterfaceIN: " + string.Join(", ", service.InterfaceIN));
        Console.WriteLine("InterfaceOUT: " + string.Join(", ", service.InterfaceOUT));
        Console.WriteLine("DeviceIN: " + string.Join(", ", service.DeviceIN));
        Console.WriteLine("DeviceOUT: " + string.Join(", ", service.DeviceOUT));
        //Console.WriteLine($"LastUpdated: {service.LastUpdated}");

        // Serialize Service object back to JSON
        string serializedJson = JsonConvert.SerializeObject(service, Formatting.Indented);
        Console.WriteLine("Serialized JSON: " + serializedJson);
    }
}
