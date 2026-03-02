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

public abstract class CarAction {}

public sealed class Drive : CarAction
{
    public float Speed { get; }

    public Drive(float speed)
    {
        Speed = speed;
    }
}

public sealed class Wait : CarAction
{
    public Vector3 AtPos { get; }

    public Wait(Vector3 atPos)
    {
        AtPos = atPos;
    }
}


public abstract class NodeBehavior
{
    protected Dictionary<RoadConnection, Lane> connections = new();
    public abstract Vector3 GetPositionOfConnection(RoadConnection connection);
    public abstract Vector3 GetPosition();
    public abstract CarAction GetCarAction(Car car, LaneConnection laneConnection, float incomingSpeedLimit);
    public abstract void UpdateLaneConnections();
    public abstract List<LaneConnection> GetLaneConnections(Lane lane);

    // Debugging for now
    public abstract List<LaneConnection> GetLaneConnections();

    public virtual void ConnectLane(Lane lane, RoadConnection connection)
    {
        connections[connection] = lane;
    }

    public virtual Vector3 GetPositionOfConnection(Lane lane)
    {
        foreach (var pair in connections)
        {
            if (pair.Value == lane)
                return GetPositionOfConnection(pair.Key);
        }

        return Vector3.zero;
    }
}

// TODO: Add rotations, yeah you wish
public class Endpoint : NodeBehavior
{
    Vector3 position;

    public Endpoint(Vector3 position)
    {
        this.position = position;
    }

    public override Vector3 GetPosition()
    {
        return position;
    }

    public override Vector3 GetPositionOfConnection(RoadConnection connection)
    {
        Vector3 position = this.position;

        switch (connection)
        {
            case RoadConnection.TopIn:
            case RoadConnection.BotOut:
            position.x -= Constants.laneWidth / 2;
            break;
            case RoadConnection.TopOut:
            case RoadConnection.BotIn:
            position.x += Constants.laneWidth / 2;
            break;
            case RoadConnection.LeftIn:
            case RoadConnection.RightOut:
            position.z -= Constants.laneWidth / 2;
            break;
            case RoadConnection.LeftOut:
            case RoadConnection.RightIn:
            position.z += Constants.laneWidth / 2;
            break;
        }

        return position;
    }

    public override CarAction GetCarAction(Car car, LaneConnection laneConnection, float incomingSpeedLimit)
    {
        return new Drive(4.0f);
    }

    public override List<LaneConnection> GetLaneConnections(Lane lane)
    {
        return new();
    }

    public override void UpdateLaneConnections()
    {
        throw new NotImplementedException();
    }

    public override List<LaneConnection> GetLaneConnections()
    {
        return new();
    }
}

public abstract class SharedGeometryIntersection : NodeBehavior
{
    readonly Vector3 position;

    // Connection positions, indexed with RoadConnection
    readonly Vector3[] cPos = new Vector3[Enum.GetValues(typeof(RoadConnection)).Length];

    // LaneConnections for each connected incoming lane
    readonly List<LaneConnection>[] laneConnections = 
        new List<LaneConnection>[Enum.GetValues(typeof(RoadConnection)).Length];

    protected SharedGeometryIntersection(Vector3 position)
    {
        this.position = position;

        for (int i = 0; i < laneConnections.Length; i++)
        {
            laneConnections[i] = new();
        }

        foreach (RoadConnection connection in Enum.GetValues(typeof(RoadConnection)))
        {
            cPos[(int)connection] = GetPositionOfConnection(connection);
        }
    }

    public override Vector3 GetPosition()
    {
        return position;
    }

    public override Vector3 GetPositionOfConnection(RoadConnection connection)
    {

        Vector3 position = this.position;

        const float laneOffset = 1.2f;
        
        switch (connection)
        {
            case RoadConnection.TopIn:
            position.x -= Constants.laneWidth / 2;
            position.z += Constants.laneWidth * laneOffset;
            break;
            case RoadConnection.TopOut:
            position.x += Constants.laneWidth / 2;
            position.z += Constants.laneWidth * laneOffset;
            break;
            case RoadConnection.LeftIn:
            position.x -= Constants.laneWidth * laneOffset;
            position.z -= Constants.laneWidth / 2;
            break;
            case RoadConnection.LeftOut:
            position.x -= Constants.laneWidth * laneOffset;
            position.z += Constants.laneWidth / 2;
            break;
            case RoadConnection.RightIn:
            position.x += Constants.laneWidth * laneOffset;
            position.z += Constants.laneWidth / 2;
            break;
            case RoadConnection.RightOut:
            position.x += Constants.laneWidth * laneOffset;
            position.z -= Constants.laneWidth / 2;
            break;
            case RoadConnection.BotIn:
            position.x += Constants.laneWidth / 2;
            position.z -= Constants.laneWidth * laneOffset;
            break;
            case RoadConnection.BotOut:
            position.x -= Constants.laneWidth / 2;
            position.z -= Constants.laneWidth * laneOffset;
            break;
        }
        return position;
    }

