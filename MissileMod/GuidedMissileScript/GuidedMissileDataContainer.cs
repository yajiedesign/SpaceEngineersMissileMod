using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace GuidedMissile.GuidedMissileScript
{
    public class GuidedMissileDataContainer
    {
        public IMyEntity Missile { get; private set; }
        public IMyEntity Target { get; set; }
        private long TrackedFrames { get; set; }
        private readonly long _deathTimer = 10000;
        private readonly float _turningSpeed = 0.1f;
        private readonly bool _hasPhysicsSteering = false;

        private bool _isOvershooting = false;
        private float _overshootDistance = 0;

        private bool _finishedOvershooting = false;
        private readonly Action<IMyEntity> _onExplode = delegate (IMyEntity entity) { Log.Info("An empty onexplode was called"); };

        public GuidedMissileDataContainer(IMyEntity missile, IMyEntity target, long safetyTimer, long deathTimer, float turningSpeed, Action<IMyEntity> onExplode, bool hasPhysicsSteering)
            : this(missile, target, safetyTimer, deathTimer, turningSpeed)
        {
            if (onExplode != null) _onExplode = onExplode;
            _hasPhysicsSteering = hasPhysicsSteering;
        }

        private GuidedMissileDataContainer(IMyEntity missile, IMyEntity target, long safetyTimer, long deathTimer, float turningSpeed)
        {
            Missile = missile;
            Target = target;
            TrackedFrames = -safetyTimer;
            _deathTimer = deathTimer;
            _turningSpeed = turningSpeed;

        }
        public void SetOvershootDistance(float distance) { _overshootDistance = distance; }
        public void ClearOvershootDistance() { _overshootDistance = 0f; }
        public void StartOverShooting() { _isOvershooting = true; }
        public void StopOverShooting() { _isOvershooting = false; }
        public GuidedMissileDataContainer(IMyEntity missile, IMyEntity target, long safetyTimer, long deathTimer)
        {
            Missile = missile;
            Target = target;
            TrackedFrames = -safetyTimer;
            _deathTimer = deathTimer;
        }

        public GuidedMissileDataContainer(IMyEntity missile, IMyEntity target, long safetyTimer)
        {
            Missile = missile;
            Target = target;
            TrackedFrames = -safetyTimer;
        }
        public GuidedMissileDataContainer(IMyEntity missile, IMyEntity target)
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
            if (Target == null) return true;
            if (Missile.MarkedForClose)
            {
                _onExplode(Missile);
                return true;
            }
            if (TrackedFrames > _deathTimer)
            {
                Missile.Close();
                return true;
            }
            if (Target.MarkedForClose)
            {
                // Log.Info("Target is marked for close, searching new one");
                if (Target.GetTopMostParent().MarkedForClose) return true;
                IMyEntity newTarget = GuidedMissileTargetGridHook.GetRandomBlockInGrid(Target);
                if (newTarget == null) return true;
                Target = newTarget;
                _isOvershooting = false;
                _overshootDistance = 0f;
                _finishedOvershooting = false;
                return false;
                //  return true;

            }
            return false;
        }

        public void Update(List<IMyEntity> missileCollection)
        {
            if (SafetyTimerIsOver())
            {
                ModDebugger.Launch();
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
                    if (_hasPhysicsSteering)
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

                                if ((_isOvershooting == false) && (_finishedOvershooting == false))
                                {
                                    _overshootDistance = Missile.Physics.LinearVelocity.Length() * (360 / _turningSpeed) / 10000 + 0;
                                    _isOvershooting = true;
                                }
                                //  targetPoint = negativeVelocityRay.Position + (float)intersectionDist * negativeVelocityRay.Direction;

                                if (_isOvershooting && !_finishedOvershooting)
                                {
                                    if (intersectionDist > _overshootDistance)
                                    {
                                        targetPoint = boundingBoxCenter;
                                        _finishedOvershooting = true;
                                        // Log.Info("we finished overshooting, returning to target.");
                                    }
                                    else
                                    {
                                        targetPoint = Missile.GetPosition() + Missile.WorldMatrix.Forward;
                                        //   Log.Info("we overshot, we keep heading forward.");
                                        //   Log.Info("         current Distance: " + intersectionDist + " and overshootCorr " + _overshootDistance);

                                    }
                                }
                                else if (_isOvershooting && _finishedOvershooting)
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



                float maxRadVelocity = MathHelper.ToRadians(_turningSpeed);
                float angle = MyUtils.GetAngleBetweenVectorsAndNormalise(Missile.WorldMatrix.Forward,
                    targetDirection);



                // Log.Info("angle = " + MathHelper.ToDegrees(angle));
                float turnPercent;

                if (Math.Abs(angle) < Double.Epsilon)
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


                Matrix targetMatrix = Matrix.CreateWorld(Missile.GetPosition(), targetDirection,
                    Vector3D.CalculatePerpendicularVector(targetDirection));
                //Matrix.CreateFromYawPitchRoll(0f, (float)Math.PI*0.5f, 0f)));

                var slerpMatrix = Matrix.Slerp(Missile.WorldMatrix, targetMatrix, turnPercent);

                RayD nextTargetLineD = new RayD(Missile.GetPosition(), slerpMatrix.Forward);

                bool iswait = false;
                foreach (var othermissile in missileCollection)
                {
                    if (othermissile.EntityId == Missile.EntityId)
                    {
                        continue;
                    }
                    if (nextTargetLineD.Intersects(othermissile.WorldVolume) != null)
                    {

                        iswait = true;
                    }
                }

                if (iswait == false)
                {


                    Missile.SetWorldMatrix(slerpMatrix);

                    if (_hasPhysicsSteering)
                    {

                        var linVel = Missile.Physics.LinearVelocity;
                        var linSpeed = linVel.Length();
                        Missile.Physics.LinearVelocity = 0.98f * linVel + 0.02f * Missile.WorldMatrix.Forward * linSpeed;

                    }
                    else
                    {
                        Vector3 linVel = Missile.Physics.LinearVelocity;
                        Missile.Physics.LinearVelocity = Vector3.Normalize(Missile.WorldMatrix.Forward) * linVel.Length();
                    }


                }
            }
            else {
                //     if (Missile.Physics.Flags == RigidBodyFlag.RBF_BULLET) Missile.Physics.Flags = RigidBodyFlag.RBF_BULLET & RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE;
            }
            Tick();
        }

        private void Tick()
        {
            TrackedFrames++;
        }

        private bool SafetyTimerIsOver()
        {
            return (TrackedFrames > 0);
        }
    }
}