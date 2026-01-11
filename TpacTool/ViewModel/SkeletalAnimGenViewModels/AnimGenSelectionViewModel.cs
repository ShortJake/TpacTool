using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TpacTool.Lib;

namespace TpacTool
{
	public class AnimGenSelectionViewModel : ViewModelBase
	{
		public static AnimGenSelectionViewModel Instance;

		public List<SkeletalAnimation> SelectedAnims;
		public ObservableCollection<AnimItemViewModel> AnimItems { private set; get; }

		public CollectionViewSource ViewedAnims { private set; get; }

		public string FilterText 
		{
			get => _filterText;
			set
			{
				if (value == _filterText) return;
				_filterText = value;
				ViewedAnims.View.Refresh();
			}
		}
		public bool CanConfirm { get => SelectedAnims.Count > 0; }
		public AnimGenSelectionViewModel()
		{
			Instance = this;
			SelectedAnims = new List<SkeletalAnimation>();
			AnimItems = new ObservableCollection<AnimItemViewModel>();
			ViewedAnims = new CollectionViewSource();
			ViewedAnims.Source = AnimItems;
			SortAlphabetically(true);
            ViewedAnims.Filter += FilterByText;
            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);
        }

		public void SortAlphabetically(bool ascending)
		{
			var dir = ascending ? ListSortDirection.Ascending : ListSortDirection.Descending;
			ViewedAnims.SortDescriptions.Clear();
			ViewedAnims.SortDescriptions.Add(new SortDescription(nameof(AnimItemViewModel.Name), dir));
		}
		public void Filter(string text)
		{
			FilterText = text;
			RaisePropertyChanged(nameof(FilterText));
		}
		private void FilterByText(object sender, FilterEventArgs args)
		{
            var item = args.Item as AnimItemViewModel;
			if (string.IsNullOrEmpty(FilterText)) args.Accepted = true;
			else
			{
				if (item == null) args.Accepted = false;
				else args.Accepted = item.Name.Contains(FilterText);
            }
		}
        private void SelectAll()
		{
			foreach (var item in ViewedAnims.View)
			{
				(item as AnimItemViewModel).Selected = true;
			}
		}

		private void DeselectAll()
		{
            foreach (var item in ViewedAnims.View)
            {
                (item as AnimItemViewModel).Selected = false;
            }
        }

        public ICommand SelectAllCommand { private set; get; }
        public ICommand DeselectAllCommand { private set; get; }

		private string _filterText;

        public class AnimItemViewModel : ViewModelBase
		{
			public string Name { get; private set; }
			public bool Selected
			{
				set
				{
					_selected = value;
					if (Anim == null) return;
					var wasAlreadySelected = _owner.SelectedAnims.Contains(Anim);
					if (_selected && !wasAlreadySelected) _owner.SelectedAnims.Add(Anim);
					else if (!_selected && wasAlreadySelected) _owner.SelectedAnims.Remove(Anim);
					_owner.RaisePropertyChanged(nameof(_owner.CanConfirm));
					RaisePropertyChanged(nameof(Selected));
                }
				get => _selected;
					
			}
			public SkeletalAnimation Anim;

            public AnimItemViewModel(AnimGenSelectionViewModel owner, SkeletalAnimation anim)
            {
				_owner = owner;
                Anim = anim;
				Name = anim == null ? string.Empty : anim.Name;
				_selected = false;
            }
			private bool _selected;
			private AnimGenSelectionViewModel _owner;
        }
	}
}