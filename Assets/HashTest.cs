using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Assets
{
    public class HashTest : MonoBehaviour
    {
        public const int TABLE_SIZE = 1024;
        struct BoidData
        {
            public float3 velocity;
            public float3 position;
        };

        struct Buffa
        {
            public uint offset;
            public uint size;
            public BoidData[] buffer;

            public void Add(BoidData value) 
            {
                if (offset == size) return;
                buffer[offset] = value;
                offset++;
            }
        };

        struct HashEntry
        {
            public uint3 key;
            public Buffa array;
        };

        HashEntry[] hashTable;

        bool KeysMatch(uint3 a, uint3 b)
        {
            return math.all(a == b);
        }

        uint HashFunction(uint3 key)
        {
            return (key.x * 73856093 ^ key.y * 19349663 ^ key.z * 83492791) % TABLE_SIZE;
        }

        void Insert(uint3 key, BoidData value)
        {
            uint index = HashFunction(key);
            uint3 originalKey = new();

            // Linear Probing
            for (uint i = 0; i < TABLE_SIZE; i++)
            {
                uint probeIndex = (index + i) % TABLE_SIZE;

                CompareExchange(ref hashTable[probeIndex].key.x, 0, key.x, out originalKey.x);
                CompareExchange(ref hashTable[probeIndex].key.y, 0, key.y, out originalKey.y);
                CompareExchange(ref hashTable[probeIndex].key.z, 0, key.z, out originalKey.z);

                if (math.all(originalKey == 0) || KeysMatch(originalKey, key))
                {
                    hashTable[probeIndex].array.Add(value);
                    return;
                }
            }
        }

        public static void CompareExchange(
            ref uint destination,
            uint compareValue,
            uint newValue,
            out uint originalValue)
        {
            originalValue = destination; // Store original value

            if (destination == compareValue)
            {
                destination = newValue; // Update if it matches expected value
            }
        }


    }
}
