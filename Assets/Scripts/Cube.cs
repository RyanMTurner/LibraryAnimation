using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum Heading {
    North,
    South,
    East,
    West,
    Up,
    Down
}

public static class WallHelpers {
    public static GameObject GetSide(this List<GameObject> wallPrefabs, Heading heading) {
        return wallPrefabs[(int)heading];
    }

    public static Heading Opposite(this Heading heading) {
        switch (heading) {
            default:
            case Heading.North:
                return Heading.South;
            case Heading.South:
                return Heading.North;
            case Heading.East:
                return Heading.West;
            case Heading.West:
                return Heading.East;
            case Heading.Up:
                return Heading.Down;
            case Heading.Down:
                return Heading.Up;
        }
    }

    public static List<Heading> Headings = new List<Heading>() { Heading.North, Heading.South, Heading.East, Heading.West, Heading.Up, Heading.Down };
}

public class CubeFace {
    public bool travelDirection;
    public bool hasFace;
    public GameObject spawnedFace;
}

public class Cube
{
    public readonly Vector3Int GridPosition;
    public readonly Vector3 WorldPosition;
    public readonly Heading Direction;
    public Dictionary<Heading, CubeFace> CubeFaces = new Dictionary<Heading, CubeFace>() {
        { Heading.North, new CubeFace() },
        { Heading.South, new CubeFace() },
        { Heading.East, new CubeFace() },
        { Heading.West, new CubeFace() },
        { Heading.Up, new CubeFace() },
        { Heading.Down, new CubeFace() },
    };

    public Cube(Vector3Int position, bool last, Heading direction, bool cap, List<GameObject> wallPrefabs) {
        GridPosition = position;
        WorldPosition = new Vector3(GridPosition.x * CubeGrid.UnitsEast, GridPosition.y * CubeGrid.UnitsUp, GridPosition.z * CubeGrid.UnitsNorth);

        if (last) { //If this is the last segment of a hallway, turn
            int newNumber = Random.Range(0, 4);
            Heading newDirection = WallHelpers.Headings.Where(x => x != direction && x != direction.Opposite()).ElementAt(newNumber); //You must change direction and cannot 180
            CubeFaces[newDirection].travelDirection = true;
            Direction = newDirection;
        }
        else { //Otherwise, don't turn
            CubeFaces[direction].travelDirection = true;
            Direction = direction;
        }

        foreach (var kvp in CubeFaces) {
            if (kvp.Key == direction.Opposite() || kvp.Value.travelDirection) { //Only seal off the direction you're going or coming from if this is the last segment of a capped hallway
                kvp.Value.hasFace = last && cap;
            }
            else {
                if (last) { //If this is the last segment of a hallway, seal all its faces. If it's the last segment of an uncapped hallway, each face has a 50/50 to be sealed.
                    kvp.Value.hasFace = cap || Random.Range(0, 2) == 1;
                }
                else { //If this is NOT the last segment of a hallway, seal all faces other than the direction you're going or coming from (see first if statement in foreach above).
                    kvp.Value.hasFace = true;
                }
            }
            if (kvp.Value.hasFace) { //Create the gameobject visual
                kvp.Value.spawnedFace = CameraMover.GlobalInstantiate(wallPrefabs.GetSide(kvp.Key), 
                        new Vector3(wallPrefabs.GetSide(kvp.Key).transform.position.x + CubeGrid.UnitsEast * position.x,
                        wallPrefabs.GetSide(kvp.Key).transform.position.y + CubeGrid.UnitsUp * position.y,
                        wallPrefabs.GetSide(kvp.Key).transform.position.z + CubeGrid.UnitsNorth * position.z), 
                    wallPrefabs.GetSide(kvp.Key).transform.rotation);
            }
        }
    }

    public void Destroy() {
        foreach (var kvp in CubeFaces) {
            if (kvp.Value.hasFace) {
                CameraMover.GlobalDestroy(kvp.Value.spawnedFace);
            }
        }
    }
}

