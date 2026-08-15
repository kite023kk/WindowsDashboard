# Windows 桌面整理器

真正的 C# WPF Windows 桌面应用，不再依赖 HTML / 浏览器。

## 运行

```powershell
.\publish\WindowsDashboard.exe
```

默认进入 Desktop Mode：窗口通过 Win32 `SetParent` 挂载到桌面 `WorkerW` 层，并保持在 `SHELLDLL_DefView` 之下，因此层级为：

```text
壁纸
  -> Dashboard 卡片
    -> Windows 桌面图标
      -> 普通应用窗口
```

普通模式：

```powershell
.\publish\WindowsDashboard.exe --normal
```

## 构建与发布

```powershell
.\scripts\build.ps1
```

等价命令：

```powershell
dotnet build WindowsDashboard.csproj -c Release -r win-x64
dotnet publish WindowsDashboard.csproj -c Release -r win-x64 --self-contained false -o publish
```

输出：`publish\WindowsDashboard.exe`。

安装程序使用 Inno Setup，脚本为 `installer.iss`，安装 Inno Setup 6 后执行：

```powershell
iscc installer.iss
```

## 主要功能

- Desktop Mode / Normal Mode 切换
- 桌面小组件模式：紧凑小组件固定在桌面层，可拖动、可展开为完整面板
- WorkerW 桌面层嵌入，Explorer 重启后自动重新挂载
- 桌面、开始菜单快捷方式扫描，`.lnk` 通过 `IShellLink` 解析真实目标
- `SHGetFileInfo` 提取程序真实图标
- 开发 / AI / 工具 / 游戏 / 其他分类，可拖入快捷方式
- 卡片拖动、缩放、隐藏、删除、恢复
- 时钟、日历、系统监控、便签、Git 项目、Project 文件夹
- 系统托盘菜单
- `Ctrl + Shift + H` 全局隐藏 / 显示
- 开机启动注册
- `config.json` 持久化布局与分类

## 配置

配置文件位于：

```text
%APPDATA%\WindowsDashboard\config.json
```

布局按当前屏幕分辨率保存；多显示器可通过配置文件中的卡片百分比坐标适配。

## 验证

已通过：

- `dotnet build` 0 警告 0 错误
- `dotnet publish` 生成单文件 EXE
- EXE 实际启动成功
- Desktop Mode 下窗口父窗口为 `WorkerW`，位于桌面图标层
- Normal Mode 下创建真实独立窗口
