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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), "FixedTargetPainter")]
    public class FixedTargetPainterHook : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase _objectBuilder;

        private const float MaxDistance = 3000;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            this._objectBuilder = objectBuilder;
            
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }
        protected IMyEntity GetTarget(IMyEntity missile)
        {
            return null;
        }

        public override void Close()
        {

            _objectBuilder = null;
        }
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return _objectBuilder;
        } 

        public bool AssignTarget(IMyEntity target)
        {
            if (target == null) return false;
            // if(target.OwnerId == Entity.OwnerId) return false; //faction check?
            bool success = GuidedMissileTargetGridHook.SetMissileTargetForGrid(Entity.GetTopMostParent(), target);

            return success;
        }

        protected HashSet<IMyEntity> GetMissilesInBoundingBox()
        {
            BoundingBoxD box = (BoundingBoxD)Entity.WorldAABB.GetInflated(2.0);
          //  box = box.Translate(Entity.LocalMatrix.Forward * 0.2f);
            //Log.Info("Bounding box in GetMissilesInBoundingBox for "+ GetType()+ " is " + box.Size);
            List<IMyEntity> entitiesFound = MyAPIGateway.Entities.GetEntitiesInAABB(ref box);
            HashSet<IMyEntity> entitySet = new HashSet<IMyEntity>();

            foreach (IMyEntity ent in entitiesFound)
            {
                if ((ent.GetType().ToString() == "Sandbox.Game.Weapons.MyMissile") && (!GuidedMissileSingleton.IsGuidedMissile(ent)))
                {
                    //   Log.Info("detected something not yet added with speed: " + (ent.Physics.LinearVelocity - Entity.GetTopMostParent().Physics.LinearVelocity).Length());
                    //   Log.Info("topmostparent velocity was " + Entity.GetTopMostParent().Physics.LinearVelocity);
                    
                    if ((ent.Physics.LinearVelocity - Entity.GetTopMostParent().Physics.LinearVelocity).Length() > 110)
                    {
                        Log.Info("found something");
                        entitySet.Add(ent);


                    }
                    else
                    {
                        //    Log.Info("Missile was too fast! Adding to ignoreSet");
                        GuidedMissileSingleton.GetInstance().IgnoreSet.Add(ent);
                    }
                }
            }
            return entitySet;
        }

        public override void UpdateBeforeSimulation()
        {
            HashSet<IMyEntity> componentSet = new HashSet<IMyEntity>();

            Entity.GetTopMostParent().Hierarchy.GetChildrenRecursive(componentSet);
            bool underControl = false;
            IMyPlayer currentPlayer = null;
            foreach (IMyEntity component in componentSet)
            {
                if (component is IMyCockpit)
                {
                    var cockpit = component as IMyCockpit;
                    if (cockpit.IsUnderControl)
                    {
                        underControl = true;
                        currentPlayer = MyAPIGateway.Players.GetPlayerControllingEntity(cockpit);
                        break;
                    }
                }
            }
            var gun = Entity as IMyUserControllableGun;
            if ((underControl)&&(gun.IsShooting))
            {
                foreach (IMyEntity missile in GetMissilesInBoundingBox())
                {
                    Ray ray = new Ray(missile.GetPosition(), Vector3.Normalize(missile.WorldMatrix.Forward));
                    IMyEntity target = GuidedMissileCore.GetClosestTargetAlongRay(ray, MaxDistance, 7, Entity.GetTopMostParent());
                   // IMyEntity target = GuidedMissileTargetGridHook.GetRandomBlockInGrid(targetGrid);

                    AssignTarget(target);
                    missile.Close();
                    if ((currentPlayer == MyAPIGateway.Session.Player)&&(target!=null)) MyAPIGateway.Utilities.ShowNotification(target.GetTopMostParent().DisplayName + " was set as missile target!", 2000, MyFontEnum.Red);
                    break;
                }
            }
            base.UpdateBeforeSimulation();
        }

    }
}