using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public abstract class FlockingBase : MonoBehaviour
{

}


public abstract class Flocking<T> : FlockingBase where T : Boid
{

    [SerializeField] protected float _maxForce;
    [SerializeField] protected float _maxSpeed;
    [SerializeField] protected float _persceptionDistance;
    public int _boidCount;
    [SerializeField] protected Vector3 _areaSize;

    [SerializeField] protected float _alignmentBias = 1; 
    [SerializeField] protected float _cohesionBias = 1;
    [SerializeField] protected float _sepparationBias = 1;
    [SerializeField] protected float _wallRepellForce = 5;
    [SerializeField] protected float _binSize = .5f;
    [SerializeField] bool _binSearch;

    protected T[] _boids;
    List<T> _flock;

    Dictionary<Vector3Int, List<T>> _bins;

    struct FlockData
    {
        public Vector3 SeparationForce;
        public Vector3 CohesionForce;
        public Vector3 AlignmentForce;
    }

    public virtual void Restart()
    {
        Debug.Log("Flock Count: " + _boidCount);
        _boids = new T[_boidCount];
        _flock = new(_boidCount);
        _bins = new(_boidCount);
        UnityEngine.Random.InitState(12);
        for (int i = 0; i < _boidCount; i++)
        {
            Vector3 pos = UnityEngine.Random.insideUnitSphere * 1.0f;
            Vector3 vel = UnityEngine.Random.insideUnitSphere * 0.1f;
            T boid = Init(pos, vel);
            _boids[i] = boid;
        }
    }

    protected abstract T Init(Vector3 pos, Vector3 vel);

    protected virtual void Update()
    {
        if(_binSearch)
            UpdateBins();

        for (int i = 0; i < _boids.Length; ++i)
        {
            Boid currBoid = _boids[i];

            //to avoid checking against every other boid
            _flock.Clear();
            Search(currBoid, _persceptionDistance, _flock);

            FlockData flock = GetFlockData(_flock, currBoid);

            currBoid.Acceleration += 
                  flock.CohesionForce   * _cohesionBias
                + flock.AlignmentForce  * _alignmentBias
                + flock.SeparationForce * _sepparationBias;

            currBoid.Acceleration += CheckSimulationBounds(currBoid.Position) * _wallRepellForce;

            currBoid.Velocity += currBoid.Acceleration * Time.deltaTime;
            currBoid.Velocity.Limit(_maxSpeed);
            currBoid.Position += currBoid.Velocity * Time.deltaTime;
            currBoid.Acceleration = Vector3.zero;
            currBoid.OutsideUpdate();

        }
    }
    
    void Search(Boid boid, float perception, List<T> flock)
    {
        if (_binSearch)
            BinsSearch(boid, perception, flock);
        else
            NaiveSearch(boid, perception, flock);
    }

    void NaiveSearch(Boid currBoid, float perception, List<T> flock)
    {
        foreach (var boid in _boids)
        {
            if (boid != currBoid && Vector3.Distance(currBoid.Position, boid.Position) <= perception)
                flock.Add(boid);
        }
    }

    void BinsSearch(Boid currBoid, float perception, List<T> flock)
    {
        int searchRadius = Mathf.CeilToInt(perception * _binSize); // how many bins to check
        Vector3Int centerBin = GetBoidBin(currBoid);

        for (int x = -searchRadius; x <= searchRadius; x++)
            for (int y = -searchRadius; y <= searchRadius; y++)
                for (int z = -searchRadius; z <= searchRadius; z++)
                {
                    Vector3Int offset = new(x, y, z);
                    Vector3Int neighborBin = centerBin + offset;

                    if (!_bins.TryGetValue(neighborBin, out List<T> binBoids)) continue;

                    foreach (T boid in binBoids)
                    {
                        if (boid != currBoid && Vector3.SqrMagnitude(currBoid.Position - boid.Position) <= perception * perception)
                        {
                            flock.Add(boid);
                        }
                    }
                }
    }

    void UpdateBins()
    {
        foreach (List<T> list in _bins.Values)
        {
            list.Clear();
        }

        foreach (var boid in _boids)
        {
            Vector3Int bin = GetBoidBin(boid);
            if (_bins.TryGetValue(bin, out List<T> list))
            {
                list.Add(boid);
            }
            else
            {
                _bins.Add(bin, new List<T>() { boid });
            }
        }
    }

    Vector3Int GetBoidBin(Boid boid)
    {
        Vector3 pos = boid.Position;
        return new((int)(pos.x * _binSize), (int)(pos.y * _binSize), (int)(pos.z * _binSize));
    }

    Vector3Int GetBoidBin(T boid)
    {
        Vector3 pos = boid.Position;
        return new((int)(pos.x * _binSize), (int)(pos.y * _binSize), (int)(pos.z * _binSize));
    }

    FlockData GetFlockData(List<T> flock, Boid currBoid)
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
        flockData.CohesionForce -=  currBoid.Velocity;
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
