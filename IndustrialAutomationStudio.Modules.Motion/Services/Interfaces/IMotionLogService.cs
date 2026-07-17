using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IMotionLogService
{
    IReadOnlyList<MotionLogEntry> Entries { get; }
    event EventHandler<MotionLogEntry>? EntryAdded;
    void Log(MotionLogEntry entry);
}
