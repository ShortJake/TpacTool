using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static TpacTool.Lib.AnimationDefinitionData;

namespace TpacTool.Lib.SkeletalAnimGenMethods
{

    /// <summary>
    /// Generates a new static BoneAnim using the bone's rest frame or using a single frame copied from another animation
    /// </summary>
    public class SkeletalAnimGenStatic : SkeletalAnimGenMethodBase
    {
        /// <summary>
		/// Animation to use for the generated static rotation. If null, uses the bone's rest matrix frame 
		/// </summary>
		public SkeletalAnimation BaseTargetAnim;
        /// <summary>
		/// Animation keyframe to copy for the generated static rotation
		/// </summary>
		public int BaseTargetFrame;
        /// <summary>
		/// Target bone index to copy for the generated static rotation
		/// </summary>
		public int BaseTargetIndex;

        public SkeletalAnimGenStatic(int ownerBoneIndex, Skeleton ownerSkeleton) : base(ownerBoneIndex, ownerSkeleton)
        {
            BaseTargetIndex = ownerBoneIndex;
        }

        public override GenerateFramesMethod GenerationMethod => GenerateFramesMethod.Static;
        public override BoneAnim GenerateBoneAnim(SkeletalAnimation anim)
        {
            // A new frame is added for every frame the pelvis bone (Bone 0) has
            var boneAnim = new BoneAnim();

            var baseQuat = Quaternion.Identity;
            var targetAnim = BaseTargetAnim;
            if (targetAnim == null) baseQuat = Quaternion.CreateFromRotationMatrix(OwnerSkeleton.Definition.Data.Bones[OwnerBoneIndex].RestFrame);
            else
            {
                baseQuat = targetAnim.Definition.Data.BoneAnims[BaseTargetIndex].RotationFrames[BaseTargetFrame].Value;
            }
            foreach (var rotationFrame in anim.Definition.Data.BoneAnims[0].RotationFrames)
            {
                boneAnim.RotationFrames.Add(rotationFrame.Key, new AnimationFrame<Quaternion>(rotationFrame.Value.Time, baseQuat));
            }
            return boneAnim;
        }

        public override SkeletalAnimGenMethodBase CreateCopy(int newOwnerBoneIndex, Skeleton newOwnerSkeleton, bool keepBoneIndices)
        {
            var copy = new SkeletalAnimGenStatic(newOwnerBoneIndex, newOwnerSkeleton);
            copy.BaseTargetAnim = BaseTargetAnim;
            copy.BaseTargetFrame = BaseTargetFrame;
            if (!keepBoneIndices) copy.BaseTargetIndex = BaseTargetIndex;
            return copy;
        }
    }
}
