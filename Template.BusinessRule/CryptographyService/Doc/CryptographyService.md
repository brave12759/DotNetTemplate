# CryptographyService 加解密服務說明文件

[← 返回方案 README](../../../README.md) ｜ [← 返回上層 README](../../README.md)

## 概述

`CryptographyService` 位於 `Template.BusinessRule` 層，提供以下四種密碼學功能：

| 功能 | 演算法 | 金鑰來源 |
|------|--------|---------|
| 對稱加解密 | AES-256-CBC + PKCS7 | `appsettings` |
| 非對稱加解密 | RSA-OAEP-SHA256 | `appsettings` |
| 數位簽章 / 驗章 | RSA-PKCS1-SHA256 | `appsettings` |
| 不可逆雜湊 | PBKDF2-SHA256 | `appsettings` |

---

## 架構位置

```
Template.BusinessRule/
└── CryptographyService/
    ├── Doc/
    │   └── CryptographyService.md            # 本文件
    ├── Models/
    │   └── CryptographyServiceRequests.cs    # 請求模型
    └── Services/
        ├── ICryptographyService.cs            # 介面
        └── CryptographyService.cs             # 實作

Template.Common/
└── Settings/
    ├── CryptographyKeySettings.cs             # 金鑰設定（AES + RSA）
    └── HashSettings.cs                        # 雜湊設定（PBKDF2 迭代次數）

Template.WebApi/
└── Controllers/
    └── CryptographyController.cs              # API 端點（需 JWT 認證）
```

---

## 設定說明（appsettings.json）

```json
{
  "CryptographyKeySettings": {
    "SymmetricKeyBase64": "",     // AES 金鑰（Base64，16/24/32 bytes）
    "SymmetricIvBase64":  "",     // AES IV（Base64，固定 16 bytes）
    "RsaPublicKeyPem":    "",     // RSA 公鑰（PEM 格式）
    "RsaPrivateKeyPem":   ""      // RSA 私鑰（PEM 格式）
  },
  "HashSettings": {
    "Iterations": 100000          // PBKDF2 迭代次數（建議 ≥ 100000）
  }
}
```

> **安全提醒**：生產環境的金鑰請勿寫入版本控制，應透過環境變數或 Secrets Manager 注入。

---

## DI 註冊（Program.cs）

```csharp
builder.Services.AddSingleton(cryptographyKeySettings);
builder.Services.AddSingleton(hashSettings);
builder.Services.AddScoped<ICryptographyService, CryptographyService>();
```

---

## 功能詳解

### 1. 對稱加解密（AES）

**演算法**：AES-CBC，填充模式 PKCS7

| 項目 | 說明 |
|------|------|
| 模式 | CBC（Cipher Block Chaining） |
| 填充 | PKCS7 |
| 金鑰長度 | 128 / 192 / 256 bits |
| IV 長度 | 128 bits（固定 16 bytes） |
| 輸出格式 | Base64 |

**加密流程**：
```
明文 UTF-8 bytes → AES-CBC 加密（金鑰+IV 取自 appsettings）→ Base64 密文
```

**解密流程**：
```
Base64 密文 → AES-CBC 解密（金鑰+IV 取自 appsettings）→ 明文
```

**產生新金鑰**（工具用途）：
- `POST /Cryptography/GenerateSymmetricKey?keySizeBits=256`
- 回傳 `KeyBase64`、`IvBase64` 供填入 appsettings

---

### 2. 非對稱加解密（RSA）

**演算法**：RSA-OAEP-SHA256

| 項目 | 說明 |
|------|------|
| 填充模式 | OAEP with SHA-256 |
| 金鑰長度 | 建議 ≥ 2048 bits |
| 公鑰用途 | 加密（外部方可持有） |
| 私鑰用途 | 解密（伺服器端保管） |
| 金鑰格式 | PEM（PKCS#1） |
| 輸出格式 | Base64 |

**加密流程**：
```
明文 UTF-8 bytes → RSA-OAEP-SHA256 加密（公鑰取自 appsettings）→ Base64 密文
```

**解密流程**：
```
Base64 密文 → RSA-OAEP-SHA256 解密（私鑰取自 appsettings）→ 明文
```

> **注意**：RSA 明文長度受金鑰大小限制，2048 bits 最大可加密約 190 bytes。  
> 若需加密大量資料，應使用 Hybrid Encryption（RSA 加密 AES 金鑰，AES 加密內容）。

**產生新金鑰對**（工具用途）：
- `POST /Cryptography/GenerateRsaKeyPair`，Request Body `{ "keySizeBits": 2048 }`
- 回傳 `PublicKeyPem`、`PrivateKeyPem` 供填入 appsettings

---

### 3. 數位簽章 / 驗章（RSA）

**演算法**：RSA-PKCS1 v1.5，雜湊 SHA-256

