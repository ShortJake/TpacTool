using System;
using System.IO;
using System.Numerics;
using JetBrains.Annotations;
using TpacTool.Lib.SkeletalAnimGenMethods;

namespace TpacTool.Lib
{
	public class Skeleton : AssetItem
	{
		public static readonly Guid TYPE_GUID = Guid.Parse("c635a3d5-eabb-45dd-883e-aa57e4196113");

		// ignore?
		public bool UnknownBool { set; get; }

		public Guid GeometryGuid { set; get; }

		[CanBeNull]
		public ExternalLoader<SkeletonDefinitionData> Definition { set; get; }

		[CanBeNull]
		public ExternalLoader<SkeletonUserData> UserData { set; get; }
        // Metadata for generating animations. This isn't saved and doesn't affect the tpac file
        //public AnimGenBoneData[] AnimGenData;
        public SkeletalAnimGenMethodBase[] AnimGenData;

        public Skeleton() : base(TYPE_GUID)
		{
			AnimGenData = new SkeletalAnimGenMethodBase[64];
			for (int i = 0; i < AnimGenData.Length; i++)
			{
				AnimGenData[i] = new SkeletalAnimGenStatic(i, this);
			}
        }

		public override void ReadMetadata(BinaryReader stream, int totalSize)
		{
			var version = stream.ReadUInt32();
			UnknownBool = stream.ReadBoolean();
			GeometryGuid = stream.ReadGuid();
		}

		public override void WriteMetadata(BinaryWriter stream)
		{
			stream.Write((int) 0);
			stream.Write(UnknownBool);
			stream.Write(GeometryGuid);
		}

		public override AssetItem Clone()
		{
			var clone = new Skeleton()
			{
				Version = this.Version,

				UnknownBool = this.UnknownBool,
				GeometryGuid = this.GeometryGuid
			};

			if (Definition?.Data != null)
			{
				clone.Definition =
					new ExternalLoader<SkeletonDefinitionData>((SkeletonDefinitionData) Definition.Data.Clone());
			}
			if (UserData != null)
			{
				clone.UserData = new ExternalLoader<SkeletonUserData>((SkeletonUserData)UserData.Data.Clone());
				clone.UserData.OwnerGuid = Guid;
			}
			clone.SetDataSegment(null, clone.Definition);
			clone.SetDataSegment(null, clone.UserData);
			clone.CloneDo(this);
			return clone;
		}

		public override void ConsumeDataSegments(AbstractExternalLoader[] externalData)
		{
			foreach (var externalLoader in externalData)
			{
				var defin = externalLoader as ExternalLoader<SkeletonDefinitionData>;
				if (defin != null)
				{
					Definition = defin;
				}

				var user = externalLoader as ExternalLoader<SkeletonUserData>;
				if (user != null)
				{
					UserData = user;
				}
			}

			base.ConsumeDataSegments(externalData);
		}
	}
}