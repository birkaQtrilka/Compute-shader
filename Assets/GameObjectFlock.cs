using System.Collections.Generic;
using UnityEngine;

public class GameObjectFlock : Flocking<GameObjectBoid>
{
    struct FlockData
    {
        public Vector3 SeparationForce;
        public Vector3 CohesionForce;
        public Vector3 AlignmentForce;
    }
    [SerializeField] GameObject _boidPrefab;
    [SerializeField] Transform _container;
    [SerializeField] float _rotationSpeed;
    [SerializeField] protected float _binSize = .5f;
    [SerializeField] bool _binSearch;
    [SerializeField] bool _debugBins = false;
    Dictionary<Vector3Int, List<GameObjectBoid>> _bins;
    List<GameObjectBoid> _flock;

    public override void Restart()
    {
        for (int i = _container.childCount-1; i >= 0; i--)
        {
            Destroy(_container.GetChild(i).gameObject);
        }
        Debug.Log("Flock Count: " + _boidCount);
        _boids = new GameObjectBoid[_boidCount];
        _flock = new(_boidCount);
        _bins = new(_boidCount);
        UnityEngine.Random.InitState(12);
        for (int i = 0; i < _boidCount; i++)
        {
            Vector3 pos = UnityEngine.Random.insideUnitSphere * 1.0f;
            Vector3 vel = UnityEngine.Random.insideUnitSphere * 0.1f;
            GameObjectBoid boid = Init(pos, vel);
            _boids[i] = boid;
        }
    }

    void Update()
    {
        if (_binSearch)
            UpdateBins();

        for (int i = 0; i < _boids.Length; ++i)
        {
            GameObjectBoid currBoid = _boids[i];

            //to avoid checking against every other boid
            _flock.Clear();
            Search(currBoid, _persceptionDistance, _flock);

            FlockData flock = GetFlockData(_flock, currBoid);

            currBoid.Acceleration +=
                  flock.CohesionForce * _cohesionBias
                + flock.AlignmentForce * _alignmentBias
                + flock.SeparationForce * _sepparationBias;

            currBoid.Acceleration += CheckSimulationBounds(currBoid.Position) * _wallRepellForce;

            currBoid.Velocity += currBoid.Acceleration * Time.deltaTime;
            currBoid.Velocity.Limit(_maxSpeed);
            currBoid.Position += currBoid.Velocity * Time.deltaTime;
            currBoid.Acceleration = Vector3.zero;
            currBoid.OutsideUpdate();

        }
    }

    protected override GameObjectBoid Init(Vector3 pos, Vector3 vel)
    {
        return new GameObjectBoid(pos, vel, Instantiate(_boidPrefab, _container), _rotationSpeed);
    }

    void UpdateBins()
    {
        foreach (List<GameObjectBoid> list in _bins.Values)
        {
            list.Clear();
        }

        foreach (var boid in _boids)
        {
            Vector3Int bin = GetBoidBin(boid);
            if (_bins.TryGetValue(bin, out List<GameObjectBoid> list))
            {
                list.Add(boid);
            }
            else
            {
                _bins.Add(bin, new List<GameObjectBoid>() { boid });
            }
        }
    }
    
    Vector3Int GetBoidBin(GameObjectBoid boid)
    {
        Vector3 pos = boid.Position;
        return new Vector3Int(
            Mathf.FloorToInt(pos.x / _binSize),
            Mathf.FloorToInt(pos.y / _binSize),
            Mathf.FloorToInt(pos.z / _binSize)
        );
    }

    void Search(GameObjectBoid boid, float perception, List<GameObjectBoid> flock)
    {
        if (_binSearch)
            BinsSearch(boid, perception, flock);
        else
            NaiveSearch(boid, perception, flock);
    }

    void NaiveSearch(GameObjectBoid currBoid, float perception, List<GameObjectBoid> flock)
    {
        foreach (var boid in _boids)
        {
            if (boid != currBoid && Vector3.Distance(currBoid.Position, boid.Position) <= perception)
                flock.Add(boid);
        }
    }

