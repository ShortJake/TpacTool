using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using TpacTool.Lib;
using TpacTool.Lib.SkeletalAnimGenMethods;

namespace TpacTool
{
    public abstract class SkeletalAnimGenBaseVM : ViewModelBase
    {
        public abstract SkeletalAnimGenMethodBase SelectedData {  get; set; }
        public void Refresh(Skeleton ownerSkeleton, int ownerBoneIndex)
        {
            // Not using ownerBoneIndex >= ownerSkeleton.AnimGenData.Count because the size is fixed at 64 and
            // doens't represent the actual number of bones
            if (ownerSkeleton == null || ownerBoneIndex >= ownerSkeleton.Definition.Data.Bones.Count) SelectedData = null;
            else SelectedData = ownerSkeleton.AnimGenData[ownerBoneIndex];
        }
        public SkeletalAnimGenBaseVM()
		{
		}
    }
}