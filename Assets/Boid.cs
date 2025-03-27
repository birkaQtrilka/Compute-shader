using UnityEngine;

public class Boid 
{
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 Acceleration;


    //public List<int> Iterator { get; } = new List<int>();

    public Boid(Vector3 position, Vector3 velocity)
    {
        Position = position;
        Velocity = velocity;
        //OldPosition = position;
        //OldVelocity = velocity;

    }

    public void OutsideUpdate()
    {
        OnUpdate();
    }

    protected virtual void OnUpdate() { }

}
