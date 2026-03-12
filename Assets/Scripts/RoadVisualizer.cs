using UnityEngine;
using System.Collections.Generic;

public class RoadVisualizer : MonoBehaviour
{
    public RoadNetwork network;
    private float yOffset = 0.05f;

    private const float padding = 1f;

    public Material RoadMaterial;

    public Material LineMaterial;

    void Start()
    {
        if (network == null) return;

        DrawLanes();
        DrawNodes();
    }

    void DrawNodes()
    {
        foreach (var node in network.GetNodes())
        {
            switch (node.Behavior)
            {
                case Endpoint:
                DrawEndpoint(node);
                    break;
                case SharedGeometryIntersection:
                DrawSharedGeometryIntersection(node);
                    break;
                default:
                    break;
            }
        }
    }

    void DrawEndpoint(RoadNode node)
    {
        GameObject endpointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        endpointObj.name = $"Endpoint_{node.Id}";
        endpointObj.transform.SetParent(transform);
        endpointObj.transform.position = node.Position + Vector3.up * yOffset;
        endpointObj.transform.localScale = new Vector3(padding/2f, 0.1f, padding/2f);
        Collider endpointCollider = endpointObj.GetComponent<Collider>();
        if (endpointCollider != null)
            Destroy(endpointCollider);
        var renderer = endpointObj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = RoadMaterial;
    }

    void DrawSharedGeometryIntersection(RoadNode node)
    {
        GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nodeObj.name = $"Node_{node.Id}";
        nodeObj.transform.SetParent(transform);
        nodeObj.transform.position = node.Position + Vector3.up * yOffset;
        nodeObj.transform.localScale = new Vector3(padding, 0.1f, padding);
        Collider intersectionCollider = nodeObj.GetComponent<Collider>();
        if (intersectionCollider != null)
            Destroy(intersectionCollider);
        var renderer = nodeObj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = RoadMaterial;
    }

    Mesh GenerateSegmentMesh(RoadSegment segment)
    {
        Mesh mesh = new();
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        float width = Constants.laneWidth;
        float totalLength = 0f; // for UV mapping

        foreach (var lane in segment.Lanes)
        {
            if (lane.Points == null || lane.Points.Count < 2)
                continue;

            // Setup first two vertices
            Vector3 perp = Vector3.Cross((lane.Points[1] - lane.Points[0]).normalized, Vector3.up);
            Vector3 v0 = lane.Points[0] + perp * (width / 2f) + Vector3.up*yOffset;
            Vector3 v1 = lane.Points[0] - perp * (width / 2f) + Vector3.up*yOffset;

            for (int i = 1; i < lane.Points.Count - 1; i++)
            {
                Vector3 p1 = lane.Points[i];
                Vector3 p2 = lane.Points[i+1];
                Vector3 direction = (p2 - p1).normalized;
                perp = Vector3.Cross(direction, Vector3.up);
                /*  Vertices of on part of the lane mesh
                <--width-->

                v2--p2--v3
                | \     |
                |   \   |
                |     \ |
                v0--p1--v1
                */
                Vector3 v2 = p2 + perp * (width / 2f) + Vector3.up*yOffset;
                Vector3 v3 = p2 - perp * (width / 2f) + Vector3.up*yOffset;
                int index = vertices.Count;

                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                // Traingels from the vertices indices
                triangles.Add(index + 0);
                triangles.Add(index + 2);
                triangles.Add(index + 1);

                triangles.Add(index + 2);
                triangles.Add(index + 3);
                triangles.Add(index + 1);

                // Textures
                float partLength = Vector3.Distance(p1, p2);

                float vStart = totalLength;
                float vEnd = totalLength + partLength;

                // Tile factor 
                float textureTiling = 0.75f; // increase for more repetition

                uvs.Add(new Vector2(0, vStart * textureTiling));
                uvs.Add(new Vector2(1, vStart * textureTiling));
                uvs.Add(new Vector2(0, vEnd * textureTiling));
                uvs.Add(new Vector2(1, vEnd * textureTiling));

                totalLength += partLength;

                v0 = v2;
                v1 = v3;
            }                                     
            
        } 
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
    void DrawLanes()
    {
        int segCount = 0; // count to id segments
        foreach (var segment in network.GetSegments())
        { 
            GameObject segmentObj = new GameObject($"Segment_{segCount}");
            segCount++;
            segmentObj.transform.SetParent(transform);

            var meshRenderer = segmentObj.AddComponent<MeshRenderer>();        
            var meshFilter = segmentObj.AddComponent<MeshFilter>();
            meshRenderer.sharedMaterial = RoadMaterial;

            Mesh mesh = GenerateSegmentMesh(segment);

            meshFilter.mesh = mesh;

            DrawLine(segment.Points);
        }       
    }

  void DrawLine(List<Vector3> points)
{
    GameObject obj = new GameObject("LaneLine");
    obj.transform.SetParent(transform);
    MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
    MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
    meshRenderer.sharedMaterial = LineMaterial;

    float width = 0.25f;

    List<Vector3> vertices = new();
    List<int> triangles = new();

    for (int i = 0; i < points.Count; i++)
    {
        Vector3 p = new Vector3(points[i].x, yOffset + 0.01f, points[i].z);

        Vector3 forward;

        if (i < points.Count - 1)
            forward = (points[i + 1] - points[i]).normalized;
        else
            forward = (points[i] - points[i - 1]).normalized;

        Vector3 perp = 0.5f * width * Vector3.Cross(Vector3.up, forward);

        vertices.Add(p - perp);
        vertices.Add(p + perp);

        
    }
    for (int i = 0; i < points.Count - 1; i++)
        {
            int v = i * 2;
            triangles.Add(v);
            triangles.Add(v + 2);
            triangles.Add(v + 1);

            triangles.Add(v + 1);
            triangles.Add(v + 2);
            triangles.Add(v + 3);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

    meshFilter.mesh = mesh;
}

    void OnDrawGizmos()
    {
        if (network == null) return;

        DrawLanesDebug();
        DrawLaneConnectionsDebug();
        DrawNodesDebug();
    }

    void DrawLanesDebug()
    {
        /*Debugging*/
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

    void DrawLaneConnectionsDebug()
    {
        /*Debugging*/
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

    void DrawNodesDebug()
    {
        /*Debugging*/
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
