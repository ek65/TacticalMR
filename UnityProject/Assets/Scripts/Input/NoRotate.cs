using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple utility component that prevents object rotation by continuously resetting to identity rotation.
/// Useful for UI elements, floating text, or other objects that should always maintain a fixed orientation.
/// Overrides any rotation changes that might be applied by other scripts or physics interactions.
/// </summary>
public class NoRotate : MonoBehaviour
{
    /// <summary>
    /// Initialize component - no setup required for this utility
    /// </summary>
    void Start()
    {
        // No initialization needed
    }

    /// <summary>
    /// Continuously reset rotation to identity (no rotation)
    /// Ensures the object maintains zero rotation on all axes regardless of external influences
    /// </summary>
    void Update()
    {
        transform.rotation = Quaternion.identity;
    }
}