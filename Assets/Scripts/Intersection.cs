using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

public enum CarAction
{
    Drive,
    Wait
}

public abstract class NodeBehavior
{
    protected Dictionary<RoadConnection, Lane> connections = new();
    public abstract float speedLimit { get; }
    public abstract Vector3 getPositionOfConnection(RoadConnection connection);
    public abstract Vector3 getPosition();
    public abstract CarAction getCarAction(Car car);
    public abstract void updateLaneConnections();
    public abstract List<LaneConnection> getLaneConnections(Lane lane);

    // Debugging for now
    public abstract List<LaneConnection> getLaneConnections();

    public virtual void connectLane(Lane lane, RoadConnection connection)
    {
        connections[connection] = lane;
    }

    public virtual Vector3 getPositionOfConnection(Lane lane)
    {
        foreach (var pair in connections)
        {
            if (pair.Value == lane)
                return getPositionOfConnection(pair.Key);
        }

        return Vector3.zero;
    }
}

// TODO: Add rotations, yeah you wish
public class Endpoint : NodeBehavior
{
    Vector3 position;
    public override float speedLimit => 0f;

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

    public override CarAction getCarAction(Car car)
    {
        return CarAction.Drive;
    }

    public override List<LaneConnection> getLaneConnections(Lane lane)
    {
        return new();
    }

    public override void updateLaneConnections()
    {
        throw new NotImplementedException();
    }

    public override List<LaneConnection> getLaneConnections()
    {
        return new();
    }
}

public class StopSignIntersection : NodeBehavior
{
    Vector3 position;

    // Connection positions, indexed with RoadConnection
    Vector3[] cPos = new Vector3[Enum.GetValues(typeof(RoadConnection)).Length];

    // LaneConnections for each connected incoming lane
    List<LaneConnection>[] laneConnections = 
        new List<LaneConnection>[Enum.GetValues(typeof(RoadConnection)).Length];

    public override float speedLimit => 2.0f;

    public StopSignIntersection(Vector3 position)
    {
        this.position = position;

        for (int i = 0; i < laneConnections.Length; i++)
        {
            laneConnections[i] = new();
        }

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

        const float laneOffset = 1.2f;
        
        switch (connection)
        {
            case RoadConnection.TopIn:
            position.x -= Consts.laneWidth / 2;
            position.z += Consts.laneWidth * laneOffset;
            break;
            case RoadConnection.TopOut:
            position.x += Consts.laneWidth / 2;
            position.z += Consts.laneWidth * laneOffset;
            break;
            case RoadConnection.LeftIn:
            position.x -= Consts.laneWidth * laneOffset;
            position.z -= Consts.laneWidth / 2;
            break;
            case RoadConnection.LeftOut:
            position.x -= Consts.laneWidth * laneOffset;
            position.z += Consts.laneWidth / 2;
            break;
            case RoadConnection.RightIn:
            position.x += Consts.laneWidth * laneOffset;
            position.z += Consts.laneWidth / 2;
            break;
            case RoadConnection.RightOut:
            position.x += Consts.laneWidth * laneOffset;
            position.z -= Consts.laneWidth / 2;
            break;
            case RoadConnection.BotIn:
            position.x += Consts.laneWidth / 2;
            position.z -= Consts.laneWidth * laneOffset;
            break;
            case RoadConnection.BotOut:
            position.x -= Consts.laneWidth / 2;
            position.z -= Consts.laneWidth * laneOffset;
            break;
        }
        return position;
    }

    List<RoadConnection> getValidOutgoing(RoadConnection from)
    {
        return new();
    }

    public override void updateLaneConnections()
    {
        for (int i = 0; i < laneConnections.Length; i++) laneConnections[i].Clear();

        var innerConnectionCurves = new (RoadConnection connectionIn, RoadConnection connectionOut, bool clockwiseCurve)[]
        {
            (RoadConnection.TopIn, RoadConnection.RightOut, false),
            (RoadConnection.TopIn, RoadConnection.LeftOut, true),
            (RoadConnection.BotIn, RoadConnection.RightOut, true),
            (RoadConnection.BotIn, RoadConnection.LeftOut, false),
            (RoadConnection.LeftIn, RoadConnection.TopOut, false),
            (RoadConnection.LeftIn, RoadConnection.BotOut, true),
            (RoadConnection.RightIn, RoadConnection.BotOut, false),
            (RoadConnection.RightIn, RoadConnection.TopOut, true),
        };

        var innerConnectionStraights = new (RoadConnection connectionIn, RoadConnection connectionOut)[]
        {
            (RoadConnection.TopIn, RoadConnection.BotOut),
            (RoadConnection.BotIn, RoadConnection.TopOut),
            (RoadConnection.LeftIn, RoadConnection.RightOut),
            (RoadConnection.RightIn, RoadConnection.LeftOut),
        };


        foreach (var (connectionIn, connectionOut, clockwiseCurve) in innerConnectionCurves)
        {
            laneConnections[(int)connectionIn].Add(new LaneConnection(
                connections[connectionIn],
                connections[connectionOut],
                Util.GenerateArc(cPos[(int)connectionIn], cPos[(int)connectionOut], clockwiseCurve),
                this
            ));
        }

        foreach (var (connectionIn, connectionOut) in innerConnectionStraights)
        {
            laneConnections[(int)connectionIn].Add(new LaneConnection(
                connections[connectionIn],
                connections[connectionOut],
                Util.GenerateLine(cPos[(int)connectionIn], cPos[(int)connectionOut]),
                this
            ));
        }
    }

    public override CarAction getCarAction(Car car)
    {
        return CarAction.Drive;
    }

    public override List<LaneConnection> getLaneConnections(Lane lane)
    {
        foreach (var pair in connections)
        {
            if (pair.Value == lane)
                return laneConnections[(int)pair.Key];
        }

        return new();
    }

    public override List<LaneConnection> getLaneConnections()
    {
        return laneConnections.SelectMany(x => x).ToList();
    }
}