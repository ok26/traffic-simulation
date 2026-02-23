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
    CarState state;

    PurePursuit purePursuit;
    IDM idm;

    public Vector3 position = Vector3.zero;
    public Vector3 direction = Vector3.forward;
    public float velocity = 0.0f;
    public float wheelbase = 1.5f;
    public float steeringAngle = 0.0f;
    public float maxSpeed = 5f;

    public float speedLimit
    {
        get
        {
            switch (state)
            {
                case CarState.Lane:
                return currentLane != null ? currentLane.segment.speedLimit : 0f;
                case CarState.Intersection:
                return currentIntersection != null ? currentIntersection.speedLimit : 0f;
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

        roadPath = Astar.findPath(roadNetwork, startPoint, goal);
        if (roadPath == null)
            return;

        currentLane = roadPath.startingLane;
        state = CarState.Lane;
        purePursuit = new PurePursuit();
        idm = new IDM();
    }

    void Update()
    {
        switch (state)
        {
            case CarState.Lane:
            followLane();
            break;
            case CarState.Intersection:
            followIntersection();
            break;
            case CarState.Lost:
            // Placeholder might delete
            break;
        }
    }

    void followIntersection()
    {
        if (currentIntersection == null)
        {
            state = CarState.Lost;
            return;
        }

        CarAction action = currentIntersection.getCarAction(this);

        switch (action)
        {
            case CarAction.Drive:
            followPath(roadPath.connections.Peek().transitionCurve);
            break;
            case CarAction.Wait:
            break;
        }
    }

    void followLane()
    {
        if (currentLane == null)
        {
            state = CarState.Lost;
            return;
        }

        followPath(currentLane.points);
    }

    void followPath(List<Vector3> path)
    {
        steeringAngle = purePursuit.CalculateSteeringAngle(this, path);
        velocity += idm.CalculateCarAcceleration(this, speedLimit) * Time.deltaTime;
        
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
            if (roadPath.connections.Count == 0)
            {
                Destroy(gameObject);
            }
            else
            {
                updatePath();
            }
        }   
    }

    void updatePath()
    {
        switch (state)
        {
            case CarState.Lane:
            state = CarState.Intersection;
            currentIntersection = roadPath.connections.Peek().behavior;
            break;
            case CarState.Intersection:
            state = CarState.Lane;
            currentLane = roadPath.connections.Peek().to;
            roadPath.connections.Pop();
            break;
        }
    }
}
