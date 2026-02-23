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
            foreach (var lane in segment.lanes)
            {
                if (lane.points == null || lane.points.Count < 2)
                    continue;

                Gizmos.color = lane.from.id < lane.to.id ? Color.yellow : Color.cyan;

                for (int i = 0; i < lane.points.Count - 1; i++)
                {
                    Vector3 a = lane.points[i] + Vector3.up * yOffset;
                    Vector3 b = lane.points[i + 1] + Vector3.up * yOffset;

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
            var connections = node.behavior.getLaneConnections();
            if (connections == null) continue;

            foreach (var connection in connections)
            {
                if (connection.transitionCurve == null || connection.transitionCurve.Count < 2)
                    continue;

                for (int i = 0; i < connection.transitionCurve.Count - 1; i++)
                {
                    Vector3 a = connection.transitionCurve[i] + Vector3.up * yOffset;
                    Vector3 b = connection.transitionCurve[i + 1] + Vector3.up * yOffset;

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
                node.position + Vector3.up * yOffset,
                0.4f
            );
        }
    }
}
