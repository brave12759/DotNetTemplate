# 設定類別說明文件

[← 返回 README](../../../README.md) ｜ [← 返回 Template.Common](../../README.md)

所有設定類別位於 `Template.Common/Settings/`，在 `Program.cs` 啟動時由 `IConfiguration` 讀入，並以 `AddSingleton` 或 `Configure<T>` 兩種方式注入。

---

## 各設定類別一覽

| 類別 | SectionName | 說明 |
|---|---|---|
| ApiSettings | ApiSettings | API 基本資訊（名稱） |
| DatabaseSettings | DatabaseSettings | 資料庫連線字串 |
| HashSettings | HashSettings | PBKDF2 密碼雜湊設定 |
| HttpsSettings | HttpsSettings | HTTPS / HSTS / 憑證設定 |
| JwtSettings | JwtSettings | JWT 簽章與 Token 生命週期 |
| LogSettings | LogSettings | Serilog 檔案輸出設定 |
| CryptographyKeySettings | CryptographyKeySettings | AES / RSA 金鑰設定 |
| TimeZoneSettings | TimeZoneSettings | 應用程式時區 |
| CorsSettings | CorsSettings | CORS 跨域存取設定 |
| BackgroundQueueSettings | BackgroundQueueSettings | 背景工作佇列設定 |

---

## ApiSettings

```json
"ApiSettings": {
  "Name": "Template API"
}
```

| 欄位 | 說明 |
|---|---|
| Name | Swagger 頁面顯示的 API 名稱 |

---

## DatabaseSettings

```json
"DatabaseSettings": {
  "ProjectConnectionString": "",
  "LogConnectionString": ""
}
```

| 欄位 | 說明 |
|---|---|
| ProjectConnectionString | 主業務資料庫（`ProjectDbContext`） |
| LogConnectionString | 日誌資料庫（`LogDbContext`，含 TokenRevocation 資料表）；空值時 Token 撤銷降級為 In-Memory |

---

## HashSettings

```json
"HashSettings": {
  "Iterations": 100000
}
```

| 欄位 | 預設值 | 說明 |
|---|---|---|
| Iterations | 100000 | PBKDF2 迭代次數，最低需 10000，正式環境建議 ≥ 100000 |

---

## HttpsSettings

```json
"HttpsSettings": {
  "EnforceHttps": true,
  "RedirectStatusCode": 307,
  "HstsEnabled": true,
  "HstsMaxAgeDays": 180,
  "HstsIncludeSubDomains": true,
  "HstsPreload": false,
  "CertificatePath": "",
  "CertificatePassword": ""
}
```

| 欄位 | 預設值 | 說明 |
|---|---|---|
| EnforceHttps | true | 啟用 HTTPS Redirection 與 HSTS |
| RedirectStatusCode | 307 | HTTP 轉 HTTPS 的狀態碼（307 暫時、308 永久） |
| HstsEnabled | true | 啟用 HSTS（僅非 Development 生效） |
| HstsMaxAgeDays | 180 | HSTS 快取天數 |
| HstsIncludeSubDomains | true | 是否套用至子網域 |
| HstsPreload | false | 是否允許加入瀏覽器 HSTS Preload 清單（慎開） |
| CertificatePath | 空 | PFX 憑證路徑；空值時使用預設 dev cert 或由反向代理終止 TLS |
| CertificatePassword | 空 | PFX 憑證密碼 |

---

## JwtSettings

```json
"JwtSettings": {
  "SecretKey": "",
  "Issuer": "Template",
  "Audience": "Template",
  "ExpiresMinutes": 60
}
```

| 欄位 | 說明 |
|---|---|
| SecretKey | HMAC-SHA256 簽章金鑰（至少 32 字元），**請勿寫入版本控制** |
| Issuer | 核發者，Token 驗證時必須匹配 |
| Audience | 受眾，Token 驗證時必須匹配 |
| ExpiresMinutes | Token 有效分鐘數，Development 可調高（如 480）方便開發 |

---

## LogSettings

```json
"LogSettings": {
  "LogDirectory": "Logs",
  "FileSizeLimitMb": 50,
  "RetainedFileCountLimit": 30,
  "MinimumLevel": "Warning"
}
```

