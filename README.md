<div align="center">

# ☁️ CloudTG

**A Telegram bot that turns your chat into a personal cloud storage**

[![C#](https://img.shields.io/badge/C%23-.NET_4.8-512BD4?style=flat-square&logo=csharp)](https://dotnet.microsoft.com/)
[![Telegram.Bot](https://img.shields.io/badge/Telegram.Bot-18.0-26A5E4?style=flat-square&logo=telegram)](https://github.com/TelegramBots/Telegram.Bot)
[![SQL Server](https://img.shields.io/badge/SQL_Server-LocalDB-CC2927?style=flat-square&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Dapper](https://img.shields.io/badge/ORM-Dapper-grey?style=flat-square)](https://github.com/DapperLib/Dapper)

<br/>

> Upload any Telegram media — photos, videos, documents, audio, stickers, voice messages —  
> organize them in folders, share storage with other users,  
> and retrieve files instantly, all without leaving Telegram.

</div>

---

## 📋 Table of Contents

- [How It Works](#-how-it-works)
- [Features](#-features)
- [Subscription Plans](#-subscription-plans)
- [Coin Shop](#-coin-shop)
- [Account Sharing](#-account-sharing)
- [Navigation & State Machine](#-navigation--state-machine)
- [Database Schema](#-database-schema)
- [Tech Stack](#-tech-stack)
- [Getting Started](#-getting-started)
- [Project Structure](#-project-structure)

---

## 🔬 How It Works

```
User sends any media or text to the bot
              │
              ▼
    HandleUpdateAsync()
              │
    ┌─────────┴──────────┐
    │ UpdateInformationOfUser() │  ← sync FirstName/Username with DB
    └─────────┬──────────┘
              │
    ┌─────────▼──────────────────────────────────┐
    │         Route by message type               │
    │                                             │
    │  Text    → ChekerMessageUpdate.TextMessage  │
    │  Photo   → PhotoMessage                     │
    │  Video   → VideoMessage                     │
    │  Audio   → AudioMessage                     │
    │  Voice   → VoiceMessage                     │
    │  Document→ DocumentMessage                  │
    │  Sticker → StickerMessage                   │
    │  VideoNote→ VideoNoteMessage                │
    └─────────┬──────────────────────────────────┘
              │
    ┌─────────▼──────────────────────────────────┐
    │    Commands.CheckStep()                     │
    │    reads User.Step from DB → dispatches     │
    │    to the correct screen/command handler    │
    └─────────────────────────────────────────────┘
```

Files are **never downloaded to the server** — only their Telegram `FileId` is stored in SQL Server. When a user opens a file, the bot forwards it back using the cached `FileId` directly from Telegram's CDN.

---

## ✨ Features

### 📁 File Storage
Send any of these directly in the chat while inside the Storage screen — the bot saves it automatically:

| Type | Extension saved as |
|---|---|
| Photo | `.photo` |
| Video | original filename or `.video` |
| Audio | original filename or `.audio` |
| Voice message | `.voice` |
| Document | original filename or `.document` |
| Sticker | `.sticker` |
| Video note (circle) | `.кружок` |

### 📂 Folder Management
- **Create folders** — nested, any depth (`Storage/trip/italy/photos/`)
- **Delete folders** — recursive: removes all subfolders and files inside
- **Navigate** — keyboard buttons list all files and subfolders of the current directory

### 🗂️ File Operations
- **Open** — bot resends the file in its original format
- **Delete** — removes from DB, restores one slot to the user's quota
- **Rename** — must include the extension (e.g. `vacation.video`)

### 🔑 Account Sharing
See [Account Sharing](#-account-sharing) section below.

### 💰 In-app Economy
- **Coins** — internal currency, purchased with real money via Telegram Payments
- **Subscriptions** — paid with coins; unlocks higher file quotas and longer validity
- Free tier starts with **25 file slots**

---

## 💳 Subscription Plans

Subscriptions are purchased with **coins** (internal currency):

| Plan | Cost | Duration | File slots |
|---|---|---|---|
| Free | — | — | 25 |
| Базовая (Basic) | 15 coins | 1 week | 100 |
| Умная (Smart) | 74 coins | 1 month | 800 |
| Vip | 887 coins | 1 year | 2 000 |
| Vip-Plus | 1 000 coins | Forever | 10 000 |
| Vip-Max | 1 500 coins | Forever | Unlimited |

---

## 🪙 Coin Shop

Coins are purchased via **Telegram Payments** (real money):

| Package | Price |
|---|---|
| 10 coins | $1 |
| 50 coins | $4 |
| 200 coins | $16 |
| 500 coins | $49 |
| 1 000 coins | $99 |

---

## 👥 Account Sharing

Any user can share their storage with others:

| Feature | Description |
|---|---|
| **Give access** | Share with a specific `@username`, set read-only (`false`) or read-write (`true`) |
| **General access** | Anyone registered in CloudTG can switch to your account |
| **Switch account** | Enter `@username` of an account that granted you access — storage switches context |
| **Remove access** | Remove one user, remove general access, or clear all at once |

When switched to another account, file uploads, downloads, and folder navigation all operate on **that account's storage and quota**, not your own.

---

## 🗺️ Navigation & State Machine

The bot is fully keyboard-driven. The current screen is stored in the `Step` field of the User row and persists across sessions:

```
Step  0  →  Main menu       [Магазин | Акаунт | Хранилище]
Step  1  →  Shop            [Коины | Подписка | Главная]
Step  2  →  Coin shop       [price buttons]
Step  3  →  Subscription    [plan buttons]
Step  4  →  Account         [Переключить | Предоставить доступ | Убрать доступ]
  Step 41  →  Switch account (waiting for @username)
  Step 42  →  Give access   (waiting for "@username true/false" or "Общий")
  Step 43  →  Remove access (waiting for choice)
Step  5  →  Storage / folder navigator
  Step 51  →  Create folder (waiting for folder name)
  Step 52  →  Delete folder confirmation
  Step 53  →  Rename file   (waiting for new name with extension)
  Step 5XY →  File context  [Удалить | Переименовать | Назад]
```

`Back` decrements the step and returns to the previous screen.  
Steps ≥ 10 are compound: `step / 10` = screen, `step % 10` = sub-action.

---

## 🗄️ Database Schema

Three tables in the `TelegramCloud` SQL Server database:

### `Users`

| Column | Type | Description |
|---|---|---|
| `Id` | INT PK | Internal row ID |
| `TelegramId` | BIGINT | Telegram user ID |
| `FirstName` | NVARCHAR | Display name (auto-updated) |
| `Username` | NVARCHAR | Telegram @username (auto-updated) |
| `CountItems` | INT | Remaining file slots |
| `Coins` | INT | Internal currency balance |
| `Step` | INT | Current navigation state |
| `CurrentDir` | NVARCHAR | Active folder path (e.g. `Storage/trip/`) |
| `CurrentAccountId` | BIGINT | TelegramId of the account in use (own or shared) |
| `SelectedFileId` | NVARCHAR | FileId of the file currently open |
| `PayStatus` | INT | Subscription level (-1 = none, 0–4 = plan) |
| `PayDateLeft` | DATETIME | Subscription expiry date |

### `Files`

| Column | Type | Description |
|---|---|---|
| `Id` | INT PK | Internal row ID |
| `FileId` | NVARCHAR | Telegram CDN FileId |
| `FileName` | NVARCHAR | Display name |
| `FilePath` | NVARCHAR | Parent folder path |
| `OwerId` | INT | References `Users.Id` |
| `Type` | NVARCHAR | `Photo`, `Video`, `Audio`, `Voice`, `Document`, `Sticker`, `VideoNote`, `Folder` |

### `Access`

| Column | Type | Description |
|---|---|---|
| `MainUserId` | BIGINT | Owner's TelegramId |
| `RefUserId` | BIGINT | Granted user's TelegramId |
| `General` | BIT | `true` = open to anyone |
| `RuleUser` | BIT | `false` = read-only, `true` = read-write |

---

## 🛠️ Tech Stack

| Component | Technology |
|---|---|
| Language | C# · .NET Framework 4.8 |
| Telegram API | Telegram.Bot 18.0.0-alpha.3 |
| Long polling | Telegram.Bot.Extensions.Polling 2.0.0-alpha.1 |
| Database | SQL Server LocalDB (`TelegramCloud`) |
| ORM | Dapper 2.0.123 + Dapper.Contrib 2.0.78 |
| Serialization | Newtonsoft.Json 13.0.1 |
| Payments | Telegram Payments API (provider token) |

---

## 🚀 Getting Started

### Prerequisites

- **Visual Studio 2019+** (with .NET desktop workload)
- **SQL Server LocalDB** — included with Visual Studio
- A **Telegram Bot Token** from [@BotFather](https://t.me/BotFather)
- A **Telegram Payments provider token** (for coin purchases; test token available from BotFather)

### 1 — Create the database

Open **SQL Server Object Explorer** in Visual Studio, connect to `(localdb)\MSSQLLocalDB`, and run:

```sql
CREATE DATABASE TelegramCloud;
USE TelegramCloud;

CREATE TABLE Users (
    Id               INT IDENTITY PRIMARY KEY,
    FirstName        NVARCHAR(100),
    Username         NVARCHAR(100),
    TelegramId       BIGINT UNIQUE,
    CountItems       INT DEFAULT 25,
    Coins            INT DEFAULT 0,
    Step             INT DEFAULT 0,
    CurrentDir       NVARCHAR(500) DEFAULT 'Storage/',
    PayStatus        INT DEFAULT -1,
    CurrentAccountId BIGINT,
    SelectedFileId   NVARCHAR(500),
    PayDateLeft      DATETIME
);

CREATE TABLE Files (
    Id       INT IDENTITY PRIMARY KEY,
    FileId   NVARCHAR(500),
    FileName NVARCHAR(300),
    FilePath NVARCHAR(500),
    OwerId   INT,
    Type     NVARCHAR(50)
);

CREATE TABLE Access (
    MainUserId BIGINT,
    RefUserId  BIGINT,
    General    BIT DEFAULT 0,
    RuleUser   BIT DEFAULT 0
);
```

### 2 — Configure the bot token

In `Program.cs`, replace the `Token` constant with your bot token from BotFather:

```csharp
static ITelegramBotClient bot = new TelegramBotClient("YOUR_BOT_TOKEN_HERE");
```

### 3 — Configure the payment provider token

In `Commands.cs`, find the `Pay()` method and replace the `providerToken` value:

```csharp
providerToken: "YOUR_PAYMENT_PROVIDER_TOKEN",
```

> For testing, BotFather issues a Stripe test token: `284685063:TEST:...`

### 4 — Build and run

```
Build → Start (F5)
```

The console will print: `Запущен бот <BotName>` when the connection is established.

---

## 📁 Project Structure

```
CloudTG/
├── CloudTG/                        # Main bot project (.NET 4.8 Console App)
│   ├── Program.cs                  # Bot startup, update routing by message type
│   ├── ChekerMessageUpdate.cs      # Handlers per message type (photo, video, etc.)
│   ├── Commands.cs                 # All bot commands and screen logic
│   ├── App.config                  # Connection string, runtime binding redirects
│   └── Properties/
│       └── Settings.settings       # App settings
│
└── Lib/                            # Shared models library (.NET 4.8 Class Library)
    └── Class1.cs                   # Data models:
                                    #   User   — user state & profile
                                    #   File   — stored file metadata
                                    #   Access — sharing permissions
                                    #   Price  — subscription/coin plan definition
```

---

<div align="center">

Made with C# · .NET 4.8 · Telegram Bot API · SQL Server

</div>