    void BinsSearch(GameObjectBoid currBoid, float perception, List<GameObjectBoid> flock)
    {
        int searchRadius = Mathf.CeilToInt(perception * _binSize); // how many bins to check
        Vector3Int centerBin = GetBoidBin(currBoid);

        for (int x = -searchRadius; x <= searchRadius; x++)
            for (int y = -searchRadius; y <= searchRadius; y++)
                for (int z = -searchRadius; z <= searchRadius; z++)
                {
                    Vector3Int offset = new(x, y, z);
                    Vector3Int neighborBin = centerBin + offset;

                    if (!_bins.TryGetValue(neighborBin, out List<GameObjectBoid> binBoids)) continue;

                    foreach (GameObjectBoid boid in binBoids)
                    {
                        if (boid != currBoid && Vector3.SqrMagnitude(currBoid.Position - boid.Position) <= perception * perception)
                        {
                            flock.Add(boid);
                        }
                    }
                }
    }

    void OnDrawGizmos()
    {
        if (!_debugBins || _bins == null) return;

        // Set the gizmo color (semi-transparent cyan in this case)
        Gizmos.color = new Color(0, 1, 1, 0.3f);

        foreach (var binEntry in _bins)
        {
            Vector3Int binCoord = binEntry.Key;
            List<GameObjectBoid> boidsInBin = binEntry.Value;

            // Calculate the center position of this bin in world space
            Vector3 binCenter = new Vector3(
                binCoord.x * _binSize,
                binCoord.y * _binSize,
                binCoord.z * _binSize
            );

            // Draw the wireframe cube
            Gizmos.DrawWireCube(binCenter, Vector3.one * _binSize);

            //// Optional: Draw a small sphere showing how many boids are in this bin
            //if (boidsInBin != null && boidsInBin.Count > 0)
            //{
            //    Gizmos.color = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(boidsInBin.Count / 10f));
            //    Gizmos.DrawSphere(binCenter, 0.2f);
            //    Gizmos.color = new Color(0, 1, 1, 0.3f); // Reset color
            //}
        }
    }

    FlockData GetFlockData(List<GameObjectBoid> flock, Boid currBoid)
    {
        FlockData flockData = new();
        foreach (Boid checkedBoid in flock)
        {
            float distance = Vector3.Distance(currBoid.Position, checkedBoid.Position);
            //alignment
            flockData.AlignmentForce += checkedBoid.Velocity;
            //cohesion
            flockData.CohesionForce += checkedBoid.Position;
            //separation
            Vector3 desired = currBoid.Position - checkedBoid.Position;
            desired /= distance * distance;//length of vector is inversly proportional to the distance between the current and checked boid
            flockData.SeparationForce += desired;
        }
        int closeBoidsCount = flock.Count - 1; //subtract 1 cuz the current boid is included in the list

        if (closeBoidsCount == 0)
            return flockData;

        //alignment
        flockData.AlignmentForce /= closeBoidsCount;
        flockData.AlignmentForce.SetLength(_maxSpeed);
        flockData.AlignmentForce -= currBoid.Velocity;
        flockData.AlignmentForce.Limit(_maxForce);
        //separation
        flockData.SeparationForce /= closeBoidsCount;
        flockData.SeparationForce.SetLength(_maxSpeed);
        flockData.SeparationForce -= currBoid.Velocity;
        flockData.SeparationForce.Limit(_maxForce);
        //cohesion
        flockData.CohesionForce /= closeBoidsCount;
        flockData.CohesionForce -= currBoid.Position;
        flockData.CohesionForce.SetLength(_maxSpeed);
        flockData.CohesionForce -= currBoid.Velocity;
        flockData.CohesionForce.Limit(_maxForce);

        return flockData;
    }

    Vector3 CheckSimulationBounds(Vector3 position)
    {
        Vector3 wc = Vector3.zero;
        Vector3 ws = _areaSize;

        Vector3 acc = new(0, 0, 0);

        acc.x = (position.x < wc.x - ws.x * 0.5f) ? 1.0f : ((position.x > wc.x + ws.x * 0.5f) ? -1.0f : 0.0f);
        acc.y = (position.y < wc.y - ws.y * 0.5f) ? 1.0f : ((position.y > wc.y + ws.y * 0.5f) ? -1.0f : 0.0f);
        acc.z = (position.z < wc.z - ws.z * 0.5f) ? 1.0f : ((position.z > wc.z + ws.z * 0.5f) ? -1.0f : 0.0f);


        return acc;
    }
}
