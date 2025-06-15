#  FinQue Microservices Demo

This is a demo .NET 8-based microservices proof of concept (POC) deployed to **my personal Azure environment**, showcasing message-driven architecture with Azure Service Bus, Cosmos DB, Docker, and simple API authentication.

---

## 🔗 Live Swagger UI

👉 [**https://finque-api-app.azurewebsites.net/index.html**](https://finque-api-app.azurewebsites.net/index.html)

Use this to test the API directly in the browser.

---

## 📬 Sending Transactions to the Queue

You can simulate adding transactions to the system via:

`POST /api/Transactions`

Click **“Try it out”** and send a JSON body like:

```json
{
  "amount": 123.45,
  "currency": "EUR"
}
```

---

## 🧠 Queue Routing Logic

Based on the values of `amount` and `currency`, transactions are automatically routed to one of several Azure Service Bus queues:

| Condition                        | Destination Queue                      |
| -------------------------------- | -------------------------------------- |
| `amount <= 0`                    | 💀 Dead-letter queue (validation fail) |
| `amount > 1000`                  | ⚠️ `transactions-highrisk`             |
| `currency` is crypto code        | ⚠️ `transactions-highrisk`             |
| `0 < amount ≤ 1000` & non-crypto | ✅ `transactions-validated`             |

**Crypto currencies** triggering high-risk queue:

```json
["BTC", "ETH", "USDT", "BNB", "XRP", "SOL", "ADA", "DOGE", "DOT", "TRX"]
```

These queues are part of the Azure Service Bus setup.

```csharp
public static class QueueNames
{
    public const string Inbound = "transactions-inbound";
    public const string DeadLetter = "transactions-inbound/$DeadLetterQueue";
    public const string HighRisk = "transactions-highrisk";
    public const string Validated = "transactions-validated";
}
```

---

## 🔐 Protected Endpoint – Admin Purge

The `POST /api/admin/Purge` endpoint is **protected** to demonstrate API key-based authentication and is used to **reset the environment to a clean state**.

To use it:

1. Click the 🔒 **padlock** icon in Swagger.
2. Authorize with this API key:

```
AdminToken: 1b9c96bc-732a-4c45-ab3b-2f7440a2a9ff
```

---

## ⚙️ Architecture Summary

- `FinQue.Api`: ASP.NET Core API deployed to Azure App Service (Docker)
- `ValidationService`: Worker service deployed to Azure Container Apps
- Azure services used:
  - **Service Bus**
  - **Cosmos DB**
  - **Container Registry**
  - **App Service / Container Apps**

This project also demonstrates clean separation of concerns and interface-based design. For example:

```csharp
namespace FinQue.Api.Services
{
    public interface ISecretProvider
    {
        Task<string?> GetSecretValueAsync(string secretName);
    }
}
```

---

## 🐳 Deployment Targets

| Component         | Deployed As                |
| ----------------- | -------------------------- |
| FinQue.Api        | Azure App Service (Docker) |
| ValidationService | Azure Container App        |

---

## 🧪 Test It Out

1. Open Swagger: [https://finque-api-app.azurewebsites.net/index.html](https://finque-api-app.azurewebsites.net/index.html)
2. Send a few transactions with different `amount` and `currency` values.
3. Observe the routing behavior.
4. Optionally use the `Purge` endpoint to clear queues (with AdminToken).

---

## 😋 User Input Options

Users can manipulate these input values to trigger different routes:

```json
{
  "amount": 0,
  "currency": "string"
}
```

- `amount <= 0`: message is sent to **dead-letter queue**
- `amount > 1000`: message is sent to **transactions-highrisk**
- `currency` matches any of the listed crypto codes: message is sent to **transactions-highrisk**
- `amount` between 0 and 1000 and `currency` is not crypto: message is sent to **transactions-validated**

---

## 🔜 Next Steps

- ✅ Complete unit testing
- ✅ Restrict currency input to defined list

---

## 💡 Purpose of this Project

This proof of concept (POC) demonstrates my capability in designing and implementing distributed systems using .NET 8, Azure Service Bus, Cosmos DB, and Docker-based microservices. It highlights best practices for message-based workflows, queue-based routing, and scalable architecture in Azure.

