using UnityEngine;

public class GameObjectFlock : Flocking<GameObjectBoid>
{
    [SerializeField] GameObject _boidPrefab;
    [SerializeField] Transform _container;
    [SerializeField] float _rotationSpeed;

    protected override GameObjectBoid Init(Vector3 pos, Vector3 vel)
    {
        return new GameObjectBoid(pos, vel, Instantiate(_boidPrefab, _container), _rotationSpeed);
    }
}
