# LabelPrinter

ControlCode 标签打印客户端（Windows 系统托盘程序）。

接收 RMA 服务推送的 EPL 打印指令，通过 RAW 方式发送到本地标签机（Zebra / Eltron 等）。

## 功能

- WebSocket 监听 `LabelPrint` 消息
- REST `POST /LabelPrint` 本地接口
- 系统托盘常驻，支持开机自启
- 设置界面：打印机、WebSocket URL、LPT 端口

## 构建

```powershell
dotnet build -c Release
```

输出：`bin\Release\net8.0-windows\LabelPrinter.exe`

## 配置

编辑与 exe 同目录的 `appsettings.json`：

```json
{
  "LabelPrinter": {
    "PrinterName": "你的标签机名称",
    "LabelPrinterUrl": "ws://rma-host:2012/websocket",
    "EnableWebSocket": true,
    "EnableRestEndpoint": true,
    "RunAtStartup": false
  }
}
```

## 使用

1. 运行 `LabelPrinter.exe`
2. 在系统托盘（任务栏右下角 ^）找到图标
3. 双击打开设置，选择打印机并保存

## REST 测试

```powershell
Invoke-WebRequest -Uri "http://localhost:8721/LabelPrint" -Method POST -ContentType "text/plain" -Body "N`nA20,20,0,4,1,1,N,`"Test`"`nP1`n"
```
