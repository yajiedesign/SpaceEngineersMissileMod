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

    public class GuidedMissileSingleton
    {
        private class MissileDataContainer
        {
            public IMyEntity Missile { get; set; }
            public IMyEntity Target { get; set; }
            public long TrackedFrames { get; set; }
            public long DeathTimer = 10000;
            public float TurningSpeed = 0.1f;
            public bool HasPhysicsSteering = false;

            public bool isOvershooting = false;
            public float overshootDistance = 0;

            public bool finishedOvershooting = false;
            public Action<IMyEntity> OnExplode = delegate(IMyEntity entity) { Log.Info("An empty onexplode was called"); };

            public MissileDataContainer(IMyEntity missile, IMyEntity target, long safetyTimer, long deathTimer, float turningSpeed, Action<IMyEntity> onExplode, bool hasPhysicsSteering)
                : this(missile, target, safetyTimer, deathTimer, turningSpeed)
            {
                if(onExplode != null) OnExplode = onExplode;
                HasPhysicsSteering = hasPhysicsSteering;
            }
            public MissileDataContainer(IMyEntity missile, IMyEntity target, long safetyTimer, long deathTimer, float turningSpeed) {
                Missile = missile;
                Target = target;
                TrackedFrames = -safetyTimer;
                DeathTimer = deathTimer;
                TurningSpeed = turningSpeed;
            
            }
            public void SetOvershootDistance(float distance) { overshootDistance = distance; }
            public void ClearOvershootDistance() { overshootDistance = 0f; }
            public void StartOverShooting() { isOvershooting = true; }
            public void StopOverShooting() {isOvershooting = false;}
            public MissileDataContainer(IMyEntity missile, IMyEntity target, long safetyTimer, long deathTimer) {
                Missile = missile;
                Target = target;
                TrackedFrames = -safetyTimer;
                DeathTimer = deathTimer;
            }

            public MissileDataContainer(IMyEntity missile, IMyEntity target, long safetyTimer)
            {
                Missile = missile;
                Target = target;
                TrackedFrames = -safetyTimer;
            }
            public MissileDataContainer(IMyEntity missile, IMyEntity target)
            {
                Missile = missile;
                Target = target;
                TrackedFrames = 0;
            }
            public void SetEmpty()
            {
                Missile = null;
                Target = null;
            }
            
            public bool IsExpired()
            {
                if (Missile == null) return true;
                if (Target == null)  return true;
                if (Missile.MarkedForClose) {
                    OnExplode(Missile);
                    return true;
                } 
                if (TrackedFrames > DeathTimer) {
                    Missile.Close();
                    return true;
                }
                if (Target.MarkedForClose) {
                   // Log.Info("Target is marked for close, searching new one");
                    if (Target.GetTopMostParent().MarkedForClose) return true;
                    IMyEntity newTarget = GuidedMissileTargetGridHook.GetRandomBlockInGrid(Target);
                    if (newTarget == null) return true;
                    Target = newTarget;
                    isOvershooting = false;
                    overshootDistance = 0f;
                    finishedOvershooting = false;
                    return false; 
                  //  return true;
                
                } 
                return false;
            }
            
            public void Update() {
                if (SafetyTimerIsOver())
                {
                    
              //      if (Missile.Physics.Flags != RigidBodyFlag.RBF_BULLET) Missile.Physics.Flags = RigidBodyFlag.RBF_BULLET;
                    //     float TURNING_SPEED = 0.1f;
                    //   float FACTOR = 2f;
                //    Log.Info("Missile.Physics.Flags");
                    //BLEH
                    Vector3 targetPoint = Vector3.Zero;
                    try
                    {

                        Vector3 boundingBoxCenter = (Target.WorldAABB.Max + Target.WorldAABB.Min) * 0.5;
                        if (boundingBoxCenter == Vector3.Zero) boundingBoxCenter = Target.GetPosition();
                        if (HasPhysicsSteering)
                        {
                            Ray positiveVelocityRay = new Ray(Missile.GetPosition(), Missile.Physics.LinearVelocity);
                            Ray negativeVelocityRay = new Ray(Missile.GetPosition(), -1f * Missile.Physics.LinearVelocity);
                            Plane targetPlane = new Plane(boundingBoxCenter, Vector3.Normalize(Missile.Physics.LinearVelocity));
                            float? intersectionDist = positiveVelocityRay.Intersects(targetPlane);
                            //  Vector3 reverseVector;
                            if (intersectionDist != null)
                            {
                                targetPoint = positiveVelocityRay.Position + (float)intersectionDist * positiveVelocityRay.Direction;
                                //  reverseVector = Target.GetPosition() - targetPoint;
                                targetPoint = 2 * boundingBoxCenter - targetPoint;
                                //  targetPoint = targetPoint +  reverseVector;

                            }
                            else
                            {
                                intersectionDist = negativeVelocityRay.Intersects(targetPlane);
                                if (intersectionDist != null)
                                {

                                    if ((isOvershooting == false) && (finishedOvershooting == false))
                                    {
                                        overshootDistance = Missile.Physics.LinearVelocity.Length() * (360 / TurningSpeed) / 10000 + 0;
                                        isOvershooting = true;
                                    }
                                    //  targetPoint = negativeVelocityRay.Position + (float)intersectionDist * negativeVelocityRay.Direction;

                                    if (isOvershooting && !finishedOvershooting)
                                    {
                                        if (intersectionDist > overshootDistance)
                                        {
                                            targetPoint = boundingBoxCenter;
                                            finishedOvershooting = true;
                                            // Log.Info("we finished overshooting, returning to target.");
                                        }
                                        else
                                        {
                                            targetPoint = Missile.GetPosition() + Missile.WorldMatrix.Forward;
                                            //   Log.Info("we overshot, we keep heading forward.");
                                            //   Log.Info("         current Distance: " + intersectionDist + " and overshootCorr " + overshootDistance);

                                        }
                                    }
                                    else if (isOvershooting && finishedOvershooting)
                                    {
                                        targetPoint = boundingBoxCenter;
                                    }
                                   
                                }
                            }


                            Vector3 diffVelocity = Target.GetTopMostParent().Physics.LinearVelocity - Missile.Physics.LinearVelocity;

                            if (targetPoint == Vector3.Zero) { targetPoint = boundingBoxCenter - diffVelocity; }

                        }
                        else
                        {
                            targetPoint = boundingBoxCenter;
                        }
                    }
                    catch (Exception e)
                    {

                        Log.Info("Caught exception during MissileData Update! " + e);
                        if (Target != null) targetPoint = Target.GetPosition();
                    }

                    Vector3 targetDirection = Vector3.Normalize(targetPoint - Missile.GetPosition());

                    float maxRadVelocity = MathHelper.ToRadians(TurningSpeed);
                    float angle = MyUtils.GetAngleBetweenVectorsAndNormalise(Missile.WorldMatrix.Forward, targetDirection);
                   // Log.Info("angle = " + MathHelper.ToDegrees(angle));
                    float turnPercent = 0f;

                    if (Math.Abs(angle) < double.Epsilon)
                    {
                        turnPercent = 0f;
                    }
                    else if (Math.Abs(angle) > maxRadVelocity)
                    {
                        turnPercent = maxRadVelocity / Math.Abs(angle);
                    }
                    else
                    {
                        turnPercent = 1f;
                    }

                    
                    Matrix targetMatrix = Matrix.CreateWorld(Missile.GetPosition(), targetDirection, Vector3D.CalculatePerpendicularVector(targetDirection));//Matrix.CreateFromYawPitchRoll(0f, (float)Math.PI*0.5f, 0f)));

                    var slerpMatrix = Matrix.Slerp(Missile.WorldMatrix, targetMatrix, turnPercent);

                    Missile.SetWorldMatrix(slerpMatrix);

                    if (HasPhysicsSteering)
                    {

                        var linVel = Missile.Physics.LinearVelocity;
                        var linSpeed = linVel.Length();
                        Missile.Physics.LinearVelocity = 0.98f * linVel + 0.02f * Missile.WorldMatrix.Forward * linSpeed;

                    }
                    else {
                        Vector3 linVel = Missile.Physics.LinearVelocity;
                        Missile.Physics.LinearVelocity = Vector3.Normalize(Missile.WorldMatrix.Forward) * linVel.Length();
                    }
 

                }
                else {
               //     if (Missile.Physics.Flags == RigidBodyFlag.RBF_BULLET) Missile.Physics.Flags = RigidBodyFlag.RBF_BULLET & RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE;
                }
                Tick();
            }

            public void Tick()
            {
                TrackedFrames++;
            }
            public bool SafetyTimerIsOver()
            {
                return (TrackedFrames > 0);
            }
        }


        private static GuidedMissileSingleton INSTANCE = null;

        private Dictionary<long, MissileDataContainer> guidedMissileDict; //A dictionary. the missiles are the keys, their targets the values.
        private HashSet<long> deleteSet;
        public HashSet<IMyEntity> IgnoreSet { get; private set; }

        private HashSet<IMyEntity> turretSet;
        private HashSet<IMyEntity> deleteTurretSet;

        private HashSet<WarningMessageContainer> warningGridSet;
        
        private GuidedMissileSingleton()
        {
            guidedMissileDict = new Dictionary<long, MissileDataContainer>();
            deleteSet = new HashSet<long>();
            turretSet = new HashSet<IMyEntity>();
            deleteTurretSet = new HashSet<IMyEntity>();
            IgnoreSet = new HashSet<IMyEntity>();
            warningGridSet = new HashSet<WarningMessageContainer>();

          //  Log.Info("GuidedMissileSingleton was created.");
        }

        public static GuidedMissileSingleton GetInstance()
        {
            if (INSTANCE == null)
            {
                INSTANCE = new GuidedMissileSingleton();
            }
            return INSTANCE;
        }

        public static void Update()
        {
            GetInstance().UpdateBeforeSimulation();

        }
        private class WarningMessageContainer {
            public long Ticks = 0;
            public IMyEntity Grid;

            public WarningMessageContainer(IMyEntity grid) {
                this.Grid = grid;
            }

            public void Update() {
                Ticks++;
            }
        }

        private void DisplayWarningMessage(IMyEntity target) {

            IMyEntity grid = target.GetTopMostParent();
            bool alreadyContained = false;
            foreach (WarningMessageContainer container in warningGridSet) {
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

                allPlayers.GetPlayers(playerList, null);
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
                                   
                                    warningGridSet.Add(new WarningMessageContainer(grid));
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
            if (!(grid is Sandbox.ModAPI.IMyCubeGrid))
            {
                Log.Info("grid wasnt an actual grid!");
                return null;
            }
            if (grid != null)
            {

              //  Log.Info("grid wasnt null");
                ICollection<MissileDataContainer> mdContainers = GetInstance().guidedMissileDict.Values;

                HashSet<IMyEntity> missileSet = new HashSet<IMyEntity>();

                foreach (MissileDataContainer container in mdContainers)
                {

                    var missile = container.Missile;
                    var target = container.Target;
                    if (target.GetTopMostParent() == grid) missileSet.Add(missile);
         //           Log.Info("added missile to targetedongridset " + missileSet.Count);

                }
                return missileSet;

            }
            Log.Info("returning null");
            return null;
        }
        public static bool SetTargetForMissile(IMyEntity missile, IMyEntity target) {
            return GetInstance().SetTargetForMissileInDict(missile, target);
        }
        public bool SetTargetForMissileInDict(IMyEntity missile, IMyEntity target) {
            if (missile == null) return false;
            if (target == null) return false;
            if (target.MarkedForClose) return false;
          //  Log.Info("neither target nor missile are null");
            if (!guidedMissileDict.ContainsKey(missile.EntityId)) return false;
            if (missile.MarkedForClose) return false;
          //  Log.Info("missiledict contains missile and isnt marked for close");
            MissileDataContainer mdC;
            guidedMissileDict.TryGetValue(missile.EntityId, out mdC);
            mdC.Target = target;
          //  Log.Info("SetTargetForMissile: return true");
            return true;
        }
        public bool AddMissileToDict(IMyEntity missile, IMyEntity target, long safetyTimer, long deathTimer, float turningSpeed, Action<IMyEntity> onExplode, bool hasPhysicsSteering)
        {
            if (!(missile.GetType().ToString() == "Sandbox.Game.Weapons.MyMissile")) return false;
            
            if ((missile == null) || (target == null)) return false;

            if (guidedMissileDict.ContainsKey(missile.EntityId)) return false;
            if (IgnoreSet.Contains(missile)) return false;
            guidedMissileDict.Add(missile.EntityId, new MissileDataContainer(missile, target, safetyTimer, deathTimer, turningSpeed, onExplode, hasPhysicsSteering));
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
                if (comp is GuidedMissileControlStationHook)
                {
                    controlStationHook = (GuidedMissileControlStationHook)comp;

                }
            }
            return controlStationHook;
        }
        public bool RemoveMissileFromDict(IMyEntity missile)
        {
            if (missile == null) return false;
            if (!guidedMissileDict.ContainsKey(missile.EntityId)) return false;
            guidedMissileDict.Remove(missile.EntityId);
            return true;
        }
        public static bool IsGuidedMissile(IMyEntity missile) {
            return GetInstance().MissileIsGuided(missile);
        }
        public bool MissileIsGuided(IMyEntity missile)
        {
            return guidedMissileDict.ContainsKey(missile.EntityId);
        }

        public bool AddTurretToSet(IMyEntity turret)
        {
       //     Log.Info("tried to add turret to set");
            return turretSet.Add(turret);
        }

        public bool RemoveTurretFromSet(IMyEntity turret)
        {
       //     Log.Info("tried to remove turret from Set");
            return turretSet.Remove(turret);
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

            foreach (IMyEntity ent in IgnoreSet) {
                if (ent != null) {
                    if (ent.MarkedForClose) delFromIgnoreSet.Add(ent);
                }
            }
            IgnoreSet.ExceptWith(delFromIgnoreSet);
            delFromIgnoreSet.Clear();

            HashSet<WarningMessageContainer> delFromWarningSet = new HashSet<WarningMessageContainer>();
            foreach (WarningMessageContainer container in warningGridSet)
            {
                if (container != null)
                {
                    container.Update();
                    if (container.Ticks > 240) delFromWarningSet.Add(container);
                    if (container.Grid == null) delFromWarningSet.Add(container);
                    if (container.Grid.MarkedForClose) delFromWarningSet.Add(container);
                }
            }
            warningGridSet.ExceptWith(delFromWarningSet);
            delFromWarningSet.Clear();

            Dictionary<long, MissileDataContainer>.KeyCollection keyCollection = guidedMissileDict.Keys;
            MissileDataContainer missileData;
            foreach (long key in keyCollection)
            {
                if (guidedMissileDict.TryGetValue(key, out missileData))
                {
                    if (missileData.IsExpired())
                    {
                        deleteSet.Add(key);
                        missileData.SetEmpty();
                    }
                    else
                    {
                        missileData.Update();
                    }
                }
            }
            foreach (long deleteId in deleteSet)
            {
                guidedMissileDict.Remove(deleteId);
            }
            deleteSet.Clear();

            foreach (IMyEntity turret in turretSet)
            {
                if (turret.MarkedForClose) { deleteTurretSet.Add(turret); }
                else
                {
                    UpdateTurret(turret);
                }
            }
            turretSet.ExceptWith(deleteTurretSet);
            deleteTurretSet.Clear();

        }


    }
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class GuidedMissileCore : MySessionComponentBase
    {

        private static Random r;

        public static Random GetSyncedRandom() { return r; }
    /**    private static void SendAction(byte[] message)
        {
            UInt64Converter idConv = new UInt64Converter(message);
            ulong id = idConv.Value;
        //    byte[] seedArray = BitConverter.GetBytes(MyAPIGateway.Session.ElapsedPlayTime.Milliseconds);
            Int32Converter c = MyAPIGateway.Session.ElapsedPlayTime.Milliseconds;
            r = new Random(c.Value);
            byte[] seedArray = { c.Byte1, c.Byte2, c.Byte3, c.Byte4 };
            MyAPIGateway.Multiplayer.SendMessageTo(0, seedArray, id, true);
            Log.Info("AskMessage received! Send Seed " + MyAPIGateway.Session.ElapsedPlayTime.Milliseconds + " to user " + id);
            
        }

        private static void ReceiveAction(byte[] message)
        {
         //   int Seed = BitConverter.ToInt32(message, 0);
            int Seed = (new Int32Converter(message)).Value;
            r = new Random(0);
            Log.Info("SeedMessage received! Set Seed to " + Seed);
        } **/

        private static void ReceiveSeed(byte[] message) {
            int seed = (new Int32Converter(message)).Value;
            r = new Random(seed);
            Log.Info("SeedMessage received! Set Seed to " + seed);
        }

        private static void ALLOCATE_RANDOM()
        {
           // Log.Info("were in MP : " + MyAPIGateway.Multiplayer.MultiplayerActive);
           // Log.Info("were the server host: " + MyAPIGateway.Multiplayer.IsServer);

         //   if (!MyAPIGateway.Multiplayer.MultiplayerActive) return;

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                //Log.Info("initial Seed created on server");
                r = new Random(MyAPIGateway.Session.ElapsedPlayTime.Milliseconds);
            //    Action<byte[]> sendAction = SendAction;
             //   MyAPIGateway.Multiplayer.RegisterMessageHandler(0, sendAction);

            }
            else
            {
             //   Log.Info("setting up message handler to receive Seed");
                MyAPIGateway.Multiplayer.RegisterMessageHandler(2, ReceiveSeed);
               // UInt64Converter idConv = new UInt64Converter(MyAPIGateway.Multiplayer.MyId);
              //  byte[] byteArray = { idConv.Byte1, idConv.Byte2, idConv.Byte3, idConv.Byte4, idConv.Byte5, idConv.Byte6, idConv.Byte7, idConv.Byte8 };
              //  MyAPIGateway.Multiplayer.SendMessageToServer(0, byteArray);
              //  Log.Info("message to server sent!");
            }
        } 
        public static bool init { get; private set; }
        public void Init()
        {

            Log.Info("Initialized.");
            init = true;
          //  r = new Random(MyAPIGateway.Session.ElapsedPlayTime.Milliseconds);
            ALLOCATE_RANDOM();
        }


        protected override void UnloadData()
        {
            Log.Info("Mod unloaded.");
            Log.Close();
            init = false;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!init)
            {
                if (MyAPIGateway.Session == null)
                    return;

                Init();
            }
            else
            {
                GuidedMissileSingleton.Update();
                if ((MyAPIGateway.Multiplayer.MultiplayerActive)&&(MyAPIGateway.Multiplayer.IsServer)) {
                    if (MyAPIGateway.Session.ElapsedPlayTime.Milliseconds % 2000 == 0)
                    {
                        int nextSeedNumber = r.Next(100000);
                        Int32Converter i = nextSeedNumber;
                        byte[] byteArray = { i.Byte1, i.Byte2, i.Byte3, i.Byte4 };
                        MyAPIGateway.Multiplayer.SendMessageToOthers(2, byteArray);
                      //  Log.Info("sent message to others");
                        r = new Random(nextSeedNumber);
                      //  Log.Info("created new random with Seed " + nextSeedNumber);
                    }
                }
            }
        }
        public static IMyEntity GetClosestTargetAlongRay(Ray ray, double maxDistance, double deviation, IMyEntity ignoreEntity) { 
            var set = new HashSet<IMyEntity>();
            set.Add(ignoreEntity);
            return GetClosestTargetAlongRay(ray, maxDistance, deviation, set);
        }

        public static IMyEntity GetClosestTargetAlongRay(Ray ray, double maxDistance, double deviation, HashSet<IMyEntity> ignoreSet) {
            if (Math.Abs(deviation) < double.Epsilon) deviation = 7.5;
            Ray directionRay = ray;
            BoundingSphereD largeSphere = new BoundingSphereD(directionRay.Position, maxDistance);

            BoundingBox targetBox;
            float lowestDistance = Single.MaxValue;
            float safetyDistance = (float)(deviation * 1.33);
            float? distance = 0;
            IMyEntity bestTarget = null;
            HashSet<IMyEntity> foundEntitySet = new HashSet<IMyEntity>();

            for (float i = (float)(deviation * 1.33); i < maxDistance; i += (float)(deviation * 1.33))
            {
                largeSphere = new BoundingSphereD(directionRay.Position + i * directionRay.Direction, deviation);
                foundEntitySet.UnionWith(MyAPIGateway.Entities.GetEntitiesInSphere(ref largeSphere));
            }

            foreach (IMyEntity foundEntity in foundEntitySet)
            {
               // Log.Info("foundentityset.count = " + foundEntitySet.Count);
                targetBox = (BoundingBox)foundEntity.GetTopMostParent().WorldAABB;
                directionRay.Intersects(ref targetBox, out distance);
                if (distance != null)
                {
                  //  Log.Info("distance wasnt zero for: " + foundEntity.GetType().ToString());
                   // if ((float)distance < lowestDistance) Log.Info("distance < lowest distance for " + foundEntity.EntityId);
                 //   if (foundEntity.GetTopMostParent().GetType().ToString() == "Sandbox.Game.Entities.MyCubeGrid") Log.Info("target is in a cubegrid  " + foundEntity.EntityId);
                  //  if ((float)distance > safetyDistance) Log.Info("distance > safety distance for " + foundEntity.EntityId);
                    if (((float)distance < lowestDistance) && ((float)distance > safetyDistance) && (foundEntity.GetTopMostParent().GetType().ToString() == "Sandbox.Game.Entities.MyCubeGrid"))
                    {
                        if (!ignoreSet.Contains(foundEntity))
                        {
                            bestTarget = foundEntity;
                            lowestDistance = (float)distance;
                          //  Log.Info("we found a fitting target: " + foundEntity.DisplayName);
                        }
                    }                    
                }
            }
            return bestTarget;
        }
    }    
}

