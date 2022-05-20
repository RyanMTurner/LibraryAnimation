using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour {

    [SerializeField] Heading currentHeading = Heading.North;
    [SerializeField] Heading previousFacing = Heading.North;
    [SerializeField] DateTime? facingChanged = null;
    [SerializeField] Heading currentFacing = Heading.North;
    Heading CurrentFacing {
        get => currentFacing;
        set {
            previousFacing = currentFacing;
            facingChanged = DateTime.Now;
            currentFacing = value;
        }
    }
    bool rolledRotationChanceThisCube = false;
    bool didRotateThisCube = false;
    [SerializeField] int minHallLength = 6;
    [SerializeField] int maxHallLength = 6;
    [SerializeField] float cameraSpeed = 10;
    [SerializeField] int rotationTimeMS = 1000;
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

        //Rotate in the direction you're facing
        if (facingChanged.HasValue) {
            TimeSpan sinceChange = DateTime.Now - (DateTime)facingChanged;
            if (sinceChange.TotalMilliseconds < rotationTimeMS) {
                Vector3 eulerAngles = Vector3.Lerp(WallHelpers.HeadingRotations[previousFacing], WallHelpers.HeadingRotations[currentFacing], (float)sinceChange.TotalMilliseconds / rotationTimeMS);
                Quaternion quaternion = new Quaternion() { eulerAngles = eulerAngles };
                transform.rotation = quaternion;
            }
        }

        if (grid.CurrentCube != null) {
            float closeToCurrent = (grid.CurrentCube.WorldPosition - transform.position).sqrMagnitude;
            //If you're close enough to the center of a junction at which you can rotate camera, see if you should do so
            if (!rolledRotationChanceThisCube && closeToCurrent < 200) {
                rolledRotationChanceThisCube = true;
                if (CurrentFacing == Heading.North) { //If facing forward, maybe face up/down for vertical sections
                    if (grid.CurrentCube.Direction == Heading.Up || grid.CurrentCube.Direction == Heading.Down) {
                        int newFacing = UnityEngine.Random.Range(0, 3);
                        switch (newFacing) {
                            default:
                                didRotateThisCube = false;
                                break;
                            case 1:
                                CurrentFacing = Heading.Up;
                                didRotateThisCube = true;
                                break;
                            case 2:
                                CurrentFacing = Heading.Down;
                                didRotateThisCube = true;
                                break;
                        }
                    }
                    else {
                        didRotateThisCube = false;
                    }
                }
                else { //When coming out of a vertical section, face forward
                    didRotateThisCube = CurrentFacing != Heading.North;
                    CurrentFacing = Heading.North;
                }
            }

            //If you're close enough to the center of a junction, change heading
            if (closeToCurrent < 2) {
                transform.position = grid.CurrentCube.WorldPosition; //We detect at "close enough"; this ensures exact centering
                currentHeading = grid.CurrentCube.Direction; //Turn
                grid.SetCurrentPosition(grid.Cubes[grid.NextPosition ?? new Vector3Int()]); //Target the next junction
                grid.SpawnCluster(minHallLength, maxHallLength, currentHeading, wallPrefabs); //Create offshoots from the next junction
                if (grid.CurrentCube.Direction == CurrentFacing || didRotateThisCube) { //If you can see the next-next junction, create offshoots from THAT
                    foreach (var kvp in grid.Cubes[(Vector3Int)grid.NextPosition].CubeFaces) {
                        if (!kvp.Value.hasFace && kvp.Key.Opposite() != grid.CurrentCube.Direction) {
                            int splitLength = UnityEngine.Random.Range(minHallLength, maxHallLength + 1);
                            grid.SpawnHallway((Vector3Int)grid.NextPosition, splitLength, kvp.Key, true, wallPrefabs);
                        }
                    }
                }
                rolledRotationChanceThisCube = false;
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
