using System;
using System.Collections.Generic;
using UnityEngine;

class Node : IComparable<Node>
{
    // Coming from
    public RoadNode FromNode;
    // Travelling on
    public Lane Lane;

    public Node Parent;
    public float F;
    public float G;

    public int CompareTo(Node other)
    {
        if (other == null) return 1;
        return F.CompareTo(other.F);
    }
}

public class RoadPath
{
    public Lane StartingLane;
    public Stack<LaneConnection> Connections;
}

public class AStar
{
    public static RoadPath FindPath(
        RoadNetwork network,
        RoadNode startNode,
        RoadNode goalNode
    )
    {
        PriorityQueue<Node> p = new();
        HashSet<Lane> vis = new();
        
        foreach (Lane lane in network.GetOutgoingLanes(startNode))
        {
            float h = Vector3.Distance(goalNode.Position, startNode.Position);
            p.Insert(new Node
            {
                FromNode = startNode,
                Lane = lane,
                Parent = null,
                F = h,
                G = 0f
            });
            vis.Add(lane);
        }

        while (!p.IsEmpty)
        {
            Node node = p.Peek();
            p.Remove();

            // Check if the lane leads to the goal
            if (node.Lane.To == goalNode)
            {
                return ConstructPath(node);
            }

            RoadNode conNode = node.Lane.To;
            foreach (Lane lane in network.GetOutgoingLanes(conNode, node.Lane))
            {
                if (vis.Contains(lane))
                    continue;

                vis.Add(lane);
                float h = Vector3.Distance(goalNode.Position, lane.From.Position);
                float g = node.G + Vector3.Distance(lane.Points[0], lane.Points[^1]);
                p.Insert(new Node
                {
                    FromNode = conNode,
                    Lane = lane,
                    Parent = node,
                    G = g,
                    F = g + h
                });
            }
        }

        Debug.Log("No path found");
        return null;
    }

    private static RoadPath ConstructPath(Node node)
    {
        Stack<LaneConnection> path = new();
        while (node.Parent != null)
        {
            Lane laneFrom = node.Parent.Lane;
            Lane laneTo = node.Lane;
            List<LaneConnection> connections = 
                node.FromNode.Behavior.GetLaneConnections(laneFrom);
            foreach (LaneConnection connection in connections)
            {
                if (connection.To == laneTo)
                {
                    path.Push(connection);
                    break;
                }
            }
            node = node.Parent;
        }

        return new RoadPath
        {
            StartingLane = node.Lane,
            Connections = path
        };
    }
}