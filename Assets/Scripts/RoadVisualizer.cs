using UnityEngine;

/*
public class RoadVisualizer : MonoBehaviour
{
    public RoadNetwork roadNetwork;

    // Just for testing, debugging
    void OnDrawGizmos()
    {
        if (roadNetwork == null)
            return;

        if (roadNetwork.root.outgoing == null || roadNetwork.root.outgoing.Count == 0)
            return;

        Gizmos.color = Color.yellow;

        RoadSegment testSegment = roadNetwork.root.outgoing[0].roadSegment;
        for (int i = 0; i < testSegment.points.Count - 1; i++)
        {
            Gizmos.DrawLine(
                testSegment.points[i],
                testSegment.points[i + 1]
            );
        }
    }
}
*/