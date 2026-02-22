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
    public List<LaneConnection> outgoing;

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

    public Vector3 getPointAtDistanceFrom(Vector3 from, float distance) {
        int closestPointIdx = 0;
        float closestsDistance = Vector3.Distance(from, points[0]);
        for (int i = 1; i < points.Count; i++)
        {
            Vector3 point = points[i];
            float distanceToPoint = Vector3.Distance(from, point);
            if (distanceToPoint < closestsDistance)
            {   
                closestsDistance = distanceToPoint;
                closestPointIdx = i;
            }   
        }

        int pointsLookAheadCnt = Mathf.CeilToInt(distance / Consts.pointSpacing);
        int pointAtDistanceIdx = Mathf.Min(points.Count - 1, closestPointIdx + pointsLookAheadCnt);
        return points[pointAtDistanceIdx];
    }

    public Vector3 getEndPos() {
        return points[^1];
    }
}

public class RoadNetwork : MonoBehaviour
{

    private Dictionary<int, RoadNode> roadNodes = new();
    private List<RoadSegment> roadSegments = new();

    void Start()
    {

        RoadNode endpointA = new RoadNode(0, new Endpoint(new Vector3(-25f, 0f, 0f)));
        RoadNode endpointB = new RoadNode(1, new Endpoint(new Vector3(25f, 0f, 0f)));
        RoadNode endpointC = new RoadNode(2, new Endpoint(new Vector3(0f, 0f, -25f)));
        RoadNode endpointD = new RoadNode(3, new Endpoint(new Vector3(0f, 0f, 25f)));
        
        RoadNode stopSignIntersection = new RoadNode(
            4, 
            new StopSignIntersection(Vector3.zero)
        );

        RoadSegment westRoad = new RoadSegment(endpointA, stopSignIntersection, 10);
        RoadSegment eastRoad = new RoadSegment(endpointB, stopSignIntersection, 10);
        RoadSegment southRoad = new RoadSegment(endpointC, stopSignIntersection, 10);
        RoadSegment northRoad = new RoadSegment(endpointD, stopSignIntersection, 10);

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
            segment.lanes.Add(toIntersection);

            // Lane going away from intersection
            Lane fromIntersection = new Lane(segment, stopSignIntersection, endpoint);
            fromIntersection.setPoints(
                stopSignIntersection.behavior.getPositionOfConnection(intersectionOut),
                endpoint.behavior.getPositionOfConnection(endpointIn)
            );
            segment.lanes.Add(fromIntersection);

            endpoint.connectedSegments.Add(segment);
            stopSignIntersection.connectedSegments.Add(segment);
        }
    }

    public List<Lane> getOutgoingLanes(RoadNode roadNode, Lane incomingLane = null)
    {
        List<Lane> lanes = new();
        foreach (RoadSegment segment in roadNode.connectedSegments)
        {
            foreach (Lane lane in segment.lanes)
            {
                if (lane.from != roadNode)
                    continue;

                if (incomingLane != null)
                {
                    foreach (LaneConnection laneConnection in incomingLane.outgoing)
                    {
                        if (laneConnection.to == lane)
                        {
                            lanes.Add(lane);
                            break;
                        }
                    }
                }
                else
                {
                    lanes.Add(lane);
                }                
            }
        }
        return lanes;
    }
}
