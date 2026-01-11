using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using TpacTool.Lib;
using TpacTool.Lib.SkeletalAnimGenMethods;

namespace TpacTool
{
    public class SkeletalAnimGenStaticVM : SkeletalAnimGenBaseVM
    {
        private string _baseTargetAnimName;
        private SkeletalAnimGenStatic _selectedData;
        public override SkeletalAnimGenMethodBase SelectedData
        {
            set
            {
                _selectedData = value as SkeletalAnimGenStatic;
                _baseTargetAnimName = _selectedData?.BaseTargetAnim == null ? "" : _selectedData.BaseTargetAnim.Name;
                RaisePropertyChanged(nameof(BaseTargetAnimName));
                RaisePropertyChanged(nameof(BaseTargetIndex));
                RaisePropertyChanged(nameof(BaseTargetFrame));
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

        public static SkeletalAnimGenStaticVM Instance;
        public SkeletalAnimGenStaticVM()
		{
            Instance = this;
            SelectedData = null;
		}
    }
}