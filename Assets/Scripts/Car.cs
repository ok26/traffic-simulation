using System.Collections.Generic;
using UnityEngine;

enum CarState
{
    Lane,
    Intersection,
    Lost
}

public class Car : MonoBehaviour
{
    public RoadNetwork roadNetwork;
    public RoadNode startPoint;
    public RoadNode goal;

    RoadPath roadPath;
    Lane currentLane;
    NodeBehavior currentIntersection;
    CarState carState;

    PurePursuit purePursuit;
    IDM idm;

    public Vector3 position = Vector3.zero;
    public Vector3 direction = Vector3.forward;
    public float velocity = 0.0f;
    public float wheelbase = 1.5f;
    public float steeringAngle = 0.0f;
    public float maxSpeed = 5f;

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

    void Start()
    {
        if (roadNetwork == null || startPoint == null || goal == null) 
            return;

        roadPath = AStar.FindPath(roadNetwork, startPoint, goal);
        if (roadPath == null)
            return;

        currentLane = roadPath.StartingLane;
        carState = CarState.Lane;
        purePursuit = new PurePursuit();
        idm = new IDM();
    }

    void Update()
    {
        switch (carState)
        {
            case CarState.Lane:
            FollowLane();
            break;
            case CarState.Intersection:
            FollowIntersection();
            break;
            case CarState.Lost:
            // Placeholder might delete
            break;
        }
    }

    void FollowIntersection()
    {
        if (currentIntersection == null)
        {
            carState = CarState.Lost;
            return;
        }

        CarAction action = currentIntersection.GetCarAction(this);

        switch (action)
        {
            case CarAction.Drive:
            FollowPath(roadPath.Connections.Peek().TransitionCurve);
            break;
            case CarAction.Wait:
            break;
        }
    }

    void FollowLane()
    {
        if (currentLane == null)
        {
            carState = CarState.Lost;
            return;
        }

        FollowPath(currentLane.Points);
    }

    void FollowPath(List<Vector3> path)
    {
        steeringAngle = purePursuit.CalculateSteeringAngle(this, path);
        velocity += idm.CalculateCarAcceleration(this, SpeedLimit) * Time.deltaTime;
        
        velocity = Mathf.Clamp(velocity, -maxSpeed, maxSpeed);

        float deltaRad = Mathf.Deg2Rad * steeringAngle;
        float angularVelocity = (velocity / wheelbase) * Mathf.Tan(deltaRad);

        if (angularVelocity != 0f)
        {
            Quaternion rot = Quaternion.AngleAxis(Mathf.Rad2Deg * angularVelocity * Time.deltaTime, Vector3.up);
            direction = rot * direction;
            direction.y = 0;
            direction.Normalize();
        }

        position += direction * velocity * Time.deltaTime;

        transform.position = position;
        if (direction != Vector3.zero) 
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up); 

        // Change (probable need to update how to detect when should change lane)
        if (Vector3.Distance(position, path[^1]) < 0.2f)
        {
            if (roadPath.Connections.Count == 0)
            {
                Destroy(gameObject);
            }
            else
            {
                UpdatePath();
            }
        }   
    }

    void UpdatePath()
    {
        switch (carState)
        {
            case CarState.Lane:
            carState = CarState.Intersection;
            currentIntersection = roadPath.Connections.Peek().Behavior;
            break;
            case CarState.Intersection:
            carState = CarState.Lane;
            currentLane = roadPath.Connections.Peek().To;
            roadPath.Connections.Pop();
            break;
        }
    }
}
