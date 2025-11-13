using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Fusion;


public class ScenarioPlanExecutor : MonoBehaviour
{
    [Header("Prefab Registry (key → prefab)")]
    [SerializeField] private List<PrefabEntry> prefabs = new();
    [SerializeField] private Transform parentForSpawns;
    
    private readonly Dictionary<string, GameObject> nameRegistry = new(StringComparer.OrdinalIgnoreCase);

    [Serializable]
    public class PrefabEntry
    {
        public string key;        // e.g., "Cube"
        public GameObject prefab; // assign in Inspector
        // public string description;
    }
    
    /// <summary>
    /// Get list of valid prefab keys for LLM constraint
    /// </summary>
    public List<string> GetValidPrefabKeys()
    {
        return prefabs.Where(p => p.prefab != null && !string.IsNullOrWhiteSpace(p.key))
            .Select(p => p.key)
            .ToList();
    }
    
    private static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        s = s.Trim();
        // strip common Unity suffixes
        if (s.EndsWith(" (Clone)", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(0, s.Length - " (Clone)".Length);
        if (s.EndsWith(" (copy)", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(0, s.Length - " (copy)".Length);
        return s;
    }
    
    private GameObject FindExistingByName(string name)
    {
        name = Normalize(name);
        if (string.IsNullOrWhiteSpace(name)) return null;

        if (nameRegistry.TryGetValue(name, out var go) && go) return go;

        var byName = GameObject.Find(name);
        if (byName) return byName;

        // also try to match by stripping clone suffixes on scene objects
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (Normalize(t.gameObject.name).Equals(name, StringComparison.OrdinalIgnoreCase))
                    return t.gameObject;
            }
        }
        return null;
    }

    public void Apply(ScenarioPlan plan)
    {
        if (plan == null || plan.Objects == null) return;

        foreach (var obj in plan.Objects)
        {
            try
            {
                GameObject target = null;

                // If a Name exists and we can find it, target it (ignore prefab)
                if (!string.IsNullOrWhiteSpace(obj.Name))
                    target = FindExistingByName(obj.Name);

                if (target == null)
                {
                    // No existing object - spawn new one
                    target = SpawnAndConfigure(obj);
                }
                else
                {
                    // Applying to existing object - execute actions
                    ApplyActions(target, obj.Actions);
                }

                // Keep registry fresh
                if (target)
                {
                    var key = Normalize(target.name);
                    if (!string.IsNullOrEmpty(key))
                        nameRegistry[key] = target;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Executor Apply() failed for '{obj?.Name ?? obj?.Prefab}': {e}");
            }
        }
    }
    
    /// <summary>
    /// Execute actions on a GameObject by calling methods on its ActionAPI component
    /// </summary>
    private void ApplyActions(GameObject go, List<ActionCall> actions)
    {
        if (actions == null || actions.Count == 0) return;

        var api = go.GetComponent<ActionAPI>();
        if (api == null)
        {
            Debug.LogWarning($"No ActionAPI on '{go.name}' - cannot execute actions");
            return;
        }
        
        foreach (var action in actions)
        {
            if (string.IsNullOrWhiteSpace(action?.Name)) continue;
            
            Debug.Log($"[Executor] Executing action '{action.Name}' on '{go.name}'");

            // Find the method on ActionAPI
            var method = typeof(ActionAPI).GetMethod(action.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (method == null)
            {
                Debug.LogWarning($"[Executor] Action method '{action.Name}' not found on ActionAPI");
                continue;
            }

            // Get method parameters
            var parms = method.GetParameters();
            var finalArgs = new object[parms.Length];

            Debug.Log($"[Executor] Method '{action.Name}' has {parms.Length} parameters");

            // Map parameters by name
            for (int i = 0; i < parms.Length; i++)
            {
                var param = parms[i];
                object src = null;
                
                if (action.Parameters != null && action.Parameters.TryGetValue(param.Name, out src))
                {
                    Debug.Log($"[Executor]   Param '{param.Name}': {src} (raw type: {src?.GetType().Name})");
                }
                else
                {
                    // Parameter not provided - use default value if available
                    if (param.HasDefaultValue)
                    {
                        finalArgs[i] = param.DefaultValue;
                        Debug.Log($"[Executor]   Param '{param.Name}': using default value {param.DefaultValue}");
                        continue;
                    }
                    else
                    {
                        Debug.LogWarning($"[Executor]   Param '{param.Name}': not provided and no default value");
                    }
                }
                
                finalArgs[i] = ConvertArg(src, param.ParameterType);
                Debug.Log($"[Executor]   Converted to: {finalArgs[i]} (type: {finalArgs[i]?.GetType().Name})");
            }

            // Invoke the method
            try
            {
                method.Invoke(api, finalArgs);
                Debug.Log($"[Executor] ✓ Successfully invoked '{action.Name}' on '{go.name}'");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Executor] Failed to invoke '{action.Name}': {e.Message}\n{e.StackTrace}");
            }
        }
    }

    private GameObject SpawnAndConfigure(SceneObject spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Prefab))
            throw new Exception("Prefab is required to spawn.");

        var prefab = FindPrefab(spec.Prefab);
        if (!prefab) throw new Exception($"Prefab '{spec.Prefab}' not found.");

        var pos = ToVector3(spec.Position);
        var rot = Quaternion.Euler(ToVector3(spec.Rotation));
        var scl = ToVector3(spec.Scale);

        GameObject go;

        NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
        
        // Fusion-authoritative spawn
        var netObj = runner.Spawn(prefab, pos, rot);
        go = netObj.gameObject;

        if (parentForSpawns) go.transform.SetParent(parentForSpawns, true);
        go.transform.localScale = scl;

        // Name is required
        if (string.IsNullOrWhiteSpace(spec.Name))
            throw new Exception("Name is required for all objects.");

        // Set name via network-safe APIs
        var pI = go.GetComponent<PlayerInterface>();
        var gI = go.GetComponent<GoalInterface>();
        var bI = go.GetComponent<BallInterface>();

        if (pI != null)
            pI.SetObjectName(spec.Name);
        else if (gI != null)
            gI.SetObjectName(spec.Name);
        else if (bI != null)
            bI.SetObjectName(spec.Name);
        else
            go.name = Normalize(spec.Name);

        // Execute any initial actions
        ApplyActions(go, spec.Actions);

        var key = Normalize(go.name);
        if (!string.IsNullOrEmpty(key)) nameRegistry[key] = go;

        return go;
    }
    
