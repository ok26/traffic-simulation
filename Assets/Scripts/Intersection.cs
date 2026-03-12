using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;
using UnityEngine;

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
    protected Dictionary<int, Lane> connections = new();
    public abstract Vector3 GetPositionOfLaneCon(int connection);
    public abstract Vector3 GetPosition();
    public abstract CarAction GetCarAction(Car car, LaneConnection laneConnection, float incomingSpeedLimit);
    public abstract void UpdateLaneConnections();
    public abstract List<LaneConnection> GetLaneConnections(Lane lane);
    public abstract Vector3 GetPositionOfSegCon(int direction);

    // Debugging for now
    public abstract List<LaneConnection> GetLaneConnections();

    public virtual void ConnectLane(Lane lane, int connection)
    {
        connections[connection] = lane;
    }

    public virtual Vector3 GetPositionOfConnection(Lane lane)
    {
        foreach (var pair in connections)
        {
            if (pair.Value == lane)
                return GetPositionOfLaneCon(pair.Key);
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

    public override Vector3 GetPositionOfLaneCon(int connection)
    {
        Vector3 position = this.position;

        switch (connection)
        {
            case 0:
            case 7:
            position.x -= Constants.laneWidth / 2;
            break;
            case 1:
            case 6:
            position.x += Constants.laneWidth / 2;
            position.x += Constants.laneWidth / 2;
            break;
            case 2:
            case 5:
            position.z -= Constants.laneWidth / 2;
            break;
            case 3:
            case 4:
            position.z += Constants.laneWidth / 2;
            break;
        }

        return position;
    }

    public override Vector3 GetPositionOfSegCon(int direction)
    {
        Vector3 position = this.position;

        switch (direction)
        {
            case 0: // North
                position.z += Constants.laneWidth / 2;
                break;
            case 1: // South
                position.z -= Constants.laneWidth / 2;
                break;
            case 2: // East
                position.x += Constants.laneWidth / 2;
                break;
            case 3: // West
                position.x -= Constants.laneWidth / 2;
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

/*
0 -> topIn
1 -> topOut
2 -> leftIn
3 -> leftOut
4 -> rightIn
5 -> rightOut
6 -> botIn
7 -> botOut 
*/
public abstract class SharedGeometryIntersection : NodeBehavior
{
    readonly Vector3 position;

    private const int numCons = 8;
    private const float laneOffset = 2.0f;

    // Connection positions, indexed with RoadConnection
    readonly Vector3[] cPos = new Vector3[numCons];

    // LaneConnections for each connected incoming lane
    readonly List<LaneConnection>[] laneConnections = 
        new List<LaneConnection>[numCons];

    protected SharedGeometryIntersection(Vector3 position)
    {
        this.position = position;

        for (int i = 0; i < laneConnections.Length; i++)
        {
            laneConnections[i] = new();
        }

        for (int connection = 0; connection < numCons; connection++)
        {
            cPos[connection] = GetPositionOfLaneCon(connection);
        }
    }

    public override Vector3 GetPosition()
    {
        return position;
    }

    public override Vector3 GetPositionOfLaneCon(int connection)
    {

        Vector3 position = this.position;
        
        switch (connection)
        {
            case 0: // TopIn
            position.x -= Constants.laneWidth / 2;
            position.z += Constants.laneWidth * laneOffset;
            break;
            case 1: // TopOut
            position.x += Constants.laneWidth / 2;
            position.z += Constants.laneWidth * laneOffset;
            break;
            case 2: // LeftIn
            position.x -= Constants.laneWidth * laneOffset;
            position.z -= Constants.laneWidth / 2;
            break;
            case 3: // LeftOut
            position.x -= Constants.laneWidth * laneOffset;
            position.z += Constants.laneWidth / 2;
            break;
            case 4: // RightIn
            position.x += Constants.laneWidth * laneOffset;
            position.z += Constants.laneWidth / 2;
            break;
            case 5: // RightOut
            position.x += Constants.laneWidth * laneOffset;
            position.z -= Constants.laneWidth / 2;
            break;
            case 6: // BotIn
            position.x += Constants.laneWidth / 2;
            position.z -= Constants.laneWidth * laneOffset;
            break;
            case 7: // BotOut
            position.x -= Constants.laneWidth / 2;
            position.z -= Constants.laneWidth * laneOffset;
            break;
        }
        return position;
        
    }
    
    public override Vector3 GetPositionOfSegCon(int direction)
    {
        Vector3 position = GetPosition();
        float intersection_padding = laneOffset;

        switch (direction)
        {
            case 0:
            position.z += Constants.laneWidth * intersection_padding;
            break;
            case 1:
            position.z -= Constants.laneWidth * intersection_padding;
            break;
            case 2:
            position.x += Constants.laneWidth * intersection_padding;
            break;
            case 3:
            position.x -= Constants.laneWidth * intersection_padding;
            break;
        }
        return position;
    }

    public override void UpdateLaneConnections()
    {
        for (int i = 0; i < laneConnections.Length; i++) laneConnections[i].Clear();

        var innerConnectionCurves = new (int connectionIn, int connectionOut, bool clockwiseCurve)[]
        {
            (0, 5, false),
            (0, 3, true), 
            (6, 5, true), 
            (6, 3, false), 
            (2, 1, false),
            (2, 7, true),
            (4, 7, false), 
            (4, 1, true),
        };

        var innerConnectionStraights = new (int connectionIn, int connectionOut)[]
        {
            (0, 7),
            (6, 1),
            (2, 5),
            (4, 3),
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
                return laneConnections[pair.Key];
        }

        return new();
    }

    protected int GetConnectionFromLane(Lane lane)
    {
        foreach (var pair in connections)
        {
            if (pair.Value == lane)
                return pair.Key;
        }

        return -1;
    }

    public override List<LaneConnection> GetLaneConnections()
    {
        return laneConnections.SelectMany(x => x).ToList();
    }

    protected int GetClosestIncomingConnection(Vector3 carPosition)
    {
        int[] incoming =
        {
            0, // TopIn
            6, // BotIn
            2, // LeftIn
            4, // RightIn
        };

        int closest = incoming[0];
        float closestDistance = float.MaxValue;

        foreach (int connection in incoming)
        {
            Vector3 point = GetPositionOfLaneCon(connection);
            float distance = Vector3.Distance(carPosition, point);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = connection;
            }
        }

        return closest;
    }

    protected List<LaneConnection> GetCrashingLanes(LaneConnection laneCon)
    {
        int connectionIn = GetConnectionFromLane(laneCon.From);
        int connectionOut = GetConnectionFromLane(laneCon.To);

        List<LaneConnection> crashingLanes = new();
        // All left-turns
        switch ((connectionIn, connectionOut))
        {
            case (6, 3):
                crashingLanes.Add(laneConnections[0].First(x => GetConnectionFromLane(x.To) == 7));
                crashingLanes.Add(laneConnections[0].First(x => GetConnectionFromLane(x.To) == 3));
                break;
            case (2, 1):
                crashingLanes.Add(laneConnections[4].First(x => GetConnectionFromLane(x.To) == 1));
                crashingLanes.Add(laneConnections[4].First(x => GetConnectionFromLane(x.To) == 3));
                break;
            case (0, 5):
                crashingLanes.Add(laneConnections[6].First(x => GetConnectionFromLane(x.To) == 1));
                crashingLanes.Add(laneConnections[6].First(x => GetConnectionFromLane(x.To) == 5));
                break;
            case (4, 7):
                crashingLanes.Add(laneConnections[2].First(x => GetConnectionFromLane(x.To) == 7));
                crashingLanes.Add(laneConnections[2].First(x => GetConnectionFromLane(x.To) == 5));
                break;
        }

        return crashingLanes;
    }

    protected bool IsLeftTurn(LaneConnection laneCon)
    {
        int connectionIn = GetConnectionFromLane(laneCon.From);
        int connectionOut = GetConnectionFromLane(laneCon.To);

        switch ((connectionIn, connectionOut))
        {
            case (6, 3):
            case (2, 1):
            case (0, 5):
            case (4, 7):
                return true;
        }

        return false;
    }
}

public class StopSignIntersection : SharedGeometryIntersection
{

    Queue<Car> waitingStack = new();
    HashSet<Car> waitingCars = new();
    Car currentDrivingCar;

    private float distanceToStopLine = 1.5f;
    private float distanceToExit = 1.0f;
    private float maxTurnClaimDuration = 8.0f;
    private float currentDrivingCarAssignedTime = -1.0f;

    public StopSignIntersection(Vector3 position) : base(position)
    {
    }

    public override CarAction GetCarAction(Car car, LaneConnection laneConnection, float incomingSpeedLimit)
    {
        int connectionIn = GetConnectionFromLane(laneConnection.From);
        int connectionOut = GetConnectionFromLane(laneConnection.To);
        if (connectionIn < 0 || connectionOut < 0)
            return new Drive(incomingSpeedLimit);

        float distanceToConIn = Vector3.Distance(car.FrontBumberPosition, GetPositionOfLaneCon(connectionIn));
        float distanceToConOut = Vector3.Distance(car.FrontBumberPosition, GetPositionOfLaneCon(connectionOut));

        if (currentDrivingCar == car && distanceToConOut <= distanceToExit)
        {
            currentDrivingCar = null;
        }

        if (currentDrivingCar != null && !currentDrivingCar.inIntersection)
        {
            float heldDuration = Time.time - currentDrivingCarAssignedTime;
            if (heldDuration > maxTurnClaimDuration)
                currentDrivingCar = null;
        }

        if (currentDrivingCar == null)
        {
            while (waitingStack.Count > 0)
            {
                Car nextCar = waitingStack.Dequeue();
                if (nextCar == null || !waitingCars.Contains(nextCar))
                    continue;

                currentDrivingCar = nextCar;
                waitingCars.Remove(currentDrivingCar);
                currentDrivingCarAssignedTime = Time.time;
                break;
            }
        }

        if (car != currentDrivingCar && !waitingCars.Contains(car) && distanceToConIn <= distanceToStopLine)
        {
            waitingStack.Enqueue(car);
            waitingCars.Add(car);
        }

        bool canDrive = currentDrivingCar == car;
        
        if (canDrive || car.inIntersection)
        {
            float reactionDistance = Mathf.Max(0.1f, 2.0f * car.velocity);
            float k = Mathf.Max(0.0f, 1.0f - (distanceToConIn / reactionDistance));
            float speedLimit = car.inIntersection ?
                laneConnection.SpeedLimit :
                Mathf.Lerp(incomingSpeedLimit, laneConnection.SpeedLimit, k);
            return new Drive(speedLimit);
        }

        return new Wait(GetPositionOfLaneCon(connectionIn));
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
        int connection = GetConnectionFromLane(laneConnection.From);
        float distanceToConnection = Vector3.Distance(car.FrontBumberPosition, GetPositionOfLaneCon(connection));

        bool hasToMakeWay = false;
        List<LaneConnection> crashingLanes = GetCrashingLanes(laneConnection);
        if (crashingLanes.Count > 0)
        {
            Lane fromLane = crashingLanes[0].From;
            foreach (LaneConnection laneCon in crashingLanes)
            {
                if (laneCon.CarsInConnection.Count > 0) hasToMakeWay = true;
            }

            if (fromLane.CarsInLane.Count > 0)
            {
                Car closestCar = fromLane.CarsInLane.Values[^1];

                if (!IsLeftTurn(closestCar.NextConnection))
                {
                    float distanceToIntersection = Vector3.Distance(
                    closestCar.FrontBumberPosition, GetPositionOfConnection(fromLane));
                    if (distanceToIntersection <= 2.5f)
                        hasToMakeWay = true;
                }
            }
        }

        if ((hasGreen && !hasToMakeWay) || car.inIntersection)
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

        return new Wait(GetPositionOfLaneCon(connection));
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

}