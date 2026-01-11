using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static TpacTool.Lib.AnimationDefinitionData;

namespace TpacTool.Lib.SkeletalAnimGenMethods
{
    /// <summary>
    /// Generates a new BoneAnim copying the bone's orientation from another bone in the same animation
    /// </summary>
    public class SkeletalAnimGenMirror : SkeletalAnimGenCopyFrom
    {
        public SkeletalAnimGenMirror(int ownerBoneIndex, Skeleton ownerSkeleton) : base(ownerBoneIndex, ownerSkeleton)
        {
            CopiedBoneIndex = 0;
            Stretch = true;
        }

        public override GenerateFramesMethod GenerationMethod => GenerateFramesMethod.Mirror;
        public override BoneAnim GenerateBoneAnim(SkeletalAnimation anim)
        {
            CopiedAnim = anim;
            return base.GenerateBoneAnim(anim);
        }
    }
}
