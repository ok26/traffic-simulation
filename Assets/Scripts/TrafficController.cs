using System.Collections.Generic;
using UnityEngine;

public class TrafficController : MonoBehaviour
{
    public RoadNetwork roadNetwork;
    public GameObject carPrefab;
    public float spawnInterval = 3.0f;
    public float minSpawnDistance = 3.0f;

    private float timer = 0f;

    private (int startId, int goalId)[] spawnConfigs = new (int, int)[]
    {
        (0, 1),
        (1, 0),
        (2, 3),
        (3, 2),
        (0, 2),
        (1, 3),
        (2, 0),
        (3, 1),
        (0, 3),
        (1, 2),
        (2, 1),
        (3, 0),
    };

    void Update()
    {
        if (roadNetwork == null || carPrefab == null)
            return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            if (SpawnCar())
                timer = 0f;
        }
    }

    bool SpawnCar()
    {
        int randomIndex = Random.Range(0, spawnConfigs.Length);
        var (startId, goalId) = spawnConfigs[randomIndex];

        RoadNode startNode = roadNetwork.GetNodeById(startId);   
        RoadNode goalNode = roadNetwork.GetNodeById(goalId);

        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning($"Failed to find nodes: start={startId}, goal={goalId}");
            return false;
        }

        List<Lane> outgoingLanes = roadNetwork.GetOutgoingLanes(startNode);
        if (outgoingLanes.Count == 0)
        {
            Debug.LogWarning($"No outgoing lanes from node {startId}");
            return false;
        }

        RoadPath path = AStar.FindPath(roadNetwork, startNode, goalNode);
        if (path == null || path.StartingLane == null)
        {
            Debug.LogWarning($"No valid path from node {startId} to node {goalId}");
            return false;
        }

        Lane startLane = path.StartingLane;
        Vector3 spawnPosition = startLane.Points[0];
        
        // Adjust so that cars are above ground
        Renderer renderer = carPrefab.GetComponent<Renderer>();
        if (renderer != null)
            spawnPosition.y += renderer.bounds.size.y / 2f;

        if (IsSpawnPositionBlocked(startLane, spawnPosition))
            return false;
        
        Vector3 laneDirection = Vector3.forward;
        if (startLane.Points.Count >= 2)
        {
            laneDirection = (startLane.Points[1] - startLane.Points[0]).normalized;
        }

        Quaternion spawnRotation = Quaternion.LookRotation(laneDirection, Vector3.up);
        GameObject carObj = Instantiate(carPrefab, spawnPosition, spawnRotation);
        Car car = carObj.GetComponent<Car>();

        if (car == null)
        {
            Debug.LogWarning("Car prefab does not have a Car component");
            Destroy(carObj);
            return false;
        }

        car.roadNetwork = roadNetwork;
        car.startPoint = startNode;
        car.goal = goalNode;
        car.position = spawnPosition;
        car.direction = laneDirection;
        return true;
    }

    bool IsSpawnPositionBlocked(Lane lane, Vector3 spawnPosition)
    {
        foreach (Car laneCar in lane.CarsInLane.Values)
        {
            if (laneCar == null)
                continue;

            if (Vector3.Distance(laneCar.position, spawnPosition) < minSpawnDistance)
                return true;
        }

        return false;
    }
}
