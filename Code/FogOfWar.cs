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
    public class FogOfWar
    {
        public  RunState? RunState { get; private set; } = null;
        public  bool editedMap { get; private set; } = false;
        public  List<(MapPoint point, List<MapPoint> oldChildren, List<MapPoint> oldParents, MapPointType oldType)> Changes = new();
        public  Logger Logger { get; set; }
        public  bool IsEnabled { get; set; } = true;

        public FogOfWar(Logger logger)
        {
            Logger = logger;
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            Logger.LogWithTimestamp($"Fog of War {(enabled ? "enabled" : "disabled")}");

            if (enabled && editedMap)
            {
                // Re-apply fog of war if it was previously disabled
                UpdateMap();
            }
            else if (!enabled && editedMap)
            {
                // Restore the map if disabling
                RestoreMap();
            }
        }

        public void SetRunState(RunState? runState)
        {
            RunState = runState;
        }

        public void RestoreMap()
        {
            Logger.LogWithTimestamp($"Restoring Map");
            editedMap = true;
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

        public void UpdateMap()
        {
            if (!IsEnabled)
            {
                Logger.LogWithTimestamp("Fog of War is disabled, skipping map update");
                return;
            }

            if (NMapScreen.Instance == null)
                return;
            Logger.LogWithTimestamp($"Updating Map");
            NMapScreen.Instance.Opened -= UpdateMap;
            var CurrentMapPoint = new MapPoint(0, 0);
            if (RunState == null)
                return;
            if (RunState.Map == null)
                return;
            if (RunState.CurrentMapPoint != null)
                CurrentMapPoint = RunState.CurrentMapPoint;

            Logger.LogWithTimestamp($"Current act index {RunState.CurrentActIndex}");
            Logger.LogWithTimestamp($"Floor {RunState.ActFloor}/{RunState.TotalFloor}");


            ActMap ModifiedActMap = RunState.Map;
            List<MapPoint> MapPoints = RunState.Map.GetAllMapPoints().ToList();
            Logger.LogWithTimestamp($"{CurrentMapPoint.coord.row}");
            foreach (MapPoint mapPoint in MapPoints)
            {
                if (mapPoint.coord.row >= CurrentMapPoint.coord.row + 1 || (!CurrentMapPoint.Children.Contains(mapPoint) && !RunState.VisitedMapCoords.Contains(mapPoint.coord)))
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
                        if (RunState.Map.GetPoint(item) != null)
                        {
                            if (RunState.Map.GetPoint(item).Children.Contains(mapPoint))
                                seenOnce = true;
                        }
                        else
                        {
                            Logger.LogWithTimestamp($"Oops trying to edit nonexistent point");
                        }

                    }
                    if ((mapPoint.coord.row >= CurrentMapPoint.coord.row + 2 || (!CurrentMapPoint.Children.Contains(mapPoint) && !RunState.VisitedMapCoords.Contains(mapPoint.coord))) && !seenOnce && mapPoint.coord.row != 1)
                    {
                        Logger.LogWithTimestamp($"Changing MapPointType from {mapPoint.PointType} to  {MapPointType.Unassigned}");
                        mapPoint.PointType = MapPointType.Unassigned;
                    }
                }
            }
            //rerender Map
            try
            {
                NMapScreen.Instance.SetMap(ModifiedActMap, RunState.Rng.Seed, false);

            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Error rendering Map {ex.Message}");
            }
            finally 
            {
                RestoreMap();
                NMapScreen.Instance.Opened += UpdateMap;
            }
        }
    }
}
