using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fog_of_war
{
    internal class FogOfWar
    {
        public static RunState? RunState { get; private set; } = null;
        public static bool editedMap { get; private set; } = false;
        public static List<(MapPoint point, List<MapPoint> oldChildren, List<MapPoint> oldParents, MapPointType oldType)> Changes = new();
        public static Logger Logger { get; set; }

        public FogOfWar(Logger logger)
        {
            Logger = logger;
        }

        public void SetRunState(RunState? runState)
        {
            RunState = runState;
        }


        public void RestoreMap()
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
                    change.point.parents.Add(parent);
                }

                change.point.PointType = change.oldType;
            }
            Changes.Clear();
        }

        private static void UpdateMap()
        {
            if (RunState == null)
            {
                return;
            }

            Logger.LogWithTimestamp($"Current act index {RunState.CurrentActIndex}");
            Logger.LogWithTimestamp($"Floor {RunState.ActFloor}/{RunState.TotalFloor}");


            ActMap ModifiedActMap = RunState.Map;
            List<MapPoint> MapPoints = RunState.Map.GetAllMapPoints().ToList();
            Logger.LogWithTimestamp($"{RunState.CurrentMapPoint.coord.row}");         
            foreach (MapPoint mapPoint in MapPoints)
            {
                if (mapPoint.coord.row >= RunState.CurrentMapPoint.coord.row + 1 || (!RunState.CurrentMapPoint.Children.Contains(mapPoint) && !RunState.VisitedMapCoords.Contains(mapPoint.coord)))
                {
                    // tracking changes since making a copy of ActMap is not possible
                    Changes.Add((
                        mapPoint,
                        mapPoint.Children.ToList(),
                        mapPoint.parents.ToList(),
                        mapPoint.PointType
                    ));

                    Logger.LogWithTimestamp($"Removing children of - {mapPoint.PointType} || {mapPoint.coord.row} || {mapPoint.coord.col}");

                    foreach (var item in mapPoint.Children)
                    {
                        mapPoint.RemoveChildPoint(item);
                    }
                    var seenOnce = false;
                    // check if RunState.VisitedMapCoords children to show the already seen icons
                    foreach (var item in RunState.VisitedMapCoords)
                    {
                        if (RunState.Map.GetPoint(item).Children.Contains(mapPoint))
                            seenOnce = true;
                    }
                    if ((mapPoint.coord.row >= RunState.CurrentMapPoint.coord.row + 2 || (!RunState.CurrentMapPoint.Children.Contains(mapPoint) && !RunState.VisitedMapCoords.Contains(mapPoint.coord))) && !seenOnce)
                    {
                        Logger.LogWithTimestamp($"Changing MapPointType from {mapPoint.PointType} to  {MapPointType.Unassigned}");
                        mapPoint.PointType = MapPointType.Unassigned;
                    }
                }
            }
            //rerender Map
            NMapScreen.Instance.SetMap(ModifiedActMap, RunState.Rng.Seed, false);
        }

        public void RequestUpdateOnMapOpen()
        {
            Logger.LogWithTimestamp("Requesting update when map opens");

            // Check if map screen is already open
            var mapScreen = NMapScreen.Instance;
            if (mapScreen != null && mapScreen.IsOpen)
            {
                Logger.LogWithTimestamp("Map screen is already open, update immediately");
                UpdateMap();
                return;
            }

            // Subscribe to map screen opened event
            if (mapScreen != null)
            {
                mapScreen.Opened += OnMapScreenOpened;
                Logger.LogWithTimestamp("Subscribed to map screen Opened event");
            }
            else
            {
                Logger.LogWithTimestamp("NMapScreen.Instance is null, will retry on next act/room");
            }
        }

        static void OnMapScreenOpened()
        {
            // Unsubscribe from the event
            var mapScreen = NMapScreen.Instance;
            if (mapScreen != null)
            {
                mapScreen.Opened -= OnMapScreenOpened;
                Logger.LogWithTimestamp("Unsubscribed from map screen Opened event");
            }
            UpdateMap();
        }
    }
}
