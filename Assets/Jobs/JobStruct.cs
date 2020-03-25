using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Runtime.InteropServices;
namespace VEngine
{
    public unsafe struct JobHandle
    {
        private fixed ulong arr[1];//8
    }

    public unsafe struct JobBucket
    {
    }

    public unsafe struct JobSystem
    { 
    }

    public static unsafe class Job
    {
        [DllImport("JobSystemDLL")] public static extern JobSystem* GetNewJobSystem(int threadSize);
        [DllImport("JobSystemDLL")] public static extern void DestroyJobSystem(JobSystem* job);
        [DllImport("JobSystemDLL")] public static extern void GetJobBuckets(JobSystem* sys, JobBucket** buckets, uint size);
        [DllImport("JobSystemDLL")] public static extern void ReleaseJobBuckets(JobSystem* sys, JobBucket** buckets, uint size);
        [DllImport("JobSystemDLL")] private static extern void ScheduleJob(JobHandle* handle, JobBucket* bucket, void* funcPtr, JobHandle* dependedHandles, uint dependCount);
        [DllImport("JobSystemDLL")] public static extern void WaitAll(JobSystem* sys);
        [DllImport("JobSystemDLL")] public static extern void ExecuteJobs(JobSystem* sys, JobBucket** buckets, uint bucketCount);

        public static JobHandle ScheduleJob(JobBucket* bucket, System.Action action, JobHandle* dependedHandles, uint dependCount)
        {
            JobHandle handle;
            System.IntPtr ptr = Marshal.GetFunctionPointerForDelegate(action);
            ScheduleJob(handle.Ptr(), bucket, (void*)ptr, dependedHandles, dependCount);
            return handle;
        }
    }
}