using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Interface contract for any object that can be controlled by the Scenic simulation system.
/// Provides a standardized method for applying movement and behavior updates to game objects.
/// </summary>
public interface IObjectInterface
{
    /// <summary>
    /// Apply a movement or behavior update produced by the Scenic simulation engine.
    /// Implementations should handle the parsing and execution of movement data,
    /// including position updates, animation triggers, and behavior state changes.
    /// </summary>
    /// <param name="data">Movement data containing position, actions, and behavior information</param>
    void ApplyMovement(ScenicMovementData data);
}