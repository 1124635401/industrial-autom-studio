using System.Diagnostics;
using System.IO;
using IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;

public sealed class LctM60MotionCardDriver : IMotionCardDriver
{
    public const string Key = "LctM60";
    private const int AlarmMask = 0x02;
    private const int PositiveLimitMask = 0x20;
    private const int NegativeLimitMask = 0x40;
    private const int ServoOnMask = 0x200;
    private const int MovingMask = 0x400;
    private const int InPositionMask = 0x800;
    private const int HomeSensorMask = 0x100000;

    private readonly MotionCardConfig _config;
    private readonly ILctM60NativeApi _native;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly string _nativeLibraryVersion;
    private readonly bool _is64BitProcess;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LctM60SlaveResource _resource;
    private bool _isOpen;
    private bool _isEtherCatConnected;
    private bool _disposed;

    public LctM60MotionCardDriver(MotionCardConfig config)
        : this(
            config,
            new LctM60NativeApi(),
            Task.Delay,
            GetNativeLibraryVersion(),
            Environment.Is64BitProcess)
    {
    }

    internal LctM60MotionCardDriver(
        MotionCardConfig config,
        ILctM60NativeApi native,
        Func<TimeSpan, CancellationToken, Task> delay,
        string nativeLibraryVersion,
        bool is64BitProcess)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(native);
        ArgumentNullException.ThrowIfNull(delay);
        _config = config;
        _native = native;
        _delay = delay;
        _nativeLibraryVersion = string.IsNullOrWhiteSpace(nativeLibraryVersion)
            ? "-"
            : nativeLibraryVersion;
        _is64BitProcess = is64BitProcess;
    }

    public int CardNo => _config.CardNo;
    public string DriverKey => Key;
    public bool IsConnected { get; private set; }
    public bool CanWriteDigitalOutputs => false;
    public bool CanControlMotion => true;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            if (IsConnected)
            {
                return;
            }

            ValidateConfiguration();
            var cardNo = checked((short)_config.CardNo);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Invoke("M_Open", () => _native.Open(cardNo, 0));
                _isOpen = true;

                Invoke(
                    "M_SetEmgInv",
                    () => _native.SetEmergencyInputInverted(
                        checked((short)_config.EmergencyInputInverted),
                        cardNo));
                Invoke(
                    "M_SetEmgAction",
                    () => _native.SetEmergencyAction(
                        checked((byte)_config.EmergencyAction),
                        cardNo));
                Invoke("M_ClrEmg", () => _native.ClearEmergency(cardNo));
                Invoke("M_LoadEni", () => _native.LoadEni(_config.EniFilePath, cardNo));
                Invoke("M_ResetFpga", () => _native.ResetFpga(cardNo));

                await _delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                Invoke("M_ConnectECAT", () => _native.ConnectEtherCat(1, cardNo));
                _isEtherCatConnected = true;

                await _delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                Invoke(
                    "M_LoadParamFromFile",
                    () => _native.LoadParameters(_config.SlaveParameterFilePath, cardNo));
                Invoke(
                    "M_GetSlaveResource",
                    () => _native.GetSlaveResource(out _resource, cardNo));
                ValidateResource();
                IsConnected = true;
            }
            catch
            {
                CleanupNative(throwOnError: false);
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_disposed)
            {
                CleanupNative(throwOnError: true);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MotionCardInfo> GetCardInfoAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("GetCardInfo");
            return new MotionCardInfo
            {
                CardNo = CardNo,
                CardName = "凌臣 M60",
                CardType = "EtherCAT",
                DriverKey = DriverKey,
                FirmwareVersion = _nativeLibraryVersion,
                AxisCount = _resource.AxisCount,
                DiCount = _resource.DigitalInputCount,
                DoCount = _resource.DigitalOutputCount
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AxisConfig>> ScanAxesAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("ScanAxes");
            return Enumerable.Range(1, _resource.AxisCount)
                .Select(axisNo => AxisConfig.CreateDefault(
                    new AxisAddress(CardNo, axisNo),
                    $"Axis{axisNo}"))
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AxisConfig> ReadAxisConfigAsync(
        AxisAddress address,
        CancellationToken cancellationToken = default)
    {
        var axes = await ScanAxesAsync(cancellationToken).ConfigureAwait(false);
        return axes.FirstOrDefault(axis => axis.Address == address)
            ?? throw Failure("ReadAxisConfig", $"未找到轴 {address.CardNo}:{address.AxisNo}。");
    }

    public Task WriteAxisConfigAsync(
        AxisConfig config,
        CancellationToken cancellationToken = default) =>
        Task.FromException(new NotSupportedException("LctM60 连接 Driver 暂不支持写入轴参数。"));

    public async Task<IoSnapshot> ReadIoSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("ReadIoSnapshot");
            return new IoSnapshot(
                new bool?[_resource.DigitalInputCount],
                new bool?[_resource.DigitalOutputCount]);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task WriteDigitalOutputAsync(
        int index,
        bool value,
        CancellationToken cancellationToken = default) =>
        Task.FromException(new NotSupportedException("LctM60 连接 Driver 暂不支持写入数字输出。"));

    public async Task<IReadOnlyList<AxisPulseState>> ReadAxisStatesAsync(
        IReadOnlyCollection<AxisAddress> addresses,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("ReadAxisStates");
            var states = new List<AxisPulseState>(addresses.Count);
            short emergencyStop = 0;
            Invoke(
                "M_GetEmg",
                () => _native.GetEmergencyStop(out emergencyStop, CardNumber));
            foreach (var address in addresses)
            {
                var axisNo = ValidateAddress(address, "ReadAxisStates");
                var status = 0;
                var command = 0d;
                var encoder = 0d;
                var velocity = 0d;
                Invoke(
                    "M_GetSts",
                    () => _native.GetStatus(axisNo, out status, 1, CardNumber));
                Invoke(
                    "M_GetCmd",
                    () => _native.GetCommandPosition(axisNo, out command, 1, CardNumber));
                Invoke(
                    "M_GetEncPos",
                    () => _native.GetEncoderPosition(axisNo, out encoder, 1, CardNumber));
                Invoke(
                    "M_GetCmdVel",
                    () => _native.GetCommandVelocity(axisNo, out velocity, 1, CardNumber));

                states.Add(new AxisPulseState
                {
                    Address = address,
                    CommandPulses = command,
                    ActualPulses = encoder,
                    VelocityPulsesPerSecond = velocity,
                    Alarm = HasFlag(status, AlarmMask),
                    PositiveLimit = HasFlag(status, PositiveLimitMask),
                    NegativeLimit = HasFlag(status, NegativeLimitMask),
                    ServoOn = HasFlag(status, ServoOnMask),
                    IsMoving = HasFlag(status, MovingMask),
                    InPosition = HasFlag(status, InPositionMask),
                    HomeSensor = HasFlag(status, HomeSensorMask),
                    EmergencyStop = emergencyStop != 0
                });
            }

            return states;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StartJogAsync(
        AxisAddress address,
        double velocityPulsesPerSecond,
        MotionProfile profile,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("StartJog");
            var axisNo = ValidateAddress(address, "StartJog");
            ValidateFiniteNonZero(velocityPulsesPerSecond, "Jog 速度", "StartJog");
            var parameters = ToNativeProfile(profile, "StartJog");
            Invoke("M_SetMove", () => _native.SetMove(axisNo, ref parameters, CardNumber));
            Invoke(
                "M_Jog",
                () => _native.Jog(axisNo, velocityPulsesPerSecond, CardNumber));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MoveAbsoluteAsync(
        AxisPulseTarget target,
        double velocityPulsesPerSecond,
        MotionProfile profile,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("MoveAbsolute");
            var axisNo = ValidateAddress(target.Address, "MoveAbsolute");
            ValidateFinitePositive(
                velocityPulsesPerSecond,
                "定位速度",
                "MoveAbsolute");
            var parameters = ToNativeProfile(profile, "MoveAbsolute");
            Invoke("M_SetMove", () => _native.SetMove(axisNo, ref parameters, CardNumber));
            Invoke(
                "M_AbsMove",
                () => _native.AbsoluteMove(
                    axisNo,
                    target.TargetPulses,
                    velocityPulsesPerSecond,
                    CardNumber));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MoveSynchronizedAsync(
        IReadOnlyList<AxisPulseTarget> targets,
        double accelerationPulsesPerSecondSquared,
        double velocityPulsesPerSecond,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targets);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("MoveSynchronized");
            if (targets.Count is < 2 or > 16)
            {
                throw Failure("MoveSynchronized", "同步定位仅支持 2 至 16 个轴。");
            }

            ValidateFinitePositive(
                accelerationPulsesPerSecondSquared,
                "同步加速度",
                "MoveSynchronized");
            ValidateFinitePositive(
                velocityPulsesPerSecond,
                "同步速度",
                "MoveSynchronized");

            var axes = targets
                .Select(target => ValidateAddress(target.Address, "MoveSynchronized"))
                .ToArray();
            if (axes.Distinct().Count() != axes.Length)
            {
                throw Failure("MoveSynchronized", "同步定位轴不能重复。");
            }

            Invoke(
                "M_Line_All",
                () => _native.LineAll(
                    checked((short)targets.Count),
                    axes,
                    targets.Select(target => target.TargetPulses).ToArray(),
                    accelerationPulsesPerSecondSquared,
                    velocityPulsesPerSecond,
                    CardNumber));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAxesAsync(
        IReadOnlyCollection<AxisAddress> addresses,
        MotionStopMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("StopAxes");
            if (addresses.Count == 0)
            {
                throw Failure("StopAxes", "至少需要选择一个停止轴。");
            }

            ulong mask = 0;
            foreach (var address in addresses)
            {
                var axisNo = ValidateAddress(address, "StopAxes");
                mask |= 1UL << (axisNo - 1);
            }

            var option = mode == MotionStopMode.Emergency ? (short)1 : (short)0;
            Invoke("M_Stop", () => _native.Stop(mask, option, CardNumber));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _disposed = true;
            _gate.Dispose();
        }
    }

    private static string GetNativeLibraryVersion()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ecat_motion.dll");
        return File.Exists(path)
            ? FileVersionInfo.GetVersionInfo(path).FileVersion ?? "-"
            : "-";
    }

    private void ValidateConfiguration()
    {
        if (!_is64BitProcess)
        {
            throw Failure("ValidateConfiguration", "LctM60 仅支持 x64 进程。");
        }

        if (_config.CardNo < 0 || _config.CardNo > short.MaxValue)
        {
            throw Failure("ValidateConfiguration", "板卡号必须在 0 到 32767 之间。");
        }

        if (_config.EmergencyInputInverted is not 0 and not 1)
        {
            throw Failure("ValidateConfiguration", "急停输入极性只能为 0 或 1。");
        }

        if (_config.EmergencyAction < byte.MinValue || _config.EmergencyAction > byte.MaxValue)
        {
            throw Failure("ValidateConfiguration", "急停动作必须在 0 到 255 之间。");
        }

        if (!File.Exists(_config.EniFilePath))
        {
            throw Failure("ValidateConfiguration", $"ENI 文件不存在：{_config.EniFilePath}");
        }

        if (!File.Exists(_config.SlaveParameterFilePath))
        {
            throw Failure(
                "ValidateConfiguration",
                $"从站参数文件不存在：{_config.SlaveParameterFilePath}");
        }
    }

    private void ValidateResource()
    {
        if (_resource.AxisCount < 0 ||
            _resource.DigitalInputCount < 0 ||
            _resource.DigitalOutputCount < 0)
        {
            throw Failure("M_GetSlaveResource", "板卡返回了无效的资源数量。");
        }
    }

    private short CardNumber => checked((short)CardNo);

    private short ValidateAddress(AxisAddress address, string operation)
    {
        if (address.CardNo != CardNo)
        {
            throw Failure(operation, $"轴 {address.CardNo}:{address.AxisNo} 不属于控制卡 {CardNo}。");
        }

        if (address.AxisNo is < 1 or > 64)
        {
            throw Failure(operation, "M60 轴号必须在 1 到 64 之间。");
        }

        return checked((short)address.AxisNo);
    }

    private LctM60CommandParameters ToNativeProfile(
        MotionProfile profile,
        string operation)
    {
        if (profile.STimeMilliseconds is < 0 or > 200)
        {
            throw Failure(operation, "S 曲线时间必须在 0 到 200 ms 之间。");
        }

        return new LctM60CommandParameters
        {
            Acceleration = ToNativePositiveInt(
                profile.AccelerationPulsesPerSecondSquared,
                "加速度",
                operation),
            Deceleration = ToNativePositiveInt(
                profile.DecelerationPulsesPerSecondSquared,
                "减速度",
                operation),
            STime = profile.STimeMilliseconds
        };
    }

    private int ToNativePositiveInt(double value, string name, string operation)
    {
        ValidateFinitePositive(value, name, operation);
        if (value > int.MaxValue)
        {
            throw Failure(operation, $"{name}超出原生接口范围。");
        }

        return checked((int)Math.Round(value, MidpointRounding.AwayFromZero));
    }

    private void ValidateFinitePositive(double value, string name, string operation)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw Failure(operation, $"{name}必须是有限正数。");
        }
    }

    private void ValidateFiniteNonZero(double value, string name, string operation)
    {
        if (!double.IsFinite(value) || value == 0)
        {
            throw Failure(operation, $"{name}必须是有限非零数。");
        }
    }

    private static bool HasFlag(int status, int mask) => (status & mask) != 0;

    private void Invoke(string operation, Func<short> action)
    {
        try
        {
            var result = action();
            if (result != 0)
            {
                throw Failure(
                    operation,
                    $"{operation} 执行失败：错误码 {result}（{LctM60ErrorCodes.Describe(result)}）。");
            }
        }
        catch (MotionDriverException)
        {
            throw;
        }
        catch (DllNotFoundException exception)
        {
            throw Failure(
                operation,
                "未找到 ecat_motion.dll 或其原生依赖项，请检查应用输出目录和凌臣驱动安装。",
                exception);
        }
        catch (BadImageFormatException exception)
        {
            throw Failure(
                operation,
                "ecat_motion.dll 与当前进程位数不匹配，应用和 DLL 必须均为 x64。",
                exception);
        }
        catch (EntryPointNotFoundException exception)
        {
            throw Failure(
                operation,
                "ecat_motion.dll 缺少所需入口点，当前 SDK 版本不匹配。",
                exception);
        }
    }

    private void CleanupNative(bool throwOnError)
    {
        if (!_isOpen && !_isEtherCatConnected)
        {
            IsConnected = false;
            _resource = default;
            return;
        }

        MotionDriverException? firstFailure = null;
        var cardNo = checked((short)_config.CardNo);

        if (_isEtherCatConnected)
        {
            try
            {
                Invoke("M_DisconnectECAT", () => _native.DisconnectEtherCat(cardNo));
            }
            catch (MotionDriverException exception)
            {
                firstFailure = exception;
            }
            finally
            {
                _isEtherCatConnected = false;
            }
        }

        if (_isOpen)
        {
            try
            {
                Invoke("M_Close", () => _native.Close(cardNo));
            }
            catch (MotionDriverException exception)
            {
                firstFailure ??= exception;
            }
            finally
            {
                _isOpen = false;
            }
        }

        IsConnected = false;
        _resource = default;
        if (throwOnError && firstFailure is not null)
        {
            throw firstFailure;
        }
    }

    private void EnsureConnected(string operation)
    {
        ThrowIfDisposed();
        if (!IsConnected)
        {
            throw Failure(operation, $"凌臣 M60 板卡 {CardNo} 尚未连接。");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private MotionDriverException Failure(
        string operation,
        string message,
        Exception? innerException = null) =>
        new(message, DriverKey, operation, CardNo, innerException);
}
