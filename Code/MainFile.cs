using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;


namespace Fog_of_war
{
    [ModInitializer(nameof(Initialize))]
    public partial class MainFile : Node
    {
        private const string ModId = "Fog_of_war";
        private static Logger Logger = new Logger(ModId);
        private static FogOfWar Fow;

        public static void Initialize()
        {
            Logger.LogWithTimestamp("Mod loaded");
            Harmony harmony = new(ModId);
            harmony.PatchAll();
            // Subscribe to game events
            var manager = RunManager.Instance;
            Fow = new FogOfWar(Logger);
            manager.RunStarted += OnRunStarted;
            manager.ActEntered += OnActEntered;
            manager.RoomEntered += OnRoomEntered;
        }

        static void OnActEntered()
        {
            Logger.LogWithTimestamp("Act entered");
            Fow.RestoreMap();         
            Fow.UpdateMap();
        }

        static void OnRunStarted(RunState runState)
        {
            Logger.LogWithTimestamp("Run startet");
            Fow.SetRunState(runState);
        }

        static void OnRoomEntered()
        {
            Logger.LogWithTimestamp("Room entered");
            Fow.RestoreMap();
            Fow.UpdateMap();
        }
    }
}
