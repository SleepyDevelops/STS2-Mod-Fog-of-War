using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using System.ComponentModel;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace Test_Mod.Test_ModCode
{
    //You're recommended but not required to keep all your code in this package and all your assets in the Test_Mod folder.
    [ModInitializer(nameof(Initialize))]
    public partial class MainFile : Node
    {
        public static RunState? RunState { get; private set; } = null;
        public const string ModId = "Test_Mod"; //At the moment, this is used only for the Logger and harmony names.
        public static bool editedMap { get; private set; } = false;
        public static List<(MapPoint point, List<MapPoint> oldChildren, List<MapPoint> oldParents, MapPointType oldType)>  Changes = new();

        public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

        public static void Initialize()
        {
            LogWithTimestamp("Mod loaded");
            //Harmony harmony = new(ModId);  harmony.PatchAll();
            // Subscribe to game events
            var manager = RunManager.Instance;
            manager.RunStarted += OnRunStarted;
            manager.ActEntered += OnActEntered;
            manager.RoomEntered += OnRoomEntered;
        }

        static void OnMapScreenOpened()
        {
            // Unsubscribe from the event
            var mapScreen = NMapScreen.Instance;
            if (mapScreen != null)
            {
                mapScreen.Opened -= OnMapScreenOpened;
                LogWithTimestamp("Unsubscribed from map screen Opened event");
            }
            UpdateBestPath();
        }


        static void OnRunStarted(RunState runState)
        {
            LogWithTimestamp("run startet");
            RunState = runState;
            addRelics();
        }


        static void OnActEntered()
        {
            // restore original state
            foreach (var change in Changes)
            {
                change.point.Children.Clear();
                foreach (var child in change.oldChildren)
                {
                    change.point.AddChildPoint(child);
                }
                change.point.PointType = change.oldType;
            }
            LogWithTimestamp("Act entered");
            //UpdateBestPath();
            RequestHighlightOnMapOpen();
        }
        private static void RestoreMap()
        {
            // restore original state
            foreach (var change in Changes)
            {
                // clear current links
                change.point.Children.Clear();
                change.point.parents.Clear();

                // restore children
                foreach (var child in change.oldChildren)
                {
                    change.point.AddChildPoint(child);
                }

                // restore parents
                foreach (var parent in change.oldParents)
                {
                    change.point.parents.Add(parent); // or equivalent
                }

                change.point.PointType = change.oldType;
            }
            Changes.Clear();
        }

        static void OnRoomEntered()
        {
            LogWithTimestamp("Room entered");
            RestoreMap();
            RequestHighlightOnMapOpen();
        }

        private static void UpdateBestPath()
        {
            if (RunState == null)
            {
                return;
            }

            LogWithTimestamp($"Current act index {RunState.CurrentActIndex}");
            LogWithTimestamp($"Floor {RunState.ActFloor}/{RunState.TotalFloor}");

            // Get current position, fallback to starting point if not set
            var startPoint = RunState.CurrentMapPoint ?? RunState.Map?.StartingMapPoint;
            if (startPoint == null)
            {
                LogWithTimestamp("No starting map point found, cannot calculate best path");
                return;
            }
            else
            {

                ActMap ModifiedActMap = RunState.Map;
                List<MapPoint> MapPoints = RunState.Map.GetAllMapPoints().ToList();
                LogWithTimestamp($"{RunState.CurrentMapPoint.coord.row}");

                // tracking changes since making a copy of ActMap is not possible

                foreach (MapPoint mapPoint in MapPoints)
                {
                    if (mapPoint.coord.row >= RunState.CurrentMapPoint.coord.row + 1 || (!RunState.CurrentMapPoint.Children.Contains(mapPoint) && !RunState.VisitedMapCoords.Contains(mapPoint.coord)))
                    {
                        // save state
                        Changes.Add((
                            mapPoint,
                            mapPoint.Children.ToList(),
                            mapPoint.parents.ToList(),
                            mapPoint.PointType
                        ));

                        LogWithTimestamp($"Removing children of - {mapPoint.PointType} || {mapPoint.coord.row} || {mapPoint.coord.col}");

                        foreach (var item in mapPoint.Children)
                        {
                            mapPoint.RemoveChildPoint(item);
                        }
                        var seenOnce = false;
                        // check if RunState.VisitedMapCoords children is dabei dann muss ned unassigned sein
                        foreach (var item in RunState.VisitedMapCoords)
                        {
                            if(RunState.Map.GetPoint(item).Children.Contains(mapPoint))
                                seenOnce = true;
                        }
                        if ((mapPoint.coord.row >= RunState.CurrentMapPoint.coord.row + 2 || (!RunState.CurrentMapPoint.Children.Contains(mapPoint) && !RunState.VisitedMapCoords.Contains(mapPoint.coord))) && !seenOnce)
                        {
                            LogWithTimestamp($"Changing MapPointType from {mapPoint.PointType} to  {MapPointType.Unassigned}");
                            mapPoint.PointType = MapPointType.Unassigned;
                        }
                    }
                }

                NMapScreen.Instance.SetMap(ModifiedActMap, RunState.Rng.Seed, false);

            }
        }

        static void RequestHighlightOnMapOpen()
        {
            LogWithTimestamp("Requesting highlight when map opens");

            // Check if map screen is already open
            var mapScreen = NMapScreen.Instance;
            if (mapScreen != null && mapScreen.IsOpen)
            {
                LogWithTimestamp("Map screen is already open, highlighting immediately");
                UpdateBestPath();
                return;
            }

            // Subscribe to map screen opened event
            if (mapScreen != null)
            {
                mapScreen.Opened += OnMapScreenOpened;
                LogWithTimestamp("Subscribed to map screen Opened event");
            }
            else
            {
                LogWithTimestamp("NMapScreen.Instance is null, will retry on next act/room");
            }
        }

        private static void LogWithTimestamp(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Logger.Warn($"[{timestamp}] MODTEST: {message}");
        }

        private static void addRelics()
        {
            //add existing relic
            RelicModel relic = ModelDb.Relic<Akabeko>().ToMutable();
            relic.FloorAddedToDeck = 1;
            RunState.Players.First().AddRelicInternal(relic);

            //add custom relic

            //unscubscribe otherwise relics get added everytime the run is opened
            //var manager = RunManager.Instance;
            //manager.RunStarted -= OnRunStarted;
        }
    }
}
