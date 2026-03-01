using System;
using System.Collections.Generic;
using UnityEngine;

public class CarNavigator
{
    Car car;
    RoadPath carPath;
    RoadNetwork roadNetwork;

    // State, updated with CarNavigator.UpdateState()
    Lane currentLane;
    NodeBehavior currentIntersection;
    CarState carState;
    List<Vector3> currentPath;
    int closestPointIdx;

    SortedList<int, Car> CurrentCarList => carState == CarState.Lane
        ? currentLane?.CarsInLane
        : carPath?.Connections?.Count > 0 ? carPath.Connections.Peek().CarsInConnection : null;

    public bool inIntersection => carState == CarState.Intersection;

    public CarNavigator(Car car, RoadNetwork network, RoadNode start, RoadNode goal)
    {
        roadNetwork = network;
        carPath = AStar.FindPath(network, start, goal);
        if (carPath == null)
            return;

        currentLane = carPath.StartingLane;
        carState = CarState.Lane;
        this.car = car;
        closestPointIdx = addCarWithUniqueKey(currentLane.CarsInLane, 0);
    }

    public void OnCarDestroyed()
    {
        removeCarByValue(CurrentCarList);
    }

    public float SpeedLimit
    {
        get
        {
            switch (carState)
            {
                case CarState.Lane:
                return currentLane != null ? currentLane.Segment.SpeedLimit : 0f;
                case CarState.Intersection:
                return currentIntersection != null ? currentIntersection.SpeedLimit : 0f;
                case CarState.Lost:
                return 0f;
            }
            return 0f;
        }
    }

    // Returns if car has finished route
    public bool UpdateState()
    {

        List<Vector3> path = new();
        int lanePathPointCnt;
        int closestPointIdx;
        switch (carState)
        {
            case CarState.Lane:
                path.AddRange(currentLane.Points);
                lanePathPointCnt = path.Count;
                
                if (carPath.Connections.Count != 0)
                {
                    path.AddRange(carPath.Connections.Peek().TransitionCurve);
                }

                closestPointIdx = getClosestPointIdx(path);

                if (carPath.Connections.Count == 0 && path.Count - closestPointIdx <= 3)
                {
                    return true;
                }
                
                if (carPath.Connections.Count != 0 && closestPointIdx >= lanePathPointCnt)
                {
                    // Swap lane to laneconnection
                    moveCarBetweenLaneLists(
                        currentLane.CarsInLane,
                        carPath.Connections.Peek().CarsInConnection);
                    carState = CarState.Intersection;
                    currentIntersection = carPath.Connections.Peek().Behavior;
                    currentPath = new List<Vector3>(path.GetRange(lanePathPointCnt, path.Count - lanePathPointCnt));
                    closestPointIdx -= lanePathPointCnt;
                }

                currentPath = path;
                updateClosestPointIdx(closestPointIdx);
                break;
    
            case CarState.Intersection:
                LaneConnection currentLaneConnection = carPath.Connections.Peek();
                path.AddRange(currentLaneConnection.TransitionCurve);
                lanePathPointCnt = path.Count;
                path.AddRange(currentLaneConnection.To.Points);

                closestPointIdx = getClosestPointIdx(path);

                if (closestPointIdx >= lanePathPointCnt)
                {
                    // Swap from laneconnection to lane
                    moveCarBetweenLaneLists(
                        currentLaneConnection.CarsInConnection,
                        currentLaneConnection.To.CarsInLane);
                    carState = CarState.Lane;
                    currentLane = currentLaneConnection.To;
                    carPath.Connections.Pop();
                    currentPath = new List<Vector3>(path.GetRange(lanePathPointCnt, path.Count - lanePathPointCnt));
                    closestPointIdx -= lanePathPointCnt;
                }

                currentPath = path;
                updateClosestPointIdx(closestPointIdx);
                break;
        }

        return false;
    }

    // Helper UpdateState
    void moveCarBetweenLaneLists(SortedList<int, Car> from, SortedList<int, Car> to)
    {
        int? carKey = removeCarByValue(from);
        if (!carKey.HasValue)
            return;

        addCarWithUniqueKey(to, carKey.Value);
    }

    // Helper UpdateState
    void updateClosestPointIdx(int newClosestPointIdx)
    {
        SortedList<int, Car> carLaneList = CurrentCarList;
        if (carLaneList == null)
            return;

        removeCarByValue(carLaneList);
        closestPointIdx = addCarWithUniqueKey(carLaneList, newClosestPointIdx);
    }

