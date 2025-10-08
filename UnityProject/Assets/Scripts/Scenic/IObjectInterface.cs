using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Interface for any Scenic-controlled object.
/// </summary>
public interface IObjectInterface
{
    /// <summary>
    /// Apply a movement or behavior update produced by Scenic.
    /// </summary>
    void ApplyMovement(ScenicMovementData data);
}