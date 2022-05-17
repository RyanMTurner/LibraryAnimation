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
    public readonly int Index;
    public Dictionary<Heading, CubeFace> CubeFaces = new Dictionary<Heading, CubeFace>() {
        { Heading.North, new CubeFace() },
        { Heading.South, new CubeFace() },
        { Heading.East, new CubeFace() },
        { Heading.West, new CubeFace() },
        { Heading.Up, new CubeFace() },
        { Heading.Down, new CubeFace() },
    };

    public Cube(Vector3Int position, bool last, Heading direction, bool cap, int index, List<GameObject> wallPrefabs) {
        GridPosition = position;
        WorldPosition = new Vector3(GridPosition.x * CubeGrid.UnitsEast, GridPosition.y * CubeGrid.UnitsUp, GridPosition.z * CubeGrid.UnitsNorth);
        Index = index;
        if (last) {
            int newNumber = Random.Range(0, 4);
            Heading newDirection = WallHelpers.Headings.Where(x => x != direction && x != direction.Opposite()).ElementAt(newNumber);
            CubeFaces[newDirection].travelDirection = true;
            Direction = newDirection;
        }
        else {
            CubeFaces[direction].travelDirection = true;
            Direction = direction;
        }
        foreach (var kvp in CubeFaces) {
            if (kvp.Key == direction.Opposite() || kvp.Value.travelDirection) {
                kvp.Value.hasFace = last && cap;
            }
            else {
                if (last) {
                    kvp.Value.hasFace = cap || Random.Range(0, 2) == 1;
                }
                else {
                    kvp.Value.hasFace = true;
                }
            }
            if (kvp.Value.hasFace) {
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
    public Vector3Int CurrentPosition => currentPosition;
    public Cube CurrentCube { get; private set; } = null;
    public void SetCurrentPosition(Cube cube) {
        CurrentCube = cube;
        currentPosition = cube.GridPosition;
    }

    private List<Vector3Int> previousPositions = new List<Vector3Int>();
    public Vector3Int? PreviousPosition {
        get => previousPositions.Count > 0 ? previousPositions?.Last() : null;
        set {
            if (previousPositions.Count > 1) {
                Cube lastCube = Cubes[(Vector3Int)previousPositions[0]];
                List<Vector3Int> toRemove = new List<Vector3Int>();
                foreach (var kvp in Cubes) {
                    if (kvp.Value.Index < lastCube.Index) {
                        toRemove.Add(kvp.Key);
                        kvp.Value.Destroy();
                    }
                }
                foreach (var item in toRemove) {
                    Cubes.Remove(item);
                }
                previousPositions.RemoveAt(0);
            }
            previousPositions.Add((Vector3Int)value);
        }
    }
    public Vector3Int? NextPosition = null;
    public Dictionary<Vector3Int, Cube> Cubes = new Dictionary<Vector3Int, Cube>();
    public int CubeCounter { get; private set; }

    public static readonly float UnitsNorth = 14.2167f;
    public static readonly float UnitsEast = 10f;
    public static readonly float UnitsUp = 10f;

    //Returns the position of the last spawned cube; e.g. to set CurrentPosition
    public Vector3Int SpawnHallway(Vector3Int startingPos, int length, Heading direction, bool cap, List<GameObject> wallPrefabs) {
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
            Cube newCube = new Cube(spawnAt, i == length, direction, cap, CubeCounter, wallPrefabs);
            if (Cubes.ContainsKey(spawnAt)) {
                Cubes[spawnAt].Destroy();
                Cubes.Remove(spawnAt);
            }
            Cubes.Add(spawnAt, newCube);
            CubeCounter++;
        }
        return spawnAt;
    }

    public void SpawnCluster(int minLength, int maxLength, Heading direction, List<GameObject> wallPrefabs) {
        int length = Random.Range(minLength, maxLength + 1);
        SetCurrentPosition(NextPosition == null ? Cubes[SpawnHallway(CurrentPosition, length, direction, false, wallPrefabs)] : Cubes[NextPosition ?? new Vector3Int()]);
        foreach (var kvp in Cubes[CurrentPosition].CubeFaces) {
            if (!kvp.Value.hasFace && kvp.Key.Opposite() != direction) {
                int splitLength = Random.Range(minLength, maxLength + 1);
                var newPosition = SpawnHallway(CurrentPosition, splitLength, kvp.Key, !kvp.Value.travelDirection, wallPrefabs);
                if (kvp.Value.travelDirection) {
                    NextPosition = newPosition;
                }
            }
        }
        Debug.Log($"Prev: {PreviousPosition}\nCurr: {CurrentPosition}\nNext: {NextPosition}");
    }

}