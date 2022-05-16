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
                return Heading.Up;
            case Heading.Down:
                return Heading.Down;
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
    public readonly Vector3Int Position;
    public Dictionary<Heading, CubeFace> CubeFaces = new Dictionary<Heading, CubeFace>() {
        { Heading.North, new CubeFace() },
        { Heading.South, new CubeFace() },
        { Heading.East, new CubeFace() },
        { Heading.West, new CubeFace() },
        { Heading.Up, new CubeFace() },
        { Heading.Down, new CubeFace() },
    };

    public Cube(Vector3Int position, bool last, Heading direction, List<GameObject> wallPrefabs) {
        Position = position;
        if (last) {
            int newNumber = Random.Range(0, 5);
            Heading newDirection = WallHelpers.Headings.Where(x => x != direction).ElementAt(newNumber);
            CubeFaces[newDirection].travelDirection = true;
        }
        else {
            CubeFaces[direction].travelDirection = true;
        }
        foreach (var kvp in CubeFaces) {
            if (kvp.Value.travelDirection) { continue; }
            if (kvp.Key == direction.Opposite()) { continue; }

            kvp.Value.hasFace = !last || Random.Range(0, 2) == 1;
            if (kvp.Value.hasFace) {
                CameraMover.GlobalInstantiate(wallPrefabs.GetSide(kvp.Key), 
                        new Vector3(wallPrefabs.GetSide(kvp.Key).transform.position.x + CubeGrid.UnitsEast * position.x,
                        wallPrefabs.GetSide(kvp.Key).transform.position.y + CubeGrid.UnitsUp * position.y,
                        wallPrefabs.GetSide(kvp.Key).transform.position.z + CubeGrid.UnitsNorth * position.z), 
                    wallPrefabs.GetSide(kvp.Key).transform.rotation);
            }
        }
    }
}

public class CubeGrid {
    public Vector3Int CurrentPosition = new Vector3Int(0, 0, -1);
    public Dictionary<Vector3Int, Cube> Cubes = new Dictionary<Vector3Int, Cube>();

    public static readonly float UnitsNorth = 14.2167f;
    public static readonly float UnitsEast = 10f;
    public static readonly float UnitsUp = 10f;

    //Returns the position of the last spawned cube; e.g. to set CurrentPosition
    public Vector3Int SpawnHallway(Vector3Int startingPos, int length, Heading direction, List<GameObject> wallPrefabs) {
        Vector3Int spawnAt = new Vector3Int(startingPos.x, startingPos.y, startingPos.z);
        for (int i = 1; i <= length; i++) {
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
            Cube newCube = new Cube(spawnAt, i == length, direction, wallPrefabs);
            Cubes.Add(spawnAt, newCube);
        }
        return spawnAt;
    }
}