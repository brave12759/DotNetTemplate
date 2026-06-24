# Filters

## GlobalExceptionLogFilter

記錄 MVC action 內發生的例外與 request context，並交由回應包裝機制回傳一致的 API 格式。

## ResponseWrapperFilter

將 controller 回傳值包裝成 `ResponseMessage<T>`。若 controller 或 action 標示 `SkipResponseWrapAttribute`，則略過包裝。

預設行為：

| Action 回傳 | 實際輸出 |
|---|---|
| `Ok(data)` | `ResponseMessage<T>.Success(data, "Success")` |
| `BadRequest(error)` | `ResponseMessage<T>.Fail(error, "Bad Request")` |
| `NotFound(error)` | `ResponseMessage<T>.Fail(error, "Not Found")` |
| `ObjectResult` + 2xx status | success 包裝 |
| `ObjectResult` + 4xx/5xx status | fail 包裝 |

`ResponseWrapperFilter` 只處理 MVC action result，不改寫檔案下載、串流或已標示 `SkipResponseWrapAttribute` 的 action。若 action 本身已回傳 `ResponseMessage<T>`，也不會重複包裝。

Swagger response schema 由 `ResponseMessageOperationFilter` 統一補上預設 response，讓文件呈現 `ResponseMessage<T>` 外層結構。若個別 action 有特殊 status code，可在 action 上另外標示 `ProducesResponseType`。

回傳格式範例：

```json
{
  "data": {
    "id": 1,
    "userName": "Alice"
  },
  "message": "Success",
  "success": true
}
```

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
