using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{

    [SerializeField] Heading currentHeading = Heading.North;
    [SerializeField] int minHallLength = 6;
    [SerializeField] float cameraSpeed = 10;
    int gridX = 0;
    int gridY = 0;

    [SerializeField] List<GameObject> wallPrefabs;

    CubeGrid grid = new CubeGrid();

    private void Start() {
        grid.SpawnHallway(grid.CurrentPosition, minHallLength + 1, currentHeading, wallPrefabs);
    }

    private void Update() {
        
    }

    public static GameObject GlobalInstantiate(GameObject go, Vector3 pos, Quaternion rot) {
        return Instantiate(go, pos, rot);
    }

}
