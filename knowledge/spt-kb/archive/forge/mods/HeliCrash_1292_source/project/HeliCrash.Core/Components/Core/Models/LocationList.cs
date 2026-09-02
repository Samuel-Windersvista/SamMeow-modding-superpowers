using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using ZLinq;

namespace SamSWAT.HeliCrash.ArysReloaded.Models;

public class LocationList : List<Location>
{
    public Vector3[] Positions { get; private set; }
    public Quaternion[] Rotations { get; private set; }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        Positions = this.AsValueEnumerable().Select(location => location.Position).ToArray();
        Rotations = this.AsValueEnumerable()
            .Select(location => Quaternion.Euler(location.Rotation))
            .ToArray();
    }
}
