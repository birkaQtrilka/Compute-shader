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


    protected T[] _boids;

    public virtual void Restart()
    {
        
    }

    protected abstract T Init(Vector3 pos, Vector3 vel);
    
}
