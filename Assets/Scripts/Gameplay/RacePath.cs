using UnityEngine;
using System.Collections.Generic;

public class RacePath : MonoBehaviour
{
    [SerializeField] private List<Transform> _waypoints = new List<Transform>();
    [SerializeField] private Color _debugColor = Color.yellow;

    private float _totalDistance;
    private float[] _accumulatedDistances;

    private void Awake()
    {
        CalculatePathData();
    }

    [ContextMenu("Auto Find Children as Waypoints")]
    public void FindWaypoints()
    {
        _waypoints.Clear();
        foreach (Transform child in transform) _waypoints.Add(child);
    }

    private void CalculatePathData()
    {
        if (_waypoints.Count < 2) return;

        _accumulatedDistances = new float[_waypoints.Count];
        _totalDistance = 0f;
        _accumulatedDistances[0] = 0f;

        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            float dist = Vector3.Distance(_waypoints[i].position, _waypoints[i + 1].position);
            _totalDistance += dist;
            _accumulatedDistances[i + 1] = _totalDistance;
        }
    }

    public float GetDistanceTraveled(Vector3 racerPos)
    {
        if (_waypoints.Count < 2) return 0f;

        int bestSegmentIndex = 0;
        float minSqrDist = float.MaxValue;
        float segmentProgress = 0f;

        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            Vector3 p1 = _waypoints[i].position;
            Vector3 p2 = _waypoints[i + 1].position;
            Vector3 segmentVec = p2 - p1;
            Vector3 pointVec = racerPos - p1;

            float t = Mathf.Clamp01(Vector3.Dot(pointVec, segmentVec) / segmentVec.sqrMagnitude);
            Vector3 closestPoint = p1 + segmentVec * t;
            float sqrDist = (racerPos - closestPoint).sqrMagnitude;

            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                bestSegmentIndex = i;
                segmentProgress = t;
            }
        }

        float distInSegment = segmentProgress * Vector3.Distance(_waypoints[bestSegmentIndex].position, _waypoints[bestSegmentIndex + 1].position);
        return _accumulatedDistances[bestSegmentIndex] + distInSegment;
    }

    private void OnDrawGizmos()
    {
        if (_waypoints == null || _waypoints.Count < 2) return;

        Gizmos.color = _debugColor;
        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            if (_waypoints[i] != null && _waypoints[i + 1] != null)
                Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
        }
    }
}