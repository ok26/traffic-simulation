using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public const float laneWidth = 2.0f;
    public const float pointSpacing = 0.1f;
    public const float speedLimit = 4f;
}

public class RoadNode
{
    public int Id;
    public NodeBehavior Behavior;
    public List<RoadSegment> ConnectedSegments = new();

    public RoadNode(int id, NodeBehavior behavior)
    {
        Id = id;
        Behavior = behavior;
    }

    public Vector3 Position => Behavior.GetPosition();
}

public class RoadSegment
{
    public RoadNode NodeA;
    public RoadNode NodeB;

    public Vector3 Apos;
    public Vector3 Bpos;
    public List<Vector3> Points;
    public List<Lane> Lanes = new(); // First rightLanes are from NodeA to NodeB, then leftLanes from NodeB to NodeA
    public int RightLanes;
    public float SpeedLimit;

    public RoadSegment(RoadNode a, RoadNode b, float speedLimit, int a_dir, int b_dir, Vector3 ctrlp1 = default, Vector3 ctrlp2 = default)
    {
        NodeA = a;
        NodeB = b;
        Apos = a.Behavior.GetPositionOfSegCon(a_dir);
        Bpos = b.Behavior.GetPositionOfSegCon(b_dir);
        SpeedLimit = speedLimit;

        Points = Util.GenerateCubicBezier(Apos, ctrlp1, ctrlp2, Bpos);
    }

    public void AddLane(int offset, int dir)
    {
        List<Vector3> lanePoints = new();

        if(dir > 0) {
            foreach (Vector3 point in Points)
            {
                Vector3 perp = Vector3.Cross((Bpos - Apos).normalized, Vector3.up);
                lanePoints.Add(point - perp * (Constants.laneWidth / 2f + offset * Constants.laneWidth));
            }
            Lanes.Add(new Lane(this, NodeA, NodeB, lanePoints));
        } else
        {
            foreach (Vector3 point in Points)
            {
                Vector3 perp = Vector3.Cross((Bpos - Apos).normalized, Vector3.up);
                lanePoints.Add(point + perp * (Constants.laneWidth / 2f + offset * Constants.laneWidth));
            }
            lanePoints.Reverse();
            Lanes.Add(new Lane(this, NodeB, NodeA, lanePoints));
        }
    }
}

public class Lane
{
    public RoadSegment Segment;
    public RoadNode From;
    public RoadNode To;
    public List<Vector3> Points;
    public SortedList<int, Car> CarsInLane = new();

    public Lane(RoadSegment segment, RoadNode from, RoadNode to, List<Vector3> points = null)
    {
        Segment = segment;
        From = from;
        To = to;
        Points = points ?? new List<Vector3>();
    }

}

public class LaneConnection
{
    public Lane From;
    public Lane To;
    public List<Vector3> TransitionCurve;
    public NodeBehavior Behavior;
    public SortedList<int, Car> CarsInConnection = new();
    public float SpeedLimit;

    public LaneConnection(
        Lane from, 
        Lane to, 
        List<Vector3> transitionCurve, 
        NodeBehavior behavior,
        float speedLimit)
    {
        From = from;
        To = to;
        TransitionCurve = transitionCurve;
        Behavior = behavior;
        SpeedLimit = speedLimit;
    }
}

public class RoadNetwork : MonoBehaviour
{

    private Dictionary<int, RoadNode> roadNodes = new();
    private List<RoadSegment> roadSegments = new();

    public IEnumerable<RoadSegment> GetSegments() => roadSegments;
    public IEnumerable<RoadNode> GetNodes() => roadNodes.Values;

    public RoadNode GetNodeById(int id)
    {
        return roadNodes.ContainsKey(id) ? roadNodes[id] : null;
    }


    void ParseText(string text)
    {
        string[] splitLines = text.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);

