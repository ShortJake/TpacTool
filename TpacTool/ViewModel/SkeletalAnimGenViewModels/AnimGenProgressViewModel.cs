using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using TpacTool.Properties;

namespace TpacTool
{
	public class AnimGenProgressViewModel : ViewModelBase
	{
		public static readonly Guid AnimGenExportingBeginEvent = Guid.NewGuid();

		public static readonly Guid AnimGenExportingProgressEvent = Guid.NewGuid();

		public static readonly Guid AnimGenExportingCancelledEvent = Guid.NewGuid();

		private bool _shouldUpdateMsg = true;

		public string CurrentItemName { set; get; }

		public string ItemProgressString { set; get; }

		public int CurrentProgress { set; get; } = 0;

		public int MaxProgress { set; get; } = 100;

		public bool IsCompletedWithoutError { set; get; }

		public ICommand CancelExportingCommand { set; get; }

		public AnimGenProgressViewModel()
		{
			if (IsInDesignMode)
			{
				CurrentItemName = "Item Name";
				ItemProgressString = "5 / 20";
			}
			else
			{
				CurrentItemName = String.Empty;
				ItemProgressString = String.Empty;
				CancelExportingCommand = new RelayCommand(CancelExporting);
				MessengerInstance.Register<object>(this, AnimGenExportingBeginEvent, OnExportBegin);
				MessengerInstance.Register<ValueTuple<int, int, string>>(this, AnimGenExportingProgressEvent, OnExportProgress);
            }
		}
		private void OnExportBegin(object message)
		{
            _shouldUpdateMsg = true;
            CurrentItemName = String.Empty;
            ItemProgressString = String.Empty;
            MaxProgress = 100;
            CurrentProgress = 0;
			RaisePropertyChanged(nameof(ItemProgressString));
			RaisePropertyChanged(nameof(CurrentItemName));
            RaisePropertyChanged(nameof(MaxProgress));
            RaisePropertyChanged(nameof(CurrentProgress));
        }

		private void OnExportProgress(ValueTuple<int, int, string> indexCountNameTuple)
		{
            if (indexCountNameTuple.Item2 > 0)
            {
                ItemProgressString = indexCountNameTuple.Item1 + " / " + indexCountNameTuple.Item2;
                CurrentItemName = indexCountNameTuple.Item3;
                MaxProgress = indexCountNameTuple.Item2;
                CurrentProgress = indexCountNameTuple.Item1;
            }
            else
            {
                if (_shouldUpdateMsg)
                    _shouldUpdateMsg = false;
                else
                    return;
                ItemProgressString = "- / -";
                CurrentItemName = Resources.Loading_Searching;
                MaxProgress = 100;
                CurrentProgress = 0;
            }
            RaisePropertyChanged(nameof(ItemProgressString));
            RaisePropertyChanged(nameof(CurrentItemName));
            RaisePropertyChanged(nameof(MaxProgress));
            RaisePropertyChanged(nameof(CurrentProgress));
        }

		private void CancelExporting()
		{
			MessengerInstance.Send<object>(null, AnimGenExportingCancelledEvent);
		}
	}
}