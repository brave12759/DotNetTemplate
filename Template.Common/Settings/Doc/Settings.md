# Settings

`Template.Common/Settings` 只放仍由設定檔或環境變數讀取的應用程式設定。

HTTPS、HSTS、憑證載入與 JWT runtime 設定不在這裡管理：

- HTTPS 由站台或基礎設施層處理，例如 IIS 站台繫結、反向代理、Load Balancer 或 container ingress。
- JWT 設定存放在資料庫 `Sys_BasicSettings`，`Type = JwtSetting`。

## 設定區段

| 區段 | 類別 | 說明 |
|---|---|---|
| `ApiSettings` | `ApiSettings` | API 顯示設定 |
| `DatabaseSettings` | `DatabaseSettings` | 專案 DB 與 Log DB 連線字串 |
| `HashSettings` | `HashSettings` | PBKDF2 雜湊設定 |
| `LogSettings` | `LogSettings` | Serilog 檔案與等級設定 |
| `CryptographyKeySettings` | `CryptographyKeySettings` | AES/RSA 金鑰 |
| `TimeZoneSettings` | `TimeZoneSettings` | 應用程式時區 |
| `CorsSettings` | `CorsSettings` | CORS policy |
| `BackgroundQueueSettings` | `BackgroundQueueSettings` | 背景工作設定 |

## JWT 設定

JWT 設定透過資料庫與 API 管理：

- DB：`Sys_BasicSettings`，`Type = JwtSetting`
- API：`GET /JwtSetting`
- API：`PUT /JwtSetting`

必要 Key：

| Key | 說明 |
|---|---|
| `SecretKey` | JWT 簽章金鑰，至少 32 bytes |
| `Issuer` | JWT issuer |
| `Audience` | JWT audience |
| `PersonalTokenExpire` | 個人 Token 有效時間，單位分鐘 |
| `ServerTokenExpire` | SSO Server Token 有效時間，單位秒 |

初始資料可執行 `Template.DataAccess/Scripts/SeedJwtSettings.sql`。

JWT 設定 API 需要 `System.JwtSetting:Manage` 權限。權限建立 SQL 已整理在 `Template.BusinessRule/SsoService/Doc/SsoService.md` 的「既有資料庫啟用 SSO」章節，建立後請指派給正式環境的系統管理員角色群組。

## HTTPS

API 程式本身以 HTTP 執行。HTTPS 請在 hosting layer 啟用，例如站台綁定 443 並安裝 SSL 憑證。應用程式不負責 HTTPS redirect、HSTS 或 PFX 憑證載入。
