using UnityEngine;
using System.Collections.Generic;

public class RoadVisualizer : MonoBehaviour
{
    public RoadNetwork network;
    public float yOffset = 0.05f;

    void OnDrawGizmos()
    {
        if (network == null) return;

        DrawLanes();
        DrawLaneConnections();
        DrawNodes();
    }

    void DrawLanes()
    {
        Gizmos.color = Color.white;

        foreach (var segment in network.GetSegments())
        {
            foreach (var lane in segment.Lanes)
            {
                if (lane.Points == null || lane.Points.Count < 2)
                    continue;

                Gizmos.color = lane.From.Id < lane.To.Id ? Color.yellow : Color.cyan;

                for (int i = 0; i < lane.Points.Count - 1; i++)
                {
                    Vector3 a = lane.Points[i] + Vector3.up * yOffset;
                    Vector3 b = lane.Points[i + 1] + Vector3.up * yOffset;

                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }

    void DrawLaneConnections()
    {
        Gizmos.color = Color.green;

        foreach (var node in network.GetNodes())
        {
            var connections = node.Behavior.GetLaneConnections();
            if (connections == null) continue;

            foreach (var connection in connections)
            {
                if (connection.TransitionCurve == null || connection.TransitionCurve.Count < 2)
                    continue;

                for (int i = 0; i < connection.TransitionCurve.Count - 1; i++)
                {
                    Vector3 a = connection.TransitionCurve[i] + Vector3.up * yOffset;
                    Vector3 b = connection.TransitionCurve[i + 1] + Vector3.up * yOffset;

                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }

    void DrawNodes()
    {
        Gizmos.color = Color.red;

        foreach (var node in network.GetNodes())
        {
            Gizmos.DrawSphere(
                node.Position + Vector3.up * yOffset,
                0.4f
            );
        }
    }
}
