using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TpacTool.Lib.Util;
using static TpacTool.Lib.AnimationDefinitionData;

namespace TpacTool.Lib.SkeletalAnimGenMethods
{
    /// <summary>
    /// Generates a new BoneAnim that simulates follow-through during motion
    /// </summary>
    public class SkeletalAnimGenTail : SkeletalAnimGenMethodBase
    {
        /// <summary>
		/// The base method to fill out the generated animation frames before simulation is added to them
		/// </summary>
        public SkeletalAnimGenMethodBase BaseMethod;
        public float Mass;
        /// <summary>
        /// Coefficient of drag for linear motion
        /// </summary>
        public float DragCoefficient;
        /// <summary>
        /// Rotation stiffness. 1 being the highest, and 0 the lowest
        /// </summary>
        public float DampenStiffness;
        /// <summary>
        /// Ignore the first frame when simulating the tail motion.
        /// Useful since a lot of animations use the T-pose as the first frame which can lead to wild movement between frame 0 and 1
        /// </summary>
        public bool IgnoreFrameZero;

        public SkeletalAnimGenTail(SkeletalAnimGenMethodBase baseMethod, int ownerBoneIndex, Skeleton ownerSkeleton) : base(ownerBoneIndex, ownerSkeleton)
        {
            BaseMethod = baseMethod;
            Mass = 6f;
            DragCoefficient = 70f;
            DampenStiffness = 0.5f;
        }

        public override GenerateFramesMethod GenerationMethod
        {
            get
            {
                return (int)BaseMethod.GenerationMethod + GenerateFramesMethod.Tail;
            }
        }

        public override BoneAnim GenerateBoneAnim(SkeletalAnimation anim)
        {
            return SimulateTail(anim, BaseMethod.GenerateBoneAnim(anim));
        }
        public override SkeletalAnimGenMethodBase CreateCopy(int newOwnerBoneIndex, Skeleton newOwnerSkeleton, bool keepBoneIndices)
        {
            var copy = new SkeletalAnimGenTail(
                BaseMethod.CreateCopy(newOwnerBoneIndex, newOwnerSkeleton, keepBoneIndices),
                newOwnerBoneIndex, newOwnerSkeleton);
            copy.Mass = Mass;
            copy.DragCoefficient = DragCoefficient;
            copy.DampenStiffness = DampenStiffness;
            copy.IgnoreFrameZero = IgnoreFrameZero;
            return copy;
        }
        private BoneAnim SimulateTail(SkeletalAnimation anim, BoneAnim baseBoneAnim)
        {
            var parentLookUp = OwnerSkeleton.Definition.Data.CreateParentLookup();
            var globalRestFrames = OwnerSkeleton.Definition.Data.CreateBoneMatrices();
            var angularVelocity = Vector3.Zero;
            var previousLinearVelocity = Vector3.Zero;
            var result = new BoneAnim();
            // Some constants, might add the ability to change them in the editor later
            var boneLength = 1f;
            var restoringTorqueMagnitude = 0.5f;
            var inertiaFactor = 2f;
            var previousTime = 0f;
            var previousKey = -1f;
            var skipFrame = IgnoreFrameZero;
            foreach (var rotationFrame in baseBoneAnim.RotationFrames)
            {
                var time = rotationFrame.Value.Time;
                // If ignoring the first frame, don't simulate. Just copy the base anim's rotation
                if (skipFrame)
                {
                    result.RotationFrames.Add(rotationFrame.Key, new AnimationFrame<Quaternion>(time, rotationFrame.Value.Value));
                    skipFrame = false;
                    continue;
                }
                float deltaTime;
                Quaternion previousRotation;
                if (previousKey < 0)
                {
                    previousTime = time;
                    deltaTime = 1f;
                    previousRotation = rotationFrame.Value.Value;
                }
                else
                {
                    deltaTime = time - previousTime;
                    previousRotation = baseBoneAnim.RotationFrames[previousKey].Value;
                }
                var currentHeadPos = SkeletalAnimationGenerationHelper.GetBoneHeadPosition(anim, OwnerSkeleton, OwnerBoneIndex, time, globalRestFrames, parentLookUp);
                var lastHeadPos = SkeletalAnimationGenerationHelper.GetBoneHeadPosition(anim, OwnerSkeleton, OwnerBoneIndex, previousTime, globalRestFrames, parentLookUp);
                var newLinearVelocity = (currentHeadPos - lastHeadPos) / deltaTime;
                // Linear acceleration of the bone's head in global space
                var acceleration = (newLinearVelocity - previousLinearVelocity) / deltaTime;
                // First part is the force due to inertia, second part simulates air-resistance
                var force = (-2 * inertiaFactor * Mass * acceleration) + (-DragCoefficient * newLinearVelocity * newLinearVelocity.Length()) / 2;
                // Get the force vector in local-space
                force = SkeletalAnimationGenerationHelper.GetVectorInLocalSpace(anim, OwnerSkeleton, force, OwnerBoneIndex, time, parentLookUp);
                // Local-space current direction
                var currentDirection = new Vector3(boneLength, 0f, 0f).RotateByQuaternion(previousRotation);
                // The rotation to which the bone wants to return (Base bone anim's rotation)
                var restDirection = Vector3.UnitX.RotateByQuaternion(rotationFrame.Value.Value);
                var restoringTorque = Vector3.Cross(currentDirection, restDirection) * restoringTorqueMagnitude;
                var torque = Vector3.Cross(currentDirection, force) + restoringTorque;
                var angularAcceleration = torque / (Mass * boneLength * boneLength);
                angularVelocity += angularAcceleration * deltaTime;
                if (angularVelocity == Vector3.Zero)
                {
                    result.RotationFrames.Add(rotationFrame.Key, new AnimationFrame<Quaternion>(time, previousRotation));
                }
                else
                {
                    var quat = Quaternion.CreateFromAxisAngle(Vector3.Normalize(angularVelocity), angularVelocity.Length()) * previousRotation;
                    result.RotationFrames.Add(rotationFrame.Key, new AnimationFrame<Quaternion>(time, quat));
                }
                previousKey = rotationFrame.Key;
                previousTime = time;
                previousLinearVelocity = newLinearVelocity;
                angularVelocity *= (1 - DampenStiffness); // dampen the angular velocity
            }
            return result;
        }
    }
}
