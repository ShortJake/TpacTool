using System;
using System.Collections.Generic;
using System.Text;
using static TpacTool.Lib.AnimationDefinitionData;

namespace TpacTool.Lib.SkeletalAnimGenMethods
{
    public abstract class SkeletalAnimGenMethodBase
    {
        public int OwnerBoneIndex;
        public Skeleton OwnerSkeleton;
        public abstract GenerateFramesMethod GenerationMethod { get; }
        public SkeletalAnimGenMethodBase(int ownerBoneIndex, Skeleton ownerSkeleton)
        {
            OwnerBoneIndex = ownerBoneIndex;
            OwnerSkeleton = ownerSkeleton;
        }

        public abstract BoneAnim GenerateBoneAnim(SkeletalAnimation anim);
        public abstract SkeletalAnimGenMethodBase CreateCopy(int newOwnerBoneIndex, Skeleton newOwnerSkeleton, bool keepBoneIndices);
    }

    public enum GenerateFramesMethod
    {
        Static,
        CopyFromStretch,
        CopyFromLoop,
        Mirror,
        Tail,
        TailAndCopyFromStretch,
        TailAndCopyFromLoop,
        TailAndMirror,
    }
}
