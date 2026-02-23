using System;
using System.Collections.Generic;
using UnityEngine;

class Node : IComparable<Node>
{
    // Coming from
    public RoadNode roadNode;
    // Travelling on
    public Lane lane;

    public Node parentNode;
    public float f;
    public float g;

    public int CompareTo(Node other)
    {
        if (other == null) return 1;
        return f.CompareTo(other.f);
    }
}

public class RoadPath
{
    public Lane startingLane;
    public Stack<LaneConnection> connections;
}

public class Astar
{
    public static RoadPath findPath(
        RoadNetwork network,
        RoadNode startNode,
        RoadNode goalNode
    )
    {
        PriorityQueue<Node> p = new();
        HashSet<Lane> vis = new();
        
        foreach (Lane lane in network.getOutgoingLanes(startNode))
        {
            float h = Vector3.Distance(goalNode.position, startNode.position);
            p.Insert(new Node
            {
                roadNode = startNode,
                lane = lane,
                parentNode = null,
                f = h,
                g = 0f
            });
            vis.Add(lane);
        }

        while (!p.IsEmpty)
        {
            Node node = p.Peek();
            p.Remove();

            // Check if the lane leads to the goal
            if (node.lane.to == goalNode)
            {
                return contructPath(node);
            }

            RoadNode conNode = node.lane.to;
            foreach (Lane lane in network.getOutgoingLanes(conNode, node.lane))
            {
                if (vis.Contains(lane))
                    continue;

                vis.Add(lane);
                float h = Vector3.Distance(goalNode.position, lane.from.position);
                float g = node.g + Vector3.Distance(lane.points[0], lane.points[^1]);
                p.Insert(new Node
                {
                    roadNode = conNode,
                    lane = lane,
                    parentNode = node,
                    g = g,
                    f = g + h
                });
            }
        }

        Debug.Log("No path found");
        return new RoadPath {
            startingLane = null,
            connections = new()
        };
    }

    private static RoadPath contructPath(Node node)
    {
        Stack<LaneConnection> path = new();
        while (node.parentNode != null)
        {
            Lane laneFrom = node.parentNode.lane;
            Lane laneTo = node.lane;
            List<LaneConnection> connections = 
                node.roadNode.behavior.getLaneConnections(laneFrom);
            foreach (LaneConnection connection in connections)
            {
                if (connection.to == laneTo)
                {
                    path.Push(connection);
                    break;
                }
            }
            node = node.parentNode;
        }

        return new RoadPath
        {
            startingLane = node.lane,
            connections = path
        };
    }
}