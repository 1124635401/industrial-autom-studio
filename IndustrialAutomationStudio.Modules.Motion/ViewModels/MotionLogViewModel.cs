using System.Collections.ObjectModel;
using System.Windows;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class MotionLogViewModel : BindableBase
{
    public MotionLogViewModel(IMotionLogService logService)
    {
        Entries = new ObservableCollection<MotionLogEntry>(logService.Entries);
        logService.EntryAdded += (_, entry) =>
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess())
            {
                Entries.Add(entry);
            }
            else
            {
                dispatcher.Invoke(() => Entries.Add(entry));
            }
        };
    }

    public ObservableCollection<MotionLogEntry> Entries { get; }
}
