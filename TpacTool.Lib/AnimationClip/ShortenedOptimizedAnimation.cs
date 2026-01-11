using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace TpacTool.Lib
{
	public class ShortenedOptimizedAnimation : OptimizedAnimation
	{
		public new static readonly Guid TYPE_GUID = Guid.Parse("8fc3fc2a-4bc9-46c5-8525-f33a6e257b72");

		public ShortenedOptimizedAnimation()
		{
			this.TypeGuid = TYPE_GUID;
		}

		public override void ReadData(BinaryReader stream, IDictionary<object, object> userdata, int totalSize)
		{
            var cur = stream.BaseStream.Position;
            UnknownInt1 = stream.ReadInt32(); // always 1
            UnknownInt2 = stream.ReadInt32(); // always 1
            FrameCount = stream.ReadInt32();
            Skeleton = stream.ReadSizedString();
            var frameNum2 = stream.ReadInt32();
            if (frameNum2 != FrameCount)
            {
                throw new Exception($"Frames not equal: {FrameCount} - {frameNum2}");
            }

            var boneNum = stream.ReadInt32();
            BoneAnims.Clear();
            BoneAnims.Capacity = boneNum;
            for (int i = 0; i < boneNum; i++)
            {
                var boneAnim = new OptimizedBoneAnim();

                boneAnim.UnknownInt1 = stream.ReadInt32();
                var boneKeyframeNum = stream.ReadInt32();

                var boneTimes = new float[boneKeyframeNum];
                for (int j = 0; j < boneKeyframeNum; j++)
                {
                    boneTimes[j] = stream.ReadSingle();
                }

                var quats = new Quaternion[boneKeyframeNum];
                for (int j = 0; j < boneKeyframeNum; j++)
                {
                    quats[j] = stream.ReadQuat();
                }

                boneAnim.RotationFrames.Capacity = boneKeyframeNum;
                for (int j = 0; j < boneKeyframeNum; j++)
                {
                    boneAnim.RotationFrames.Add(boneTimes[j], new AnimationFrame<Quaternion>(
                        boneTimes[j], quats[j]));
                }

                boneAnim.UnknownInt2 = stream.ReadInt32();
                boneAnim.UnknownInt3 = stream.ReadInt32();
                BoneAnims.Add(boneAnim);
            }

            int rootKeyframeNum = stream.ReadInt32();
            var rootTimes = new float[rootKeyframeNum];
            for (int i = 0; i < rootKeyframeNum; i++)
            {
                rootTimes[i] = stream.ReadSingle();
            }
            var positions = new Vector4[rootKeyframeNum];
            for (int i = 0; i < rootKeyframeNum; i++)
            {
                positions[i] = stream.ReadVec4();
            }

            RootPositionFrames.Clear();
            RootPositionFrames.Capacity = rootKeyframeNum;
            for (int i = 0; i < rootKeyframeNum; i++)
            {
                RootPositionFrames.Add(rootTimes[i], new AnimationFrame<Vector4>(
                    rootTimes[i], positions[i]));
            }

            UnknownInt3 = stream.ReadInt32(); // always 0
            UnknownByte = stream.ReadByte(); // always 1
            byte boneNum2 = stream.ReadByte();
            int activityFrameNum = stream.ReadInt32();
            UnknownInt4 = stream.ReadInt32(); // always 0
            BoneActivities.FrameCount = activityFrameNum;
            BoneActivities.BoneCount = boneNum2;

            byte[] lastBytes = null;
            for (int i = 0; i < activityFrameNum; i++)
            {
                var bytes = stream.ReadBytes(boneNum2);

                if (lastBytes == null)
                {
                    BoneActivities.SetBoneActivityRange(i,
                        bytes.Select(b => b != 0).ToArray());
                }
                else
                {
                    BoneActivities.SetBoneActivityRange(i,
                        bytes.Select((b, index) => b > lastBytes[index]).ToArray());
                }

                lastBytes = bytes;
            }

            UnknownOffset = stream.ReadInt32(); // always equals length - boneNum2 * 17
            var length = stream.BaseStream.Position - cur;
            int delta = (int)length - UnknownOffset;

            // not finished yet
        }
    }
}