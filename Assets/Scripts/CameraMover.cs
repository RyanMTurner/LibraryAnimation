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
        //Always start with the same straightaway
        grid.SpawnCluster(minHallLength, maxHallLength, currentHeading, wallPrefabs);
    }

    private void Update() {
        //Move in the direction you're heading
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

        //If you're close enough to the center of a junction, turn
        if (grid.CurrentCube != null) {
            if ((grid.CurrentCube.WorldPosition - transform.position).sqrMagnitude < 2) {
                transform.position = grid.CurrentCube.WorldPosition; //We detect at "close enough"; this ensures exact centering
                currentHeading = grid.CurrentCube.Direction; //Turn
                grid.SetCurrentPosition(grid.Cubes[grid.NextPosition ?? new Vector3Int()]); //Target the next junction
                grid.SpawnCluster(minHallLength, maxHallLength, currentHeading, wallPrefabs); //Create offshoots from the next junction
                if (grid.CurrentCube.Direction == currentFacing) { //If you can see the next-next junction, create offshoots from THAT
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

    //Used to allow non-monobehaviors to instantiate prefabs
    public static GameObject GlobalInstantiate(GameObject go, Vector3 pos, Quaternion rot) {
        return Instantiate(go, pos, rot);
    }

    //Used to allow non-monobehaviors to destroy gameobjects they've instantiated
    public static void GlobalDestroy(GameObject go) {
        Destroy(go);
    }

}
