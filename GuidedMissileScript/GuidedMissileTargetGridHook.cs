using Sandbox;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VRage;
using VRage.Common.Utils;
using VRage.Game.Components;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace GuidedMissile.GuidedMissileScript
{
    public enum Messagetype : ushort { 
        Seed = 0,
        PullRequest = 1,
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
    public class GuidedMissileTargetGridHook : MyGameLogicComponent
    {
        public static IMyEntity GetRandomBlockInGrid(IMyEntity grid)
        {
            if (grid == null) return null;
            // if(target.OwnerId == Entity.OwnerId) return false; //faction check?
            if (grid.Hierarchy == null) return null;
            if (grid.Hierarchy.Children == null) return null;
            HashSet<IMyEntity> componentSet = new HashSet<IMyEntity>();

            grid.Hierarchy.GetChildrenRecursive(componentSet);
            List<IMyEntity> componentList = new List<IMyEntity>();
            componentList.AddRange(componentSet);
            IMyEntity randomTarget = null;
            //  Random r = new Random();

            if (componentList.Count == 0) return null;
            // Log.Info("ComponentList.Count = " + componentList.Count);
            while (randomTarget == null)
            {
                int randInt = GuidedMissileCore.GetSyncedRandom().Next(0, componentList.Count - 1);
                
                randomTarget = componentList[randInt];
                   //Log.Info("Got random block: " + randomTarget.GetType() + " by virtue of random number: " + randInt);
            }
            
            return randomTarget;
        }

        public static bool SetMissileTargetForGrid(IMyEntity grid, IMyEntity target)
        {
            if (grid == null) {
                Log.Info("The grid seems to be null, no target could be assigned.");
                return false;
            }
            if (target == null) {
                Log.Info("The target seems to be null, no target could be assigned.");
                return false;
            }

            MyEntityComponentContainer componentContainer = grid.Components;
            if (componentContainer == null) {
           //     Log.Info("The grid.Components are null. Could not assign a target!");
            }

            GuidedMissileTargetGridHook targetGridHook = null;
            foreach (MyComponentBase comp in componentContainer)
            {
                if (comp is GuidedMissileTargetGridHook)
                {
                    targetGridHook = (GuidedMissileTargetGridHook)comp;

                }
                if (comp is MyCompositeGameLogicComponent)
                {
             //       Log.Info("we got a composite component!");
                    targetGridHook = comp.GetAs<GuidedMissileTargetGridHook>();
                }
            }
            if (targetGridHook == null) {
              //  Log.Info("We did not found our TargetGridHook. A target could not be assigned!");
              //  Log.Info("This is what we had: ");
                foreach (MyComponentBase comp in componentContainer)
                {
                    Log.Info(comp.ToString());
                }
                return false;
            }
            return targetGridHook.SetMissileTarget(target);
        }

        public static IMyEntity GetMissileTargetForGrid(IMyEntity grid)
        {
            try
            {
                MyEntityComponentContainer componentContainer = grid.Components;
                GuidedMissileTargetGridHook targetGridHook = null;
                
                foreach (MyComponentBase comp in componentContainer)
                {
                    if (comp is GuidedMissileTargetGridHook)
                    {
                        targetGridHook = (GuidedMissileTargetGridHook)comp;

                    }
                    if (comp is MyCompositeGameLogicComponent) {
                      //  Log.Info("we got a composite component!");
                        targetGridHook = comp.GetAs<GuidedMissileTargetGridHook>();
                    }
                }
                return targetGridHook.GetMissileTarget();
            }
            catch (Exception e) {
                Log.Info("Error during GetMissileTargetForGrid: " + e.ToString());
                return null;
            }
        }

        private MyObjectBuilder_EntityBase objectBuilder;
        public IMyEntity missileTarget = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            this.objectBuilder = objectBuilder;
            removeSet = new HashSet<IMyEntity>();
            turretTargetDict = new Dictionary<IMyLargeTurretBase, IMyEntity>();
            Entity.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
      //      Log.Info("We initialized a grid hook");
        }

        public bool SetMissileTarget(IMyEntity target) //returns true if operation was succesful and this.target is not null after the operation
        {
            try
            {

                this.missileTarget = target;
                if (this.missileTarget != null) return true;

            }
            catch
            {
                return false;
            }
            return false;
        }

        public IMyEntity GetMissileTarget()
        {
            return missileTarget;
        }
        public override void Close()
        {
            objectBuilder = null;
            removeSet = null;
            turretTargetDict = null;
        }
        private Dictionary<IMyLargeTurretBase, IMyEntity> turretTargetDict;
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)objectBuilder.Clone() : objectBuilder;
        }
    /**    public override void UpdateBeforeSimulation10() //Methods for turret targeting 
        {
            IMyEntity target;
            Vector3 turretVector;
            Vector3 destinationVector;
            float angle;
            foreach (IMyLargeTurretBase turretBase in turretTargetDict.Keys) {
                if (turretTargetDict.TryGetValue(turretBase, out target)) {
                    turretVector = Vector3.Normalize(Vector3.Transform(turretBase.WorldMatrix.Forward, Matrix.CreateFromYawPitchRoll(turretBase.Azimuth, turretBase.Elevation, 0f)));
                    destinationVector = Vector3.Normalize(target.GetPosition() - turretBase.GetPosition());
                    angle = MyUtils.GetAngleBetweenVectors(turretVector, destinationVector);
                    if (angle < 0.02f) {
                        List<Sandbox.ModAPI.Interfaces.ITerminalAction> actionList = new List<Sandbox.ModAPI.Interfaces.ITerminalAction>();
                        turretBase.GetActions(actionList, null);
                        Log.Info("actionlist " + actionList);
                        foreach (var action in actionList) {
                            Log.Info(action.ToString());
                        }
                    }
                }
            }
            base.UpdateBeforeSimulation10();
        }  **/
        private HashSet<IMyEntity> removeSet;
        public override void UpdateBeforeSimulation100()
        {
            if (GetMissileTarget() != null) {
                ISet<IMyLargeTurretBase> turretSet = new HashSet<IMyLargeTurretBase>();
                if (Entity == null) return;
                // if(target.OwnerId == Entity.OwnerId) return false; //faction check?
                if (Entity.Hierarchy == null) return;
                if (Entity.Hierarchy.Children == null) return;
                HashSet<IMyEntity> componentSet = new HashSet<IMyEntity>();

                Entity.Hierarchy.GetChildrenRecursive(componentSet);
                
               foreach (IMyLargeTurretBase dictTurret in turretTargetDict.Keys)
               {
                   if (dictTurret.MarkedForClose) removeSet.Add(dictTurret);
                }
               foreach (var key in removeSet ) {
                   turretTargetDict.Remove((IMyLargeTurretBase)key);
               }
               removeSet.Clear();

               if ((GetMissileTarget() == null) || (GetMissileTarget().MarkedForClose))
               {
                   foreach (var localTurret in turretTargetDict.Keys)
                   {
                       if ((localTurret != null) && (!localTurret.MarkedForClose))
                       {
                           localTurret.ResetTargetingToDefault();
                       }
                   }
                   turretTargetDict.Clear();
                   return;
               }

                foreach (IMyEntity entity in componentSet) {
                    if (entity is IMyLargeTurretBase) {
                        var turret = entity as IMyLargeTurretBase;
                        var targetGrid = GetMissileTarget();



                        
                        if (turret.AIEnabled) {
                            IMyEntity turretTarget;
                            if (turretTargetDict.TryGetValue(turret, out turretTarget))
                            {

                                if ((turretTarget != null)&&(turretTarget.GetTopMostParent() == targetGrid))
                                {
                                  //  Log.Info("target is unchanged. Resuming tracking!");
                                    turret.TrackTarget(turretTarget);
                                }
                                else
                                {
                                    if (turret.Range > (Entity.GetPosition() - GetMissileTarget().GetPosition()).Length())
                                    {
                                        var randomTarget = GetRandomBlockInGrid(GetMissileTarget().GetTopMostParent());
                                        turret.TrackTarget(randomTarget);
                                      //  Log.Info("Set Target for Turret " + ((Sandbox.ModAPI.IMyCubeBlock)turret).DisplayName);
                                        if (turretTargetDict.ContainsKey(turret))
                                        {
                                            turretTargetDict.Remove(turret);
                                            turretTargetDict.Add(turret, randomTarget);
                                        }
                                        else {
                                            turretTargetDict.Add(turret, randomTarget);
                                        }
                                        
                                        
                                    }
                                }
                            }
                            else {
                                if (turret.Range > (Entity.GetPosition() - GetMissileTarget().GetPosition()).Length())
                                {
                                    var randomTarget = GetRandomBlockInGrid(GetMissileTarget().GetTopMostParent());
                                    turret.TrackTarget(randomTarget);
                                //    Log.Info("Set Target for Turret " + ((Sandbox.ModAPI.IMyCubeBlock)turret).DisplayName);
                                    if (turretTargetDict.ContainsKey(turret))
                                    {
                                        turretTargetDict.Remove(turret);
                                        turretTargetDict.Add(turret, randomTarget);
                                    }
                                    else
                                    {
                                        turretTargetDict.Add(turret, randomTarget);
                                    }
                                }
                            }                            
                        }
                    }
                } 
                
            }
            base.UpdateBeforeSimulation100();
        } 
    }

}

