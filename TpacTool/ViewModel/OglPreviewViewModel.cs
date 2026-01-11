using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using OpenTK;
using OpenTK.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using TpacTool.Lib;
using TpacTool.Properties;

namespace TpacTool
{
	public sealed class OglPreviewViewModel : ViewModelBase
	{
		public static readonly Guid PreviewAssetEvent = Guid.NewGuid();

		public static readonly Guid PreviewTextureEvent = Guid.NewGuid();

		public static readonly Guid PreviewSkeletonEvent = Guid.NewGuid();

		public static readonly Guid PreviewAnimationEvent = Guid.NewGuid();

		public const float MAX_GRID_LENGTH = 256;

		public const int CHANNEL_MODE_RGBA = 1;

		public const int CHANNEL_MODE_RGB = 2;

		public const int CHANNEL_MODE_RG = 3;

		public const int CHANNEL_MODE_R = 4;

		public const int CHANNEL_MODE_G = 5;

		public const int CHANNEL_MODE_B = 6;

		public const int CHANNEL_MODE_ALPHA = 7;

		internal static string LIGHTMODE_SUN = Resources.Preview_Lights_Single;
		internal static string LIGHTMODE_THREEPOINTS = Resources.Preview_Lights_Tri;
		internal static string LIGHTMODE_DEFAULT = Resources.Preview_Lights_Quad;

		internal static string CENTERMODE_ORIGIN = Resources.Preview_Center_Origin;
		//internal static string CENTERMODE_BOUNDINGBOX = "Center of Bounding Box";
		internal static string CENTERMODE_MASS = Resources.Preview_Center_Mass;
		internal static string CENTERMODE_CENTER = Resources.Preview_Center_Geometry;

		internal static string KEEPSCALEMODE_OFF = "Off";
		internal static string KEEPSCALEMODE_ON = "On";
		internal static string KEEPSCALEMODE_ON_INERTIAL = "On, inertial";

		internal static string[] _lightModeItems = new[]
		{
			LIGHTMODE_SUN,
			LIGHTMODE_THREEPOINTS,
			LIGHTMODE_DEFAULT
		};

		internal static string[] _centerModeItems = new[]
		{
			CENTERMODE_ORIGIN,
			//CENTERMODE_BOUNDINGBOX,
			CENTERMODE_MASS,
			CENTERMODE_CENTER
		};

		internal static string[] _keepScaleModeItems = new[]
		{
			KEEPSCALEMODE_OFF,
			KEEPSCALEMODE_ON,
			//KEEPSCALEMODE_ON_INERTIAL
		};

		private static Mesh[] emptyMeshes = new Mesh[0];

		private int _lightMode = Settings.Default.PreviewLight;

		private int _centerMode = Settings.Default.PreviewCenter;

		private bool _keepScaleMode = Settings.Default.PreviewKeepScale;

		private bool _enableInertia = Settings.Default.PreviewInertia;

		private bool _showGridLines = Settings.Default.PreviewShowGrid;

		private bool _enableTransitionInertia = Settings.Default.PreviewTransitionInertia;

		private bool _enableScaleInertia = Settings.Default.PreviewScaleInertia;

		private float _currentTime;

		public string[] LightModeItems => _lightModeItems;

		public string[] CenterModeItems => _centerModeItems;

		public string[] KeepScaleModeItems => _keepScaleModeItems;

		public BoundingBox ModelBoundingBox { get; set; } = new BoundingBox();

		public Vector3 CenterOfMass { get; set; } = new Vector3();

		public Vector3 CenterOfGeometry { get; set; } = new Vector3();

		public Vector3 CameraTarget { set; get; } = new Vector3(0, 0, 0);

		public double ReferenceScale { set; get; } = 1;

		public OglPreviewPage.Mode PreviewTarget { private set; get; } = OglPreviewPage.Mode.Model;

		public bool IsModelMode => PreviewTarget == OglPreviewPage.Mode.Model;

		public bool IsImageMode => PreviewTarget == OglPreviewPage.Mode.Image;

