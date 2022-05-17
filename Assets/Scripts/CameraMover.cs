using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour {

    [SerializeField] Heading currentHeading = Heading.North;
    [SerializeField] Heading currentFacing = Heading.North;
    [SerializeField] int minHallLength = 6;
    [SerializeField] int maxHallLength = 6;
    [SerializeField] float cameraSpeed = 10;
    int gridX = 0;
    int gridY = 0;

    bool moving = true;

    [SerializeField] List<GameObject> wallPrefabs;

    CubeGrid grid = new CubeGrid();

    private void Start() {
        grid.SpawnCluster(minHallLength, maxHallLength, currentHeading, wallPrefabs);
    }

    private void Update() {
        float move = cameraSpeed * Time.deltaTime * (moving ? 1 : 0);
        switch (currentHeading) {
            case Heading.North:
                transform.position += new Vector3(0, 0, move);
                break;
            case Heading.South:
                transform.position += new Vector3(0, 0, -move);
                break;
            case Heading.East:
                transform.position += new Vector3(move, 0, 0);
                break;
            case Heading.West:
                transform.position += new Vector3(-move, 0, 0);
                break;
            case Heading.Up:
                transform.position += new Vector3(0, move, 0);
                break;
            case Heading.Down:
                transform.position += new Vector3(0, -move, 0);
                break;
        }

        if (grid.CurrentCube != null) {
            if ((grid.CurrentCube.WorldPosition - transform.position).sqrMagnitude < 2) {
                transform.position = grid.CurrentCube.WorldPosition;
                currentHeading = grid.CurrentCube.Direction;
                if (grid.NextPosition != null) {
                    grid.PreviousPosition = grid.CurrentPosition;
                }
                try {
                    grid.SetCurrentPosition(grid.Cubes[grid.NextPosition ?? new Vector3Int()]);
                }
                catch (KeyNotFoundException ex) {
                    Debug.LogError($"Unable to find key {grid.NextPosition}\n{ex.Message}");
                    Debug.Break();
                    return;
                }
                grid.SpawnCluster(minHallLength, maxHallLength, currentHeading, wallPrefabs);
                if (grid.CurrentCube.Direction == currentFacing) {
                    foreach (var kvp in grid.Cubes[(Vector3Int)grid.NextPosition].CubeFaces) {
                        if (!kvp.Value.hasFace && kvp.Key.Opposite() != grid.CurrentCube.Direction) {
                            int splitLength = UnityEngine.Random.Range(minHallLength, maxHallLength + 1);
                            grid.SpawnHallway((Vector3Int)grid.NextPosition, splitLength, kvp.Key, true, wallPrefabs);
                        }
                    }
                }
            }
        }
    }

    public static GameObject GlobalInstantiate(GameObject go, Vector3 pos, Quaternion rot) {
        return Instantiate(go, pos, rot);
    }

    public static void GlobalDestroy(GameObject go) {
        Destroy(go);
    }

}
