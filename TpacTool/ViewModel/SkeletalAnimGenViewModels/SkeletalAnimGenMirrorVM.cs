using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using TpacTool.Lib;
using TpacTool.Lib.SkeletalAnimGenMethods;

namespace TpacTool
{
    public class SkeletalAnimGenMirrorVM : SkeletalAnimGenBaseVM
    {
        private string _baseTargetAnimName;
        private SkeletalAnimGenMirror _selectedData;
        public override SkeletalAnimGenMethodBase SelectedData
        {
            set
            {
                _selectedData = value as SkeletalAnimGenMirror;
                _baseTargetAnimName = _selectedData?.BaseTargetAnim == null ? "" : _selectedData.BaseTargetAnim.Name;
                RaisePropertyChanged(nameof(BaseTargetAnimName));
                RaisePropertyChanged(nameof(BaseTargetIndex));
                RaisePropertyChanged(nameof(BaseTargetFrame));
                RaisePropertyChanged(nameof(CopiedBoneIndex));
                RaisePropertyChanged(nameof(TimeOffset));
                RaisePropertyChanged(nameof(AddFromBase));
                RaisePropertyChanged(nameof(IgnoreThisZeroFrame));
                RaisePropertyChanged(nameof(IgnoreSourceZeroFrame));
            }
            get => _selectedData;
        }
        public string BaseTargetAnimName
        {
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    _baseTargetAnimName = value;
                    if (SelectedData != null)
                        _selectedData.BaseTargetAnim = MainViewModel.Instance.AssetManager.LoadedAssets.FirstOrDefault(a => a is SkeletalAnimation s && s.Name == value) as SkeletalAnimation;
                }
                else
                {
                    _baseTargetAnimName = String.Empty;
                    if (SelectedData != null)
                        _selectedData.BaseTargetAnim = null;
                } 
                RaisePropertyChanged(nameof(BaseTargetAnimName));
            }
            get => _baseTargetAnimName;
        }
        public int BaseTargetIndex
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.BaseTargetIndex = value;
            }
            get => SelectedData == null ? 0 : _selectedData.BaseTargetIndex;
        }
        public int BaseTargetFrame
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.BaseTargetFrame = value;
            }
            get => SelectedData == null ? 0 : _selectedData.BaseTargetFrame;
        }
        
        public int CopiedBoneIndex
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.CopiedBoneIndex = value;
            }
            get => SelectedData == null ? 0 : _selectedData.CopiedBoneIndex;
        }
        public float TimeOffset
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.TimeOffset = value;
            }
            get => SelectedData == null ? 0 : _selectedData.TimeOffset;
        }
        public bool AddFromBase
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.AddFromBase = value;
            }
            get => SelectedData == null ? false : _selectedData.AddFromBase;
        }
        public bool IgnoreThisZeroFrame
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.IgnoreThisZeroFrame = value;
            }
            get => SelectedData == null ? false : _selectedData.IgnoreThisZeroFrame;
        }
        public bool IgnoreSourceZeroFrame
        {
            set
            {
                if (SelectedData == null) return;
                _selectedData.IgnoreSourceZeroFrame = value;
            }
            get => SelectedData == null ? false : _selectedData.IgnoreSourceZeroFrame;
        }

        public static SkeletalAnimGenMirrorVM Instance;
        public SkeletalAnimGenMirrorVM()
		{
            Instance = this;
            SelectedData = null;
		}
    }
}