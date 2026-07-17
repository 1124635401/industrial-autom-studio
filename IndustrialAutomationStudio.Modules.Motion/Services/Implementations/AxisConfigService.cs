using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class AxisConfigService : IAxisConfigService
{
    private readonly IAxisConfigRepository _repository;
    private readonly IMotionCardService _motionCardService;
    private readonly IAxisConfigValidator _validator;
    private readonly IMotionLogService _logService;

    public AxisConfigService(
        IAxisConfigRepository repository,
        IMotionCardService motionCardService,
        IAxisConfigValidator validator,
        IMotionLogService logService)
    {
        _repository = repository;
        _motionCardService = motionCardService;
        _validator = validator;
        _logService = logService;
    }

    public Task<IReadOnlyList<AxisConfig>> LoadAsync(
        CancellationToken cancellationToken = default) =>
        _repository.LoadAsync(cancellationToken);

    public async Task<IReadOnlyList<AxisConfig>> ScanAndMergeAsync(
        CancellationToken cancellationToken = default)
    {
        var local = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var scanned = await _motionCardService.ScanAxesAsync(cancellationToken).ConfigureAwait(false);
        var localByAddress = local.ToDictionary(axis => axis.Address);
        var scannedAddresses = scanned.Select(axis => axis.Address).ToHashSet();
        var merged = scanned.Select(axis =>
                localByAddress.TryGetValue(axis.Address, out var configured) ? configured : axis)
            .Concat(local.Where(axis => !scannedAddresses.Contains(axis.Address)))
            .OrderBy(axis => axis.Address.CardNo)
            .ThenBy(axis => axis.Address.AxisNo)
            .ToArray();

        Log(MotionLogLevel.Information, "扫描轴", $"发现 {scanned.Count} 根轴");
        return merged;
    }

    public AxisConfigValidationResult Validate(AxisConfig config) =>
        _validator.Validate(config);

    public async Task SaveAsync(
        AxisConfig config,
        CancellationToken cancellationToken = default)
    {
        EnsureValid(config);
        var configs = (await _repository.LoadAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(axis => axis.Address);
        configs[config.Address] = config;
        await _repository.SaveAsync(configs.Values.ToArray(), cancellationToken).ConfigureAwait(false);
        Log(MotionLogLevel.Information, "保存配置", $"轴 {config.Address.AxisNo} 保存成功");
    }

    public async Task SaveAllAsync(
        IReadOnlyCollection<AxisConfig> configs,
        CancellationToken cancellationToken = default)
    {
        foreach (var config in configs)
        {
            EnsureValid(config);
        }

        await _repository.SaveAsync(configs, cancellationToken).ConfigureAwait(false);
        Log(MotionLogLevel.Information, "保存配置", $"保存 {configs.Count} 根轴");
    }

    public Task<AxisConfig> ReadFromCardAsync(
        AxisAddress address,
        CancellationToken cancellationToken = default) =>
        _motionCardService.ReadAxisConfigAsync(address, cancellationToken);

    public async Task<AxisConfig> WriteToCardAsync(
        AxisConfig config,
        CancellationToken cancellationToken = default)
    {
        if (!_motionCardService.IsConnected)
        {
            throw new InvalidOperationException("运动控制卡尚未连接，不能写入轴参数。");
        }

        EnsureValid(config);
        await _motionCardService.WriteAxisConfigAsync(config, cancellationToken).ConfigureAwait(false);
        var readBack = await _motionCardService.ReadAxisConfigAsync(config.Address, cancellationToken)
            .ConfigureAwait(false);
        Log(MotionLogLevel.Information, "写入控制卡", $"轴 {config.Address.AxisNo} 写入成功");
        return readBack;
    }

    private void EnsureValid(AxisConfig config)
    {
        var result = _validator.Validate(config);
        if (!result.IsValid)
        {
            throw new AxisConfigValidationException(result);
        }
    }

    private void Log(MotionLogLevel level, string operation, string result) =>
        _logService.Log(new MotionLogEntry(
            DateTimeOffset.Now,
            level,
            "AxisConfig",
            "Axes",
            operation,
            result));
}
