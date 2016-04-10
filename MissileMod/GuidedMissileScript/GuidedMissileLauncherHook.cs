using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace GuidedMissile.GuidedMissileScript
{


    public abstract class GuidedMissileLauncherHook : MyGameLogicComponent
    {
        #region MissileAttributes

        public abstract float TurningSpeed { get; }
        public abstract long SafetyTimer { get; }
        public abstract long DeathTimer { get; }
        public abstract bool HasPhysicsSteering { get; }
        protected double BbInflateAmount = 0;
        protected abstract double BoundingBoxOffsetFront { get; }

        public abstract void OnExplodeMissile(IMyEntity missile);

        public const float MaxSpeedForGuidance = 95f; //WORKAROUND FOR DUMBFIRE MISSILES! ITS THE THRESHOLD!

        #endregion


        protected MyObjectBuilder_EntityBase ObjectBuilder;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _lastIsShooting = false;
            ObjectBuilder = objectBuilder;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            // BoundingBoxD box = (BoundingBoxD)Entity.WorldAABB.GetInflated(BB_INFLATE_AMOUNT);
            //     Log.Info("Bounding box for "+GetType()+": " + box.Size);
        }

        public override void Close()
        {
            ObjectBuilder = null;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)ObjectBuilder.Clone() : ObjectBuilder;
        }
        protected virtual HashSet<IMyEntity> GetMissilesInBoundingBox()
        {
            BoundingBoxD box = Entity.WorldAABB.GetInflated(BbInflateAmount);
            if (Math.Abs(BoundingBoxOffsetFront) > double.Epsilon) box = box.Translate(Entity.LocalMatrix.Forward * (float)BoundingBoxOffsetFront);
            //Log.Info("Bounding box in GetMissilesInBoundingBox for "+ GetType()+ " is " + box.Size);
            List<IMyEntity> entitiesFound = MyAPIGateway.Entities.GetEntitiesInAABB(ref box);
            HashSet<IMyEntity> entitySet = new HashSet<IMyEntity>();

            foreach (IMyEntity ent in entitiesFound)
            {
                // Log.Info("in guidedmissilehook: found Entity : " + ent);
                if ((ent.GetType().ToString() == GuidedMissileSingleton.SandboxGameWeaponsMyMissile) && (!GuidedMissileSingleton.IsGuidedMissile(ent)))
                {
                    Log.Info("detected something not yet added with speed: " + (ent.Physics.LinearVelocity - Entity.GetTopMostParent().Physics.LinearVelocity).Length());
                    Log.Info("topmostparent velocity was " + Entity.GetTopMostParent().Physics.LinearVelocity);
                    if ((ent.Physics.LinearVelocity - Entity.GetTopMostParent().Physics.LinearVelocity).Length() < MaxSpeedForGuidance)
                    {
                        entitySet.Add(ent);
                    }
                    else {
                        Log.Info("Missile was too fast! Adding to ignoreSet");
                        GuidedMissileSingleton.GetInstance().IgnoreSet.Add(ent);
                    }
                }
            }
            return entitySet;
        }
        private bool _lastIsShooting;
        protected virtual void GuideMissiles(HashSet<IMyEntity> missileSet)
        {
            if (missileSet == null) return;
            if (missileSet.Count == 0) return;

            Action<IMyEntity> onExplode = OnExplodeMissile; //Hook for implementation of abstract method
            foreach (IMyEntity ent in missileSet)
            {
                var target = GetTarget(ent);
                if (target != null) GuidedMissileSingleton.GetInstance().AddMissileToDict(ent, target, SafetyTimer, DeathTimer, TurningSpeed, onExplode, HasPhysicsSteering);
            }
        }
        protected virtual IMyEntity GetTarget(IMyEntity missile)
        {
            IMyEntity targetGrid = GuidedMissileTargetGridHook.GetMissileTargetForGrid(Entity.GetTopMostParent());
            IMyEntity target = GuidedMissileTargetGridHook.GetRandomBlockInGrid(targetGrid);
            if (target == null) target = targetGrid;
            return target;
        }
        public override void UpdateBeforeSimulation()
        {
            try
            {
                var gun = Entity as IMyUserControllableGun;

                if (gun != null && gun.IsShooting != _lastIsShooting)
                {
                    Log.Info("is shooting is " + gun.IsShooting + " for " + ((Sandbox.ModAPI.IMyTerminalBlock)Entity).CustomName);
                    _lastIsShooting = gun.IsShooting;
                }
                if (gun != null && gun.IsShooting)
                {
                    GuideMissiles(GetMissilesInBoundingBox());
                }
  

            }
            catch (Exception e)
            {
                Log.Error(e);
                MyAPIGateway.Utilities.ShowNotification("" + e, 1000, MyFontEnum.Red);
            }
        }


    }
}

