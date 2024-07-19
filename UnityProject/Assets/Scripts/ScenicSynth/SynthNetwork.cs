using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Storage;
using System.IO;

namespace SynthNetworkKit
{
    public class SynthNetwork : MonoBehaviour
    {
        [Header("Service")] [SerializeField] private string id;
        private FirebaseFirestore db;
        private SynthService service;

        private FirebaseStorage storage;
        private StorageReference storageRef;

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("Starting SynthNetwork.");
            db = FirebaseFirestore.DefaultInstance;

            storage = FirebaseStorage.DefaultInstance;
            storageRef = storage.GetReferenceFromUrl("gs://scenicsynth.appspot.com");

            // DocumentReference docRef2 = db.Collection("cities").Document("LA");
            // Dictionary<string, object> city = new Dictionary<string, object>
            // {
            //         { "Name", "Los Angeles" },
            //         { "State", "CA" },
            //         { "Country", "USA" }
            // };
            // docRef2.SetAsync(city).ContinueWithOnMainThread(task => {
            //         Debug.Log("Added data to the LA document in the cities collection.");
            // });

            var docRef = db.Collection("services").Document(id);
            print(docRef);
            Debug.Log("Listening to document: " + id);
            docRef.Listen(snapshot => {
                if (snapshot.Exists)
                {
                    Debug.Log("Callback received document snapshot.");
                    Debug.Log(String.Format("Document data for {0} document:", snapshot.Id));
                    Dictionary<string, object> data = snapshot.ToDictionary();

                    string json = JsonConvert.SerializeObject(data);
                    service = JsonConvert.DeserializeObject<SynthService>(json);

                    if (service == null)
                    {
                        Debug.LogError("Failed to deserialize SynthService.");
                        return;
                    }

                    Debug.Log($"ID: {service.Id}");
                    Debug.Log("InterfaceIN: " + string.Join(", ", service.InterfaceIN));
                    Debug.Log("InterfaceOUT: " + string.Join(", ", service.InterfaceOUT));
                    Debug.Log("DeviceIN: " + string.Join(", ", service.DeviceIN));
                    Debug.Log("DeviceOUT: " + string.Join(", ", service.DeviceOUT));
                    Debug.Log($"LastUpdated: {service.LastUpdated}");
                }
                else
                {
                    Debug.Log("No such document!");
                }
            });
        }


        // Update is called once per frame
        void Update()
        {

        }

        public void UploadTask(string type, string content)
        {
            Debug.Log(content);

            ServiceTask serviceTask = new ServiceTask(type, content);
            Debug.Log("ID");
            Debug.Log(id);
            
            DocumentReference docRef = db.Collection("services").Document(id).Collection("tasks")
                .Document(serviceTask.id);
            docRef.SetAsync(serviceTask);
            

            print("hi");
            print(service.InterfaceIN);
            service.InterfaceIN.Add(serviceTask.id);
            print(service.InterfaceIN);

            DocumentReference serviceRef = db.Collection("services").Document(id);
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "interfaceIN", service.InterfaceIN },
                { "lastUpdated", DateTime.Now }
            };

            serviceRef.UpdateAsync(updates).ContinueWithOnMainThread(task =>
            {
                Debug.Log("Updated the Capital field of the new-city-id document in the cities collection.");
            });
        }

        public void StoreScene(string data, string id) {
            // Create a secure file path in the persistent data path
            string filePath = Path.Combine(Application.persistentDataPath, id + ".json");

            // Write the JSON data to the file
            File.WriteAllText(filePath, data);

            // Define the Firebase Storage reference path
            StorageReference fileRef = storageRef.Child("scenes/" + id + ".json");

            // Upload the file to Firebase Storage
            fileRef.PutFileAsync(filePath).ContinueWithOnMainThread(task => {
                if (task.IsCompleted) {
                    Debug.Log("File uploaded successfully.");
                } else {
                    Debug.LogError("File upload failed: " + task.Exception);
                }
            });
        }
    }


    [FirestoreData]
public class ServiceTask {

    [FirestoreProperty]
    public string id { get; set; }
    [FirestoreProperty]
    public string type { get; set; }
    [FirestoreProperty]
    public string content { get; set; }
    
    public ServiceTask(string type, string content)
    {
        id = Guid.NewGuid().ToString();
        this.type = type;
        this.content = content;
    }
    
    public ServiceTask()
    {
    }
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
        Console.WriteLine($"LastUpdated: {service.LastUpdated}");

        // Serialize Service object back to JSON
        string serializedJson = JsonConvert.SerializeObject(service, Formatting.Indented);
        Console.WriteLine("Serialized JSON: " + serializedJson);
    }
}

    }
