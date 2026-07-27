using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class MotionExecutionService(IMotionCardService cardService)
    : IMotionExecutionService
{
    public bool IsMotionAvailable => cardService.CanControlMotion;

    public async Task<IReadOnlyList<AxisState>> ReadAxisStatesAsync(
        IReadOnlyCollection<AxisConfig> axes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(axes);

        var configs = axes.ToDictionary(axis => axis.Address);
        var pulseStates = await cardService.ReadAxisStatesAsync(
            configs.Keys.ToArray(),
            cancellationToken).ConfigureAwait(false);

        return pulseStates
            .Where(state => configs.ContainsKey(state.Address))
            .Select(state => ToAxisState(configs[state.Address], state))
            .ToArray();
    }

    public async Task StartJogAsync(
        AxisConfig axis,
        int direction,
        double speed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(axis);
        if (direction is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                "Jog 方向必须是 -1 或 1。");
        }

        ValidateSpeed(speed);
        var scale = GetPulseScale(axis);
        var limitedSpeed = Math.Min(speed, PositiveOrThrow(
            axis.MaxVelocity,
            nameof(axis.MaxVelocity)));
        var velocity = limitedSpeed * scale * direction * NormalizeJogReverse(axis.JogReverse);

        EnsureMotionAvailable();
        ValidateAxisEnabled(axis);
        var state = Assert.SingleOrDefault(
            await cardService.ReadAxisStatesAsync([axis.Address], cancellationToken)
                .ConfigureAwait(false),
            axis.Address);
        ValidateReadyState(axis, state);
        var currentPosition = state.ActualPulses / scale;
        if ((velocity > 0 &&
             axis.PositiveSoftLimit is { } positive &&
             currentPosition >= positive) ||
            (velocity < 0 &&
             axis.NegativeSoftLimit is { } negative &&
             currentPosition <= negative))
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 已到达 Jog 方向软限位。");
        }

        if ((velocity > 0 && state.PositiveLimit) ||
            (velocity < 0 && state.NegativeLimit))
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 已触发 Jog 方向限位。");
        }

        await cardService.StartJogAsync(
            axis.Address,
            velocity,
            CreateProfile(axis, scale),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task MoveToPointAsync(
        AxisGroupConfig group,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        PositionPoint point,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(point);
        ValidateSpeed(point.Speed);

        if (!string.Equals(group.Id, point.GroupId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("点位不属于当前分组。");
        }

        var members = group.Members.ToDictionary(member => member.Address);
        var pointPositions = point.AxisPositions.ToDictionary(position => position.Address);
        if (members.Count == 0 ||
            !members.Keys.ToHashSet().SetEquals(pointPositions.Keys) ||
            !members.Keys.ToHashSet().SetEquals(axes.Keys))
        {
            throw new InvalidOperationException("点位、分组与轴配置的轴集合不一致。");
        }

        EnsureMotionAvailable();
        foreach (var axis in axes.Values)
        {
            ValidateAxisEnabled(axis);
        }

        var pulseStates = await cardService.ReadAxisStatesAsync(
            members.Keys.ToArray(),
            cancellationToken).ConfigureAwait(false);
        var states = pulseStates.ToDictionary(state => state.Address);
        if (!members.Keys.ToHashSet().SetEquals(states.Keys))
        {
            throw new InvalidOperationException("无法读取当前分组的完整轴状态。");
        }

        foreach (var axis in axes.Values)
        {
            ValidateReadyState(axis, states[axis.Address]);
            if (states[axis.Address].PositiveLimit ||
                states[axis.Address].NegativeLimit)
            {
                throw new InvalidOperationException($"轴 {axis.AxisName} 已触发硬限位。");
            }
        }

        var planned = members.Values
            .Select(member => PlanAxisMove(
                member,
                axes[member.Address],
                pointPositions[member.Address].Position,
                point.Speed))
            .ToArray();

        try
        {
            foreach (var move in planned.Where(move => IsRotary(move.Role)))
            {
                await cardService.MoveAbsoluteAsync(
                    move.Target,
                    move.Velocity,
                    move.Profile,
                    cancellationToken).ConfigureAwait(false);
            }

            var linear = planned.Where(move => !IsRotary(move.Role)).ToArray();
            if (linear.Length == 1)
            {
                var move = linear[0];
                await cardService.MoveAbsoluteAsync(
                    move.Target,
                    move.Velocity,
                    move.Profile,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (linear.Length > 1)
            {
                await cardService.MoveSynchronizedAsync(
                    linear.Select(move => move.Target).ToArray(),
                    linear.Min(move => move.Profile.AccelerationPulsesPerSecondSquared),
                    linear.Min(move => move.Velocity),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception motionException)
        {
            try
            {
                await cardService.StopAxesAsync(
                    members.Keys.ToArray(),
                    MotionStopMode.Emergency,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception stopException)
            {
                throw new MotionFailStopException(
                    "定位下发失败，且整组停止失败；必须保持操作锁定并检查设备。",
                    motionException,
                    stopException);
            }

            throw;
        }
    }

    public Task StopAsync(
        IReadOnlyCollection<AxisAddress> addresses,
        MotionStopMode mode = MotionStopMode.Smooth,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        return cardService.StopAxesAsync(addresses, mode, cancellationToken);
    }

    private static AxisState ToAxisState(AxisConfig config, AxisPulseState state)
    {
        var scale = GetPulseScale(config);
        return new AxisState
        {
            Address = state.Address,
            AxisName = config.AxisName,
            CommandPosition = state.CommandPulses / scale,
            ActualPosition = state.ActualPulses / scale,
            CurrentVelocity = state.VelocityPulsesPerSecond / scale,
            ServoOn = state.ServoOn,
            IsMoving = state.IsMoving,
            Alarm = state.Alarm,
            PositiveLimit = state.PositiveLimit,
            NegativeLimit = state.NegativeLimit,
            HomeSensor = state.HomeSensor,
            InPosition = state.InPosition,
            EmergencyStop = state.EmergencyStop
        };
    }

    private static PlannedAxisMove PlanAxisMove(
        AxisGroupMember member,
        AxisConfig axis,
        double position,
        double pointSpeed)
    {
        ValidateSoftLimits(axis, position);
        var scale = GetPulseScale(axis);
        var pulses = ToPulseTarget(position, scale);
        var velocity = Math.Min(
            pointSpeed,
            PositiveOrThrow(axis.MaxVelocity, nameof(axis.MaxVelocity))) * scale;

        return new PlannedAxisMove(
            member.Role,
            new AxisPulseTarget(axis.Address, pulses),
            velocity,
            CreateProfile(axis, scale));
    }

    private static MotionProfile CreateProfile(AxisConfig axis, double scale) => new(
        PositiveOrThrow(axis.Acceleration, nameof(axis.Acceleration)) * scale,
        PositiveOrThrow(axis.Deceleration, nameof(axis.Deceleration)) * scale,
        checked((int)Math.Round(axis.STime, MidpointRounding.AwayFromZero)));

    private static double GetPulseScale(AxisConfig axis)
    {
        if (string.Equals(axis.Unit, "pulse", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var scale = axis.Resolution * axis.GearRatio;
        return PositiveOrThrow(scale, "Resolution × GearRatio");
    }

    private static int ToPulseTarget(double position, double scale)
    {
        var pulses = position * scale;
        if (!double.IsFinite(pulses) ||
            pulses < int.MinValue ||
            pulses > int.MaxValue)
        {
            throw new InvalidOperationException("脉冲目标超出范围。");
        }

        return checked((int)Math.Round(pulses, MidpointRounding.AwayFromZero));
    }

    private static int NormalizeJogReverse(int value) => value < 0 ? -1 : 1;

    private static bool IsRotary(AxisRole role) => role is AxisRole.R or AxisRole.U;

    private static void ValidateSpeed(double speed)
    {
        _ = PositiveOrThrow(speed, "speed");
    }

    private void EnsureMotionAvailable()
    {
        if (!cardService.CanControlMotion)
        {
            throw new InvalidOperationException("运动控制卡未连接或当前驱动不支持运动控制。");
        }
    }

    private static void ValidateAxisEnabled(AxisConfig axis)
    {
        if (!axis.IsEnabled)
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 未启用。");
        }
    }

    private static void ValidateReadyState(AxisConfig axis, AxisPulseState state)
    {
        if (state.EmergencyStop)
        {
            throw new InvalidOperationException("控制卡急停已触发。");
        }

        if (!state.ServoOn)
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 伺服未使能。");
        }

        if (state.Alarm)
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 存在报警。");
        }

        if (state.IsMoving)
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 正在运动。");
        }
    }

    private static void ValidateSoftLimits(AxisConfig axis, double position)
    {
        if (!double.IsFinite(position))
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 的目标位置不是有限数值。");
        }

        if (axis.NegativeSoftLimit is { } negative &&
            (!double.IsFinite(negative) || position < negative))
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 的目标位置超出负软限位。");
        }

        if (axis.PositiveSoftLimit is { } positive &&
            (!double.IsFinite(positive) || position > positive))
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 的目标位置超出正软限位。");
        }

        if (axis.NegativeSoftLimit is { } minimum &&
            axis.PositiveSoftLimit is { } maximum &&
            minimum > maximum)
        {
            throw new InvalidOperationException($"轴 {axis.AxisName} 的软限位配置无效。");
        }
    }

    private static class Assert
    {
        public static AxisPulseState SingleOrDefault(
            IReadOnlyList<AxisPulseState> states,
            AxisAddress expectedAddress)
        {
            if (states.Count != 1 || states[0].Address != expectedAddress)
            {
                throw new InvalidOperationException("无法读取 Jog 轴的实时状态。");
            }

            return states[0];
        }
    }

    private static double PositiveOrThrow(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new InvalidOperationException($"{name} 必须是有限正数。");
        }

        return value;
    }

    private sealed record PlannedAxisMove(
        AxisRole Role,
        AxisPulseTarget Target,
        double Velocity,
        MotionProfile Profile);
}
