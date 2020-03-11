using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using Unity.Collections;
using VEngine;
public unsafe class Test : MonoBehaviour
{
    static int i = 0;
    public static void act()
    {
        System.Threading.Interlocked.Increment(ref i);
    }
    const int tasks = 5000;
    System.Action actt = null;
    JobSystem* system;
    NativeArray<ulong> jobbuckets;
    private void Start()
    {
        system = Job.GetNewJobSystem(20);
        jobbuckets = new NativeArray<ulong>(1, Unity.Collections.Allocator.Persistent);
        Job.GetJobBuckets(system, (JobBucket**)jobbuckets.Ptr(), 1);
        if (actt == null) actt = act;
    }
    int ii = 0;
    void Update()
    {
        if (ii < tasks)
        {
            Job.ScheduleJob((JobBucket*)jobbuckets[0], actt, null, 0);
            ii++;
        }
        else if(ii == tasks)
        {
            Job.ExecuteJobs(system, (JobBucket**)jobbuckets.Ptr(), 1);
            Job.WaitAll(system);
            Debug.Log(i);

            ii++;
        }
    }

    private void OnDestroy()
    {
        Job.ReleaseJobBuckets(system, (JobBucket**)jobbuckets.Ptr(), 1);
        Job.DestroyJobSystem(system);
        jobbuckets.Dispose();
    }
}
