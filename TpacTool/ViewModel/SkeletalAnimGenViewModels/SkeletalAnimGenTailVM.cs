using GalaSoft.MvvmLight;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using TpacTool.Lib;
using TpacTool.Lib.SkeletalAnimGenMethods;

namespace TpacTool
{
    public class SkeletalAnimGenTailVM : SkeletalAnimGenBaseVM
    {
        private static readonly Uri Uri_AnimGenPage_Static = new Uri("AnimGenStatic.xaml", UriKind.Relative);
        private static readonly Uri Uri_AnimGenPage_CopyFromLoop = new Uri("AnimGenCopyFromLoop.xaml", UriKind.Relative);
        private static readonly Uri Uri_AnimGenPage_CopyFromStretch = new Uri("AnimGenCopyFromStretch.xaml", UriKind.Relative);
        private static readonly Uri Uri_AnimGenPage_Mirror = new Uri("AnimGenMirror.xaml", UriKind.Relative);

        private SkeletalAnimGenTail _selectedData;
        public SkeletalAnimGenBaseVM BaseMethodVM
        {
            get
            {
                if (_selectedData?.BaseMethod == null) return SkeletalAnimGenStaticVM.Instance;
                switch (_selectedData.BaseMethod.GenerationMethod)
                {
                    case GenerateFramesMethod.CopyFromLoop:
                    case GenerateFramesMethod.CopyFromStretch:
                        return SkeletalAnimGenCopyFromVM.Instance;
                    case GenerateFramesMethod.Mirror:
                        return SkeletalAnimGenMirrorVM.Instance;
                    default:
                        return SkeletalAnimGenStaticVM.Instance;
                }
            }
        }
        public Uri BaseMethodPageUri
        {
            get
            {
                if (_selectedData?.BaseMethod == null) return Uri_AnimGenPage_Static;
                switch (_selectedData.BaseMethod.GenerationMethod)
                {
                    case GenerateFramesMethod.CopyFromLoop:
                        return Uri_AnimGenPage_CopyFromLoop;
                    case GenerateFramesMethod.CopyFromStretch:
                        return Uri_AnimGenPage_CopyFromStretch;
                    case GenerateFramesMethod.Mirror:
                        return Uri_AnimGenPage_Mirror;
                    default:
                        return Uri_AnimGenPage_Static;
                }
            }
        }
        public override SkeletalAnimGenMethodBase SelectedData
        {
            set
            {
                _selectedData = value as SkeletalAnimGenTail;
                BaseMethodVM.SelectedData = _selectedData?.BaseMethod;
                RaisePropertyChanged(nameof(BaseMethodPageUri));
                RaisePropertyChanged(nameof(Mass));
                RaisePropertyChanged(nameof(DragCoefficient));
                RaisePropertyChanged(nameof(DampenStiffness));
                RaisePropertyChanged(nameof(IgnoreFrameZero));
            }
            get => _selectedData;
        }
        public float Mass
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.Mass = value;
            }
            get => _selectedData == null ? 0 : _selectedData.Mass;
        }
        public float DragCoefficient
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.DragCoefficient = value;
            }
            get => _selectedData == null ? 0 : _selectedData.DragCoefficient;
        }
        public float DampenStiffness
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.DampenStiffness = value;
            }
            get => SelectedData == null ? 0 : _selectedData.DampenStiffness;
        }
        public bool IgnoreFrameZero
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.IgnoreFrameZero = value;
            }
            get => SelectedData == null ? false : _selectedData.IgnoreFrameZero;
        }

        public static SkeletalAnimGenTailVM Instance;
        public SkeletalAnimGenTailVM()
		{
            Instance = this;
            SelectedData = null;
		}
    }
}