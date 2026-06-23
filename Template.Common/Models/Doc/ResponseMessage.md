# ResponseMessage 說明文件

[← 返回方案 README](../../../README.md) ｜ [← 返回上層 README](../../README.md)

## 概述

`ResponseMessage<T>` 是全專案統一的 API 回傳包裝格式，所有 Controller 的回傳值會透過 `ResponseWrapperFilter` 自動包裝成此格式，開發者通常不需要手動建立。

---

## 架構位置

```
Template.Common/
├── Models/
│   └── ResponseMessage.cs      # 通用回傳包裝類別
└── Enums/
    └── MessageEnum.cs          # 標準狀態碼與預設訊息對照
```

---

## ResponseMessage&lt;T&gt; 結構

```csharp
public class ResponseMessage<T>
{
    public int    Status  { get; set; }   // HTTP 狀態碼
    public string Message { get; set; }   // 回傳訊息文字
    public T?     Content { get; set; }   // 實際資料
}
```

### 回傳範例

```json
{
  "Status": 200,
  "Message": "成功",
  "Details": { "Token": "eyJhbGci..." }
}
```

---

## 靜態工廠方法

| 方法 | 說明 |
|---|---|
| `Success(content, message)` | Status=200，預設 Message="成功" |
| `Fail(status, message)` | 指定任意 Status 與 Message，Details=null |
| `From(MessageEnum, content, message)` | 由 MessageEnum 取得 Status，message 省略時取 Description |

### 使用範例

```csharp
// 在 Controller 中手動回傳（通常不需要，Filter 會自動包裝）
return Ok(ResponseMessage<string>.From(MessageEnum.NotFound));

// 自訂錯誤訊息
return Ok(ResponseMessage<object>.Fail(400, "帳號格式不正確。"));
```

---

## MessageEnum

| 列舉值 | Status | 預設 Description |
|---|---|---|
| Success | 200 | 成功 |
| Created | 201 | 建立成功 |
| NoContent | 204 | 無內容 |
| BadRequest | 400 | 請求參數錯誤 |
| Unauthorized | 401 | 未授權，請重新登入 |
| Forbidden | 403 | 權限不足 |
| NotFound | 404 | 資源不存在 |
| Conflict | 409 | 資料衝突 |
| UnprocessableEntity | 422 | 資料驗證失敗 |
| InternalServerError | 500 | 伺服器發生錯誤 |
| ServiceUnavailable | 503 | 服務暫時無法使用 |

---

## 自動包裝機制（ResponseWrapperFilter）

`ResponseWrapperFilter`（`IResultFilter`, `Order = int.MaxValue`）會攔截所有 Action 結果：

- `ObjectResult` → 包成 `ResponseMessage<T>`
- `EmptyResult`（void/無回傳）→ 包成 `ResponseMessage<object>` Details=null
- 已是 `ResponseMessage<T>` → 跳過，不重複包裝
- 標記 `[SkipResponseWrap]` 的 Action / Controller → 跳過

---

## 跳過包裝

```csharp
[SkipResponseWrap]
public IActionResult DownloadFile()
{
    return File(...);   // 直接回傳，不包裝
}
```

