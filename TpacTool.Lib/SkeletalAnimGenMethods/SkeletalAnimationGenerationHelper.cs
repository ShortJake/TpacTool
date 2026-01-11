using System;
using System.Numerics;
using TpacTool.Lib.Util;

namespace TpacTool.Lib
{
    public static class SkeletalAnimationGenerationHelper
    {
        /// <summary>
        /// Transforms a quaternion from the local space of a bone in a SkeletalAnimation at a particular time to global space
        /// </summary>
        /// <param name="parentLookup">Dictionary that holds each bone's parent bone index</param>
        public static Quaternion GetQuaternionInWorldSpace(SkeletalAnimation anim, Quaternion quat, int boneIndex, float time, int[] parentLookup)
        {
            var parent = parentLookup[boneIndex];
            var boneAnims = anim.Definition.Data.BoneAnims;
            // Apply the rotation from every parent bone to the given quaternion
            // Remember, quat multiplication applies the right quat first
            while (parent >= 0)
            {
                if (boneAnims.Count > parent)
                    quat = boneAnims[parent].GetInterpolatedRotation(time, out _, out _) * quat;
                parent = parentLookup[parent];
            }
            return quat;
        }
        /// <summary>
        /// Transforms a vector from global space to the local space of a bone in a SkeletalAnimation at a particular time
        /// </summary>
        /// <param name="parentLookup">Dictionary that holds each bone's parent bone index</param>
        public static Vector3 GetVectorInLocalSpace(SkeletalAnimation anim, Skeleton skeleton, Vector3 v, int boneIndex, float time, int[] parentLookup)
        {
            // Get the global-space quaternion that describes this bone's orientation
            // Then reverse it and apply it to the given vector to transform to local space
            var transformationQuat = GetQuaternionInWorldSpace(anim, Quaternion.Identity, boneIndex, time, parentLookup);
            return v.RotateByQuaternion(Quaternion.Conjugate(transformationQuat));
        }

        /// <summary>
        /// Gets the global-space position of a bone's head in a specific point in an animation
        /// </summary>
        /// <param name="absoluteRestFrames">Dictionary that holds each bone's rest matrix frame in global-space. <see cref="SkeletonDefinitionData.CreateBoneMatrices"/> </param>
        /// <param name="parentLookup">Dictionary that holds each bone's parent bone index</param>
        public static Vector3 GetBoneHeadPosition(SkeletalAnimation anim, Skeleton skeleton, int boneIndex, float time, Matrix4x4[] absoluteRestFrames, int[] parentLookup)
        {
            // Combine the animation's position animation and the root bone's position animation to get the root position
            var rootRestPos = skeleton.Definition.Data.Bones[0].RestFrame.Translation;
            var vec4 = anim.Definition.Data.GetInterpolatedPosition(time, out var _, out _);
            var rootPos = rootRestPos + new Vector3(vec4.X, vec4.Y, vec4.Z);
            if (boneIndex == 0)
            {
                return rootPos;
            }
            var boneAnims = anim.Definition.Data.BoneAnims;
            var parent = parentLookup[boneIndex];
            // Get the global position of the parent bone's head
            var headPos = GetBoneHeadPosition(anim, skeleton, parent, time, absoluteRestFrames, parentLookup);
            // Position of this bone's head in the rest pose relative to the parent bone
            var offsetFromParent = absoluteRestFrames[boneIndex].Translation - absoluteRestFrames[parent].Translation;
            // Quaternion that undoes the rotation of the parent bone in global-space IN THE REST POSE
            // This let's us remove the rotation from offsetFromParent
            var transformationQuat = Quaternion.Conjugate(Quaternion.CreateFromRotationMatrix(absoluteRestFrames[parent]));
            // Apply the cumulative rotation of the parents' IN THE ANIMATION POSE
            transformationQuat = GetQuaternionInWorldSpace(anim, transformationQuat, boneIndex, time, parentLookup);
            // Add the rotated offset to the parent's head position
            headPos += offsetFromParent.RotateByQuaternion(transformationQuat);
            return headPos;
        }
    }
}