| 欄位 | 預設值 | 說明 |
|---|---|---|
| LogDirectory | Logs | 日誌資料夾，支援相對路徑（相對於 `AppContext.BaseDirectory`）與絕對路徑 |
| FileSizeLimitMb | 50 | 單一日誌檔大小上限（MB），超過則滾動 |
| RetainedFileCountLimit | 30 | 最多保留幾個日誌檔案 |
| MinimumLevel | Warning | 最低記錄等級：Verbose / Debug / Information / Warning / Error / Fatal |

---

## CryptographyKeySettings

```json
"CryptographyKeySettings": {
  "SymmetricKeyBase64": "",
  "SymmetricIvBase64": "",
  "RsaPublicKeyPem": "",
  "RsaPrivateKeyPem": ""
}
```

| 欄位 | 說明 |
|---|---|
| SymmetricKeyBase64 | AES 對稱金鑰（Base64 編碼，原始長度 16 / 24 / 32 bytes） |
| SymmetricIvBase64 | AES IV（Base64 編碼，原始長度 16 bytes） |
| RsaPublicKeyPem | RSA 公鑰（PEM 格式，用於加密） |
| RsaPrivateKeyPem | RSA 私鑰（PEM 格式，用於解密），**請勿寫入版本控制** |

---

## TimeZoneSettings

```json
"TimeZoneSettings": {
  "TimeZoneId": "Asia/Taipei"
}
```

| 欄位 | 說明 |
|---|---|
| TimeZoneId | IANA 時區 ID（Linux / macOS 使用），Windows 亦相容。此值會被 `TimeZoneInfo.FindSystemTimeZoneById` 解析，並注入為全域 `TimeZoneInfo` 使用於 JSON DateTime 序列化 |

---

## DI 注入方式（Program.cs）

所有設定類別在讀取後以 `AddSingleton(instance)` 直接注入，可在 Service / Filter / Middleware 中透過建構子直接注入使用，無需 `IOptions<T>` 包裝。

---

## CorsSettings

```json
"CorsSettings": {
  "AllowAnyOrigin": false,
  "AllowedOrigins": [],
  "AllowCredentials": false
}
```

| 欄位 | 預設値 | 說明 |
|---|---|---|
| AllowAnyOrigin | false | 允許任何來源，开發環境便利用。正式環境建議設為 false 並列出 AllowedOrigins |
| AllowedOrigins | [] | 允許的來源清單，例如 `["https://example.com"]`。AllowAnyOrigin=false 且此清單為空時，不允許任何跨域 |
| AllowCredentials | false | 允許請求攜帶 Cookie / Authorization Header。不可與 AllowAnyOrigin=true 同用 |

> **安全提示**：`AllowCredentials=true` 要求指定明確的 Origins，無法搭配 `AllowAnyOrigin=true`（浏覽器會拒絕）。

**Development appsettings.Development.json 建議設定：**
```json
"CorsSettings": {
  "AllowAnyOrigin": true,
  "AllowedOrigins": [],
  "AllowCredentials": false
}
```

---

## BackgroundQueueSettings

```json
"BackgroundQueueSettings": {
  "Enabled": true,
  "DefaultPollingIntervalSeconds": 5,
  "DefaultLockTimeoutSeconds": 300,
  "DefaultMaxRetryCount": 3,
  "ShutdownTimeoutSeconds": 30,
  "Workers": []
}
```

| 欄位 | 預設值 | 說明 |
|---|---|---|
| Enabled | true | 是否啟用背景佇列 worker |
| DefaultPollingIntervalSeconds | 5 | 預設輪詢間隔秒數 |
| DefaultLockTimeoutSeconds | 300 | 預設工作鎖定秒數，超過後可重新取得 |
| DefaultMaxRetryCount | 3 | 預設最大重試次數 |
| ShutdownTimeoutSeconds | 30 | 應用程式停止時等待背景工作完成的秒數 |
| Workers | [] | 各 `BackgroundWorkType` 的 worker 設定，`WorkType` 請填 enum 對應的 int 值 |

此設定會由 `Program.cs` 讀取後交給 `AddBusinessRuleServices` 統一註冊背景工作佇列基礎設施。詳細使用方式請參考 [BackgroundQueue.md](../../BackgroundQueue/Doc/BackgroundQueue.md)。
