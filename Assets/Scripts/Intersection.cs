using System;
using System.Collections.Generic;
using UnityEngine;

public class LaneConnection
{
    public Lane from;
    public Lane to;
    public List<Vector3> transitionCurve;
}

public enum RoadConnection
{
    TopIn,
    TopOut,
    LeftIn,
    LeftOut,
    RightIn,
    RightOut,
    BotIn,
    BotOut
}

public abstract class NodeBehavior
{
    public abstract Vector3 getPositionOfConnection(RoadConnection connection);
    public abstract Vector3 getPosition();
}

// TODO: Add rotations
public class Endpoint : NodeBehavior
{
    Vector3 position;

    public Endpoint(Vector3 position)
    {
        this.position = position;
    }

    public override Vector3 getPosition()
    {
        return position;
    }

    public override Vector3 getPositionOfConnection(RoadConnection connection)
    {
        Vector3 position = this.position;

        switch (connection)
        {
            case RoadConnection.TopIn:
            case RoadConnection.BotOut:
            position.x -= Consts.laneWidth / 2;
            break;
            case RoadConnection.TopOut:
            case RoadConnection.BotIn:
            position.x += Consts.laneWidth / 2;
            break;
            case RoadConnection.LeftIn:
            case RoadConnection.RightOut:
            position.z -= Consts.laneWidth / 2;
            break;
            case RoadConnection.LeftOut:
            case RoadConnection.RightIn:
            position.z += Consts.laneWidth / 2;
            break;
        }

        return position;
    }
}

public class StopSignIntersection : NodeBehavior
{
    Vector3 position;

    // Connection positions, indexed with RoadConnection
    List<Vector3> cPos = new List<Vector3>(8);

    public StopSignIntersection(Vector3 position)
    {
        this.position = position;
        foreach (RoadConnection connection in Enum.GetValues(typeof(RoadConnection)))
        {
            cPos[(int)connection] = getPositionOfConnection(connection);
        }
    }

    public override Vector3 getPosition()
    {
        return position;
    }

    public override Vector3 getPositionOfConnection(RoadConnection connection)
    {

        Vector3 position = this.position;

        switch (connection)
        {
            case RoadConnection.TopIn:
            position.x -= Consts.laneWidth / 2;
            position.z += Consts.laneWidth;
            break;
            case RoadConnection.TopOut:
            position.x += Consts.laneWidth / 2;
            position.z += Consts.laneWidth;
            break;
            case RoadConnection.LeftIn:
            position.x -= Consts.laneWidth;
            position.z -= Consts.laneWidth / 2;
            break;
            case RoadConnection.LeftOut:
            position.x -= Consts.laneWidth;
            position.z += Consts.laneWidth / 2;
            break;
            case RoadConnection.RightIn:
            position.x += Consts.laneWidth;
            position.z += Consts.laneWidth / 2;
            break;
            case RoadConnection.RightOut:
            position.x += Consts.laneWidth;
            position.z -= Consts.laneWidth / 2;
            break;
            case RoadConnection.BotIn:
            position.x += Consts.laneWidth / 2;
            position.z -= Consts.laneWidth;
            break;
            case RoadConnection.BotOut:
            position.x -= Consts.laneWidth / 2;
            position.z -= Consts.laneWidth;
            break;
        }
        return position;
    }

    public List<LaneConnection> getConnectionArcs(RoadConnection from, Lane laneFrom, Lane laneTo)
    {
        List<List<Vector3>> arcs = new();

        switch (from)
        {
            case RoadConnection.TopIn:
            arcs.Add(Util.GenerateArc(cPos[(int)RoadConnection.TopIn], cPos[(int)RoadConnection.RightOut], false));
            arcs.Add(Util.GenerateArc(cPos[(int)RoadConnection.TopIn], cPos[(int)RoadConnection.LeftOut], true));
            arcs.Add(Util.GenerateLine(cPos[(int)RoadConnection.TopIn], cPos[(int)RoadConnection.BotOut]));
            break;
            case RoadConnection.BotIn:
            arcs.Add(Util.GenerateArc(cPos[(int)RoadConnection.BotIn], cPos[(int)RoadConnection.RightOut], true));
            arcs.Add(Util.GenerateArc(cPos[(int)RoadConnection.BotIn], cPos[(int)RoadConnection.LeftOut], false));
            arcs.Add(Util.GenerateLine(cPos[(int)RoadConnection.BotIn], cPos[(int)RoadConnection.TopOut]));
            break;
            case RoadConnection.LeftIn:
            arcs.Add(Util.GenerateArc(cPos[(int)RoadConnection.LeftIn], cPos[(int)RoadConnection.TopOut], false));
            arcs.Add(Util.GenerateArc(cPos[(int)RoadConnection.LeftIn], cPos[(int)RoadConnection.BotOut], true));
            arcs.Add(Util.GenerateLine(cPos[(int)RoadConnection.LeftIn], cPos[(int)RoadConnection.RightOut]));
            break;
            case RoadConnection.RightIn:
            arcs.Add(Util.GenerateArc(cPos[(int)RoadConnection.RightIn], cPos[(int)RoadConnection.BotOut], false));
            arcs.Add(Util.GenerateArc(cPos[(int)RoadConnection.RightIn], cPos[(int)RoadConnection.TopOut], true));
            arcs.Add(Util.GenerateLine(cPos[(int)RoadConnection.RightIn], cPos[(int)RoadConnection.LeftOut]));
            break;
        }

        List<LaneConnection> res = new();
        for (int i = 0; i < arcs.Count; i++)
        {
            res.Add(new LaneConnection
            {
                from = laneFrom,
                to = laneTo,
                transitionCurve = arcs[i]
            });
        }

        return res;
    }
}