using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using TpacTool.Lib;
using TpacTool.Lib.SkeletalAnimGenMethods;
using static TpacTool.Lib.AssetManager;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace TpacTool
{
    public class SkeletonViewModel : ViewModelBase
    {
        public static readonly Uri Uri_Page_BoneBody = new Uri("SkeletonBody.xaml", UriKind.Relative);
        public static readonly Uri Uri_Page_D6Joint = new Uri("SkeletonD6Joint.xaml", UriKind.Relative);
        public static readonly Uri Uri_Page_IKJoint = new Uri("SkeletonIKJoint.xaml", UriKind.Relative);
        public static readonly Uri Uri_AnimGenPage_Static = new Uri("SkeletalAnimGenPages\\AnimGenStatic.xaml", UriKind.Relative);
        public static readonly Uri Uri_AnimGenPage_CopyFromLoop = new Uri("SkeletalAnimGenPages\\AnimGenCopyFromLoop.xaml", UriKind.Relative);
        public static readonly Uri Uri_AnimGenPage_CopyFromStretch = new Uri("SkeletalAnimGenPages\\AnimGenCopyFromStretch.xaml", UriKind.Relative);
        public static readonly Uri Uri_AnimGenPage_Mirror = new Uri("SkeletalAnimGenPages\\AnimGenMirror.xaml", UriKind.Relative);
        public static readonly Uri Uri_AnimGenPage_Tail = new Uri("SkeletalAnimGenPages\\AnimGenTail.xaml", UriKind.Relative);
        public static readonly Uri Uri_AnimGenPage_TailAndCopyFromLoop = new Uri("SkeletalAnimGenPages\\AnimGenTailAndCopyFromLoop.xaml", UriKind.Relative);
        public static readonly Uri Uri_AnimGenPage_TailAndCopyFromStretch = new Uri("SkeletalAnimGenPages\\AnimGenTailAndCopyFromStretch.xaml", UriKind.Relative);
        public static readonly Uri Uri_AnimGenPage_TailAndMirror = new Uri("SkeletalAnimGenPages\\AnimGenTailAndMirror.xaml", UriKind.Relative);

        private Skeleton _humanSkeleton;
        private Skeleton _horseSkeleton;
        private List<SkeletalAnimation> _allHumanSkeletalAnimations;
        /// <summary>
        /// Holds all animation clips tied to a skeletal animation that uses the human_skeleton. The Value is the skeletal animation of that clip
        /// </summary>
        private Dictionary<AnimationClip, SkeletalAnimation> _allHumanAnimationClips;

        private SaveFileDialog _saveFileDialog;
        private AnimGenProgressWindow _exportProgressWindow;
        private bool _interruptExporting;
        private AnimGenSelectionWindow _selectAnimsWindow;

        private Skeleton _selectedSkeleton;
        private List<SkeletonUserData.Body> _skeletonBones;
        private SkeletonUserData.Body _selectedBone;
        private int _selectedBoneIndex;
        private List<SkeletonUserData.D6JointConstraint> _skeletonD6Joints;
        private SkeletonUserData.D6JointConstraint _selectedD6Joint;
        private List<SkeletonUserData.IKConstraint> _skeletonIKJoints;
        private SkeletonUserData.IKConstraint _selectedIKJoint;

        private SkeletonUserData _copiedUserData;
        private SkeletalAnimGenMethodBase _copiedBoneAnimGenData;
        private SkeletalAnimGenBaseVM _currentAnimGenVM;

        private Uri _currentPageUri;

        public Skeleton Asset { private set; get; }
        public Skeleton SelectedSkeleton
        {
            private set
            {
                _selectedSkeleton = value;
                RefreshAnimGenVM();
                if (_selectedSkeleton != null)
                {
                    SkeletonBones = _selectedSkeleton.UserData.Data.Bodies;
                    SkeletonD6Joints = _selectedSkeleton.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>();
                    SkeletonIKJoints = _selectedSkeleton.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>();
                }
                else
                {
                    SkeletonBones = null;
                    SkeletonD6Joints = null;
                    SkeletonIKJoints = null;
                }
                RaisePropertyChanged(nameof(SelectedSkeleton));
            }
            get => _selectedSkeleton;
        }
        public IEnumerable<SkeletonUserData.Body> SkeletonBones
        {
            private set
            {
                if (value != null) _skeletonBones = value.ToList();
                else _skeletonBones = null;
                RaisePropertyChanged(nameof(SkeletonBones));
            }
            get => _skeletonBones;
        }
        public SkeletonUserData.Body SelectedBone
        {
            set
            {
                if (value != null) _selectedBone = value;
                else _selectedBone = null;               
                RaisePropertyChanged(nameof(SelectedBone));
            }
            get => _selectedBone;
        }
        public int SelectedBoneIndex
        {
            set
            {
                _selectedBoneIndex = value;
                if (SelectedSkeleton == null) _selectedBoneIndex = 0;
                else if (_selectedBoneIndex > SelectedSkeleton.Definition.Data.Bones.Count - 1) _selectedBoneIndex = SelectedSkeleton.Definition.Data.Bones.Count - 1;
                else if (_selectedBoneIndex < 0) _selectedBoneIndex = 0;
                RefreshAnimGenVM();
                RaisePropertyChanged(nameof(SelectedBoneIndex));
                RaisePropertyChanged(nameof(SelectedBoneGenerateMethodIndex));
                RaisePropertyChanged(nameof(AnimGenDataPageUri));
            }
            get => _selectedBoneIndex;
        }
        public IEnumerable<SkeletonUserData.D6JointConstraint> SkeletonD6Joints
        {
            private set
            {
                if (value != null) _skeletonD6Joints = value.ToList();
                else _skeletonD6Joints = null;
                RaisePropertyChanged(nameof(SkeletonD6Joints));
            }
            get => _skeletonD6Joints;
        }
        public SkeletonUserData.D6JointConstraint SelectedD6Joint
        {
            set
            {
                if (value != null) _selectedD6Joint = value;
                else _selectedD6Joint = null;
                RaisePropertyChanged(nameof(SelectedD6Joint));
            }
            get => _selectedD6Joint;
        }
        public IEnumerable<SkeletonUserData.IKConstraint> SkeletonIKJoints
        {
            private set
            {
                if (value != null) _skeletonIKJoints = value.ToList();
                else _skeletonIKJoints = null;
                RaisePropertyChanged(nameof(SkeletonIKJoints));
            }
            get => _skeletonIKJoints;
        }
        public SkeletonUserData.IKConstraint SelectedIKJoint
        {
            set
            {
                if (value != null) _selectedIKJoint = value;
                else _selectedIKJoint = null;
                RaisePropertyChanged(nameof(SelectedIKJoint));
            }
            get => _selectedIKJoint;
        }
        public int SelectedBoneGenerateMethodIndex
        {
            set
            {
                var val = (GenerateFramesMethod)value;
                if (SelectedSkeleton == null) return;
                if (val != SelectedSkeleton.AnimGenData[SelectedBoneIndex].GenerationMethod)
                {
                    SetBoneAnimGenMethod(val);
                    RaisePropertyChanged(nameof(AnimGenDataPageUri));
                }
            }
            get => SelectedSkeleton == null ? 0 : (int)SelectedSkeleton.AnimGenData[SelectedBoneIndex].GenerationMethod;
        }
        public IEnumerable<string> AvailableGenerateFramesMethods
        {
            get
            {
                foreach (var value in Enum.GetValues(typeof(GenerateFramesMethod)))
                {
                    yield return value.ToString();
                }
            }
        }
        public Uri CurrentPageUri
        {
            set
            {
                if (value != null)
                {
                    _currentPageUri = value;
                    RaisePropertyChanged(nameof(CurrentPageUri));
                }
            }
            get => _currentPageUri;
        }
        public Uri AnimGenDataPageUri
        {
            get
            {
                var val = GenerateFramesMethod.Static;
                if (SelectedSkeleton != null && SelectedBoneIndex < SelectedSkeleton.AnimGenData.Length)
                {
                    val = SelectedSkeleton.AnimGenData[SelectedBoneIndex].GenerationMethod;
                }
                switch (val)
                {
                    default:
                        return Uri_AnimGenPage_Static;
                    case GenerateFramesMethod.CopyFromLoop:
                        return Uri_AnimGenPage_CopyFromLoop;
                    case GenerateFramesMethod.CopyFromStretch:
                        return Uri_AnimGenPage_CopyFromStretch;
                    case GenerateFramesMethod.Mirror:
                        return Uri_AnimGenPage_Mirror;
                    case GenerateFramesMethod.Tail:
                        return Uri_AnimGenPage_Tail;
                    case GenerateFramesMethod.TailAndCopyFromLoop:
                        return Uri_AnimGenPage_TailAndCopyFromLoop;
                    case GenerateFramesMethod.TailAndCopyFromStretch:
                        return Uri_AnimGenPage_TailAndCopyFromStretch;
                    case GenerateFramesMethod.TailAndMirror:
                        return Uri_AnimGenPage_TailAndMirror;
                }
            }
        }
        public string AnimationExportPrefix { set; get; } = "prefix_";
        public bool CanPasteProperties => _copiedUserData != null;
        public ICommand SaveTPACCommand { private set; get; }
        public ICommand CopyPropertiesCommand { private set; get; }
        public ICommand PastePropertiesCommand { private set; get; }
        public ICommand CopyBoneAnimGenDataCommand { private set; get; }
        public ICommand PasteBoneAnimGenDataCommand { private set; get; }
        public ICommand GenerateHumanAnimationsCommand { private set; get; }
        public ICommand GenerateHumanAnimationsWithCopiedFrameCommand { private set; get; }
        public ICommand SaveHumanSkeletonCommand { private set; get; }
        public ICommand AddBoneBodyCommand { private set; get; }
        public ICommand RemoveBoneBodyCommand { private set; get; }
        public ICommand AddD6JointCommand { private set; get; }
        public ICommand RemoveD6JointCommand { private set; get; }
        public ICommand AddIKJointCommand { private set; get; }
        public ICommand RemoveIKJointCommand { private set; get; }
        public ICommand SelectBonesPageCommand { private set; get; }
        public ICommand SelectD6JointsPageCommand { private set; get; }
        public ICommand SelectIKJointsPageCommand { private set; get; }

        public SkeletonViewModel()
		{
			if (IsInDesignMode)
			{
			}
			else
			{
                CurrentPageUri = Uri_Page_BoneBody;
                _allHumanSkeletalAnimations = new List<SkeletalAnimation>();
                _allHumanAnimationClips = new Dictionary<AnimationClip, SkeletalAnimation>();

                _saveFileDialog = new SaveFileDialog();
				_saveFileDialog.CreatePrompt = false;
				_saveFileDialog.OverwritePrompt = true;
				_saveFileDialog.AddExtension = true;
                _saveFileDialog.DefaultExt = "tpac";
                _saveFileDialog.Title = "Select path for created packages";

                SaveTPACCommand = new RelayCommand(SaveTPAC);
                CopyPropertiesCommand = new RelayCommand(CopyJoints);
                PastePropertiesCommand = new RelayCommand(PasteJoints);
                CopyBoneAnimGenDataCommand = new RelayCommand(CopyBoneAnimGenData);
                PasteBoneAnimGenDataCommand = new RelayCommand<bool>(PasteBoneAnimGenData);
                GenerateHumanAnimationsCommand = new RelayCommand(GenerateHumanAnimations);
                SaveHumanSkeletonCommand = new RelayCommand(SaveHumanSkeleton);
                AddBoneBodyCommand = new RelayCommand(AddBoneBody);
                RemoveBoneBodyCommand = new RelayCommand(RemoveBoneBody);
                AddD6JointCommand = new RelayCommand(AddD6Joint);
                RemoveD6JointCommand = new RelayCommand(RemoveD6Joint);
                AddIKJointCommand = new RelayCommand(AddIKJoint);
                RemoveIKJointCommand = new RelayCommand(RemoveIKJoint);
                SelectBonesPageCommand = new RelayCommand(() => CurrentPageUri = Uri_Page_BoneBody);
                SelectD6JointsPageCommand = new RelayCommand(() => CurrentPageUri = Uri_Page_D6Joint);
                SelectIKJointsPageCommand = new RelayCommand(() => CurrentPageUri = Uri_Page_IKJoint);


                MessengerInstance.Register<AssetItem>(this, AssetTreeViewModel.AssetSelectedEvent, OnSelectAsset);
				MessengerInstance.Register<object>(this, MainViewModel.CleanupEvent, Cleanup);
                MessengerInstance.Register<object>(this, AnimGenProgressViewModel.AnimGenExportingCancelledEvent, OnExportCancelled);
                MessengerInstance.Register<IEnumerable<Skeleton>>(this, ModelViewModel.UpdateSkeletonListEvent, UpdateSkeletonsAndAnimations);
			    
            }
		}
        private void Cleanup(object _)
        {
            SelectedSkeleton = null;
            _allHumanAnimationClips.Clear();
            _allHumanSkeletalAnimations.Clear();
            Asset = null;
            RaisePropertyChanged(nameof(SelectedSkeleton));
            RaisePropertyChanged(nameof(Asset));
        }

        private void UpdateSkeletonsAndAnimations(IEnumerable<Skeleton> skeletons)
        {
            // Called when new packages are loaded
            _humanSkeleton = null;
            _horseSkeleton = null;
            _allHumanSkeletalAnimations.Clear();
            _allHumanAnimationClips.Clear();
            var assetManager = MainViewModel.Instance.AssetManager;
            foreach (var skeleton in skeletons)
			{
                if (skeleton.Name == "human_skeleton")
					_humanSkeleton = skeleton;
				else if (skeleton.Name == "horse_skeleton")
					_horseSkeleton = skeleton;
			}
            if (_humanSkeleton == null) return;

            // Update the list of all human skeletal animations and animation clips
            AnimGenSelectionViewModel.Instance.AnimItems.Clear();
            foreach (var item in assetManager.LoadedAssets.OfType<SkeletalAnimation>())
            {
                if (item.Skeleton != _humanSkeleton.Guid) continue;
                Console.WriteLine("Found Skeletal Animation: " + item.Name);
				_allHumanSkeletalAnimations.Add(item);
                // Add it to list of selectable animations during animation generation
                AnimGenSelectionViewModel.Instance.AnimItems.Add(new AnimGenSelectionViewModel.AnimItemViewModel(AnimGenSelectionViewModel.Instance, item));
            }

            foreach (var item in assetManager.LoadedAssets.OfType<AnimationClip>())
            {
                // Don't add autogenerated blended combat animations
                if (item.GeneratedIndex != -1) continue;

                var originalSkeletalAnimation = assetManager.GetAsset<SkeletalAnimation>(item.Animation);
                if (originalSkeletalAnimation == null || originalSkeletalAnimation.Skeleton != _humanSkeleton.Guid) continue;
                Console.WriteLine("Found Animation Clip: " + item.Name + " for Skeletal Animation: " + originalSkeletalAnimation.Name);
                _allHumanAnimationClips.Add(item, originalSkeletalAnimation);
            }
        }

		private void OnSelectAsset(AssetItem assetItem)
		{
			var skeleton = assetItem as Skeleton;
			if (skeleton != null)
			{
				Asset = skeleton;
				SelectedSkeleton = skeleton;
                SelectedBone = Asset.UserData.Data.Bodies.FirstOrDefault();
                SelectedD6Joint = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>().FirstOrDefault();
                SelectedIKJoint = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>().FirstOrDefault();
                MessengerInstance.Send(skeleton, OglPreviewViewModel.PreviewSkeletonEvent);
                RaisePropertyChanged(nameof(Asset));
			} 
        }

        /// <summary>
        /// Copy properties (bone bodies, joints) from the selected skeleton
        /// </summary>
		private void CopyJoints()
		{
			if (SelectedSkeleton == null) return;
            _copiedUserData = SelectedSkeleton.UserData.Data;
            RaisePropertyChanged(nameof(CanPasteProperties));
        }
        /// <summary>
        /// Pastes properties (bone bodies, joints) to the selected skeleton
        /// </summary>
        private void PasteJoints()
        {
            if (_copiedUserData == null) return;
            //The original target skeleton's bone bodies
            var targetBoneBodies = Asset.UserData.Data.Bodies;
            foreach (SkeletonUserData.Body copiedBody in _copiedUserData.Bodies)
            {
                // If there exists the same bone in the target skeleton, replace it with the copied body
                var targetBone = targetBoneBodies.FirstOrDefault(b => b.BoneName == copiedBody.BoneName);
                if (targetBone != null)
                {
                    targetBoneBodies[targetBoneBodies.IndexOf(targetBone)] = copiedBody.Clone();
                }
            }

            //The original target skeleton's joints
            var targetConstraints = Asset.UserData.Data.Constraints;
            foreach (SkeletonUserData.Constraint copiedConstraint in _copiedUserData.Constraints)
            {
                // If there exists the same joint in the target skeleton, replace it with the copied body
                var targetConstraint = targetConstraints.FirstOrDefault(c => c.Bone1 == copiedConstraint.Bone1 && c.Bone2 == copiedConstraint.Bone2 && c.GetType() == copiedConstraint.GetType());
                if (targetConstraint != null)
                {
                    targetConstraints[targetConstraints.IndexOf(targetConstraint)] = copiedConstraint.Clone();
                }
                // If there is no equivalent, add it to the list, but make sure valid bones exist
                else if (targetBoneBodies.Exists(b => b.BoneName == copiedConstraint.Bone1) && targetBoneBodies.Exists(b => b.BoneName == copiedConstraint.Bone2))
                {
                    targetConstraints.Add(copiedConstraint.Clone());
                }
            }
            RaisePropertyChanged(nameof(SelectedSkeleton));
            RaisePropertyChanged(nameof(SkeletonBones));
            RaisePropertyChanged(nameof(SkeletonD6Joints));
            RaisePropertyChanged(nameof(SkeletonIKJoints));
            RaisePropertyChanged(nameof(SelectedBone));
            RaisePropertyChanged(nameof(SelectedD6Joint));
            RaisePropertyChanged(nameof(SelectedIKJoint));
            RaisePropertyChanged(nameof(CurrentPageUri));
        }

        /// <summary>
        /// Saves a tpac file of the selected skeleton with the edited data
        /// </summary>
        private void SaveTPAC()
        {
			if (Asset == null) return;
            // Make sure the asset exists in a package already
            var loadedPackages = MainViewModel.Instance.AssetManager.LoadedPackages;
            var thisPackage = loadedPackages.FirstOrDefault(p => p.Items.Contains(Asset));
            if (thisPackage == null) return;
            _saveFileDialog.FileName = "generated_" + thisPackage.File.Name;

            if (_saveFileDialog.ShowDialog().GetValueOrDefault(false))
            {
                var path = _saveFileDialog.FileName;
                thisPackage.Save(path);
            }
        }
        private void SetBoneAnimGenMethod(GenerateFramesMethod method)
        {
            SkeletalAnimGenMethodBase baseMethod;
            // If the method is a tail method, get the base method used
            var isTail = method >= GenerateFramesMethod.Tail;
            if (isTail)
            {
                method -= GenerateFramesMethod.Tail;
            }
            switch (method)
            {
                case GenerateFramesMethod.CopyFromStretch:
                    baseMethod = new SkeletalAnimGenCopyFrom(SelectedBoneIndex, SelectedSkeleton) { Stretch = true};
                    break;
                case GenerateFramesMethod.CopyFromLoop:
                        baseMethod = new SkeletalAnimGenCopyFrom(SelectedBoneIndex, SelectedSkeleton) { Stretch = false };
                    break;
                case GenerateFramesMethod.Mirror:
                        baseMethod = new SkeletalAnimGenMirror(SelectedBoneIndex, SelectedSkeleton);
                    break;
                default:
                    baseMethod = new SkeletalAnimGenStatic(SelectedBoneIndex, SelectedSkeleton);
                    break;
            }
            if (isTail) SelectedSkeleton.AnimGenData[SelectedBoneIndex] = new SkeletalAnimGenTail(baseMethod, SelectedBoneIndex, SelectedSkeleton);
            else SelectedSkeleton.AnimGenData[SelectedBoneIndex] = baseMethod;
            RefreshAnimGenVM();
        }
        private void CopyBoneAnimGenData()
        {
            if (SelectedSkeleton == null || SelectedBoneIndex >= SelectedSkeleton.AnimGenData.Length) return;
            _copiedBoneAnimGenData = SelectedSkeleton.AnimGenData[SelectedBoneIndex];
        }

        /// <param name="keepBoneIndices">Don't paste the bone indices in the animation generation data</param>
        private void PasteBoneAnimGenData(bool keepBoneIndices)
        {
            if (SelectedSkeleton == null || SelectedBoneIndex >= SelectedSkeleton.AnimGenData.Length) return;
            SelectedSkeleton.AnimGenData[SelectedBoneIndex] = _copiedBoneAnimGenData.CreateCopy(SelectedBoneIndex, SelectedSkeleton, keepBoneIndices);
            RefreshAnimGenVM();
            RaisePropertyChanged(nameof(SelectedBoneGenerateMethodIndex));
            RaisePropertyChanged(nameof(AnimGenDataPageUri));
        }
        private void RefreshAnimGenVM()
        {
            // Default to the static animation generation view model
            if (SelectedSkeleton == null || SelectedBoneIndex >= SelectedSkeleton.Definition.Data.Bones.Count)
            {
                _currentAnimGenVM = SkeletalAnimGenStaticVM.Instance;
            }
            else switch (SelectedSkeleton.AnimGenData[SelectedBoneIndex].GenerationMethod)
            {
                    case GenerateFramesMethod.CopyFromStretch:
                    case GenerateFramesMethod.CopyFromLoop:
                        _currentAnimGenVM = SkeletalAnimGenCopyFromVM.Instance;
                        break;
                    case GenerateFramesMethod.Mirror:
                        _currentAnimGenVM = SkeletalAnimGenMirrorVM.Instance;
                        break;
                    case GenerateFramesMethod.Tail:
                    case GenerateFramesMethod.TailAndCopyFromStretch:
                    case GenerateFramesMethod.TailAndCopyFromLoop:
                    case GenerateFramesMethod.TailAndMirror:
                        _currentAnimGenVM = SkeletalAnimGenTailVM.Instance;
                        break;
                    default:
                        _currentAnimGenVM = SkeletalAnimGenStaticVM.Instance;
                        break;
            }
            _currentAnimGenVM.Refresh(SelectedSkeleton, SelectedBoneIndex);
        }

        /// <summary>
        /// Creates a copy of the package that contains the human skeleton so that it contains the selected skeleton instead
        /// </summary>
        private void SaveHumanSkeleton()
        {
            if (Asset == null || _humanSkeleton == null || Asset == _humanSkeleton) return;
            var loadedPackages = MainViewModel.Instance.AssetManager.LoadedPackages;
            var humanSkeletonPackage = loadedPackages.FirstOrDefault(p => p.Items.Contains(_humanSkeleton));
            if (humanSkeletonPackage == null) return;
            _saveFileDialog.FileName = "generated_" + humanSkeletonPackage.File.Name;

            if (_saveFileDialog.ShowDialog().GetValueOrDefault(false))
            {
                var path = _saveFileDialog.FileName;

                var clonedPackage = new AssetPackage(humanSkeletonPackage.Guid);
                clonedPackage.Items.AddRange(humanSkeletonPackage.Items);
                var clonedSkeleton = (Skeleton)_humanSkeleton.Clone();

                clonedSkeleton.Definition.Data.Bones.Clear();
                clonedSkeleton.Definition.Data.Bones.AddRange(Asset.Definition.Data.Bones);
                clonedSkeleton.UserData.Data.Bodies.Clear();
                clonedSkeleton.UserData.Data.Bodies.AddRange(Asset.UserData.Data.Bodies);
                clonedSkeleton.UserData.Data.Constraints.Clear();
                clonedSkeleton.UserData.Data.Constraints.AddRange(Asset.UserData.Data.Constraints);

                var index = clonedPackage.Items.IndexOf(_humanSkeleton);
                clonedPackage.Items.Remove(_humanSkeleton);
                clonedPackage.Items.Insert(index, clonedSkeleton);
                clonedPackage.Save(path);
            }

        }

        /// <summary>
        /// Generates a skeletal animation (with associated animation clips) for the selected skeleton for each selected human_skeleton animation, adding animation data for any additional bones
        /// </summary>
        private void GenerateHumanAnimations()
        {
            var list = new List<SkeletalAnimation>();
            _selectAnimsWindow = new AnimGenSelectionWindow();
            _selectAnimsWindow.Owner = Application.Current.MainWindow;
            if (_selectAnimsWindow.ShowDialog() == true)
            {
                list.AddRange(AnimGenSelectionViewModel.Instance.SelectedAnims);
                GenerateHumanAnimations(list);
            }
        }

        private void GenerateHumanAnimations(List<SkeletalAnimation> animsToGenerate)
        {
            if (Asset == null || _humanSkeleton == null) return;
            // Show Save File Dialog
            var path = "";
            var packageName = "autogenerated.tpac";
            _saveFileDialog.FileName = packageName;
            if (_saveFileDialog.ShowDialog().GetValueOrDefault(false))
            {
                path = Path.GetDirectoryName(_saveFileDialog.FileName);
                packageName = Path.GetFileName(_saveFileDialog.FileName);
            }
            else return;

            _interruptExporting = false;
            _exportProgressWindow = new AnimGenProgressWindow();
            _exportProgressWindow.Owner = Application.Current.MainWindow;
            MessengerInstance.Send<object>(null, AnimGenProgressViewModel.AnimGenExportingBeginEvent);
            
            Thread thread = new Thread(() =>
            {
                var skeletalAnimationsDictionary = GenerateSkeletalAnimations(path, packageName, animsToGenerate, ExportCallback);
                GenerateAnimationClips(path, skeletalAnimationsDictionary);
            });
            thread.Name = "Anim Generator";
            thread.IsBackground = true;
            thread.Start();
            
            _exportProgressWindow.ShowDialog();
            MessageBox.Show("Done Exporting Animations");
        }

        /// <returns>A dictionary that holds the original skeletal animation as key and the corresponding generated skeletal animation as the value</returns>
        private Dictionary<SkeletalAnimation, SkeletalAnimation> GenerateSkeletalAnimations(string path, string packageName, List<SkeletalAnimation> animsToGenerate, ProgressCallback callback)
        {
            var reportProgress = callback != null;
            // Find any bones that don't exist in the original human skeleton and add them to a list
            var additionalBonesList = new List<int>();
            for (int i = 0; i < Asset.Definition.Data.Bones.Count; i++)
            {
                var customSkeletonBone = Asset.Definition.Data.Bones[i];
                if (_humanSkeleton.Definition.Data.Bones.Exists(b => b.Name == customSkeletonBone.Name)) continue;
                Console.WriteLine("Added Extra Bone: " + customSkeletonBone.Name + " With index " + i);
                additionalBonesList.Add(i);
            }

            // This dictionary is used to match the generated animation clips to the correct generated skeletal animation
            // Original as Key. Generated as Value
            var skeletalAnimationsDictionary = new Dictionary<SkeletalAnimation, SkeletalAnimation>();
            if (animsToGenerate == null || animsToGenerate.Count == 0) return skeletalAnimationsDictionary;
            for (var i = 0; i < animsToGenerate.Count; i++)
            {
                var item = animsToGenerate[i];
                if (reportProgress && !callback(i + 1, animsToGenerate.Count, item.Name, false))
                {
                    return skeletalAnimationsDictionary;
                }
                var clonedItem = item.Clone() as SkeletalAnimation;
                clonedItem.Guid = Guid.NewGuid();

                clonedItem.Name = AnimationExportPrefix + clonedItem.Name;
                clonedItem.Definition.Data.Name = AnimationExportPrefix + clonedItem.Definition.Data.Name;
                clonedItem.BoneNum = Asset.UserData.Data.Bodies.Count;
                clonedItem.Definition.OwnerGuid = clonedItem.Guid;
                foreach (var additionalBoneIndex in additionalBonesList)
                {
                    AnimationDefinitionData.BoneAnim boneAnim;
                    var boneData = Asset.AnimGenData[additionalBoneIndex];
                    if (boneData != null) clonedItem.Definition.Data.BoneAnims.Add(boneData.GenerateBoneAnim(clonedItem));
                }
                // Remove root scale frames, as the engine doesn't support them
                clonedItem.Definition.Data.RootScaleFrames.Clear();
                clonedItem.Skeleton = Asset.Guid;
                skeletalAnimationsDictionary.Add(item, clonedItem);
            }

            // Save all skeletal animations to one package
            var skeletalAnimsPackage = new AssetPackage(Guid.NewGuid());
            skeletalAnimsPackage.Items.AddRange(skeletalAnimationsDictionary.Values.ToList());
            skeletalAnimsPackage.Save(path + "\\" + packageName);
            if (reportProgress) callback(animsToGenerate.Count, animsToGenerate.Count, String.Empty, true);
            return skeletalAnimationsDictionary;
        }
        private void GenerateAnimationClips(string path, Dictionary<SkeletalAnimation, SkeletalAnimation> skeletalAnimationsDictionary)
        {
            foreach (var item in _allHumanAnimationClips)
            {
                var animClip = item.Key;
                var originalSkeletalAnimation = item.Value;
                // Check that there's a generated skeletal animation corresponding to this clip's original skeletal animation
                if (!skeletalAnimationsDictionary.TryGetValue(originalSkeletalAnimation, out var generatedSkeletalAnimation))
                {
                    Console.WriteLine("Generated Skeletal Animation not found for Original: " + originalSkeletalAnimation.Name);
                    continue;
                }
                var clonedItem = animClip.Clone() as AnimationClip;
                clonedItem.Guid = Guid.NewGuid();

                clonedItem.Name = AnimationExportPrefix + clonedItem.Name;
                // Engine doens't work with animation clips with names of 64 characters
                // Truncate the name if longer than 63
                if (clonedItem.Name.Length > 63)
                {
                    var removeLength = clonedItem.Name.Length - 63;
                    clonedItem.Name = clonedItem.Name.Remove(clonedItem.Name.Length - 1, removeLength);
                }
                
                clonedItem.Animation = generatedSkeletalAnimation.Guid;

                //Change the name of the Blend With Animation similar to the clip's name
                if (clonedItem.BlendWithAnimation != "")
                {
                    var blendWithName = AnimationExportPrefix + clonedItem.BlendWithAnimation;
                    if (blendWithName.Length > 63)
                    {
                        var removeLength = blendWithName.Length - 63;
                        blendWithName = blendWithName.Remove(blendWithName.Length - 1, removeLength);
                    }
                    clonedItem.BlendWithAnimation = blendWithName;
                }
                Console.WriteLine("Exported Anim Clip: " + clonedItem.Name);
                // Animation clips are saved separately. Saving them in one package seems (?) to cause issues
                var package = new AssetPackage(Guid.NewGuid());
                package.Items.Add(clonedItem);
                package.Save(path + "\\clips\\" + clonedItem.Name + "_anm.tpac", false);
            }

        }

        /// <summary>
        /// Callback that updates the progress bar and checks if exporting is interrupted, or completed 
        /// </summary>
        /// <returns>Exporting is interrupted</returns>
        private bool ExportCallback(int currentItem, int itemCount, string itemName, bool completed)
        {
            if (_interruptExporting)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    _exportProgressWindow.DialogResult = false;
                    _exportProgressWindow.Close();
                });
                return false;
            }
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (!completed)
                    MessengerInstance.Send((currentItem, itemCount, itemName), AnimGenProgressViewModel.AnimGenExportingProgressEvent);
                else
                {
                    if (itemCount <= 0)
                    {
                        _exportProgressWindow.DialogResult = false;
                    }
                    else
                    {
                        _exportProgressWindow.DialogResult = true;
                    }
                    _exportProgressWindow.Close();
                }
            });
            return true;
        }
        private void OnExportCancelled(object obj)
        {
            _interruptExporting = true;
        }
        private void AddBoneBody()
        {
            var newBody = new SkeletonUserData.Body();
            newBody.BoneName = "new_bone_body";
            Asset.UserData.Data.Bodies.Add(newBody);
            SelectedBone = newBody;
            SkeletonBones = Asset.UserData.Data.Bodies;
        }
        private void RemoveBoneBody()
        {
            if (!Asset.UserData.Data.Bodies.Contains(SelectedBone)) return;
            Asset.UserData.Data.Bodies.Remove(SelectedBone);
            SelectedBone = Asset.UserData.Data.Bodies.FirstOrDefault();
            SkeletonBones = Asset.UserData.Data.Bodies;
        }

        private void AddD6Joint()
        {
            var newD6Joint = new SkeletonUserData.D6JointConstraint();
            newD6Joint.Bone1 = "write_bone_1_here";
            newD6Joint.Bone2 = "write_bone_2_here";
            newD6Joint.Name = "new_ik_joint";
            Asset.UserData.Data.Constraints.Add(newD6Joint);
            SelectedD6Joint = newD6Joint;
            SkeletonD6Joints = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>();
        }
        private void RemoveD6Joint()
        {
            if (!Asset.UserData.Data.Constraints.Contains(SelectedD6Joint)) return;
            Asset.UserData.Data.Constraints.Remove(SelectedD6Joint);
            SelectedD6Joint = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>().FirstOrDefault();
            SkeletonD6Joints = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>();
        }
        private void AddIKJoint()
        {
            var newIKJoint = new SkeletonUserData.IKConstraint();
            newIKJoint.Bone1 = "write_bone_1_here";
            newIKJoint.Bone2 = "write_bone_2_here";
            newIKJoint.Name = "new_ik_joint";
            Asset.UserData.Data.Constraints.Add(newIKJoint);
            SelectedIKJoint = newIKJoint;
            SkeletonIKJoints = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>();
        }
        private void RemoveIKJoint()
        {
            if (!Asset.UserData.Data.Constraints.Contains(SelectedIKJoint)) return;
            Asset.UserData.Data.Constraints.Remove(SelectedIKJoint);
            SelectedIKJoint = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>().FirstOrDefault();
            SkeletonIKJoints = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>();
        }
    }
}