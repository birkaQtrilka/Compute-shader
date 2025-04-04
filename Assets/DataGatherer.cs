using System.Collections;
using System.Linq;
using UnityEngine;

public class DataGatherer : MonoBehaviour
{
    [SerializeField] float _waitTime = 5;
    [SerializeField] int _dataFrames = 200;
    [SerializeField] int[] _boidCounts;
    [SerializeField] GameObjectFlock _goFlock;
    [SerializeField] ComputeShaderFlock _csFlock;
    
    WaitForSeconds _startWait;
    
    float[] _framesData;

    void Start()
    {
        _startWait = new WaitForSeconds(_waitTime);
        _framesData = new float[_dataFrames];
        StartCoroutine(Gather());
    }

    IEnumerator Gather()
    {
        foreach (int count in _boidCounts)
        {
            if(_goFlock.gameObject.activeInHierarchy)
            {
                _goFlock._boidCount = count;
                _goFlock.Restart();
            }
            else
            {
                _csFlock._boidCount = count;
                _csFlock.Restart();
            }

            yield return _startWait;
            for (int i = 0; i < _dataFrames; i++)
            {
                yield return null;
                _framesData[i] = 1f / Time.deltaTime;
            }

            Debug.Log("Mean: " + _framesData.Average() + "\tMin: " + _framesData.Min() + "\tMax: " + _framesData.Max());
        }

        Debug.Log("Test ended");
    }
}
