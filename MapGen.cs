using UnityEngine;
using System.Collections.Generic;

public class MapGen : MonoBehaviour
{
    // Array to store different room prefabs
    public GameObject[] roomPrefabs;

    // Reference to the first room, which must be manually assigned in the Inspector
    public GameObject firstRoom;

    // Distance between consecutive rooms along X and Z axes
    public float roomWidth = 20f;
    public float roomHeight = 20f;

    // Number of columns and rows in the grid
    public int numberOfColumns = 5;
    public int numberOfRows = 5;

    // Maximum number of rooms to spawn
    public int numberOfRooms = 20;

    // Grid visibility in Scene View
    public bool drawGrid = true;

    // Dimensions for the boxes in the grid
    public float boxWidth = 20f;
    public float boxHeight = 20f;

    private Vector3 firstRoomPos;
    private Dictionary<Vector3, GameObject> spawnedRooms = new Dictionary<Vector3, GameObject>();

    void Start()
    {
        
        if (firstRoom != null)
        {
            firstRoomPos = firstRoom.transform.position;
            GenerateMap(firstRoomPos);
        }
        else
        {
            Debug.LogError("First room not assigned. Please assign a first room in the inspector.");
        }
    }

    void Update()
    {
        // Draw grid in Scene View
        if (drawGrid)
        {
            DrawGrid();
        }
    }

    void GenerateMap(Vector3 startPos)
    {
        
        if (roomPrefabs.Length == 0)
        {
            Debug.LogError("No room prefabs assigned. Please assign room prefabs in the inspector.");
            return;
        }

        
        GameObject firstRoomInstance = SpawnRoomAtPosition(startPos, Vector3.zero);
        spawnedRooms.Add(RoundToGrid(startPos), firstRoomInstance);

        
        Queue<Vector3> positionsToProcess = new Queue<Vector3>();
        positionsToProcess.Enqueue(RoundToGrid(startPos));

        while (positionsToProcess.Count > 0 && spawnedRooms.Count < numberOfRooms)
        {
            Vector3 currentPos = positionsToProcess.Dequeue();
            GameObject currentRoom = spawnedRooms[currentPos];

            Transform spawnPosForward = currentRoom.transform.Find("SpawnPosForward");
            Transform spawnPosRight = currentRoom.transform.Find("SpawnPosRight");
            Transform spawnPosLeft = currentRoom.transform.Find("SpawnPosLeft");

           
            ProcessSpawnPosition(spawnPosForward, Vector3.forward, positionsToProcess);
            ProcessSpawnPosition(spawnPosRight, Vector3.right, positionsToProcess);
            ProcessSpawnPosition(spawnPosLeft, Vector3.left, positionsToProcess);
        }
    }

    void ProcessSpawnPosition(Transform spawnPos, Vector3 direction, Queue<Vector3> positionsToProcess)
    {
        if (spawnPos != null)
        {
            Vector3 position = RoundToGrid(spawnPos.position);

            
            if (!spawnedRooms.ContainsKey(position))
            {
                
                GameObject newRoom = SpawnRoomAtPosition(position, -spawnPos.forward);

                
                spawnedRooms.Add(position, newRoom);

                
                positionsToProcess.Enqueue(position);
            }
        }
    }

    GameObject SpawnRoomAtPosition(Vector3 position, Vector3 forwardDirection)
    {
        
        GameObject roomPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

        
        GameObject room = Instantiate(roomPrefab, position, Quaternion.identity);

        
        room.transform.position = new Vector3(position.x, 0, position.z);

        
        RotateRoomToMatchDirection(room, forwardDirection);

        return room;
    }

    void RotateRoomToMatchDirection(GameObject room, Vector3 direction)
    {
        
        float angle = Vector3.SignedAngle(room.transform.forward, direction, Vector3.up);

        
        room.transform.Rotate(0, angle, 0);
    }

    Vector3 RoundToGrid(Vector3 position)
    {
        
        float x = Mathf.Round((position.x - firstRoomPos.x) / roomWidth) * roomWidth + firstRoomPos.x;
        float z = Mathf.Round((position.z - firstRoomPos.z) / roomHeight) * roomHeight + firstRoomPos.z;
        return new Vector3(x, 0, z);
    }

    void DrawGrid()
    {
        
        for (int x = -numberOfColumns / 2; x < numberOfColumns / 2; x++)
        {
            for (int z = -numberOfRows / 2; z < numberOfRows / 2; z++)
            {
                Vector3 center = firstRoomPos + new Vector3(x * boxWidth, 0, z * boxHeight);
                DrawBox(center, boxWidth, boxHeight);
            }
        }
    }

    void DrawBox(Vector3 center, float width, float height)
    {
        Vector3 halfExtents = new Vector3(width / 2, 0, height / 2);

        // Calculate the corners of the box
        Vector3 topLeft = center + new Vector3(-halfExtents.x, 0, halfExtents.z);
        Vector3 topRight = center + new Vector3(halfExtents.x, 0, halfExtents.z);
        Vector3 bottomLeft = center + new Vector3(-halfExtents.x, 0, -halfExtents.z);
        Vector3 bottomRight = center + new Vector3(halfExtents.x, 0, -halfExtents.z);

        // Draw the lines of the box
        Debug.DrawLine(topLeft, topRight, Color.green);
        Debug.DrawLine(topRight, bottomRight, Color.green);
        Debug.DrawLine(bottomRight, bottomLeft, Color.green);
        Debug.DrawLine(bottomLeft, topLeft, Color.green);
    }
}
