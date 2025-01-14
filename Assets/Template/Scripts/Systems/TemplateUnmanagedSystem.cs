namespace Template
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// Template Unmanaged System
    /// Note: Use this for Operating on Unmanaged Components
    /// Note: Burst compile OnCreate, OnUpdate, OnDestory
    /// </summary>
    [RequireMatchingQueriesForUpdate] // Run OnUpdate Only when there's valid Queries
    [BurstCompile]
    [DisableAutoCreation]
    public partial struct TemplateUnmanagedSystem : ISystem
    {
        ComponentLookup<LocalTransform> localTransformLU;
        ComponentLookup<TemplateData> dataLU;

        /// <summary>
        /// Called when System is created
        /// </summary>
        [BurstCompile] public void OnCreate(ref SystemState state) 
        {
            localTransformLU = state.GetComponentLookup<LocalTransform>(false);
            dataLU = state.GetComponentLookup<TemplateData>(true);
        }

        /// <summary>
        /// Called when System is Destoryed
        /// </summary>
        [BurstCompile] public void OnDestroy(ref SystemState state) { }

        [BurstCompile] public void OnUpdate(ref SystemState state)
        {
            // Create ECB Method 1 - Create From System - https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-entity-command-buffer-automatic-playback.html
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Create ECB Method 2 - Create Manual Adhoc - https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-entity-command-buffer-use.html
            // Manually do ecs.Playback() and ecb.Dispose()
            //var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Choose!! Uncomment one 
            //DoSystemAPIForeach(ref state, ref ecb);
            //DoECSJobs(ref state);
            DoECSJobsDataLookup(ref state,ref localTransformLU, ref dataLU);

            // For ECB Method 2
            //Dependency.Complete();
            //ecb.Playback(state.EntityManager);
            //ecb.Dispose();
        }


        /// <summary>
        /// https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-systemapi-query.html
        /// 
        /// === System API Query Foreach ===
        /// Get Components using SystemAPI.Query which will return Entities matching 
        /// Note: Runs on main thread
        /// Note: SystemAPI.Query is replaced with a pre-cached EntityQuery using source generator after compile
        /// Note: Use RefRO , RefRW when possible
        /// Note: Up to 7 supported type parameter can be passed
        /// Note: Add .WithEntityAccess() to get access to entity at tuple end
        /// </summary>
        void DoSystemAPIForeach(ref SystemState state, ref EntityCommandBuffer ecb)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, data, entity) in SystemAPI.Query<RefRW<LocalTransform>, TemplateData>().WithEntityAccess())
            {
                var t = transform.ValueRO;
                t.Position = new float3(t.Position.x, math.sin(data.Speed.y * time), t.Position.z);
                transform.ValueRW = t;
            }
        }

        /// <summary>
        /// https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-data-ijobentity.html
        /// 
        /// === ECS Jobs aka IJobEntity or Jobs for Entity===
        /// Note: IJobEntity is reusable
        /// Note: IJobEntity takes less time to compile than Entities.ForEach
        /// </summary>
        void DoECSJobs(ref SystemState state)
        {
            var job = new TemplateJob
            {
                time = (float)SystemAPI.Time.ElapsedTime,
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-looking-up-data.html
        /// 
        /// === ECS Jobs with Arbitrary Data Lookup ===
        /// Note: This cannot be Schedule ScheduleParallel if there are write operations
        /// </summary>
        void DoECSJobsDataLookup(ref SystemState state,
            ref ComponentLookup<LocalTransform> localTransformLU,
            ref ComponentLookup<TemplateData> dataLU)
        {
            localTransformLU.Update(ref state);
            dataLU.Update(ref state);

            var job = new TemplateJobDataLookup
            {
                localTransformLU = localTransformLU,
                dataLU = dataLU,
                time = (float)SystemAPI.Time.ElapsedTime,
            };
            state.Dependency = job.Schedule(state.Dependency);
        }
    }
}