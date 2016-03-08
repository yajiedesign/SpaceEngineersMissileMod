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
using VRage.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace GuidedMissile.GuidedMissileScript
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class GuidedMissileCore : MySessionComponentBase
    {

        private static Random _r;

        public static Random GetSyncedRandom() { return _r; }
        /**    private static void SendAction(byte[] message)
            {
                UInt64Converter idConv = new UInt64Converter(message);
                ulong id = idConv.Value;
            //    byte[] seedArray = BitConverter.GetBytes(MyAPIGateway.Session.ElapsedPlayTime.Milliseconds);
                Int32Converter c = MyAPIGateway.Session.ElapsedPlayTime.Milliseconds;
                _r = new Random(c.Value);
                byte[] seedArray = { c.Byte1, c.Byte2, c.Byte3, c.Byte4 };
                MyAPIGateway.Multiplayer.SendMessageTo(0, seedArray, id, true);
                Log.Info("AskMessage received! Send Seed " + MyAPIGateway.Session.ElapsedPlayTime.Milliseconds + " to user " + id);
                
            }

            private static void ReceiveAction(byte[] message)
            {
             //   int Seed = BitConverter.ToInt32(message, 0);
                int Seed = (new Int32Converter(message)).Value;
                _r = new Random(0);
                Log.Info("SeedMessage received! Set Seed to " + Seed);
            } **/

        private static void ReceiveSeed(byte[] message)
        {
            int seed = (new Int32Converter(message)).Value;
            _r = new Random(seed);
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
                _r = new Random(MyAPIGateway.Session.ElapsedPlayTime.Milliseconds);
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
        public static bool IsInit { get; private set; }
        public void Init()
        {

            Log.Info("Initialized.");
            IsInit = true;

             var temp = MyDefinitionManager.Static.GetAllDefinitions()
                .FirstOrDefault(w => w.Id.SubtypeId == MyStringHash.GetOrCompute("GuidedMissileTargeterAmmoMagazine"));
            if (temp != null)
            {
                GuidedMissileTargeterAmmo = temp.Id;
            }
            //  _r = new Random(MyAPIGateway.Session.ElapsedPlayTime.Milliseconds);
            ALLOCATE_RANDOM();
        }

        public static MyDefinitionId? GuidedMissileTargeterAmmo { get; set; }


        protected override void UnloadData()
        {
            Log.Info("Mod unloaded.");
            Log.Close();
            IsInit = false;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!IsInit)
            {
                if (MyAPIGateway.Session == null)
                { return;}

                Init();
            }
            else
            {
                GuidedMissileSingleton.Update();
                if ((MyAPIGateway.Multiplayer.MultiplayerActive) && (MyAPIGateway.Multiplayer.IsServer))
                {
                    if (MyAPIGateway.Session.ElapsedPlayTime.Milliseconds % 2000 == 0)
                    {
                        int nextSeedNumber = _r.Next(100000);
                        Int32Converter i = nextSeedNumber;
                        byte[] byteArray = { i.Byte1, i.Byte2, i.Byte3, i.Byte4 };
                        MyAPIGateway.Multiplayer.SendMessageToOthers(2, byteArray);
                        //  Log.Info("sent message to others");
                        _r = new Random(nextSeedNumber);
                        //  Log.Info("created new random with Seed " + nextSeedNumber);
                    }
                }
            }
        }
        public static IMyEntity GetClosestTargetAlongRay(Ray ray, double maxDistance, double deviation, IMyEntity ignoreEntity)
        {
            var set = new HashSet<IMyEntity> {ignoreEntity};
            return GetClosestTargetAlongRay(ray, maxDistance, deviation, set);
        }

        public static IMyEntity GetClosestTargetAlongRay(Ray ray, double maxDistance, double deviation, HashSet<IMyEntity> ignoreSet)
        {
            if (Math.Abs(deviation) < double.Epsilon) deviation = 7.5;
            Ray directionRay = ray;

            float baseDistance = 100.0f;
            float lowestDistance = float.MaxValue;
            float safetyDistance = (float)(deviation * 1.33);
            IMyEntity bestTarget = null;
            HashSet<IMyEntity> foundEntitySet = new HashSet<IMyEntity>();

            for (float i = (float)(deviation * 1.33); i < maxDistance; i += (float)((deviation / 2) / baseDistance * i))
            {
                var largeSphere = new BoundingSphereD(directionRay.Position + i * directionRay.Direction, (deviation / 2) / baseDistance * i * 2);
                foundEntitySet.UnionWith(MyAPIGateway.Entities.GetEntitiesInSphere(ref largeSphere));
            }

            foreach (IMyEntity foundEntity in foundEntitySet)
            {
                // Log.Info("foundentityset.count = " + foundEntitySet.Count);
                var targetBox =
                    BoundingSphere.CreateFromBoundingBox((BoundingBox) foundEntity.GetTopMostParent().WorldAABB);
                var posDistance = Vector3.Distance(targetBox.Center, ray.Position);
                if (posDistance >= baseDistance)
                {
                    targetBox.Radius *= posDistance / baseDistance;
                }

                var direction = targetBox.Center - ray.Position;
                var angle = Math.Abs(Math.Acos(ray.Direction.Dot(direction) / (ray.Direction.Length() * direction.Length())));

                //  Log.Info("distance wasnt zero for: " + foundEntity.GetType().ToString());
                //  if ((float)distance < lowestDistance) Log.Info("distance < lowest distance for " + foundEntity.EntityId);
                //  if (foundEntity.GetTopMostParent().GetType().ToString() == "Sandbox.Game.Entities.MyCubeGrid") Log.Info("target is in a cubegrid  " + foundEntity.EntityId);
                //  if ((float)distance > safetyDistance) Log.Info("distance > safety distance for " + foundEntity.EntityId);
                if (((float)angle < lowestDistance) && ((float)posDistance > safetyDistance) && (foundEntity.GetTopMostParent().GetType().ToString() == "Sandbox.Game.Entities.MyCubeGrid"))
                {
                    if (!ignoreSet.Contains(foundEntity))
                    {
                        bestTarget = foundEntity;
                        lowestDistance = (float)angle;
                        //  Log.Info("we found a fitting target: " + foundEntity.DisplayName);
                    }
                }
            }
            return bestTarget;
        }
    }
}

