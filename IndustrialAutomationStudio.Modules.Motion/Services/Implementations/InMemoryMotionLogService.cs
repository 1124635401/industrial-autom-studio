using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class InMemoryMotionLogService : IMotionLogService
{
    private readonly Lock _sync = new();
    private readonly List<MotionLogEntry> _entries = [];

    public IReadOnlyList<MotionLogEntry> Entries
    {
        get
        {
            lock (_sync)
            {
                return _entries.ToArray();
            }
        }
    }

    public event EventHandler<MotionLogEntry>? EntryAdded;

    public void Log(MotionLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        lock (_sync)
        {
            _entries.Add(entry);
        }

        EntryAdded?.Invoke(this, entry);
    }
}