		public bool IsSkeletonMode => PreviewTarget == OglPreviewPage.Mode.Skeleton;
		public bool IsAnimationMode => PreviewTarget == OglPreviewPage.Mode.Animation;
		public int LightMode
		{
			set
			{
				_lightMode = value;
				RaisePropertyChanged(nameof(LightMode));
				Settings.Default.PreviewLight = _lightMode;
			}
			get => _lightMode;
		}

		public int CenterMode
		{
			set
			{
				_centerMode = value;
				RefocusCenter();
				Settings.Default.PreviewCenter = _centerMode;
			}
			get => _centerMode;
		}

		public bool KeepScaleMode
		{
			set
			{
				_keepScaleMode = value;
				RaisePropertyChanged(nameof(KeepScaleMode));
				Settings.Default.PreviewKeepScale = _keepScaleMode;
			}
			get => _keepScaleMode;
		}

		public bool EnableInertia
		{
			set
			{
				_enableInertia = value;
				RaisePropertyChanged(nameof(EnableInertia));
				Settings.Default.PreviewInertia = _enableInertia;
			}
			get => _enableInertia;
		}

		public bool EnableTransitionInertia
		{
			set
			{
				_enableTransitionInertia = value;
				RaisePropertyChanged(nameof(EnableTransitionInertia));
				Settings.Default.PreviewTransitionInertia = _enableTransitionInertia;
			}
			get => _enableTransitionInertia;
		}

		public bool EnableScaleInertia
		{
			set
			{
				_enableScaleInertia = value;
				RaisePropertyChanged(nameof(EnableScaleInertia));
				Settings.Default.PreviewScaleInertia = _enableScaleInertia;
			}
			get => _enableScaleInertia;
		}

		public bool ShowGridLines
		{
			set
			{
				_showGridLines = value;
				RaisePropertyChanged(nameof(ShowGridLines));
				Settings.Default.PreviewShowGrid = _showGridLines;
			}
			get => _showGridLines;
		}

		public bool ClearOnNextTick { set; get; }

		public int GridLineX { set; get; }

		public int GridLineY { set; get; }

		#region Model

		public Mesh[] Meshes { private set; get; } = emptyMeshes;

		#endregion

		#region Texture & Material

		public string ImageText { private set; get; }

		public Texture Texture { set; get; }

		public int MaxTextureSize { set; get; }

		public Matrix4 ImageColorMask { set; get; } = Matrix4.Identity;

		public bool EnableAlpha { set; get; } = false;

		#endregion

		#region Skeleton & Animation
		public Skeleton Skeleton { private set; get; }
		public SkeletalAnimation Animation { private set; get; }
		public AnimationClip AnimationClip { private set; get; }
		public int FPS { set; get; }
		public float CurrentTime
		{
			set
			{
				if (value != _currentTime)
				{
					_currentTime = value;
					RaisePropertyChanged(nameof(CurrentTime));
				}
			}
			get
			{
				return _currentTime;
			}
		}
		public int AnimationStart
		{
			get
			{
				if (Animation == null || AnimationClip == null || AnimationClip.Source1 > Animation.Duration) return 0;
				else return (int)AnimationClip.Source1;
			}
		}
        public int AnimationEnd
        {
            get
            {
				if (Animation == null) return 0;
                if (AnimationClip == null) return Animation.Duration;
                else return (int)AnimationClip.Source2;
            }
        }
        public int AnimationDuration
        {
            get
            {
				return (int)Math.Abs(AnimationEnd - AnimationStart);
            }
        }
        public ICommand PlayAnimationCommand { private set; get; }
		public bool PlayingAnimation { private set; get; }
        #endregion
        public OglPreviewViewModel()
		{
			if (!IsInDesignMode)
			{
				MessengerInstance.Register<List<Mesh>>(this, PreviewAssetEvent, OnPreviewModel);
				MessengerInstance.Register<Texture>(this, PreviewTextureEvent, SetRenderTexture);
                MessengerInstance.Register<Skeleton>(this, PreviewSkeletonEvent, OnPreviewSkeleton);
                MessengerInstance.Register<(Skeleton, SkeletalAnimation, int, AnimationClip)>(this, PreviewAnimationEvent, OnPreviewAnimation);
				MessengerInstance.Register<object>(this, MainViewModel.CleanupEvent, OnCleanup);
				PlayAnimationCommand = new RelayCommand(() =>
                {
                    PlayingAnimation = !PlayingAnimation;
					RaisePropertyChanged(nameof(PlayingAnimation));
                });
            }
		}