public class CubeGrid {
    private Vector3Int currentPosition = new Vector3Int(0, 0, -1);
    public Vector3Int CurrentPosition => currentPosition; //The location to which the camera is currently moving
    public Cube CurrentCube { get; private set; } = null;
    public void SetCurrentPosition(Cube cube) {
        CurrentCube = cube;
        currentPosition = cube.GridPosition;
    }

    public Vector3Int? NextPosition = null; //The location to which the camera should start moving once it reaches CurrentPosition
    public Dictionary<Vector3Int, Cube> Cubes = new Dictionary<Vector3Int, Cube>();
    public List<Cube> CubeList = new List<Cube>();

    public static readonly float UnitsNorth = 14.2167f;
    public static readonly float UnitsEast = 10f;
    public static readonly float UnitsUp = 10f;

    //Returns the position of the last spawned cube; e.g. to set as the next target
    public Vector3Int SpawnHallway(Vector3Int startingPos, int length, Heading direction, bool cap, List<GameObject> wallPrefabs) {
        Vector3Int spawnAt = new Vector3Int(startingPos.x, startingPos.y, startingPos.z);

        for (int i = 1; i <= length; i++) { //Start at one so we don't replace the cube already at startingPos
            spawnAt = new Vector3Int(startingPos.x, startingPos.y, startingPos.z);
            switch (direction) {
                case Heading.North:
                    spawnAt += new Vector3Int(0, 0, i);
                    break;
                case Heading.South:
                    spawnAt += new Vector3Int(0, 0, -i);
                    break;
                case Heading.East:
                    spawnAt += new Vector3Int(i, 0, 0);
                    break;
                case Heading.West:
                    spawnAt += new Vector3Int(-i, 0, 0);
                    break;
                case Heading.Up:
                    spawnAt += new Vector3Int(0, i, 0);
                    break;
                case Heading.Down:
                    spawnAt += new Vector3Int(0, -i, 0);
                    break;
            }

            Cube newCube = new Cube(spawnAt, i == length, direction, cap, wallPrefabs);

            //If we run into a previously generated cube, replace it
            if (Cubes.ContainsKey(spawnAt)) {
                Cubes[spawnAt].Destroy();
                CubeList.Remove(Cubes[spawnAt]);
                Cubes.Remove(spawnAt);
            }

            //Keep track of all spawned objects
            Cubes.Add(spawnAt, newCube);
            CubeList.Add(newCube);

            //Cap at 100 spawned cubes
            if (CubeList.Count > 100) {
                var delete = CubeList[0];
                CubeList.RemoveAt(0);
                Cubes.Remove(delete.GridPosition);
                delete.Destroy();
            }
        }
        return spawnAt;
    }

    public void SpawnCluster(int minLength, int maxLength, Heading direction, List<GameObject> wallPrefabs) {
        int length = Random.Range(minLength, maxLength + 1);

        //Target the next junction (creating the next junction if this is the first spawn, i.e. NextPosition is null)
        SetCurrentPosition(NextPosition == null ? Cubes[SpawnHallway(CurrentPosition, length, direction, false, wallPrefabs)] : Cubes[NextPosition ?? new Vector3Int()]);

        //Create a hallway off of each open face of the next junction (which together imo form a "cluster")
        foreach (var kvp in Cubes[CurrentPosition].CubeFaces) {
            if (!kvp.Value.hasFace && kvp.Key.Opposite() != direction) {
                int splitLength = Random.Range(minLength, maxLength + 1);
                var newPosition = SpawnHallway(CurrentPosition, splitLength, kvp.Key, !kvp.Value.travelDirection, wallPrefabs);
                if (kvp.Value.travelDirection) { //Set the way you'll travel from the cluster (see Cube constructor for where this is generated)
                    NextPosition = newPosition;
                }
            }
        }
    }

}