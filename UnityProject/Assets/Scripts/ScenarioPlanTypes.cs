using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class ScenarioPlan
{
    [JsonProperty("objects")] public List<SceneObject> Objects = new();
}

[Serializable]
public class SceneObject
{
    [JsonProperty("prefab")]     public string Prefab;
    [JsonProperty("name")]       
    [JsonRequired]               public string Name; // REQUIRED - every object must have a name
    [JsonProperty("position")]   public float[] Position; // [x,y,z]
    [JsonProperty("rotation")]   public float[] Rotation; // [x,y,z]
    [JsonProperty("scale")]      public float[] Scale;    // [x,y,z]
    [JsonProperty("actions")]    public List<ActionCall> Actions = new();
}

[Serializable]
public class ActionCall
{
    [JsonProperty("name")]       public string Name;  // e.g., "MoveToPos"
    [JsonProperty("parameters")] public Dictionary<string, object> Parameters = new();
}

public static class ScenarioPlanParser
{
    public static bool TryParse(string json, out ScenarioPlan plan, out string error)
    {
        plan = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Empty JSON";
            return false;
        }

        try
        {
            plan = JsonConvert.DeserializeObject<ScenarioPlan>(json);
            if (plan?.Objects == null)
            {
                error = "Missing 'objects' array.";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}