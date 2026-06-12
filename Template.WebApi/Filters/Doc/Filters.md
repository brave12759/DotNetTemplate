# Filters

## GlobalExceptionLogFilter

記錄 MVC action 內發生的例外與 request context，並交由回應包裝機制回傳一致的 API 格式。

## ResponseWrapperFilter

將 controller 回傳值包裝成 `ResponseMessage<T>`。若 controller 或 action 標示 `SkipResponseWrapAttribute`，則略過包裝。

## SkipResponseWrapAttribute

用於指定 controller 或 action 不套用回應包裝。

## RequirePermissionAttribute

用於指定 controller 或 action 需要特定功能權限。權限來源為 `Sys_UserRoleGroup`、`Sys_RoleGroupFunctionPermission` 與 `Sys_FunctionPermission`。

## Middleware 順序

目前 `Program.cs` pipeline：

1. `UseForwardedHeaders`
2. `UseSerilogRequestLogging`
3. `UseExceptionHandler`
4. `UseStaticFiles`
5. `UseCors`
6. `UseAuthentication`
7. `UseAuthorization`
8. `MapHealthChecks`
9. `MapSignalRInfrastructure`
10. `MapControllers`

HTTPS 由應用程式外部處理，例如 IIS、反向代理、Load Balancer 或 ingress。API 本身不負責 HTTPS redirect、HSTS 或應用程式層級憑證載入。
