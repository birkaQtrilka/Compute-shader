using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Searcher.SearcherWindow;

public abstract class Flocking<T> : MonoBehaviour where T : Boid
{

    [SerializeField] protected float _maxForce;
    [SerializeField] protected float _maxSpeed;
    [SerializeField] protected float _persceptionDistance;
    [SerializeField] protected int _boidCount;
    [SerializeField] protected Vector3 _areaSize;

    [SerializeField] protected float _alignmentBias = 1; 
    [SerializeField] protected float _cohesionBias = 1;
    [SerializeField] protected float _sepparationBias = 1;
    [SerializeField] protected float _wallRepellForce = 5;

    protected T[] _boids;
    // readonly QuadTree<Boid> _quadTree;
    readonly List<T> _flock = new();

    struct FlockData
    {
        public Vector3 SeparationForce;
        public Vector3 CohesionForce;
        public Vector3 AlignmentForce;
    }

    protected virtual void Awake()
    {
        _boids = new T[_boidCount];

        for (int i = 0; i < _boidCount; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 1.0f;
            Vector3 vel = Random.insideUnitSphere * 0.1f;
            _boids[i] = Init(pos, vel);
        }
    }

    protected abstract T Init(Vector3 pos, Vector3 vel);

    protected virtual void Update()
    {
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

            currBoid.Acceleration.Limit(_maxForce);
            ApplyWallRepellent(currBoid);

            currBoid.Velocity += currBoid.Acceleration * Time.deltaTime;
            currBoid.Velocity.Limit(_maxSpeed);
            currBoid.Position += currBoid.Velocity * Time.deltaTime;
            currBoid.Acceleration = Vector3.zero;
            currBoid.OutsideUpdate();
            //ColorBoidBasedOnDensity(_flock.Count - 1);

        }
        //_spacePartitioning.Update(this);//drawing the tree
    }
    
    void Search(Boid boid, float perception, List<T> flock)
    {
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
        flockData.CohesionForce.SetLength(_maxSpeed);
        flockData.CohesionForce -= currBoid.Position + currBoid.Velocity;
        flockData.CohesionForce.Limit(_maxForce);

        return flockData;
    }

    //void ColorBoidBasedOnDensity(float density, float maxCount = 50)
    //{

    //    float ratio = 1 - density / maxCount;
    //    ratio = Mathf.Clamp(ratio, 0, 1);
    //    Color red = Color.red;
    //    Color green = Color.green;

    //    int gradientR = (int)Utils.Lerp(red.r, green.r, ratio);
    //    int gradientG = (int)Utils.Lerp(red.g, green.g, ratio);
    //    int gradientB = (int)Utils.Lerp(red.b, green.b, ratio);

    //}

    void TeleportBetweenEdges(Boid boid)
    {
        if (boid.Position.x < 0)
            boid.Position.x = _areaSize.x;

        if (boid.Position.x > _areaSize.x)
            boid.Position.x = 0;

        if (boid.Position.y < 0)
            boid.Position.y = _areaSize.y;

        if (boid.Position.y > _areaSize.y)
            boid.Position.y = 0;
    }
    void ApplyWallRepellent(Boid boid)
    {
        if (boid.Position.x < 0)
            boid.Acceleration.x = _wallRepellForce;

        if (boid.Position.x > _areaSize.x)
            boid.Acceleration.x = -_wallRepellForce;

        if (boid.Position.y < 0)
            boid.Acceleration.y = _wallRepellForce;

        if (boid.Position.y > _areaSize.y)
            boid.Acceleration.y = -_wallRepellForce;

        if (boid.Position.z < 0)
            boid.Acceleration.z = _wallRepellForce;

        if (boid.Position.z > _areaSize.z)
            boid.Acceleration.z = -_wallRepellForce;
    }
}