    int? removeCarByValue(SortedList<int, Car> list)
    {
        if (list == null)
            return null;

        int removeKey = -1;
        foreach (var pair in list)
        {
            if (pair.Value == car)
            {
                removeKey = pair.Key;
                break;
            }
        }

        if (removeKey < 0)
            return null;

        list.Remove(removeKey);
        return removeKey;
    }

    int addCarWithUniqueKey(SortedList<int, Car> list, int desiredKey)
    {
        int key = Mathf.Max(0, desiredKey);
        while (list.ContainsKey(key))
            key++;

        list.Add(key, car);
        return key;
    }

    // Returns (speedLimit, distanceToNextCar, velocityOfNextCar)
    public (float, float, float) GetRoadInfo()
    {
        // Find next Car in lane
        SortedList<int, Car> carLaneList = carState == CarState.Lane ?
            currentLane.CarsInLane :
            carPath.Connections.Peek().CarsInConnection;

        int index = carLaneList.IndexOfKey(closestPointIdx);
        if (index == -1)
        {
            throw new Exception("Car not found");
        }

        Car nextCar = null;
        if (index + 1 < carLaneList.Count)
        {
            nextCar = carLaneList.Values[index + 1];
        }

        switch (carState)
        {
            case CarState.Lane:
                return getRoadInfoLane(nextCar);
            case CarState.Intersection:
                return getRoadInfoIntersection(nextCar);
        }

        throw new Exception("Unreachable");
    }

    // Helper GetRoadInfo
    (float, float, float) getRoadInfoLane(Car nextCar)
    {
        if (nextCar != null)
        {
            float distanceToNextCar = Vector3.Distance(
                car.FrontBumberPosition,
                nextCar.BackBumperPosition
            );
            return (SpeedLimit, distanceToNextCar, nextCar.velocity);
        }

        if (carPath.Connections.Count == 0)
            return (SpeedLimit, 100f, SpeedLimit);

        CarAction action = carPath.Connections.Peek().Behavior.GetCarAction(car);
        switch (action)
        {
            case Drive drive:
                return (drive.Speed, 100f, drive.Speed);
            case Wait wait:
                float distanceToWait = Vector3.Distance(car.FrontBumberPosition, wait.AtPos);
                return (SpeedLimit, distanceToWait, 0f);
        }

        throw new Exception("Unreachable");
    }

    // Helper GetRoadInfo
    (float, float, float) getRoadInfoIntersection(Car nextCar)
    {
        CarAction action = carPath.Connections.Peek().Behavior.GetCarAction(car);
        switch (action)
        {
            case Drive drive:
                if (nextCar != null)
                {
                    float distanceToNextCar = Vector3.Distance(
                        car.FrontBumberPosition,
                        nextCar.BackBumperPosition
                    );
                    return (drive.Speed, distanceToNextCar, nextCar.velocity);
                }
                return (drive.Speed, 100f, drive.Speed);

            case Wait wait:
                float distanceToWait = Vector3.Distance(car.FrontBumberPosition, wait.AtPos);
                if (nextCar != null)
                {
                    float distanceToNextCar = Vector3.Distance(
                        car.FrontBumberPosition,
                        nextCar.BackBumperPosition
                    );
                    if (distanceToNextCar < distanceToWait)
                        return (SpeedLimit, distanceToNextCar, nextCar.velocity);
                }
                
                return (SpeedLimit, distanceToWait, 0f);
        }

        throw new Exception("Unreachable");
    }



    // Can connect Lane- and LaneConnection-paths, second argument: closestPointIndex
    public (List<Vector3>, int) GetUpcomingPath()
    {
        return (currentPath, closestPointIdx);
    }

    // Helper UpdateState
    int getClosestPointIdx(List<Vector3> points)
    {
        int closestPointIdx = 0;
        float closestDistance = Vector3.Distance(car.position, points[0]);
        for (int i = 1; i < points.Count; i++)
        {
            Vector3 point = points[i];
            float distanceToPoint = Vector3.Distance(car.position, point);
            if (distanceToPoint < closestDistance)
            {   
                closestDistance = distanceToPoint;
                closestPointIdx = i;
            }   
        }

        return closestPointIdx;
    }
}