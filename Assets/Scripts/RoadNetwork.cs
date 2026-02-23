using System.Collections.Generic;
using UnityEngine;

public static class Consts
{
    public const float laneWidth = 2.0f;
    public const float pointSpacing = 0.1f;
}

public class RoadNode
{
    public int id;
    public NodeBehavior behavior;
    public List<RoadSegment> connectedSegments = new();

    public RoadNode(int id, NodeBehavior behavior)
    {
        this.id = id;
        this.behavior = behavior;
    }

    public Vector3 position => behavior.getPosition();
}

public class RoadSegment
{
    public RoadNode a;
    public RoadNode b;
    public List<Lane> lanes = new();
    public int speedLimit;

    public RoadSegment(RoadNode a, RoadNode b, int speedLimit)
    {
        this.a = a;
        this.b = b;
        this.speedLimit = speedLimit;
    }
}

public class Lane
{
    public RoadSegment segment;
    public RoadNode from;
    public RoadNode to;
    public List<Vector3> points;

    public Lane(RoadSegment segment, RoadNode from, RoadNode to)
    {
        this.segment = segment;
        this.from = from;
        this.to = to;
    }

    public void setPoints(Vector3 posFrom, Vector3 posTo)
    {
        points = Util.GenerateLine(posFrom, posTo);
    }
}

public class LaneConnection
{
    public Lane from;
    public Lane to;
    public List<Vector3> transitionCurve;
    public NodeBehavior behavior;

    public LaneConnection(
        Lane from, 
        Lane to, 
        List<Vector3> transitionCurve, 
        NodeBehavior behavior)
    {
        this.from = from;
        this.to = to;
        this.transitionCurve = transitionCurve;
        this.behavior = behavior;
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
        
        RoadNode stopSignIntersection = new RoadNode(
            4, 
            new StopSignIntersection(Vector3.zero)
        );

        RoadSegment westRoad = new RoadSegment(endpointA, stopSignIntersection, 4);
        RoadSegment eastRoad = new RoadSegment(endpointB, stopSignIntersection, 4);
        RoadSegment southRoad = new RoadSegment(endpointC, stopSignIntersection, 4);
        RoadSegment northRoad = new RoadSegment(endpointD, stopSignIntersection, 4);

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
            Lane toIntersection = new Lane(segment, endpoint, stopSignIntersection);
            toIntersection.setPoints(
                endpoint.behavior.getPositionOfConnection(endpointOut),
                stopSignIntersection.behavior.getPositionOfConnection(intersectionIn)
            );
            stopSignIntersection.behavior.connectLane(toIntersection, intersectionIn);
            endpoint.behavior.connectLane(toIntersection, endpointOut);
            segment.lanes.Add(toIntersection);

            // Lane going away from intersection
            Lane fromIntersection = new Lane(segment, stopSignIntersection, endpoint);
            fromIntersection.setPoints(
                stopSignIntersection.behavior.getPositionOfConnection(intersectionOut),
                endpoint.behavior.getPositionOfConnection(endpointIn)
            );
            stopSignIntersection.behavior.connectLane(fromIntersection, intersectionOut);
            endpoint.behavior.connectLane(fromIntersection, endpointIn);
            segment.lanes.Add(fromIntersection);

            endpoint.connectedSegments.Add(segment);
            stopSignIntersection.connectedSegments.Add(segment);
        }

        stopSignIntersection.behavior.updateLaneConnections();

        roadSegments.Add(westRoad);
        roadSegments.Add(eastRoad);
        roadSegments.Add(southRoad);
        roadSegments.Add(northRoad);

        roadNodes.Add(endpointA.id, endpointA);
        roadNodes.Add(endpointB.id, endpointB);
        roadNodes.Add(endpointC.id, endpointC);
        roadNodes.Add(endpointD.id, endpointD);
        roadNodes.Add(stopSignIntersection.id, stopSignIntersection);
    }

    public List<Lane> getOutgoingLanes(RoadNode roadNode, Lane incomingLane = null)
    {
        List<Lane> lanes = new();

        if (incomingLane != null)
        {
            List<LaneConnection> connections = roadNode.behavior.getLaneConnections(incomingLane);
            foreach (LaneConnection connection in connections)
            {
                lanes.Add(connection.to);
            }
        }
        else foreach (RoadSegment segment in roadNode.connectedSegments)
        {
            foreach (Lane lane in segment.lanes)
            {
                if (lane.from == roadNode)
                    lanes.Add(lane);        
            }
        }
        return lanes;
    }
}
