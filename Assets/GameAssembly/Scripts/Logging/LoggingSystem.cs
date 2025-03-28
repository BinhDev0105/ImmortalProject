using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Logging;
using Unity.Logging.Sinks;
using Unity.IO.LowLevel.Unsafe;

namespace GameAssembly.Scripts.Logging
{
    public partial struct LoggingSystem : ISystem
    {
        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var logConfig = new LoggerConfig().MinimumLevel.Debug()
                .OutputTemplate("{Level} - {Message}")
                .WriteTo.File("logs/logging_debug.txt", minLevel:LogLevel.Verbose)
                .WriteTo.UnityEditorConsole().CreateLogger();
            Log.Logger = logConfig;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //state.Enabled = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}