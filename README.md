# IndustrialAutomationStudio

基于 .NET 10、WPF、Prism 9 和 DryIoc 的工业自动化调试平台。当前包含独立调试宿主、可嵌入的 Motion 模块、Mock Driver、JSON 配置持久化和自动化测试。

## 技术要求

- Windows
- .NET SDK 10
- Prism.Wpf 9.0.537
- Prism.DryIoc 9.0.537

Prism 9 使用社区或商业许可证，使用者需要自行确认并遵守当前 Prism 许可条款。

## 仓库结构

项目采用平铺目录，项目文件夹直接位于解决方案根目录：

```text
IndustrialAutomationStudio.slnx
├─ IndustrialAutomationStudio.App
├─ IndustrialAutomationStudio.Modules.Motion
└─ IndustrialAutomationStudio.Modules.Motion.Tests
```

测试项目用于开发期验证，不会被 App 引用，也不参与正式程序发布。

## 构建与测试

```powershell
dotnet restore IndustrialAutomationStudio.slnx
dotnet test IndustrialAutomationStudio.slnx --no-restore -m:1
dotnet build IndustrialAutomationStudio.slnx --no-restore -m:1
```

当前环境中，解决方案级测试和构建使用 `-m:1`，避免 WPF 构建的并行 MSBuild 节点挂起。

## 运行

```powershell
dotnet run --project IndustrialAutomationStudio.App/IndustrialAutomationStudio.App.csproj
```

默认配置目录：

```text
%LocalAppData%\IndustrialAutomationStudio\Motion\Configs
```

首次运行时，模块从程序集内置默认值创建 `MotionCardConfig.json` 和 `AxisConfig.json`。

## 轴配置页行为

轴配置页是纯离线、静态配置编辑器，`AxisConfig.json` 是配置事实来源：

- 页面加载配置文件，不扫描控制卡，也不会自动将参数写入控制卡。
- 添加和删除用于在未连接控制卡时维护轴，或补充未扫描到的轴。
- 修改、添加、删除和导入只改变内存；必须点击“保存”才写入默认配置。
- “刷新”恢复到最近一次成功加载或保存的内存快照。
- “导入”读取外部 JSON 但不自动覆盖默认配置；“导出”只写用户选择的目标文件。
- 保存采用临时文件校验和替换，并为已有目标生成 `.bak` 备份。
- 硬件扫描属于连接/诊断流程，不合并、不覆盖 `AxisConfig.json`。

页面严格显示以下 18 个字段：

`CardNo`、`AxisNo`、`AxisName`、`AxisType`、`GearRatio`、`Resolution`、`MaxVelocity`、`Acceleration`、`Deceleration`、`STime`、`InPositionError`、`JogReverse`、`HomeAcceleration`、`HomeVelocity1`、`HomeVelocity2`、`HomeMode`、`TxPdoStart`、`RxPdoStart`。

领域模型中其他兼容字段不会显示，但编辑现有轴时会原样保留。

## 在其他项目中引用

宿主项目使用 .NET 10 和 Prism 9。引用 `IndustrialAutomationStudio.Modules.Motion` 后注册模块选项和模块：

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterInstance(new MotionModuleOptions
    {
        HostRegionName = "MainContentRegion",
        WorkspaceRegionName = MotionRegionNames.WorkspaceContent,
        DefaultDriverKey = "Mock"
    });
}

protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<MotionModule>();
}
```

打开完整工作区：

```csharp
regionManager.RequestNavigate(
    "MainContentRegion",
    MotionNavigationNames.Workspace);
```

也可直接导航到 `MotionNavigationNames.AxisConfig`，复用同一套配置逻辑、主题、对话框和调试 UI。

## 新增 Driver

1. 在 `Hardware/Drivers/<Vendor>` 中实现 `IMotionCardDriver`。
2. 实现对应的 `IMotionCardDriverFactory`，提供稳定且唯一的 `DriverKey`。
3. 在 `MotionModule.RegisterTypes` 中注册工厂。
4. 将 `MotionCardConfig.DriverKey` 设置为新的 DriverKey。

View、ViewModel 和通用 Service 不引用厂商 SDK。多个 SDK 如有版本冲突，可拆为独立 Driver 项目，接口与调用链保持不变。