    // JSON → C# argument conversion (numbers, strings, Vector3)
    private object ConvertArg(object val, Type targetType)
    {
        if (val == null) 
        {
            Debug.Log($"[ConvertArg] Value is null, returning default for {targetType.Name}");
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
        
        // Handle Vector3 conversion
        if (targetType == typeof(Vector3))
        {
            Debug.Log($"[ConvertArg] Converting to Vector3 from {val.GetType().Name}");
            
            // Handle JObject (from JSON deserialization)
            if (val is JObject jobj)
            {
                float x = jobj["x"]?.ToObject<float>() ?? 0f;
                float y = jobj["y"]?.ToObject<float>() ?? 0f;
                float z = jobj["z"]?.ToObject<float>() ?? 0f;
                Debug.Log($"[ConvertArg] JObject -> Vector3({x}, {y}, {z})");
                return new Vector3(x, y, z);
            }
            
            // Handle Dictionary<string, object>
            if (val is Dictionary<string, object> dict)
            {
                float x = dict.ContainsKey("x") ? Convert.ToSingle(dict["x"]) : 0f;
                float y = dict.ContainsKey("y") ? Convert.ToSingle(dict["y"]) : 0f;
                float z = dict.ContainsKey("z") ? Convert.ToSingle(dict["z"]) : 0f;
                Debug.Log($"[ConvertArg] Dictionary -> Vector3({x}, {y}, {z})");
                return new Vector3(x, y, z);
            }
            
            // Handle JArray [x,y,z]
            if (val is JArray jarr && jarr.Count == 3)
            {
                var result = new Vector3(jarr[0].ToObject<float>(), jarr[1].ToObject<float>(), jarr[2].ToObject<float>());
                Debug.Log($"[ConvertArg] JArray -> Vector3({result})");
                return result;
            }
            
            Debug.LogWarning($"[ConvertArg] Could not convert {val.GetType().Name} to Vector3");
        }

        // Handle primitive type conversions
        if (val is IConvertible)
        {
            try { 
                var result = Convert.ChangeType(val, targetType);
                Debug.Log($"[ConvertArg] IConvertible conversion successful: {result}");
                return result;
            } 
            catch (Exception e) { 
                Debug.LogWarning($"[ConvertArg] IConvertible conversion failed: {e.Message}");
            }
        }

        // Fallback: try JSON re-hydration
        try
        {
            var token = val as JToken ?? JToken.FromObject(val);
            var result = token.ToObject(targetType);
            Debug.Log($"[ConvertArg] JSON rehydration successful: {result}");
            return result;
        }
        catch (Exception e) 
        { 
            Debug.LogWarning($"[ConvertArg] JSON rehydration failed: {e.Message}");
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null; 
        }
    }