		private void OnCleanup(object unused = null)
		{
			ClearOnNextTick = true;
			ImageText = string.Empty;
			Texture = null;
			RaisePropertyChanged(nameof(ImageText));
			RaisePropertyChanged(nameof(Texture));
		}

		private void OnPreviewModel(List<Mesh> meshes)
		{
			// TODO: a better fix. use edit data rather than vertex stream
			try
			{
				SetRenderMeshes(meshes.ToArray());
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				SetRenderMeshes();
			}
		}

		public void SetRenderMeshes(params Mesh[] meshes)
		{
			SetPreviewTarget(OglPreviewPage.Mode.Model);
			Meshes = meshes;

			bool firstMesh = true;
			BoundingBox bb = new BoundingBox();

			foreach (var mesh in meshes)
			{
				double comX = 0d, comY = 0d, comZ = 0d;
				if (mesh.VertexStream != null)
				{
					foreach (var position in mesh.VertexStream.Data.Positions)
					{
						comX += position.X;
						comY += position.Y;
						comZ += position.Z;
					}
					var vertexCount = mesh.VertexStream.Data.Positions.Length;
					if (vertexCount > 0)
					{
						comX /= vertexCount;
						comY /= vertexCount;
						comZ /= vertexCount;
					}
				}
				else if (mesh.EditData != null)
				{
					foreach (var position in mesh.EditData.Data.Positions)
					{
						comX += position.X;
						comY += position.Y;
						comZ += position.Z;
					}
					var vertexCount = mesh.EditData.Data.Positions.Length;
					if (vertexCount > 0)
					{
						comX /= vertexCount;
						comY /= vertexCount;
						comZ /= vertexCount;
					}
				}

				CenterOfMass += new Vector3((float) comX, (float) comY, (float) comZ);
				if (firstMesh)
				{
					bb = mesh.BoundingBox;
					firstMesh = false;
				}
				else
				{
					bb = BoundingBox.Merge(bb, mesh.BoundingBox);
				}
			}

			if (meshes.Length > 0)
			{
				CenterOfMass /= meshes.Length;
			}

			ClampBoundingBox(ref bb);
			ModelBoundingBox = bb;
			var center = ModelBoundingBox.Center;
			CenterOfGeometry = new Vector3(center.X, center.Y, center.Z);
			ReferenceScale = CalculateReferenceScale(ModelBoundingBox);
			GridLineX = (int) bb.Width + 16;
			GridLineY = (int) bb.Depth + 16;
			RaisePropertyChanged(nameof(GridLineX));
			RaisePropertyChanged(nameof(GridLineY));

			RaisePropertyChanged(nameof(Meshes));
			RefocusCenter();
			RaisePropertyChanged(nameof(ReferenceScale));
			RaisePropertyChanged(nameof(ModelBoundingBox));
		}

