using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

public class DataGatherer : MonoBehaviour
{
    [SerializeField] float _waitTime = 5;
    [SerializeField] int _dataFrames = 200;
    [SerializeField] int[] _boidCounts;
    [SerializeField] GameObjectFlock _goFlock;
    [SerializeField] ComputeShaderFlock _csFlock;
    [SerializeField] bool _writeToCSV;
    [SerializeField] bool _stopGathering;

    WaitForSeconds _startWait;
    
    float[] _framesData;

    void Start()
    {
        _startWait = new WaitForSeconds(_waitTime);
        _framesData = new float[_dataFrames];
        StartCoroutine(Gather());
    }
    
    struct BoidData
    {
        public float Mean;
        public float Max;
        public float Min;
    }

    IEnumerator Gather()
    {
        List<BoidData> dataList = new (_boidCounts.Length);

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

                if(_stopGathering)
                {
                    _stopGathering = false;
                    WriteOnCsv(dataList);
                    yield break;
                }
            }
            BoidData set = new()
            {
                Mean = _framesData.Average(),
                Min = _framesData.Min(),
                Max = _framesData.Max(),
            };

            dataList.Add(set);
            Debug.Log("Mean: " + set.Mean + "\tMin: " + set.Min + "\tMax: " + set.Max);
        }

        if(_writeToCSV)
        {
           WriteOnCsv(dataList);
        }

        Debug.Log("Test ended");
    }

    void WriteOnCsv(List<BoidData> dataList)
    {
        char s = ';';
        //string filePath = Path.Combine(Application.dataPath, "BoidsData.csv");
        string filePath = Path.Combine(Application.persistentDataPath, "BoidsData.csv");
        Debug.Log("Writing to: " + filePath);
        using var writer = new StreamWriter(filePath);
        // Write header
        writer.WriteLine($"Mean{s}Min{s}Max");
        var culture = new CultureInfo("de-DE");
        // Write each record
        foreach (BoidData data in dataList)
        {
            writer.WriteLine($"{data.Mean.ToString(culture)}{s}" +
             $"{data.Min.ToString(culture)}{s}" +
             $"{data.Max.ToString(culture)}");

        }
    }
}
