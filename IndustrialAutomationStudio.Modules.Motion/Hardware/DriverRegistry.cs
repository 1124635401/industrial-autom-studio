using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware;

public sealed class DriverRegistry
{
    private readonly IReadOnlyDictionary<string, IMotionCardDriverFactory> _factories;

    public DriverRegistry(IEnumerable<IMotionCardDriverFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);
        var map = new Dictionary<string, IMotionCardDriverFactory>(StringComparer.OrdinalIgnoreCase);
        foreach (var factory in factories)
        {
            if (!map.TryAdd(factory.DriverKey, factory))
            {
                throw new InvalidOperationException($"DriverKey '{factory.DriverKey}' 已重复注册。");
            }
        }

        _factories = map;
    }

    public IMotionCardDriverFactory Resolve(string driverKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driverKey);
        if (_factories.TryGetValue(driverKey, out var factory))
        {
            return factory;
        }

        throw new MotionDriverException(
            $"未找到 DriverKey '{driverKey}' 对应的运动控制卡驱动。",
            driverKey,
            "Resolve");
    }
}