		public void SetRenderTexture(Texture texture)
		{
			SetPreviewTarget(OglPreviewPage.Mode.Image);
			Texture = null;
			if (texture == null)
			{
				ImageText = string.Empty;
			}
			else if (texture.HasPixelData && texture.TexturePixels.IsLargeData)
			{
				ImageText = Resources.Preview_Msg_TextureSizeTooLarge;
			}
			else if (!texture.HasPixelData)
			{
				ImageText = Resources.Preview_Msg_TextureHasNoData;
			}
			else if (!TextureManager.IsFormatSupported(texture.Format))
			{
				ImageText = string.Format(Resources.Preview_Msg_TextureFormatUnsupported, texture.Format.ToString());
			}
			else
			{
				ImageText = string.Empty;
				Texture = texture;
				var maxTextureSize = TextureViewModel._clampMode;
				var textureChannelMode = TextureViewModel._channelMode;
				switch (maxTextureSize)
				{
					case 1: // 2048
						maxTextureSize = 2048; break;
					case 2: // 1024
						maxTextureSize = 1024; break;
					case 3: // 512
						maxTextureSize = 512; break;
				}

				if (maxTextureSize != MaxTextureSize)
				{
					MaxTextureSize = maxTextureSize;
					ClearOnNextTick = true;
					RaisePropertyChanged(nameof(MaxTextureSize));
					RaisePropertyChanged(nameof(ClearOnNextTick));
				}

				if (textureChannelMode == 0)
				{
					switch (texture.Format.GetColorChannel())
					{
						case 4:
							textureChannelMode = CHANNEL_MODE_RGBA; break;
						case 3:
							textureChannelMode = CHANNEL_MODE_RGB; break;
						case 2:
							textureChannelMode = CHANNEL_MODE_RG; break;
						case 1:
							textureChannelMode = CHANNEL_MODE_R; break;
					}
				}

				EnableAlpha = false;
				switch (textureChannelMode)
				{
					case CHANNEL_MODE_RGBA:
						ImageColorMask = Matrix4.Identity;
						EnableAlpha = true;
						break;
					case CHANNEL_MODE_RGB:
						ImageColorMask = Matrix4.Identity;
						break;
					case CHANNEL_MODE_RG:
						ImageColorMask = new Matrix4(
							1, 0, 0, 0,
							0, 1, 0, 0,
							0, 0, 0, 0,
							0, 0, 0, 0
							);
						break;
					case CHANNEL_MODE_R:
						ImageColorMask = new Matrix4(
							1, 1, 1, 0,
							0, 0, 0, 0,
							0, 0, 0, 0,
							0, 0, 0, 0
						);
						break;
					case CHANNEL_MODE_G:
						ImageColorMask = new Matrix4(
							0, 0, 0, 0,
							1, 1, 1, 0,
							0, 0, 0, 0,
							0, 0, 0, 0
						);
						break;
					case CHANNEL_MODE_B:
						ImageColorMask = new Matrix4(
							0, 0, 0, 0,
							0, 0, 0, 0,
							1, 1, 1, 0,
							0, 0, 0, 0
						);
						break;
					case CHANNEL_MODE_ALPHA:
						ImageColorMask = new Matrix4(
							0, 0, 0, 0,
							0, 0, 0, 0,
							0, 0, 0, 0,
							1, 1, 1, 0
						);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(textureChannelMode));
				}

				RaisePropertyChanged(nameof(ImageColorMask));
				RaisePropertyChanged(nameof(EnableAlpha));
			}
			RaisePropertyChanged(nameof(ImageText));
			RaisePropertyChanged(nameof(Texture));
		}
        private void OnPreviewSkeleton(Skeleton skeleton)
        {
            SetPreviewTarget(OglPreviewPage.Mode.Skeleton);
            Skeleton = skeleton;

			CenterOfMass = Vector3.Zero;
            BoundingBox bb = new BoundingBox();
			bb.BoundingSphereRadius = 2f;
			// CenterOfMass as the average position of all bone global positions
			var globalBoneFrames = Skeleton.Definition.Data.CreateBoneMatrices(true);
			var maxVec = System.Numerics.Vector3.Zero;
            foreach (var frame in globalBoneFrames)
            { 
                CenterOfMass += new Vector3(frame.Translation.X, frame.Translation.Y, frame.Translation.Z);
				maxVec = System.Numerics.Vector3.Max(maxVec, System.Numerics.Vector3.Abs(frame.Translation));
            }
			
            if (globalBoneFrames.Count() > 0)
            {
                CenterOfMass /= globalBoneFrames.Count();
            }
			bb = new BoundingBox(-maxVec, maxVec);
            ClampBoundingBox(ref bb);
            ModelBoundingBox = bb;
            var center = ModelBoundingBox.Center;
            CenterOfGeometry = new Vector3(center.X, center.Y, center.Z);
            ReferenceScale = CalculateReferenceScale(ModelBoundingBox);
            GridLineX = (int)bb.Width + 16;
            GridLineY = (int)bb.Depth + 16;
            RaisePropertyChanged(nameof(GridLineX));
            RaisePropertyChanged(nameof(GridLineY));
            RaisePropertyChanged(nameof(Skeleton));
            RefocusCenter();
            RaisePropertyChanged(nameof(ReferenceScale));
            RaisePropertyChanged(nameof(ModelBoundingBox));
        }
        private void OnPreviewAnimation((Skeleton, SkeletalAnimation, int, AnimationClip) tuple)
        {
            SetPreviewTarget(OglPreviewPage.Mode.Animation);
			Skeleton = tuple.Item1;
            Animation = tuple.Item2;
			FPS = tuple.Item3;
			AnimationClip = tuple.Item4;

            CenterOfMass = Vector3.Zero;
            BoundingBox bb = new BoundingBox();
            bb.BoundingSphereRadius = 2f;
			if (Skeleton != null)
			{
                var globalBoneFrames = Skeleton.Definition.Data.CreateBoneMatrices(true);
                var maxVec = System.Numerics.Vector3.Zero;
                foreach (var frame in globalBoneFrames)
                {
                    CenterOfMass += new Vector3(frame.Translation.X, frame.Translation.Y, frame.Translation.Z);
                    maxVec = System.Numerics.Vector3.Max(maxVec, System.Numerics.Vector3.Abs(frame.Translation));
                }

                if (globalBoneFrames.Count() > 0)
                {
                    CenterOfMass /= globalBoneFrames.Count();
                }
                bb = new BoundingBox(-maxVec, maxVec);
            }
            ClampBoundingBox(ref bb);
            ModelBoundingBox = bb;
            var center = ModelBoundingBox.Center;
            CenterOfGeometry = new Vector3(center.X, center.Y, center.Z);
            ReferenceScale = CalculateReferenceScale(ModelBoundingBox);
            GridLineX = (int)bb.Width + 16;
            GridLineY = (int)bb.Depth + 16;
            RaisePropertyChanged(nameof(GridLineX));
            RaisePropertyChanged(nameof(GridLineY));
			CurrentTime = 0;
			PlayingAnimation = true;
			RaisePropertyChanged(nameof(Skeleton));
            RaisePropertyChanged(nameof(Animation));
            RaisePropertyChanged(nameof(AnimationStart));
            RaisePropertyChanged(nameof(AnimationEnd));
            RaisePropertyChanged(nameof(AnimationDuration));
            RaisePropertyChanged(nameof(CurrentTime));
            RaisePropertyChanged(nameof(FPS));
            RaisePropertyChanged(nameof(PlayingAnimation));
            RefocusCenter();
            RaisePropertyChanged(nameof(ReferenceScale));
            RaisePropertyChanged(nameof(ModelBoundingBox));
        }