    private GameObject FindPrefab(string key)
        => prefabs.FirstOrDefault(p => string.Equals(p.key, key, StringComparison.OrdinalIgnoreCase))?.prefab;

    private static Vector3 ToVector3(float[] arr)
    {
        if (arr == null || arr.Length != 3)
            throw new Exception("Invalid array for Vector3 conversion - must have exactly 3 elements");
        return new Vector3(arr[0], arr[1], arr[2]);
    }

    private static bool TryParseColor(string value, out Color c)
    {
        c = Color.white;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (ColorUtility.TryParseHtmlString(value, out c)) return true; // supports #RRGGBB
        switch (value.ToLowerInvariant())
        {
            case "red": c = Color.red; return true;
            case "green": c = Color.green; return true;
            case "blue": c = Color.blue; return true;
            case "yellow": c = Color.yellow; return true;
            case "white": c = Color.white; return true;
            case "black": c = Color.black; return true;
            case "gray": c = Color.gray; return true;
        }
        return false;
    }

    private static void ApplyParameters(Component comp, Dictionary<string, object> parameters)
    {
        if (parameters == null || parameters.Count == 0) return;

        var type = comp.GetType();
        foreach (var kv in parameters)
        {
            var name = kv.Key;
            var value = kv.Value;

            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite && TryCoerce(prop.PropertyType, value, out var pv))
            { prop.SetValue(comp, pv); continue; }

            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (field != null && TryCoerce(field.FieldType, value, out var fv))
            { field.SetValue(comp, fv); }
        }
    }

    private static bool TryCoerce(Type target, object value, out object coerced)
    {
        coerced = null;
        if (value == null) return false;
        try
        {
            if (target == typeof(string))  { coerced = value.ToString(); return true; }
            if (target == typeof(int))     { coerced = Convert.ToInt32(value); return true; }
            if (target == typeof(float))   { coerced = Convert.ToSingle(value); return true; }
            if (target == typeof(double))  { coerced = Convert.ToDouble(value); return true; }
            if (target == typeof(bool))    { coerced = Convert.ToBoolean(value); return true; }
            if (target == typeof(Vector3) && value is Newtonsoft.Json.Linq.JArray arr && arr.Count == 3)
            { coerced = new Vector3((float)arr[0], (float)arr[1], (float)arr[2]); return true; }
            coerced = Convert.ChangeType(value, target);
            return true;
        }
        catch { return false; }
    }

    private static Type FindTypeInAssemblies(string simpleName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(simpleName, false, true);
            if (t != null) return t;
        }
        return null;
    }
}