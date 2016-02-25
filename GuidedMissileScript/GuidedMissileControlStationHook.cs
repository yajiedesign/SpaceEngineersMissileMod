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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), "GuidedMissileTargeter")]
    public class GuidedMissileControlStationHook : MyGameLogicComponent
    {

        private MyObjectBuilder_EntityBase _objectBuilder;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            this._objectBuilder = objectBuilder;

            //    Log.Info("A missile Control turret was created");
            try
            {
                GuidedMissileSingleton.GetInstance().AddTurretToSet(Entity);
            }
            catch
            {
                Log.Info("apparently entity or something else was null...tracking");
                Log.Info("GuidedMissileSingleton.GetInstance: " + GuidedMissileSingleton.GetInstance());
                Log.Info("Entity: " + Entity);
            }
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void Close()
        {

            _objectBuilder = null;

        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)_objectBuilder.Clone() : _objectBuilder;
        }


        // private bool playerEntered = false;
        /**      private void SetPlayerStatus(bool status)
              {

                  playerEntered = status;
                  if (status == true)
                  {
                   //   ShowTargetScreen();
                  }
                  if (status == false)
                  {
                 //     HideTargetScreen();
                  }
                  Log.Info("Set player status to " + status);

              } **/

        public override void UpdateAfterSimulation10()
        {

        }
        public override void UpdatingStopped()
        {

        }
        public bool AssignTarget(IMyEntity target)
        {
            Log.Info("called assignTarget with entity " + target);
            if (target == null) return false;
            // if(target.OwnerId == Entity.OwnerId) return false; //faction check?
            bool success = GuidedMissileTargetGridHook.SetMissileTargetForGrid(Entity.GetTopMostParent(), target);

            return success;
        }

        public void UpdateManually()
        {
            //     Log.Info("updating manually: turret");
            try
            {

                bool localPlayerEntered = false;


                IMyPlayerCollection allPlayers = MyAPIGateway.Players;
                IMyPlayer currentPlayer = null;
                List<IMyPlayer> playerList = new List<IMyPlayer>();
                allPlayers.GetPlayers(playerList, null);
                // Log.Info("update manually:");
                //   Log.Info("" + allPlayers + currentPlayer + playerList);
                foreach (IMyPlayer player in playerList)
                {
                    // Log.Info("iterating over player : " + player);
                    //    Log.Info("iterating over player " + player);

                    if ((player != null) && (player.Controller != null) && (player.Controller.ControlledEntity != null) && (player.Controller.ControlledEntity.Entity != null))
                    {
                        //   Log.Info("first if");
                        //      Log.Info("a controlled entity was not zero");

                        if (player.Controller.ControlledEntity.Entity.EntityId == Entity.EntityId)
                        {
                            // Log.Info("second if");
                            currentPlayer = player;
                            // Log.Info("currentplayer found");
                            //      Log.Info("set current player");
                            if (MyAPIGateway.Session.Player == player)
                            {
                                //  Log.Info("third if");
                                //currentPlayer = player;
                                if (localPlayerEntered == false) localPlayerEntered = true;
                            }
                        }
                    }
                }
                //  string pId = "";
                bool success = false;
                string targetName = "NoTargetFound";
                if (currentPlayer != null)
                {
                    // Log.Info("fouth if");
                    //    Log.Info("currentplayer wasnt null");
                    //   if (currentPlayer.Controller.ControlledEntity.Entity != null) pId = currentPlayer.Controller.ControlledEntity.Entity.ToString();

                    BoundingSphereD sphere = new BoundingSphereD(Entity.GetPosition() + Vector3.Normalize(Entity.WorldMatrix.Up) * 2, 5);

                    // MyAPIGateway.Entities.EnableEntityBoundingBoxDraw(Entity, true, new Vector4(255, 100, 100, 100), 0.01f, null);
                    List<IMyEntity> entitiesFound = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);


                    foreach (IMyEntity ent in entitiesFound)
                    {

                        //   Log.Info("found an entity around me " + ent.GetType().ToString());
                        // if (ent is Sandbox.Game.Weapons.MyMissile)
                        if (ent.GetType().ToString() == "Sandbox.Game.Weapons.MyMissile") //CHECK FOR OWNER OR SOMETHING ;_;
                        {
                            bool isActualMissile = false;
                            if ((Entity.Physics != null) && ((ent.Physics.LinearVelocity - Entity.Physics.LinearVelocity).Length() > 450f))
                            {
                                Log.Info("physics wasnt null and speed was sufficient");
                                isActualMissile = true;
                            }
                            else if ((Entity.Physics == null) || (Entity.Physics.LinearVelocity == null))
                            {
                                Log.Info("physics was null!");
                                Log.Info("ent speed was: " + ent.Physics.LinearVelocity.Length());
                                if (ent.Physics.LinearVelocity.Length() > 450f) isActualMissile = true;
                            }
                            if (isActualMissile)
                            {
                                Log.Info("found a missile");
                                Ray directionRay = new Ray(ent.GetPosition(), Vector3.Normalize(ent.Physics.LinearVelocity));

                                IMyEntity bestTarget = GuidedMissileCore.GetClosestTargetAlongRay(directionRay, 5000, 7.5, Entity.GetTopMostParent());
                                //   Log.Info("assign target, trying to call with : " + bestTarget);

                                if (bestTarget != null)
                                {
                                    success = AssignTarget(bestTarget);
                                    targetName = bestTarget.DisplayName;
                                }
                            }
                            ent.Close();
                        }
                    }

                }

                /**      if (localPlayerEntered != this.playerEntered)
                      {
                          SetPlayerStatus(localPlayerEntered);
                      } **/
                if (localPlayerEntered == true)
                {
                    if (success) MyAPIGateway.Utilities.ShowNotification(targetName + " was set as missile target!", 1000, MyFontEnum.Red);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                MyAPIGateway.Utilities.ShowNotification("" + e.ToString(), 1000, MyFontEnum.Red);
            }
        }
    }


}
