using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public const float laneWidth = 2.0f;
    public const float pointSpacing = 0.1f;
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
    public List<Lane> Lanes = new();
    public int SpeedLimit;

    public RoadSegment(RoadNode a, RoadNode b, int speedLimit)
    {
        NodeA = a;
        NodeB = b;
        SpeedLimit = speedLimit;
    }
}

public class Lane
{
    public RoadSegment Segment;
    public RoadNode From;
    public RoadNode To;
    public List<Vector3> Points;
    public SortedList<int, Car> CarsInLane = new();

    public Lane(RoadSegment segment, RoadNode from, RoadNode to)
    {
        Segment = segment;
        From = from;
        To = to;
    }

    public void SetPoints(Vector3 posFrom, Vector3 posTo)
    {
        // float strength = 3f; 
        // Vector3 bezierControlPoint = posFrom + (posTo - posFrom) * 0.5f + Vector3.Cross((posTo - posFrom).normalized, Vector3.up) * strength; 
        // Points = Util.GenerateQuadraticBezier(posFrom, bezierControlPoint, posTo);
        Points = Util.GenerateLine(posFrom, posTo);
    }
}

public class LaneConnection
{
    public Lane From;
    public Lane To;
    public List<Vector3> TransitionCurve;
    public NodeBehavior Behavior;
    public SortedList<int, Car> CarsInConnection = new();

    public LaneConnection(
        Lane from, 
        Lane to, 
        List<Vector3> transitionCurve, 
        NodeBehavior behavior)
    {
        From = from;
        To = to;
        TransitionCurve = transitionCurve;
        Behavior = behavior;
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

    void Start()
    {

        RoadNode endpointA = new RoadNode(0, new Endpoint(new Vector3(-15f, 0f, 0f)));
        RoadNode endpointB = new RoadNode(1, new Endpoint(new Vector3(15f, 0f, 0f)));
        RoadNode endpointC = new RoadNode(2, new Endpoint(new Vector3(0f, 0f, -15f)));
        RoadNode endpointD = new RoadNode(3, new Endpoint(new Vector3(0f, 0f, 15f)));
        
        RoadNode trafficLightIntersection = new RoadNode(
            4, 
            new TrafficLightIntersection(Vector3.zero)
        );

        RoadSegment westRoad = new RoadSegment(endpointA, trafficLightIntersection, 4);
        RoadSegment eastRoad = new RoadSegment(endpointB, trafficLightIntersection, 4);
        RoadSegment southRoad = new RoadSegment(endpointC, trafficLightIntersection, 4);
        RoadSegment northRoad = new RoadSegment(endpointD, trafficLightIntersection, 4);

        // Snyggaste kod någonsin skrivit
        var roadConfigs = new (RoadNode endpoint, RoadSegment segment, RoadConnection endpointOut, RoadConnection intersectionIn, RoadConnection intersectionOut, RoadConnection endpointIn)[]
        {
            (endpointA, westRoad, RoadConnection.RightOut, RoadConnection.LeftIn, RoadConnection.LeftOut, RoadConnection.RightIn),
            (endpointB, eastRoad, RoadConnection.LeftOut, RoadConnection.RightIn, RoadConnection.RightOut, RoadConnection.LeftIn),
            (endpointC, southRoad, RoadConnection.TopOut, RoadConnection.BotIn, RoadConnection.BotOut, RoadConnection.TopIn),
            (endpointD, northRoad, RoadConnection.BotOut, RoadConnection.TopIn, RoadConnection.TopOut, RoadConnection.BotIn)
        };

        foreach (var (endpoint, segment, endpointOut, intersectionIn, intersectionOut, endpointIn) in roadConfigs)
        {
            // Lane going toward intersection
            Lane toIntersection = new Lane(segment, endpoint, trafficLightIntersection);
            toIntersection.SetPoints(
                endpoint.Behavior.GetPositionOfConnection(endpointOut),
                trafficLightIntersection.Behavior.GetPositionOfConnection(intersectionIn)
            );
            trafficLightIntersection.Behavior.ConnectLane(toIntersection, intersectionIn);
            endpoint.Behavior.ConnectLane(toIntersection, endpointOut);
            segment.Lanes.Add(toIntersection);

            // Lane going away from intersection
            Lane fromIntersection = new Lane(segment, trafficLightIntersection, endpoint);
            fromIntersection.SetPoints(
                trafficLightIntersection.Behavior.GetPositionOfConnection(intersectionOut),
                endpoint.Behavior.GetPositionOfConnection(endpointIn)
            );
            trafficLightIntersection.Behavior.ConnectLane(fromIntersection, intersectionOut);
            endpoint.Behavior.ConnectLane(fromIntersection, endpointIn);
            segment.Lanes.Add(fromIntersection);

            endpoint.ConnectedSegments.Add(segment);
            trafficLightIntersection.ConnectedSegments.Add(segment);
        }

        trafficLightIntersection.Behavior.UpdateLaneConnections();

        roadSegments.Add(westRoad);
        roadSegments.Add(eastRoad);
        roadSegments.Add(southRoad);
        roadSegments.Add(northRoad);

        roadNodes.Add(endpointA.Id, endpointA);
        roadNodes.Add(endpointB.Id, endpointB);
        roadNodes.Add(endpointC.Id, endpointC);
        roadNodes.Add(endpointD.Id, endpointD);
        roadNodes.Add(trafficLightIntersection.Id, trafficLightIntersection);
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
