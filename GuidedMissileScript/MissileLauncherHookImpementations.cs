using Sandbox;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
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
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using VRage.Game;
using VRage.Game.Components;

namespace GuidedMissile.GuidedMissileScript
{
    /** WARNING: GUIDED MISSILES MUSNTN BE FASTER THAN A CERTAIN AMOUNT SPECIFIED IN GUIDEDMISSILELAUNCHERHOOK.cs **/

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), "CruiseMissileLauncher")]
    public class CruiseMissileLauncherHook : GuidedMissileLauncherHook
    {
        protected override double BoundingBoxOffsetFront
        {
            get { return 0; }
        }
        public override float TurningSpeed // IN DEGREES!
        {
            get { return 1f; }
        }
        public override long DeathTimer
        {
            get { return 1000; }
        }
        public override long SafetyTimer
        {
            get { return 30; }
        }

        public override bool HasPhysicsSteering
        {
            get { return true; }
        }
        public override void OnExplodeMissile(IMyEntity missile)
        {

        }
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            BbInflateAmount = 2; //4

            base.Init(objectBuilder);


        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), "CatapultTorpedoLauncher")]
    public class CatapultTorpedoLauncherHook : GuidedMissileLauncherHook
    {
        protected override double BoundingBoxOffsetFront
        {
            get { return 0; } //0
        }
        public override float TurningSpeed // IN DEGREES!
        {
            get { return 0.5f; }
        }
        public override long DeathTimer
        {
            get { return 9000; }
        }
        public override long SafetyTimer
        {
            get { return 80; }
        }

        public override bool HasPhysicsSteering
        {
            get { return true; }
        }
        public override void OnExplodeMissile(IMyEntity missile)
        {
            //    Log.Info("A catapult torpedo exploded!");
        }
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            BbInflateAmount = 4; //4

            base.Init(objectBuilder);


        }
    }
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), "SidewinderSmallMissileLauncher")]
    public class SidewinderLauncherHook : GuidedMissileLauncherHook
    {

        protected override double BoundingBoxOffsetFront
        {
            get { return 0; }
        }
        public override float TurningSpeed
        {
            get { return 4f; }
        }
        public override long DeathTimer
        {
            get { return 200; }
        }
        public override long SafetyTimer
        {
            get { return 30; }
        }

        public override bool HasPhysicsSteering
        {
            get { return false; }
        }

        public override void OnExplodeMissile(IMyEntity missile)
        {
            //  Log.Info("sidewinder missile exploded!");

        }
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            BbInflateAmount = 0.9;

            base.Init(objectBuilder);


        }

        protected override IMyEntity GetTarget(IMyEntity missile)
        {

            //IMyEntity targetGrid = GuidedMissileTargetGridHook.GetMissileTargetForGrid(Entity.GetTopMostParent());


            Ray ray = new Ray(missile.GetPosition(), Vector3.Normalize(missile.WorldMatrix.Forward));
            IMyEntity targetGrid = GuidedMissileCore.GetClosestTargetAlongRay(ray, 3000, 7, Entity.GetTopMostParent());
            IMyEntity target = GuidedMissileTargetGridHook.GetRandomBlockInGrid(targetGrid);
            if (target == null)
            {
                //  Log.Info("target was null...");
                //   if (targetGrid == null) Log.Info("targetgrid was null as well!");
                target = targetGrid;
                Log.Info("target was null, now set to grid");
                //  IMyEntity grid = target;

            }

            //  Log.Info("got a target: " + target.ToString());

            if (target != null)
            {

                IMyPlayerCollection allPlayers = MyAPIGateway.Players;

                List<IMyPlayer> playerList = new List<IMyPlayer>();

                HashSet<IMyEntity> componentSet = new HashSet<IMyEntity>();

                Entity.GetTopMostParent().Hierarchy.GetChildrenRecursive(componentSet);
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
                                    MyAPIGateway.Utilities.ShowNotification("" + target.GetTopMostParent().DisplayName + " was set as missile target!", 2000, MyFontEnum.Red);
                                }
                            }
                        }
                    }
                }


            }
            return target;
        }
    }
}


// Log.Info("my delegated method was called!");
//not allowed:    Log.Info("gravity: " + MyGravityProviderSystem.CalculateNaturalGravityInPoint(missile.GetPosition()));
/**
MyParticleEffect m_smokeEffect;
MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Missile, out m_smokeEffect);
if (m_smokeEffect != null)
{
    var matrix = missile.WorldMatrix;
    matrix.Translation -= matrix.Forward * 1f;
    m_smokeEffect.WorldMatrix = matrix;
    m_smokeEffect.AutoDelete = false;
    m_smokeEffect.CalculateDeltaMatrix = true;
} **/
