using System;
using System.Collections.Generic;
using UnityEngine;

class Node : IComparable<Node>
{
    public RoadNode roadNode;
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

public class Astar
{
    public static Stack<LaneConnection> findPath(
        RoadNetwork network,
        RoadNode startNode,
        RoadNode goalNode
    )
    {
        PriorityQueue<Node> p = new();
        HashSet<Lane> vis = new();
        
        foreach (Lane lane in network.getOutgoingLanes(startNode))
        {
            p.Insert(new Node
            {
                roadNode = startNode,
                lane = lane,
                parentNode = null,
                f = 0f,
                g = 0f
            });
            vis.Add(lane);
        }

        while (!p.IsEmpty)
        {
            Node node = p.Peek();
            p.Remove();

            if (node.roadNode == goalNode)
            {
                return contructPath(node);
            }

            RoadNode conNode = node.lane.to;
            foreach (Lane lane in network.getOutgoingLanes(conNode, node.lane))
            {
                if (vis.Contains(lane))
                    continue;

                vis.Add(lane);
                float h = Vector3.Distance(goalNode.position, lane.to.position);
                float g = node.g + Vector3.Distance(lane.points[0], lane.points[^0]);
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

        return new();
    }

    private static Stack<LaneConnection> contructPath(Node node)
    {
        Stack<LaneConnection> path = new();
        return path;
    }
}