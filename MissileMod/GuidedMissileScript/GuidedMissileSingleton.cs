using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace GuidedMissile.GuidedMissileScript
{
    public class GuidedMissileSingleton
    {
        public static  readonly string  SandboxGameWeaponsMyMissile = "Sandbox.Game.Weapons.MyMissile";


        private static GuidedMissileSingleton _instance = null;

        private readonly Dictionary<long, GuidedMissileDataContainer> _guidedMissileDict; //A dictionary. the missiles are the keys, their targets the values.
        private readonly HashSet<long> _deleteSet;
        public HashSet<IMyEntity> IgnoreSet { get; private set; }

        private readonly HashSet<IMyEntity> _turretSet;
        private readonly HashSet<IMyEntity> _deleteTurretSet;

        private readonly HashSet<WarningMessageContainer> _warningGridSet;

        private GuidedMissileSingleton()
        {
            _guidedMissileDict = new Dictionary<long, GuidedMissileDataContainer>();
            _deleteSet = new HashSet<long>();
            _turretSet = new HashSet<IMyEntity>();
            _deleteTurretSet = new HashSet<IMyEntity>();
            IgnoreSet = new HashSet<IMyEntity>();
            _warningGridSet = new HashSet<WarningMessageContainer>();

            //  Log.Info("GuidedMissileSingleton was created.");
        }

        public static GuidedMissileSingleton GetInstance()
        {
            if (_instance == null)
            {
                _instance = new GuidedMissileSingleton();
            }
            return _instance;
        }

        public static void Update()
        {
            GetInstance().UpdateBeforeSimulation();

        }

        private void DisplayWarningMessage(IMyEntity target)
        {

            IMyEntity grid = target.GetTopMostParent();
            bool alreadyContained = false;
            foreach (WarningMessageContainer container in _warningGridSet)
            {
                if (container.Grid == grid) alreadyContained = true;
            }
            if (!alreadyContained)
            {
                IMyPlayerCollection allPlayers = MyAPIGateway.Players;

                List<IMyPlayer> playerList = new List<IMyPlayer>();

                if (grid.Hierarchy == null) return;
                if (grid.Hierarchy.Children == null) return;
                HashSet<IMyEntity> componentSet = new HashSet<IMyEntity>();

                grid.Hierarchy.GetChildrenRecursive(componentSet);
                List<IMyEntity> componentList = new List<IMyEntity>();
                componentList.AddRange(componentSet);

                allPlayers.GetPlayers(playerList);
                componentSet.Clear();


                foreach (IMyEntity component in componentList)
                {
                    if (component is IMyCockpit) componentSet.Add(component);
                }

                foreach (IMyPlayer player in playerList)
                {
                    if (MyAPIGateway.Session.Player == player)
                    {
                        if ((player != null) && (player.Controller != null) && (player.Controller.ControlledEntity != null) && (player.Controller.ControlledEntity.Entity != null))
                        {
                            foreach (IMyEntity entity in componentSet)
                            {
                                if (player.Controller.ControlledEntity.Entity.EntityId == entity.EntityId)
                                {
                                    MyAPIGateway.Utilities.ShowNotification("WARNING! MISSILE LOCKON DETECTED!", 5000, MyFontEnum.Red);

                                    _warningGridSet.Add(new WarningMessageContainer(grid));
                                }
                            }
                        }
                    }
                }
            }
            else {

            }
        }
        public static HashSet<IMyEntity> GetMissilesByTargetGrid(IMyEntity grid)
        {
            if (grid != null && !(grid is IMyCubeGrid))
            {
                Log.Info("grid wasnt an actual grid!");
                return null;
            }
            //  Log.Info("grid wasnt null");
            ICollection<GuidedMissileDataContainer> mdContainers = GetInstance()._guidedMissileDict.Values;

            HashSet<IMyEntity> missileSet = new HashSet<IMyEntity>();

            foreach (GuidedMissileDataContainer container in mdContainers)
            {

                var missile = container.Missile;
                var target = container.Target;
                if (target.GetTopMostParent() == grid) missileSet.Add(missile);
                //           Log.Info("added missile to targetedongridset " + missileSet.Count);

            }
            return missileSet;
        }
        public static bool SetTargetForMissile(IMyEntity missile, IMyEntity target)
        {
            return GetInstance().SetTargetForMissileInDict(missile, target);
        }
        public bool SetTargetForMissileInDict(IMyEntity missile, IMyEntity target)
        {
            if (missile == null) return false;
            if (target == null) return false;
            if (target.MarkedForClose) return false;
            //  Log.Info("neither target nor missile are null");
            if (!_guidedMissileDict.ContainsKey(missile.EntityId)) return false;
            if (missile.MarkedForClose) return false;
            //  Log.Info("missiledict contains missile and isnt marked for close");
            GuidedMissileDataContainer mdC;
            _guidedMissileDict.TryGetValue(missile.EntityId, out mdC);
            if (mdC != null) mdC.Target = target;
            //  Log.Info("SetTargetForMissile: return true");
            return true;
        }
        public bool AddMissileToDict(IMyEntity missile, IMyEntity target, long safetyTimer, long deathTimer, float turningSpeed, Action<IMyEntity> onExplode, bool hasPhysicsSteering)
        {
            if ((missile == null) || (target == null)) { return false;}
            if (missile.GetType().ToString() != SandboxGameWeaponsMyMissile) { return false;}
            if (_guidedMissileDict.ContainsKey(missile.EntityId)) { return false;}
            if (IgnoreSet.Contains(missile)) { return false;}
            _guidedMissileDict.Add(missile.EntityId, new GuidedMissileDataContainer(missile, target, safetyTimer, deathTimer, turningSpeed, onExplode, hasPhysicsSteering));
            //   Log.Info("Added missile " + missile.EntityId + " with Target " + target.EntityId + " and safetyTimer " + safetyTimer + " Frames to the dictionary!");
            DisplayWarningMessage(target);
            return true;
        }

        public bool AddMissileToDict(IMyEntity missile, IMyEntity target, long safetyTimer)
        {

            return AddMissileToDict(missile, target, safetyTimer, 10000, 0.1f, null, false);
        }

        public GuidedMissileControlStationHook GetControlStationHook(IMyEntity turret)
        {

            MyEntityComponentContainer componentContainer = turret.Components;

            GuidedMissileControlStationHook controlStationHook = null;
            foreach (MyComponentBase comp in componentContainer)
            {
                var hook = comp as GuidedMissileControlStationHook;
                if (hook != null)
                {
                    controlStationHook = hook;
                }
            }
            return controlStationHook;
        }
        public bool RemoveMissileFromDict(IMyEntity missile)
        {
            if (missile == null) return false;
            if (!_guidedMissileDict.ContainsKey(missile.EntityId)) return false;
            _guidedMissileDict.Remove(missile.EntityId);
            return true;
        }
        public static bool IsGuidedMissile(IMyEntity missile)
        {
            return GetInstance().MissileIsGuided(missile);
        }
        public bool MissileIsGuided(IMyEntity missile)
        {
            return _guidedMissileDict.ContainsKey(missile.EntityId);
        }

        public bool AddTurretToSet(IMyEntity turret)
        {
            //     Log.Info("tried to add turret to set");
            return _turretSet.Add(turret);
        }

        public bool RemoveTurretFromSet(IMyEntity turret)
        {
            //     Log.Info("tried to remove turret from Set");
            return _turretSet.Remove(turret);
        }

        public void UpdateTurret(IMyEntity turret)
        {

            if (!turret.MarkedForClose)
            {
                GetControlStationHook(turret).UpdateManually();
            }

        }



        public void UpdateBeforeSimulation()
        {
            HashSet<IMyEntity> delFromIgnoreSet = new HashSet<IMyEntity>();

            foreach (IMyEntity ent in IgnoreSet)
            {
                if (ent != null)
                {
                    if (ent.MarkedForClose) delFromIgnoreSet.Add(ent);
                }
            }
            IgnoreSet.ExceptWith(delFromIgnoreSet);
            delFromIgnoreSet.Clear();

            HashSet<WarningMessageContainer> delFromWarningSet = new HashSet<WarningMessageContainer>();
            foreach (WarningMessageContainer container in _warningGridSet)
            {
                if (container != null)
                {
                    container.Update();
                    if (container.Ticks > 240) { delFromWarningSet.Add(container); }
                    if (container.Grid == null) { delFromWarningSet.Add(container); }
                    else
                    {
                        if (container.Grid.MarkedForClose) { delFromWarningSet.Add(container); }
                    }

                }
            }
            _warningGridSet.ExceptWith(delFromWarningSet);
            delFromWarningSet.Clear();

            Dictionary<long, GuidedMissileDataContainer>.KeyCollection keyCollection = _guidedMissileDict.Keys;
            var missileCollection = _guidedMissileDict.Values.Select(s => s.Missile).ToList();
            foreach (long key in keyCollection)
            {
                GuidedMissileDataContainer guidedMissileData;
                if (_guidedMissileDict.TryGetValue(key, out guidedMissileData))
                {
                    if (guidedMissileData.IsExpired())
                    {
                        _deleteSet.Add(key);
                        guidedMissileData.SetEmpty();
                    }
                    else
                    {
                        guidedMissileData.Update(missileCollection);
                    }
                }
            }
            foreach (long deleteId in _deleteSet)
            {
                _guidedMissileDict.Remove(deleteId);
            }
            _deleteSet.Clear();

            foreach (IMyEntity turret in _turretSet)
            {
                if (turret.MarkedForClose) { _deleteTurretSet.Add(turret); }
                else
                {
                    UpdateTurret(turret);
                }
            }
            _turretSet.ExceptWith(_deleteTurretSet);
            _deleteTurretSet.Clear();

        }


    }

    public class WarningMessageContainer
    {
        public long Ticks;
        public readonly IMyEntity Grid;

        public WarningMessageContainer(IMyEntity grid)
        {
            Grid = grid;
        }

        public void Update()
        {
            Ticks++;
        }
    }
}