using System;
using UnityEngine;

[Serializable]
public sealed class TradesConfiguration
{
    [field: SerializeField] public int TradesCountLimit { get; set; } = 1000;
}

