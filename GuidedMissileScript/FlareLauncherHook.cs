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
using VRage.Game.Components;

namespace GuidedMissile.GuidedMissileScript
{
    /** WARNING: GUIDED MISSILES MUSNTN BE FASTER THAN A CERTAIN AMOUNT SPECIFIED IN GUIDEDMISSILELAUNCHERHOOK.cs **/

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), "FlareLauncher")]
    public class FlareLauncherHook : GuidedMissileLauncherHook
    {
        private const float DeflectChance = (7f / 10f); //rational number plox

        protected override double BoundingBoxOffsetFront
        {
            get { return 0.5; }
        }
        public override float TurningSpeed
        {
            get { return 0f; }
        }
        public override long DeathTimer
        {
            get { return 80; }
        }
        public override long SafetyTimer
        {
            get { return 0; }
        }

        public override bool HasPhysicsSteering
        {
            get { return false; }
        }

        public override void OnExplodeMissile(IMyEntity missile)
        {
            //Log.Info("flare missile exploded!");


        }
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            BbInflateAmount = 1.5;
            flareSet = new HashSet<IMyEntity>();
            deleteSet = new HashSet<IMyEntity>();
            base.Init(objectBuilder);
            //  Entity.DebugDraw();
            //   MyAPIGateway.Entities.EnableEntityBoundingBoxDraw(Entity, true, null, 0.05f, null);
            //  Log.Info("a sidewinder launcher was created");

        }
        public override void Close()
        {
            flareSet.Clear();
            deleteSet.Clear();
            flareSet = null;
            deleteSet = null;
            base.Close();
        }
        protected override IMyEntity GetTarget(IMyEntity missile)
        {
            return null;
        }

        public HashSet<IMyEntity> flareSet;
        public HashSet<IMyEntity> deleteSet;
        protected override void GuideMissiles(HashSet<IMyEntity> missileSet)
        {
            // Log.Info("called guidemissiles in flarelauncher");
            try
            {
                if (missileSet == null) return;
                if (missileSet.Count == 0) return;
                Log.Info("Set wasnt 0 or empty");
                ISet<IMyEntity> incomingMissiles = GuidedMissileSingleton.GetMissilesByTargetGrid(Entity.GetTopMostParent());
                if (incomingMissiles.Count == 0) return;
                foreach (IMyEntity guidedMissile in incomingMissiles)
                {
                    foreach (IMyEntity flare in missileSet)
                    {
                        if (!flareSet.Contains(flare))
                        {
                            Log.Info("got a flare: ");
                            flareSet.Add(flare);
                            int randomNumber = GuidedMissileCore.GetSyncedRandom().Next(1, 10);
                            if (randomNumber > (int)Math.Round(DeflectChance * 10))
                            {
                                GuidedMissileSingleton.SetTargetForMissile(guidedMissile, flare);
                                Log.Info("won dice roll! setting flare target for missile! " + randomNumber);
                            }
                            else {
                                Log.Info("failed dice roll! not deflecting missile! " + randomNumber);
                            }
                        }
                    }
                }
                foreach (IMyEntity flare in flareSet)
                {
                    if ((flare == null) || (flare.MarkedForClose))
                    {
                        deleteSet.Add(flare);
                    }
                    flareSet.ExceptWith(deleteSet);
                    deleteSet.Clear();
                }
            }
            catch (Exception e)
            {
                Log.Info("flares failed to work! caught exception: " + e);
            }
        }
    }
}