        private void SetPreviewTarget(OglPreviewPage.Mode mode)
		{
			if (PreviewTarget != mode)
			{
				PreviewTarget = mode;
				RaisePropertyChanged(nameof(PreviewTarget));
				RaisePropertyChanged(nameof(IsModelMode));
				RaisePropertyChanged(nameof(IsImageMode));
                RaisePropertyChanged(nameof(IsAnimationMode));
                RaisePropertyChanged(nameof(IsSkeletonMode));
            }
		}

		private void RefocusCenter()
		{
			switch (CenterMode)
			{
				case 1: // mass
					CameraTarget = CenterOfMass;
					break;
				case 2: // geo
					CameraTarget = CenterOfGeometry;
					break;
				default:
					CameraTarget = new Vector3();
					break;
			}
			RaisePropertyChanged(nameof(CameraTarget));
		}

		private void ClampBoundingBox(ref BoundingBox bb)
		{
			bb.Min = System.Numerics.Vector3.Max(bb.Min, new System.Numerics.Vector3(-MAX_GRID_LENGTH));
			bb.Max = System.Numerics.Vector3.Min(bb.Max, new System.Numerics.Vector3(MAX_GRID_LENGTH));
		}

		private double CalculateReferenceScale(BoundingBox bb)
		{
			return Math.Max(bb.BoundingSphereRadius, 0.001);
		}
	}
}