        int idx = 0;
        int num_nodes = int.Parse(splitLines[idx++]);
        for (int i = 0; i < num_nodes; i++)
        {
            int type = int.Parse(splitLines[idx++]);
            float x = float.Parse(splitLines[idx++]);
            float z = float.Parse(splitLines[idx++]);
            Vector3 pos = new Vector3(x, 0f, z);
            NodeBehavior behavior = type switch
            {
                0 => new Endpoint(pos),
                1 => new TrafficLightIntersection(pos),
                2 => new StopSignIntersection(pos),
                _ => throw new System.Exception("Invalid node type in input data")
            };
            RoadNode node = new RoadNode(i, behavior);
            roadNodes.Add(i, node);
        }
        int num_segments = int.Parse(splitLines[idx++]);
        for (int i = 0; i < num_segments; i++)
        {
            int nodeA = int.Parse(splitLines[idx++]);
            int nodeB = int.Parse(splitLines[idx++]);
            int a_dir = int.Parse(splitLines[idx++]);
            int b_dir = int.Parse(splitLines[idx++]);
            float p1x = float.Parse(splitLines[idx++]);
            float p1z = float.Parse(splitLines[idx++]);
            float p2x = float.Parse(splitLines[idx++]);
            float p2z = float.Parse(splitLines[idx++]);
            Vector3 ctrlp1 = new(p1x, 0f, p1z);
            Vector3 ctrlp2 = new(p2x, 0f, p2z);
            int left_lanes = int.Parse(splitLines[idx++]);
            int right_lanes = int.Parse(splitLines[idx++]);
            RoadSegment segment = new RoadSegment(roadNodes[nodeA], roadNodes[nodeB], Constants.speedLimit, a_dir, b_dir, ctrlp1, ctrlp2);
 
            for (int j = 0; j < right_lanes; j++)
            {
                int con_1 = int.Parse(splitLines[idx++]);
                int con_2 = int.Parse(splitLines[idx++]);
                segment.AddLane(j, 1);
                Lane AB = segment.Lanes[^1];
                roadNodes[nodeA].Behavior.ConnectLane(AB, con_1);
                roadNodes[nodeB].Behavior.ConnectLane(AB, con_2);
            }
            for (int j = 0; j < left_lanes; j++)
            {
                int con_1 = int.Parse(splitLines[idx++]);
                int con_2 = int.Parse(splitLines[idx++]);
                segment.AddLane(j, 0);
                Lane BA = segment.Lanes[^1];

                roadNodes[nodeB].Behavior.ConnectLane(BA, con_1);
                roadNodes[nodeA].Behavior.ConnectLane(BA, con_2);
            }
            roadNodes[nodeA].ConnectedSegments.Add(segment);
            roadNodes[nodeB].ConnectedSegments.Add(segment);
            roadSegments.Add(segment);
        }
        foreach (RoadNode node in roadNodes.Values)
        {
            switch (node.Behavior)
            {
                case Endpoint:
                    break;
                case TrafficLightIntersection:
                    node.Behavior.UpdateLaneConnections();
                    break;
                case StopSignIntersection:
                    node.Behavior.UpdateLaneConnections();
                    break;
            }
        }
    }
    void Start()
    {
        TextAsset textFile = Resources.Load<TextAsset>("network2");
         if (textFile != null)
        {
            string fileContents = textFile.text;
            Debug.Log(fileContents);

            ParseText(fileContents);
        }
        else
        {
            Debug.LogError("Data file not found!");
        }
        // DebugPrint();
    }

    void DebugPrint() 
    {
        foreach (RoadNode node in roadNodes.Values)
        {
            Debug.Log($"Node {node.Id} at {node.Position} with behavior {node.Behavior.GetType().Name}");
            Debug.Log($"Connected Segments: {node.ConnectedSegments.Count}");
            foreach (RoadSegment segment in node.ConnectedSegments)
            {
                Debug.Log($"  Connected to Node {(segment.NodeA == node ? segment.NodeB.Id : segment.NodeA.Id)} with {segment.Lanes.Count} lanes");
                foreach (Lane lane in segment.Lanes)
                {
                    if (lane.From == node)
                        Debug.Log($"    Lane from Node {lane.From.Id} to Node {lane.To.Id}");
                    else
                        Debug.Log($"    Lane from Node {lane.From.Id} to Node {lane.To.Id}");
                }
            }
        }
    }

    public List<Lane> GetOutgoingLanes(RoadNode roadNode, Lane incomingLane = null)
    {
        List<Lane> lanes = new();

        if (incomingLane != null)
        {
            List<LaneConnection> connections = roadNode.Behavior.GetLaneConnections(incomingLane);
            foreach (LaneConnection connection in connections)
            {
                lanes.Add(connection.To);
            }
        }
        else foreach (RoadSegment segment in roadNode.ConnectedSegments)
        {
            foreach (Lane lane in segment.Lanes)
            {
                if (lane.From == roadNode)
                    lanes.Add(lane);        
            }
        }
        return lanes;
    }
}
