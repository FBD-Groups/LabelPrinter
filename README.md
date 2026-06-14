# LabelPrinter

ControlCode 标签打印客户端 —— Windows 系统托盘程序。

接收 RMA 服务推送的 EPL 打印指令，通过 RAW 方式发送到本地标签机（Zebra / Eltron 等），也支持 LPT 并口直连。

## 环境要求

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## 功能

| 功能 | 说明 |
|------|------|
| WebSocket 客户端 | 连接 RMA 服务，接收 `LabelPrint` 消息并打印 |
| REST 本地接口 | `POST /LabelPrint`，供本机脚本或其他程序调用 |
| 系统托盘 | 后台常驻，托盘图标显示 WebSocket 连接状态 |
| 设置界面 | 选择打印机、WebSocket 地址、LPT 端口等 |
| 开机自启 | 写入当前用户注册表 `Run` 项 |
| 自动重连 | WebSocket 断线后按配置间隔自动重试 |
| 日志 | 运行日志写入 `logs/labelprinter.log` |

## 架构

```
RMA Server (WebSocket)
        │
        ▼
┌───────────────────┐     REST POST /LabelPrint
│  LabelPrinter     │◄──────────────────────── 本地调用
│  (系统托盘)        │
└─────────┬─────────┘
          │ RAW / LPT
          ▼
   标签机 (Zebra / Eltron …)
```

## 快速开始

### 构建

```powershell
dotnet build -c Release
```

输出：`bin\Release\net8.0-windows\LabelPrinter.exe`

### 运行

1. 运行 `LabelPrinter.exe`（单实例，重复启动会提示已在托盘运行）
2. 在系统托盘（任务栏右下角 **^**）找到图标
3. 双击图标或右键 **设置…** 打开配置窗口
4. 选择打印机，填写 WebSocket 地址，点击 **保存**

设置窗口内可点击 **测试 EPL** 发送样张验证打印机是否正常。

## 配置

配置文件位于 exe 同目录的 `appsettings.json`，也可在设置界面修改后自动保存。

```json
{
  "LabelPrinter": {
    "LabelPrinterUrl": "ws://your-rma-host:2012/websocket",
    "PrinterName": "ZDesigner GK420t",
    "PrinterAlias": "",
    "UseLptPrinter": false,
    "LptPort": "LPT1",
    "RestListenPrefix": "http://localhost:8721/",
    "EnableRestEndpoint": true,
    "EnableWebSocket": true,
    "ReconnectDelaySeconds": 5,
    "WebSocketConnectTimeoutSeconds": 10,
    "RunAtStartup": false
  }
}
```

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `LabelPrinterUrl` | RMA WebSocket 地址 | `ws://localhost:2012/websocket` |
| `PrinterName` | Windows 打印机名称 | 空 |
| `PrinterAlias` | 消息中的打印机别名，用于路由到本机 | 空 |
| `UseLptPrinter` | 是否使用 LPT 并口而非 Windows 打印机 | `false` |
| `LptPort` | LPT 端口名，如 `LPT1` | `LPT1` |
| `RestListenPrefix` | REST 监听前缀（须以 `/` 结尾） | `http://localhost:8721/` |
| `EnableRestEndpoint` | 是否启用 REST 接口 | `true` |
| `EnableWebSocket` | 是否启用 WebSocket 客户端 | `true` |
| `ReconnectDelaySeconds` | WebSocket 断线重连间隔（秒） | `5` |
| `WebSocketConnectTimeoutSeconds` | WebSocket 连接超时（秒） | `10` |
| `RunAtStartup` | 是否开机自启 | `false` |

## 消息格式

### WebSocket

服务端推送文本消息，支持两种格式：

```
LabelPrint {epl 指令}
LabelPrint|{alias}|{epl 指令}
```

- 多条 EPL 任务可用空行分隔
- `alias` 与配置中的 `PrinterAlias` 匹配时，使用本机 `PrinterName` 打印

### REST

**端点：** `POST {RestListenPrefix}LabelPrint`

**方式一：纯文本 EPL**

```
Content-Type: text/plain

N
A20,20,0,4,1,1,N,"Test"
P1
```

**方式二：JSON**

```json
{
  "epl": "N\nA20,20,0,4,1,1,N,\"Test\"\nP1\n",
  "alias": "warehouse-1"
}
```

**响应：** `200 OK` / `400` / `500`，正文为纯文本。

## 测试

### REST（PowerShell）

```powershell
Invoke-WebRequest `
  -Uri "http://localhost:8721/LabelPrint" `
  -Method POST `
  -ContentType "text/plain" `
  -Body "N`nA20,20,0,4,1,1,N,`"Test`"`nP1`n"
```

### REST（JSON）

```powershell
$body = @{ epl = "N`nA20,20,0,4,1,1,N,`"Test`"`nP1`n" } | ConvertTo-Json
Invoke-WebRequest `
  -Uri "http://localhost:8721/LabelPrint" `
  -Method POST `
  -ContentType "application/json" `
  -Body $body
```

## 托盘菜单

| 菜单项 | 说明 |
|--------|------|
| 设置… | 打开配置窗口 |
| 重新连接 | 按当前配置重启 WebSocket / REST 服务 |
| 退出 | 关闭程序 |

托盘图标悬停提示显示 WebSocket 状态：`WS:已连接` / `WS:未连接` / `WS:off`。

## 项目结构

```
LabelPrinter/
├── Program.cs                 # 入口，单实例 Mutex
├── TrayApplicationContext.cs    # 系统托盘与生命周期
├── SettingsForm.cs              # 设置界面
├── Config.cs                    # appsettings.json 读写
├── PrintHostService.cs          # 打印服务编排
├── StartupRegistration.cs       # 开机自启注册表
├── Services/
│   ├── WebSocketPrintListener.cs
│   ├── RestPrintListener.cs
│   └── LabelPrintMessageParser.cs
└── Printing/
    ├── PrintModel.cs            # EPL 分块与打印调度
    ├── RawPrinterHelper.cs      # Windows RAW 打印
    └── LptPrinter.cs            # LPT 并口输出
```

## 常见问题

**REST 接口无法访问**

- 确认 `EnableRestEndpoint` 为 `true`
- 默认监听 `http://localhost:8721/`，仅本机可访问
- 若需其他机器访问，可修改 `RestListenPrefix` 为 `http://+:8721/` 并以管理员身份运行，或执行 `netsh http add urlacl`

**WebSocket 一直未连接**

- 检查 RMA 服务地址与端口
- 查看 `logs/labelprinter.log` 中的错误信息
- 托盘右键 **重新连接** 手动触发

**打印无输出**

- 在设置中确认已选择正确的 Windows 打印机名称
- 使用 **测试 EPL** 验证驱动与 RAW 打印是否正常
- LPT 模式需勾选 **使用 LPT** 并填写正确端口

## 许可证

ControlCode 内部使用。