    public override void UpdateLaneConnections()
    {
        for (int i = 0; i < laneConnections.Length; i++) laneConnections[i].Clear();

        var innerConnectionCurves = new (RoadConnection connectionIn, RoadConnection connectionOut, bool clockwiseCurve)[]
        {
            (RoadConnection.TopIn, RoadConnection.RightOut, false), //Left
            (RoadConnection.TopIn, RoadConnection.LeftOut, true), // Right
            (RoadConnection.BotIn, RoadConnection.RightOut, true), // Right
            (RoadConnection.BotIn, RoadConnection.LeftOut, false), // Left
            (RoadConnection.LeftIn, RoadConnection.TopOut, false), // Left
            (RoadConnection.LeftIn, RoadConnection.BotOut, true), // Right
            (RoadConnection.RightIn, RoadConnection.BotOut, false), // 
            (RoadConnection.RightIn, RoadConnection.TopOut, true),
        };

        var innerConnectionStraights = new (RoadConnection connectionIn, RoadConnection connectionOut)[]
        {
            (RoadConnection.TopIn, RoadConnection.BotOut),
            (RoadConnection.BotIn, RoadConnection.TopOut),
            (RoadConnection.LeftIn, RoadConnection.RightOut),
            (RoadConnection.RightIn, RoadConnection.LeftOut),
        };


        // Reasonable speeds in different turns
        float leftTurnSpeedLimit = 3.0f;
        float rightTurnSpeedLimit = 2.0f;
        float straightSpeedLimit = 4.0f;

        foreach (var (connectionIn, connectionOut, clockwiseCurve) in innerConnectionCurves)
        {

            // CW/CCW turns also correspond to Right/Left turns
            float speedLimit = clockwiseCurve ? rightTurnSpeedLimit : leftTurnSpeedLimit;

            laneConnections[(int)connectionIn].Add(new LaneConnection(
                connections[connectionIn],
                connections[connectionOut],
                Util.GenerateArc(cPos[(int)connectionIn], cPos[(int)connectionOut], clockwiseCurve),
                this,
                speedLimit
            ));
        }

        foreach (var (connectionIn, connectionOut) in innerConnectionStraights)
        {
            laneConnections[(int)connectionIn].Add(new LaneConnection(
                connections[connectionIn],
                connections[connectionOut],
                Util.GenerateLine(cPos[(int)connectionIn], cPos[(int)connectionOut]),
                this,
                straightSpeedLimit
            ));
        }
    }

    public override List<LaneConnection> GetLaneConnections(Lane lane)
    {
        foreach (var pair in connections)
        {
            if (pair.Value == lane)
                return laneConnections[(int)pair.Key];
        }

        return new();
    }

    public override List<LaneConnection> GetLaneConnections()
    {
        return laneConnections.SelectMany(x => x).ToList();
    }
}

public class StopSignIntersection : SharedGeometryIntersection
{

    public StopSignIntersection(Vector3 position) : base(position)
    {
    }

    public override CarAction GetCarAction(Car car, LaneConnection laneConnection, float incomingSpeedLimit)
    {
        return new Drive(2.0f);
    }
}

public class TrafficLightIntersection : SharedGeometryIntersection
{
    enum LightPhase
    {
        NorthSouthGreen,
        NorthSouthYellow,
        EastWestGreen,
        EastWestYellow
    }

    readonly float greenDuration = 8f;
    readonly float yellowDuration = 1f; // Will try to stop for yellow
    readonly float phaseOffset;
    readonly float comfortableRedStopDecel = 3.0f;
    readonly float stopBufferDistance = 0.75f;

    public TrafficLightIntersection(Vector3 position) : base(position)
    {
        phaseOffset = UnityEngine.Random.Range(0f, 6f);
    }

    public override CarAction GetCarAction(Car car, LaneConnection laneConnection, float incomingSpeedLimit)
    {
        LightPhase phase = GetCurrentPhase();
        bool northSouthOpen = phase == LightPhase.NorthSouthGreen;
        bool eastWestOpen = phase == LightPhase.EastWestGreen;

        bool carIsNorthSouth = Mathf.Abs(car.direction.z) >= Mathf.Abs(car.direction.x);
        bool hasGreen = carIsNorthSouth ? northSouthOpen : eastWestOpen;
        RoadConnection connection = GetClosestIncomingConnection(car.position);
        float distanceToConnection = Vector3.Distance(car.FrontBumberPosition, GetPositionOfConnection(connection));

        if (hasGreen || car.inIntersection)
        {
            float reactionDistance = Mathf.Max(0.1f, 2.0f * car.velocity);
            float k = Mathf.Max(0.0f, 1.0f - (distanceToConnection / reactionDistance));
            float speedLimit = car.inIntersection ?
                laneConnection.SpeedLimit :
                Mathf.Lerp(incomingSpeedLimit, laneConnection.SpeedLimit, k);
            return new Drive(speedLimit);
        }

        float requiredStopDistance = (car.velocity * car.velocity) / (2f * comfortableRedStopDecel) + stopBufferDistance;
        bool tooLateToStopComfortably = distanceToConnection <= requiredStopDistance;

        if (tooLateToStopComfortably)
            return new Drive(laneConnection.SpeedLimit);

        return new Wait(GetPositionOfConnection(connection));
    }

    LightPhase GetCurrentPhase()
    {
        float fullCycle = (greenDuration + yellowDuration) * 2f;
        float t = (Time.time + phaseOffset) % fullCycle;

        if (t < greenDuration)
            return LightPhase.NorthSouthGreen;

        t -= greenDuration;
        if (t < yellowDuration)
            return LightPhase.NorthSouthYellow;

        t -= yellowDuration;
        if (t < greenDuration)
            return LightPhase.EastWestGreen;

        return LightPhase.EastWestYellow;
    }

    RoadConnection GetClosestIncomingConnection(Vector3 carPosition)
    {
        RoadConnection[] incoming =
        {
            RoadConnection.TopIn,
            RoadConnection.BotIn,
            RoadConnection.LeftIn,
            RoadConnection.RightIn,
        };

        RoadConnection closest = incoming[0];
        float closestDistance = float.MaxValue;

        foreach (RoadConnection connection in incoming)
        {
            Vector3 point = GetPositionOfConnection(connection);
            float distance = Vector3.Distance(carPosition, point);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = connection;
            }
        }

        return closest;
    }
}