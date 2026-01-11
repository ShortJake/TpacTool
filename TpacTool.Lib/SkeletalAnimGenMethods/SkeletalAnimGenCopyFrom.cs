using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static TpacTool.Lib.AnimationDefinitionData;

namespace TpacTool.Lib.SkeletalAnimGenMethods
{
    /// <summary>
    /// Generates a new BoneAnim copying the bone's orientation from another animation
    /// </summary>
    public class SkeletalAnimGenCopyFrom : SkeletalAnimGenMethodBase
    {
        /// <summary>
        /// Animation from which this bone's orientation is copied
		/// </summary>
		public SkeletalAnimation CopiedAnim;
        /// <summary>
        /// The index of the bone from which the orientation is copied
        /// </summary>
        public int CopiedBoneIndex;
        /// <summary>
        /// Copy the target bone's animation with a offset in time
        /// </summary>
        public float TimeOffset;
        /// <summary>
        /// If true, adds the copied orientation to the base orientation (specified by <see cref="BaseTargetAnim"/>). Otherwise uses the copied orientation as is
        /// </summary>
        public bool AddFromBase;
        /// <summary>
        /// Don't overwrite the first keyframe of the generated animation, and use the base animation (See <see cref="BaseTargetAnim">). Useful since a lot of animations start with the T-pose
        /// </summary>
        public bool IgnoreThisZeroFrame;
        /// <summary>
        /// Don't copy from first keyframe of the copied animation. Useful since a lot of animations start with the T-pose
        /// </summary>
        public bool IgnoreSourceZeroFrame;
        /// <summary>
		/// Animation to use as the base for the generated rotation, to which the copied rotation may be added. If null, uses the bone's rest matrix frame 
		/// </summary>
		public SkeletalAnimation BaseTargetAnim;
        /// <summary>
		/// Animation keyframe to copy for the base rotation 
		/// </summary>
		public int BaseTargetFrame;
        /// <summary>
		/// Target bone index to copy for the generated static rotation
		/// </summary>
		public int BaseTargetIndex;
        /// <summary>
        /// Stretch the copied animation to fit the generated animation's duration. Otherwise, the copied animation is looped
        /// </summary>
        public bool Stretch;

        public SkeletalAnimGenCopyFrom(int ownerBoneIndex, Skeleton ownerSkeleton) : base(ownerBoneIndex, ownerSkeleton)
        {
            BaseTargetIndex = ownerBoneIndex;
            CopiedBoneIndex = ownerBoneIndex;
        }

        public override GenerateFramesMethod GenerationMethod
        {
            get
            {
                if (Stretch) return GenerateFramesMethod.CopyFromStretch;
                else return GenerateFramesMethod.CopyFromLoop;
            }
        }
        public override BoneAnim GenerateBoneAnim(SkeletalAnimation anim)
        {
            // A new frame is added for every frame the pelvis bone (Bone 0) has
            var boneAnim = new BoneAnim();
            // The base quat is either the rest frame quat, or copied from another animation
            var baseQuat = Quaternion.Identity;
            var targetAnim = BaseTargetAnim;
            if (targetAnim == null) baseQuat = Quaternion.CreateFromRotationMatrix(OwnerSkeleton.Definition.Data.Bones[OwnerBoneIndex].RestFrame);
            else
            {
                baseQuat = targetAnim.Definition.Data.BoneAnims[BaseTargetIndex].RotationFrames[BaseTargetFrame].Value;
            }
            var ignoreThisOffset = IgnoreThisZeroFrame ? 1f : 0f;
            var ignoreSourceOffset = IgnoreSourceZeroFrame ? 1f : 0f;
            var duration = anim.Duration - ignoreThisOffset;
            var sourceDuration = CopiedAnim.Duration - ignoreSourceOffset;
            var skipFrame = IgnoreThisZeroFrame;
            float stretchFactor;
            if (Stretch) stretchFactor = sourceDuration / duration;
            else 
            {
                // If looping, then stretch the copied animation just enough to fit a whole number of loops
                stretchFactor = (float)(sourceDuration * Math.Max(Math.Round(duration / sourceDuration), 1.0) / duration);
            }
            foreach (var rotationFrame in anim.Definition.Data.BoneAnims[0].RotationFrames)
            {
                // If skipping the first frame of the generated, just use the base quat and don't copy
                if (skipFrame)
                {
                    boneAnim.RotationFrames.Add(rotationFrame.Key, new AnimationFrame<Quaternion>(rotationFrame.Value.Time, baseQuat));
                    skipFrame = false;
                    continue;
                }
                // If ignoring the generated animation's first frame, then copy from the target's previous frame (2nd generated frame copies from the 1st, 1st from 2nd, etc...)
                var copiedRotationTime = (rotationFrame.Value.Time + TimeOffset - ignoreThisOffset) * stretchFactor;
                if (!Stretch)
                {
                    var currentLoop = (int)(copiedRotationTime / sourceDuration);
                    copiedRotationTime -= sourceDuration * currentLoop;
                }
                // If ignoring the target's first frame, make sure to use copy one frame ahead (0th generated frame copies the 1st, 1st the 2nd, etc...)
                copiedRotationTime += ignoreSourceOffset;

                var copiedQuat = CopiedAnim.Definition.Data.BoneAnims[CopiedBoneIndex].GetInterpolatedRotation(copiedRotationTime, out _, out _);
                if (AddFromBase)
                {
                    // Get the rotation of the copied bone relative to its resting frame
                    copiedQuat = Quaternion.Conjugate(Quaternion.CreateFromRotationMatrix(OwnerSkeleton.Definition.Data.Bones[CopiedBoneIndex].RestFrame)) * copiedQuat;
                    // Apply the relative rotation to the base rotation of this bone
                    var boneQuat = copiedQuat * baseQuat;
                    boneAnim.RotationFrames.Add(rotationFrame.Key, new AnimationFrame<Quaternion>(rotationFrame.Value.Time, boneQuat));
                }
                else boneAnim.RotationFrames.Add(rotationFrame.Key, new AnimationFrame<Quaternion>(rotationFrame.Value.Time, copiedQuat));
            }
            return boneAnim;
        }
        public override SkeletalAnimGenMethodBase CreateCopy(int newOwnerBoneIndex, Skeleton newOwnerSkeleton, bool keepBoneIndices)
        {
            var copy = new SkeletalAnimGenCopyFrom(newOwnerBoneIndex, newOwnerSkeleton);
            copy.BaseTargetAnim = BaseTargetAnim;
            copy.BaseTargetFrame = BaseTargetFrame;
            if (!keepBoneIndices) copy.BaseTargetIndex = BaseTargetIndex;
            copy.CopiedAnim = CopiedAnim;
            if (!keepBoneIndices) copy.CopiedBoneIndex = CopiedBoneIndex;
            copy.IgnoreSourceZeroFrame = IgnoreSourceZeroFrame;
            copy.IgnoreThisZeroFrame = IgnoreThisZeroFrame;
            copy.Stretch = Stretch;
            copy.TimeOffset = TimeOffset;
            copy.AddFromBase = AddFromBase;
            return copy;
        }
    }
}
