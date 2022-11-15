using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Utopia.Noise;

public class NoiseTest : MonoBehaviour
{
    SimplexFractal2D noisemap = new SimplexFractal2D()
    {
        origin = new double2(42104.0, 2961263.0),
        index = new int2(0, 0),
        size = 256,
        scale = 1.0,
        octaves = 5,
        gain = 0.5,
        lacunarity = 2.0
    };

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(SimplexFractal2D.Sample(new double2(4, 6)));

        noisemap.octaveOffsets = new NativeArray<double2>(new double2[]
        {
            new double2(-32523.0, 3205.0),
            new double2(691.0, 2015.0),
            new double2(-5196.0, 135.0),
            new double2(0325.0, 150.0),
            new double2(23510.0, -12350.0)
        }, Allocator.TempJob);

        noisemap.result = new NativeArray<double>(256, Allocator.TempJob);
        noisemap.Schedule(256, 4).Complete();

        noisemap.octaveOffsets.Dispose();

        foreach (double val in noisemap.result)
        {
            Debug.Log(val.ToString());
        }

        noisemap.result.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
