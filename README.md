# Keyboard Speed YOKONEX

一个面向 Windows 桌面的键盘测速与蓝牙 EMS 联动工具。程序会全局监听键盘输入，计算实时键速，并根据触发模式、速度区间规则或空闲超时提醒来驱动已连接的 YOKONEX / EMS 设备输出波形。

## 当前能力

- 全局键盘监听与实时键速计算
- 蓝牙 BLE 设备扫描、连接、状态刷新与停止输出
- 按键即触发、键速规则触发、空闲超时触发
- 内置波形库与自定义波形脚本编辑
- 波形预览、波形拖拽调整、规则绑定
- 悬浮遥测小窗，实时展示连接状态、波形与通道强度
- 配置持久化与运行日志输出

## 技术栈

- `.NET 9`
- `WPF`
- `Windows BLE API`
- `xUnit`

## 项目结构

```text
src/
  KeyboardSpeed.Core/               业务模型、规则、波形、配置、诊断
  KeyboardSpeed.Input.Windows/      Windows 全局键盘输入监听
  KeyboardSpeed.Bluetooth.Windows/  Windows BLE 连接与 EMS 发包
  KeyboardSpeed.Desktop/            WPF 桌面界面与运行时编排
tests/
  KeyboardSpeed.Tests/              单元测试与布局测试
docs/
  superpowers/specs/                设计文档
  superpowers/plans/                实施计划
```

## 环境要求

- Windows 10 / 11
- .NET SDK `9.0`
- 支持 BLE 的本机蓝牙环境

## 本地开发

恢复、构建：

```powershell
dotnet restore .\KeyboardSpeed-YOKONEX.slnx
dotnet build .\KeyboardSpeed-YOKONEX.slnx
```

运行测试：

```powershell
dotnet test .\tests\KeyboardSpeed.Tests\KeyboardSpeed.Tests.csproj
```

启动桌面程序：

```powershell
dotnet run --project .\src\KeyboardSpeed.Desktop\KeyboardSpeed.Desktop.csproj
```

## 发布

常规发布：

```powershell
dotnet publish .\src\KeyboardSpeed.Desktop\KeyboardSpeed.Desktop.csproj -c Release
```

`win-x64` 自包含单文件发布：

```powershell
dotnet publish .\src\KeyboardSpeed.Desktop\KeyboardSpeed.Desktop.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
```

仓库内约定的发布产物通常输出到：

- `artifacts/`

GitHub 自动发版：

- 推送符合 `v*` 的 tag 会触发 GitHub Action
- Action 会自动执行测试、发布 `win-x64` 自包含单文件版本、打包 zip
- 然后自动创建 GitHub Release 并上传产物

示例：

```powershell
git tag v0.1.0
git push origin v0.1.0
```

## 配置与日志

应用设置文件：

- `%AppData%\KeyboardSpeed-YOKONEX\app-settings.json`

运行日志：

- `<程序目录>\logs\debug.log`

## 触发模式说明

- `键速规则触发`
  - 根据总输入字符数和总耗时计算字符/分钟，命中规则区间并触发对应波形
- `按键即触发`
  - 每次有效按键直接触发所选波形
- `空闲超时触发`
  - 在指定时间内没有新的有效按键时触发空闲提醒波形

## 内置默认行为

- 默认规则冷却时间：`600ms`
- 默认空闲超时：`2000ms`
- 默认空闲提醒波形：`idle-jolt / 高压警醒`
- 当本地已保存波形缺少新内置波形时，启动时会自动补齐

## 已知限制

- 当前输入联动以键盘为主，数位板支持仍处于设计与计划阶段
- BLE 行为依赖目标设备协议与 Windows 蓝牙环境
- 全局数位板 Windows Ink 方案若要落地，后续可能涉及额外的系统权限与发布约束

## 文档

- 设计文档：`docs/superpowers/specs/`
- 实施计划：`docs/superpowers/plans/`

## 许可证

当前仓库未单独附带许可证文件；如需对外分发，请先补充明确的许可证声明。
