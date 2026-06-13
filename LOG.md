# PUBG 地图标记叠加层 — 开发日志

## 项目简介

PUBG（绝地求生）游戏辅助工具，在游戏全屏/无边框窗口上叠加透明图层，显示地图关键地点标记（密室、熊洞、撬棍房、钥匙房、补给点等），支持 7 张地图切换、快捷键控制、系统托盘集成。

---

## 轮次记录

### 第 1 轮：初始需求与方向选择

**需求**：在 PUBG 游戏运行时显示额外地图图层（Overlay），将密室、熊洞、撬棍房等坐标叠加到游戏地图界面中，并提供开关控制。

**建议方案**：C# .NET WinForms 透明覆盖层（`WS_EX_LAYERED | WS_EX_TRANSPARENT`），低层键盘钩子（`WH_KEYBOARD_LL`）实现全局热键。

**用户选择**：先做一个屏幕中心红点准心叠加层作为基础验证。

---

### 第 2 轮：屏幕中心红点（C# 方案）

- 创建 `Crosshair.cs`，基于 .NET 4.0 WinForms
- 窗口设置为 `TopMost`、`FormBorderStyle.None`、`TransparencyKey = Color.Black`
- 通过 `WS_EX_LAYERED | WS_EX_TRANSPARENT` 实现点击穿透
- GDI+ 绘制红色圆点（`SmoothingMode.None`，避免抗锯齿导致黑边）
- 全局热键 `` ` `` / `F2` 切换显示/隐藏
- **编译命令**：`csc.exe /target:winexe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll Crosshair.cs`

---

### 第 3 轮：尝试 AutoHotkey 方案

用户要求切换到 AutoHotkey 方案。经过多次尝试（GDI+ DIBSection、WinSetTransColor、圆形窗口区域）均因 AHK 安装/配置问题无法运行。**最终放弃 AHK，回到 C# 方案**。

---

### 第 4 轮：样式优化

- 去掉红点周围的黑圈 — 设置 `SmoothingMode.None` 消除抗锯齿
- 去掉光晕效果，只保留纯红色实心圆点

---

### 第 5 轮：系统托盘集成

- 添加 `NotifyIcon` 在系统托盘中显示红色图标
- 窗口启动时默认隐藏（`SetLayeredWindowAttributes` alpha = 0）
- 托盘图标提示文字显示当前状态

---

### 第 6 轮：右键菜单

- 托盘图标右键弹出菜单，包含「显示/隐藏」和「退出」
- **首次问题**：菜单被任务栏遮挡 — 使用 `ContextMenuStrip` 原生弹出修复
- **二次问题**：点击其他区域菜单不消失 — 改为使用 `trayIcon.ContextMenuStrip = trayMenu` 原生绑定 + `Opening` 事件更新菜单文字

---

### 第 7 轮：地图标记点系统

- 在 `img/` 目录放置 7 张参考图片（带红点标记）
- 编写 `scan.cs` 扫描图片红点坐标（R>200, G<100, B<100）
- **艾伦格（Erangel）**：15 个标记点
- **米拉玛（Miramar）**：15 个标记点
- **泰戈（Taego）**：15 个标记点
- **维寒迪（Vikendi）**：10 个标记点 → 后扩展为 39 个（含多色分类）
- **帕拉莫（Paramo）**：8 个标记点
- **帝斯顿（Deston）**：24 个标记点 → 后修正为 25 个
- **荣都（Rondo）**：15 个标记点

---

### 第 8 轮：地图切换与显示

- `←` `→` 键切换地图
- 图片居中显示，等比缩放（`Math.Min(sw/imgW, sh/imgH)`）
- 标记点按原始像素坐标等比例换算屏幕位置
- 切换时屏幕中央显示 1 秒地图中文名称（半透明样式）
- 托盘图标文字同步更新当前地图名

---

### 第 9 轮：热键可靠性修复

- **问题**：点击 BitFun（Electron 应用）后热键失效
- **原因**：`RegisterHotKey` + `WndProc` 和 `IMessageFilter` 均依赖消息队列，Electron 拦截输入
- **解决**：改用 `WH_KEYBOARD_LL` 低层键盘钩子，在系统层面捕获按键，早于任何应用的消息处理
- **额外修复**：使用 `SetLayeredWindowAttributes`（alpha 0/255）替代 `Show()`/`Hide()` 解决窗口句柄失效问题

---

### 第 10 轮：地图池（多选过滤）

- 在右键菜单中添加「地图池」子菜单
- 每张地图前带复选框（默认全选）
- 只勾选的地图才会进入 `←` `→` 切换循环
- 取消勾选当前显示的地图时自动跳转到下一个勾选的地图
- 点击复选框不关闭菜单，支持连续勾选

---

### 第 11 轮：游戏截图校准坐标

- 用户提供 `re.png`（1920×1080 全屏游戏截图）
- 扫描 `re.png` 红点位置，与现有坐标映射结果对比
- **结论**：所有地图坐标与图片一致（误差 ≤1px），**艾伦格**15 个点验证通过
- **发现并修复**：**帝斯顿**第 17 个点偏差较大 `(832,580)→(847,573)`
- 使用图片扫描工具（`verify.cs`）批量验证 7 张地图的坐标准确性

---

### 第 12 轮：维寒迪多色标记（密室/撬棍房/熊洞）

- 用户创建 3 张独立参考图：`维寒迪密室.png`、`维寒迪撬棍房.png`、`维寒迪熊洞.png`
- 每张图以红色标记点表示对应类型，程序分别赋予不同颜色
- **MapData** 新增 `DotTypes` 字段（`byte[]`，0=红/密室, 1=紫/撬棍房, 2=绿/熊洞）
- **OnPaint** 绘制逻辑改为根据类型选择颜色：
  - 🔴 红色 `(255,40,40)` — 密室
  - 🟣 紫色 `(160,40,200)` — 撬棍房
  - 🟢 绿色 `(40,200,40)` — 熊洞
- 维寒迪标记总数：**39 个**（10 密室 + 19 撬棍房 + 10 熊洞）
- 其他地图保持全部红色标记，`DotTypes` 为 `null`

---

### 第 13 轮：坐标文件导出

- 创建 `Coordinate/` 目录，导出 7 个可读文本文件
- 每行格式：`序号. (X, Y) [颜色-类型]`
- 按 Y 坐标排序，UTF-8 编码
- 维寒迪文件标注了每点的类型（密室/撬棍房/熊洞）
- 附带 `说明.txt` 描述文件格式

---

## 技术架构

| 组件 | 方案 |
|------|------|
| 语言 | C#，.NET 4.0 Framework |
| 编译器 | `csc.exe`（C:\Windows\Microsoft.NET\Framework\v4.0.30319\） |
| 窗口样式 | 无边框、全屏、置顶、`TransparencyKey = Black` |
| 点击穿透 | `WS_EX_LAYERED \| WS_EX_TRANSPARENT` |
| 透明度控制 | `SetLayeredWindowAttributes`（alpha 0=隐藏, 255=显示） |
| 全局热键 | `WH_KEYBOARD_LL` 低层键盘钩子 |
| 图形绘制 | GDI+，`SmoothingMode.None` |
| 系统托盘 | `NotifyIcon` + `ContextMenuStrip` |

## 快捷键

| 按键 | 功能 |
|------|------|
| `` ` `` / `F2` | 切换标记显示/隐藏 |
| `←` | 上一张地图 |
| `→` | 下一张地图 |

## 文件结构

```
├───────────────
├── Crosshair.cs          # 主程序源码（~455 行）
├── Crosshair.exe         # 编译输出
├── LOG.md                # 本文件
├── Coordinate/           # 坐标导出文件
│   ├── 说明.txt
│   ├── 艾伦格.txt
│   ├── 米拉玛.txt
│   ├── 泰戈.txt
│   ├── 维寒迪.txt
│   ├── 帕拉莫.txt
│   ├── 帝斯顿.txt
│   └── 荣都.txt
└── img/
    ├── 艾伦格.png
    ├── 米拉玛.png
    ├── 泰戈.png
    ├── 维寒迪.png
    ├── 维寒迪密室.png
    ├── 维寒迪撬棍房.png
    ├── 维寒迪熊洞.png
    ├── 帕拉莫.png
    ├── 帝斯顿.png
    ├── 荣都.png
    └── re.png             # 游戏截图（校准用）
```
