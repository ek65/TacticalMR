using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ScenarioPlanExecutor : MonoBehaviour
{
    [Header("Prefab Registry (key → prefab)")]
    [SerializeField] private List<PrefabEntry> prefabs = new();
    [SerializeField] private Transform parentForSpawns;

    [Serializable]
    public class PrefabEntry
    {
        public string key;        // e.g., "Cube"
        public GameObject prefab; // assign in Inspector
    }

    public void Apply(ScenarioPlan plan)
    {
        if (plan == null || plan.Objects == null) return;

        foreach (var obj in plan.Objects)
        {
            try { SpawnAndConfigure(obj); }
            catch (Exception e) { Debug.LogError($"Spawn '{obj?.Prefab}' failed: {e.Message}"); }
        }
    }

    private void SpawnAndConfigure(SceneObject spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Prefab))
            throw new Exception("Prefab is required.");

        var prefab = FindPrefab(spec.Prefab);
        if (!prefab) throw new Exception($"Prefab '{spec.Prefab}' not found in registry.");

        // Defaults align with the prompt
        var pos = ToVector3(spec.Position, new Vector3(0, 0, 5));
        var rot = Quaternion.Euler(ToVector3(spec.Rotation, Vector3.zero));
        var scl = ToVector3(spec.Scale, Vector3.one);

        var go = Instantiate(prefab, pos, rot, parentForSpawns ? parentForSpawns : null);
        go.transform.localScale = scl;
        if (!string.IsNullOrEmpty(spec.Name)) go.name = spec.Name;

        ApplyAttributes(go, spec.Attributes);

        foreach (var b in spec.Behaviors)
        {
            if (string.IsNullOrWhiteSpace(b.Name)) continue;
            var type = Type.GetType(b.Name) ?? FindTypeInAssemblies(b.Name);
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                Debug.LogWarning($"Behavior '{b.Name}' not found as a Component type.");
                continue;
            }
            var comp = go.GetComponent(type) ?? go.AddComponent(type);
            ApplyParameters(comp, b.Parameters);
        }
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
