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
    [JsonProperty("name")]       public string Name; // optional
    [JsonProperty("position")]   public float[] Position; // [x,y,z]
    [JsonProperty("rotation")]   public float[] Rotation; // [x,y,z]
    [JsonProperty("scale")]      public float[] Scale;    // [x,y,z]
    [JsonProperty("attributes")] public List<AttrKV> Attributes = new();
    [JsonProperty("behaviors")]  public List<BehaviorSpec> Behaviors = new();
}

[Serializable]
public class AttrKV
{
    [JsonProperty("key")]   public string Key;
    [JsonProperty("value")] public string Value;
}

[Serializable]
public class BehaviorSpec
{
    [JsonProperty("name")]       public string Name;
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