| 項目 | 說明 |
|------|------|
| 簽章填充 | PKCS1 v1.5 |
| 雜湊演算法 | SHA-256 |
| 私鑰用途 | 簽章（伺服器端） |
| 公鑰用途 | 驗章（任何接收方） |
| 輸出格式 | Base64 |

**簽章流程**：
```
原文 UTF-8 bytes → SHA-256 雜湊 → RSA-PKCS1 私鑰簽章 → Base64 簽章
```

**驗章流程**：
```
原文 UTF-8 bytes + Base64 簽章 → RSA-PKCS1 公鑰驗章 → true / false
```

---

### 4. 不可逆雜湊（PBKDF2）

**演算法**：PBKDF2-HMAC-SHA256

| 項目 | 說明 |
|------|------|
| 演算法 | PBKDF2（Password-Based Key Derivation Function 2） |
| 內部雜湊 | HMAC-SHA256 |
| 鹽值 | 每次隨機產生 16 bytes |
| 迭代次數 | 由 `HashSettings.Iterations` 控制（預設 100,000） |
| 輸出長度 | 32 bytes（256 bits） |

**儲存格式**：
```
PBKDF2$SHA256${iterations}${salt_base64}${hash_base64}
```

範例：
```
PBKDF2$SHA256$100000$abc123==$xyz456==
```

> 迭代次數內嵌在雜湊字串中，未來調整迭代次數不影響既有雜湊的驗證。

**雜湊流程**：
```
明文 → 隨機鹽值 → PBKDF2(iterations, SHA256) → 格式化輸出
```

**驗證流程**：
```
輸入明文 + 儲存的雜湊字串 → 解析鹽值/迭代次數 → 重新計算 → FixedTimeEquals 比對
```

> `CryptographicOperations.FixedTimeEquals` 用於防止 Timing Attack。

---

## API 端點一覽

> 所有端點均需 JWT Bearer 認證（繼承 `AuthenticationController`）。

| HTTP Method | 路由 | 功能 | 說明 |
|-------------|------|------|------|
| POST | `/Cryptography/GenerateSymmetricKey` | 產生 AES 金鑰 | Query: `keySizeBits`（預設 256） |
| POST | `/Cryptography/SymmetricEncrypt` | AES 加密 | Body: `{ plainText }` |
| POST | `/Cryptography/SymmetricDecrypt` | AES 解密 | Body: `{ cipherTextBase64 }` |
| POST | `/Cryptography/GenerateRsaKeyPair` | 產生 RSA 金鑰對 | Body: `{ keySizeBits }`（預設 2048） |
| POST | `/Cryptography/AsymmetricEncrypt` | RSA 加密 | Body: `{ plainText }` |
| POST | `/Cryptography/AsymmetricDecrypt` | RSA 解密 | Body: `{ cipherTextBase64 }` |
| POST | `/Cryptography/Sign` | RSA 簽章 | Body: `{ plainText }` |
| POST | `/Cryptography/VerifySignature` | RSA 驗章 | Body: `{ plainText, signatureBase64 }` |
| POST | `/Cryptography/Hash` | PBKDF2 雜湊 | Body: `{ plainText }` |
| POST | `/Cryptography/VerifyHash` | 雜湊驗證 | Body: `{ plainText, hashValue }` |

---

## 常見使用情境

### 情境 A：密碼儲存

1. 使用者註冊時呼叫 `ICryptographyService.Hash(password)` 取得雜湊值，儲存至資料庫。
2. 登入時呼叫 `ICryptographyService.VerifyHash(inputPassword, storedHash)` 驗證。

### 情境 B：敏感欄位加密儲存（如身份證、手機號）

1. 寫入前呼叫 `SymmetricEncrypt(value)` 加密後存入 DB。
2. 讀取後呼叫 `SymmetricDecrypt(cipherText)` 解密顯示。

### 情境 C：API 回應內容簽章（防竄改）

1. 伺服器對回應 payload 呼叫 `Sign(payload)` 取得簽章。
2. 用戶端（或接收方）使用公鑰呼叫 `VerifySignature(payload, signature)` 驗章。

---

## 安全注意事項

1. **金鑰管理**：`CryptographyKeySettings` 中的私鑰與對稱金鑰屬高機密，生產環境應使用 Azure Key Vault、AWS Secrets Manager 或環境變數注入，嚴禁提交至版本控制。
2. **AES IV 重複使用**：當前實作 IV 固定於 appsettings，相同明文每次加密結果相同，適合資料庫欄位加密查詢。若需要語意安全（每次密文不同），應改為每次加密產生隨機 IV 並附加於密文前。
3. **RSA 明文長度限制**：2048-bit RSA 加密明文上限約 190 bytes（OAEP-SHA256），超過需改用混合加密。
4. **PBKDF2 迭代次數**：隨硬體效能提升，建議定期評估並調高 `Iterations`（目前 100,000）。
5. **Timing Attack 防護**：雜湊驗證已使用 `CryptographicOperations.FixedTimeEquals` 確保常數時間比對。
