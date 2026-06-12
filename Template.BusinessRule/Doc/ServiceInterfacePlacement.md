# BusinessRule 服務介面放置規則

業務流程相關介面應放在對應的 BusinessRule 模組內，不應放在 `Template.Common`。

`Template.Common` 只保留跨層共用、且不綁定特定業務流程的介面或模型。若介面的實作位於 BusinessRule，介面也應一起放在 BusinessRule 對應模組，避免 Common 反向承載業務邏輯契約。

## 目前放置位置

| 功能 | 介面 | 實作 |
|---|---|---|
| 使用者管理 | `Template.BusinessRule/UserService/Services/IUserService.cs` | `Template.BusinessRule/UserService/Services/UserService.cs` |
| 登入登出 | `Template.BusinessRule/LoginService/Services/ILoginService.cs` | `Template.BusinessRule/LoginService/Services/LoginService.cs` |
| 部門管理 | `Template.BusinessRule/DepartmentService/Services/IDepartmentService.cs` | `Template.BusinessRule/DepartmentService/Services/DepartmentService.cs` |
| 加解密 | `Template.BusinessRule/CryptographyService/Services/ICryptographyService.cs` | `Template.BusinessRule/CryptographyService/Services/CryptographyService.cs` |
| 密碼管理 | `Template.BusinessRule/PasswordManager/Services/IPasswordManager.cs` | `Template.BusinessRule/PasswordManager/Services/PasswordManager.cs` |
| Token 撤銷 | `Template.BusinessRule/TokenRevocationService/Services/ITokenRevocationService.cs` | `Template.BusinessRule/TokenRevocationService/Services` |
| SSO | `Template.BusinessRule/SsoService/Services/ISsoService.cs` | `Template.BusinessRule/SsoService/Services/SsoService.cs` |

## Common 保留介面

`Template.Common/Services` 目前只保留 WebApi 與 BusinessRule 都需要依賴、且不屬於特定業務模組的介面：

- `ICurrentUserService`
- `IJwtService`
