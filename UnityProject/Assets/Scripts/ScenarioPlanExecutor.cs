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

                // **Key rule**: if a Name exists and we can find it, target it (ignore prefab)
                if (!string.IsNullOrWhiteSpace(obj.Name))
                    target = FindExistingByName(obj.Name);

                if (target == null)
                {
                    // no existing with that name — only now we consider spawning
                    target = SpawnAndConfigure(obj);         // sets name once, registers
                }
                else
                {
                    // applying to existing — DO NOT rename here
                    ApplyAttributes(target, obj.Attributes);
                    ApplyBehaviorsOrActions(target, obj.Behaviors);  // see section 2B from earlier
                }

                // keep registry fresh
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
    
    private void ApplyBehaviorsOrActions(GameObject go, List<BehaviorSpec> behaviors)
    {
        if (behaviors == null) return;

        var api = go.GetComponent<ActionAPI>(); // may be null for some prefabs
        foreach (var b in behaviors)
        {
            if (string.IsNullOrWhiteSpace(b?.Name)) continue;

            bool invokedAction = false;
            if (api != null)
            {
                // Try ActionAPI method first
                var m = typeof(ActionAPI).GetMethod(b.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (m != null)
                {
                    var parms = m.GetParameters();
                    var finalArgs = new object[parms.Length];

                    for (int i = 0; i < parms.Length; i++)
                    {
                        object src = null;
                        if (b.Parameters != null)
                        {
                            // common keys e.g. "destinationPosition","speed","lookAt"
                            // but we also accept positional [0],[1],[2] if you choose later
                            var p = parms[i];
                            b.Parameters.TryGetValue(p.Name, out src);
                        }
                        finalArgs[i] = ConvertArg(src, parms[i].ParameterType);
                    }

                    m.Invoke(api, finalArgs);
                    invokedAction = true;
                }
            }

            if (!invokedAction)
            {
                // Fallback to your existing "add component by type name" path
                var type = Type.GetType(b.Name) ?? FindTypeInAssemblies(b.Name);
                if (type == null || !typeof(Component).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"Behavior '{b.Name}' not found as ActionAPI method or Component.");
                    continue;
                }
                var comp = go.GetComponent(type) ?? go.AddComponent(type);
                ApplyParameters(comp, b.Parameters);
            }
        }
    }
    
    private void ConfigureExisting(GameObject go, SceneObject spec)
    {
        ApplyAttributes(go, spec.Attributes); // already in your file
        ApplyBehaviorsOrActions(go, spec.Behaviors); // new helper below
    }

    private GameObject SpawnAndConfigure(SceneObject spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Prefab))
            throw new Exception("Prefab is required to spawn.");

        var prefab = FindPrefab(spec.Prefab);
        if (!prefab) throw new Exception($"Prefab '{spec.Prefab}' not found.");

        var pos = ToVector3(spec.Position, new Vector3(0, 0, 5));
        var rot = Quaternion.Euler(ToVector3(spec.Rotation, Vector3.zero));
        var scl = ToVector3(spec.Scale, Vector3.one);

        GameObject go;

        NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
        
        // Fusion-authoritative spawn
        var netObj = runner.Spawn(prefab, pos, rot);
        go = netObj.gameObject;

        // optional: organize under parent without breaking networking
        if (parentForSpawns) go.transform.SetParent(parentForSpawns, true);

        go.transform.localScale = scl;

        // Name setting for networked objects:
        // Prefer the component's own network-safe naming API (e.g., SetObjectName / RPC)
        if (!string.IsNullOrWhiteSpace(spec.Name))
        {
            var pI = go.GetComponent<PlayerInterface>();
            var gI = go.GetComponent<GoalInterface>();
            var bI = go.GetComponent<BallInterface>();

            if (pI != null)
                pI.SetObjectName(spec.Name);     // your components already do this post-Spawned
            else if (gI != null)
                gI.SetObjectName(spec.Name);
            else if (bI != null)
                bI.SetObjectName(spec.Name);
            else
                go.name = Normalize(spec.Name);  // non-networked / generic case
        }

        ApplyAttributes(go, spec.Attributes);
        ApplyBehaviorsOrActions(go, spec.Behaviors);

        var key = Normalize(go.name);
        if (!string.IsNullOrEmpty(key)) nameRegistry[key] = go;

        return go;
    }
    
    // resolve "ref" to an existing GO
    private GameObject ResolveTargets(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;

        // Prefer registry
        if (nameRegistry.TryGetValue(reference, out var regGo) && regGo) return regGo;

        // Fallback: Unity Find (name) or tag
        var byName = GameObject.Find(reference);
        if (byName) return byName;

        try { return GameObject.FindGameObjectWithTag(reference); } catch { }

        return null;
    }
    
    // reflect into ActionAPI and call methods by name with converted args
    private void ExecuteActions(GameObject target, List<ActionCall> actions)
    {
        var api = target.GetComponent<ActionAPI>();
        if (!api)
        {
            Debug.LogWarning($"No ActionAPI on '{target.name}', cannot run actions.");
            return;
        }

        var type = api.GetType();
        foreach (var call in actions)
        {
            if (string.IsNullOrWhiteSpace(call.Func)) continue;

            var method = type.GetMethod(call.Func, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (method == null)
            {
                Debug.LogWarning($"Action '{call.Func}' not found on ActionAPI.");
                continue;
            }

            var parms = method.GetParameters();
            var finalArgs = new object[parms.Length];

            for (int i = 0; i < parms.Length; i++)
            {
                object src = i < call.Args.Count ? call.Args[i] : null;
                finalArgs[i] = ConvertArg(src, parms[i].ParameterType);
            }

            method.Invoke(api, finalArgs);
        }
    }
    
    // JSON → C# argument conversion (numbers, strings, Vector3)
    private object ConvertArg(object val, Type targetType)
    {
        if (targetType == typeof(Vector3))
        {
            // Accept {x:..,y:..,z:..} or [x,y,z]
            if (val is JObject jobj)
            {
                float x = jobj["x"]?.ToObject<float>() ?? 0f;
                float y = jobj["y"]?.ToObject<float>() ?? 0f;
                float z = jobj["z"]?.ToObject<float>() ?? 0f;
                return new Vector3(x, y, z);
            }
            if (val is JArray jarr && jarr.Count == 3)
            {
                return new Vector3(jarr[0].ToObject<float>(), jarr[1].ToObject<float>(), jarr[2].ToObject<float>());
            }
        }

        if (val is IConvertible)
        {
            try { return Convert.ChangeType(val, targetType); } catch { }
        }

        // Fallback: try JSON re-hydration
        try
        {
            var token = val as JToken ?? JToken.FromObject(val);
            return token.ToObject(targetType);
        }
        catch { return targetType.IsValueType ? Activator.CreateInstance(targetType) : null; }
    }

    private GameObject FindPrefab(string key)
        => prefabs.FirstOrDefault(p => string.Equals(p.key, key, StringComparison.OrdinalIgnoreCase))?.prefab;

    private static Vector3 ToVector3(float[] arr, Vector3 fallback)
        => (arr == null || arr.Length != 3) ? fallback : new Vector3(arr[0], arr[1], arr[2]);

    private static void ApplyAttributes(GameObject go, List<AttrKV> attrs)
    {
        if (attrs == null) return;
        foreach (var a in attrs)
        {
            if (string.IsNullOrWhiteSpace(a?.Key)) continue;
            switch (a.Key.ToLowerInvariant())
            {
                case "color":
                    if (TryParseColor(a.Value, out var color))
                    {
                        var r = go.GetComponentInChildren<Renderer>();
                        if (r && r.material) r.material.color = color;
                    }
                    break;
                case "tag":
                    if (!string.IsNullOrEmpty(a.Value)) go.tag = a.Value;
                    break;
                case "layer":
                    if (int.TryParse(a.Value, out var layer)) go.layer = layer;
                    break;
                default:
                    break;
            }
        }
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
