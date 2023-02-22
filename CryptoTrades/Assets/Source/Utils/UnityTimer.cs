using System;
using UnityEngine;

public class UnityTimer : MonoBehaviour
{
    [field: SerializeField] public float Interval { get; set; } = 1.0f;
    public event Action Elapsed;
    private bool _stopped = true;
    private float _timePassed = 0;

    void Update()
    {
        if (_stopped)
            return;
        if (_timePassed >= Interval)
        {
            _timePassed = 0;
            Elapsed?.Invoke();
        }
        else
        {
            _timePassed += Time.deltaTime;
        }
    }

    public void StopTimer() => _stopped = true;
    public void StartTimer() => _stopped = false;
}