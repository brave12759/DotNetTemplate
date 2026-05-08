# Filters 說明

[← 返回 README](../../../README.md) ｜ [← 返回 Template.WebApi](../../README.md)

本資料夾放置 MVC Filter，作用範圍是 Controller / Action 執行流程。

## 1) GlobalExceptionLogFilter

檔案：GlobalExceptionLogFilter.cs

用途：
- 捕捉 MVC Action 內未處理例外。
- 寫入錯誤 Log（含 TraceId、路由、QueryString、UserId、TokenId、IP）。
- 回傳統一 500 格式：ResponseMessage<object>.Fail(...)

重點：
- 這是 MVC 層處理。
- 若例外發生在 MVC 之外（例如 middleware 早期階段），不會進到此 Filter。
- 已設定 Order = int.MinValue，確保在 Exception Filter 中最早執行。

## 2) ResponseWrapperFilter

檔案：ResponseWrapperFilter.cs

用途：
- 將 Action 回傳值統一包成 ResponseMessage<T>。
- 已是 ResponseMessage<T> 時不重複包裝。
- EmptyResult 會包成 ResponseMessage<object>.Success(null)

重點：
- 只影響 MVC Result。
- 可搭配 SkipResponseWrapAttribute 局部跳過。
- 已設定 Order = int.MaxValue，確保在 Result 階段最後統一包裝。

## 3) SkipResponseWrapAttribute

檔案：SkipResponseWrapAttribute.cs

用途：
- 標記在 Controller 或 Action 上，讓 ResponseWrapperFilter 跳過該端點。

適用情境：
- 檔案串流、第三方回呼、需維持原始回應格式的端點。

---

## Middleware 與 Filter 的關係

目前專案同時有 middleware 與 Filter 兩層錯誤處理：

- Program.cs 的 UseExceptionHandler：
  - 覆蓋整條 HTTP pipeline。
  - 捕捉 MVC 之外未處理例外。

- GlobalExceptionLogFilter：
  - 專注 MVC Action 層級例外與回應。

Program.cs 目前 pipeline 順序（重點）：

1. UseForwardedHeaders（先還原 X-Forwarded-Proto / X-Forwarded-For）
2. UseSerilogRequestLogging
3. UseExceptionHandler（全域例外）
4. UseHsts（僅非 Development）
5. UseHttpsRedirection
6. UseStaticFiles
7. UseAuthentication
8. UseAuthorization
9. MapControllers

HTTPS 重點：

- 專案以 HTTPS 為主，預設啟用 EnforceHttps。
- 若部署於反向代理，必須先 UseForwardedHeaders 再做 HSTS / HTTPS Redirection。
- 憑證由 HttpsSettings.CertificatePath / CertificatePassword 控制；未提供時 Development 可用本機 dev certificate。

建議：
- 保留兩者，達到完整覆蓋。
- 若要避免重複處理，維持「同一例外由最先接住的一層處理」即可。
