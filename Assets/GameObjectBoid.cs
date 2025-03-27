using UnityEngine;

public class GameObjectBoid : Boid
{
    readonly GameObject _boid;
    readonly float _rotationSpeed;

    public GameObjectBoid(Vector3 position, Vector3 velocity, GameObject instance, float rotationSpeed) : base(position, velocity)
    {
        _boid = instance;
        _boid.transform.position = position;
        _rotationSpeed = rotationSpeed;
    }

    protected override void OnUpdate()
    {
        _boid.transform.position = Position;
        _boid.transform.up = Vector3.Lerp(_boid.transform.up, Velocity, Time.deltaTime * _rotationSpeed);
    }
}
