using System.Diagnostics;
using System.IO;
using IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;

public sealed class LctM60MotionCardDriver : IMotionCardDriver
{
    public const string Key = "LctM60";

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
