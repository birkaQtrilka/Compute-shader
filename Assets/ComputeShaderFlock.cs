using UnityEngine;

public class ComputeShaderFlock : Flocking<GameObjectBoid>
{
    [SerializeField] ComputeShader boidsComputeShader;
    [SerializeField] GameObject _boidPrefab;
    [SerializeField] Transform _container;
    [SerializeField] float _rotationSpeed;
    
    [System.Serializable]
    struct BoidData
    {
        public Vector3 velocity;
        public Vector3 position;
    }

    ComputeBuffer _boidsSteeringForcesBuffer; // Buffer for Boids steering forces values storage
    ComputeBuffer _boidsDataBuffer; // Buffer storing basic data of Boids (velocity, position, Transform, etc.)


    uint _storedThreadGroupSize; // Thread group size received from Compute Shader
    int _dispatchedThreadGroupSize; // Thread group size calculated

    int _steeringForcesKernelId; // Kernel for processing boids steering forces calculation
    int _boidsDataKernelId; // Kernel for processing boids steering forces calculation

    BoidData[] boidDataArr;
    Vector3[] forceArr;

    protected override void Awake()
    {
        InitBuffers();
        InitKernels();
    }

    protected override GameObjectBoid Init(Vector3 pos, Vector3 vel)
    {
        return new GameObjectBoid(pos, vel, Instantiate(_boidPrefab, _container), _rotationSpeed);
    }

    void InitBuffers()
    {
        _boidsDataBuffer = new ComputeBuffer(_boidCount, sizeof(float) * 6); // 6 for two Vector3
        _boidsSteeringForcesBuffer = new ComputeBuffer(_boidCount, sizeof(float) * 3); // 3 for one Vector3

        // Prepare data arrays
        forceArr = new Vector3[_boidCount];
        boidDataArr = new BoidData[_boidCount];
        _boids = new GameObjectBoid[_boidCount];

        for (var i = 0; i < _boidCount; i++)
        {
            forceArr[i] = Vector3.zero;
            Vector3 pos = Random.insideUnitSphere * 1.0f;
            Vector3 vel = Random.insideUnitSphere * 0.1f;
            boidDataArr[i].position = pos;
            boidDataArr[i].velocity = vel;
            _boids[i] = Init(pos,vel);
        }

        // Set data to buffers
        _boidsSteeringForcesBuffer.SetData(forceArr);
        _boidsDataBuffer.SetData(boidDataArr);
    }

    void InitKernels()
    {
        _steeringForcesKernelId = boidsComputeShader.FindKernel("SteeringForcesCS");
        _boidsDataKernelId = boidsComputeShader.FindKernel("BoidsDataCS");

        boidsComputeShader.GetKernelThreadGroupSizes(_steeringForcesKernelId, out _storedThreadGroupSize, out _, out _);
        var dispatchedThreadGroupSize = _boidCount / (int)_storedThreadGroupSize;

        if (dispatchedThreadGroupSize % _storedThreadGroupSize == 0) return;

        while (dispatchedThreadGroupSize % _storedThreadGroupSize != 0)
        {
            dispatchedThreadGroupSize += 1;
            if (dispatchedThreadGroupSize % _storedThreadGroupSize != 0) continue;

            _dispatchedThreadGroupSize = dispatchedThreadGroupSize;

            Debug.LogFormat("Initial threads: {0}", _storedThreadGroupSize);
            Debug.LogFormat("Threads X used: {0}", _dispatchedThreadGroupSize);
            break;
        }
    }

    protected override void Update()
    {
        Simulation(_steeringForcesKernelId, _boidsDataKernelId);
    }

    void Simulation(int steeringForcesKernelId, int boidsDataKernelId)
    {
        if (boidsComputeShader == null) return;

        boidsComputeShader.SetInt("_BoidsCount", _boidCount);

        boidsComputeShader.SetBuffer(steeringForcesKernelId, "_BoidsDataBuffer", _boidsDataBuffer);
        boidsComputeShader.SetBuffer(steeringForcesKernelId, "_BoidsSteeringForcesBufferRw", _boidsSteeringForcesBuffer);
        boidsComputeShader.SetBuffer(boidsDataKernelId, "_BoidsSteeringForcesBuffer", _boidsSteeringForcesBuffer);
        boidsComputeShader.SetBuffer(boidsDataKernelId, "_BoidsDataBufferRw", _boidsDataBuffer);

        boidsComputeShader.SetFloat("_PerceptionRadius", _persceptionDistance);
        //boidsComputeShader.SetFloat("_CohesionRadius", _persceptionDistance);
        //boidsComputeShader.SetFloat("_AlignmentRadius", _persceptionDistance);
        //boidsComputeShader.SetFloat("_SeparationRadius", _persceptionDistance);
        boidsComputeShader.SetFloat("_BoidMaximumSpeed", _maxSpeed);
        boidsComputeShader.SetFloat("_BoidMaximumSteeringForce", _maxForce);
        boidsComputeShader.SetFloat("_SeparationWeight", _sepparationBias);
        boidsComputeShader.SetFloat("_CohesionWeight", _cohesionBias);
        boidsComputeShader.SetFloat("_AlignmentWeight", _alignmentBias);
        boidsComputeShader.SetFloat("_SimulationBoundsAvoidWeight", _wallRepellForce);

        boidsComputeShader.SetVector("_SimulationCenter", Vector3.zero);
        boidsComputeShader.SetVector("_SimulationDimensions", _areaSize);

        boidsComputeShader.SetFloat("_DeltaTime", Time.deltaTime);

        boidsComputeShader.Dispatch(steeringForcesKernelId, _dispatchedThreadGroupSize, 1, 1);
        boidsComputeShader.Dispatch(boidsDataKernelId, _dispatchedThreadGroupSize, 1, 1);

        _boidsDataBuffer.GetData(boidDataArr);  // Copy data from GPU buffer to CPU array
        for (int i = 0; i < _boidCount; i++)
        {
            _boids[i].Velocity = boidDataArr[i].velocity;  // Update your boid objects
            _boids[i].Position = boidDataArr[i].position;  
            _boids[i].OutsideUpdate();
        }
    }

    void OnDestroy()
    {
        ReleaseBuffer();
    }

    void SafeReleaseBuffer(ref ComputeBuffer buffer)
    {
        if (buffer == null) return;
        buffer.Release();
        buffer = null;
    }

    void ReleaseBuffer()
    {
        SafeReleaseBuffer(ref _boidsDataBuffer);
        SafeReleaseBuffer(ref _boidsSteeringForcesBuffer);
    }
}