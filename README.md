# CrybbBot

CrybbBot is a Windows desktop application for composing, scheduling, managing, and sending structured Slack messages across multiple channels.

Built with **C#**, **.NET 6**, and **WPF**, it combines a rich message editor, channel groups, attachments, message history, local scheduling, and a dedicated Slack integration layer in one desktop UI.

## Highlights

- Send Slack messages to one or many channels
- Group channels into reusable bundles
- Compose rich-text messages with Slack-compatible formatting
- Send messages immediately or schedule them for later
- Attach images and files
- Add file references as links
- Store scheduled and sent-message history locally
- Filter and review previous messages
- Edit scheduled messages before they are sent
- Manage Slack channels and channel bundles
- Start and stop the Slack bot connection from the application
- Persist data locally in SQLite
- Optional SQL Server support in developer tools
- Material Design WPF interface
- Application logging with NLog

## Message composer

CrybbBot includes a rich WPF message editor instead of a plain text box.

Supported formatting includes:

- bold;
- italic;
- underline;
- strikethrough;
- hyperlinks;
- bulleted lists;
- numbered lists;
- quotes;
- inline code;
- code blocks.

The editor stores rich content as WPF XAML and converts it into Slack-compatible rich-text blocks when a message is sent.

Messages can target:

- individual channels;
- multiple channels;
- reusable channel bundles;
- a combination of channels and bundles.

## Attachments and references

A message can include:

- images;
- regular files;
- file references represented as links.

Files can be added to a message and are uploaded through the Slack API during delivery. Images are added to the Slack message as image blocks, while file references are represented as structured links.

## Scheduling

Messages can be sent immediately or scheduled for a specific date and time.

Scheduled messages are persisted in the local database together with their:

- formatted content;
- selected channels and bundles;
- attachments;
- file references;
- scheduling metadata.

A background scheduler checks for pending messages and delivers them through the Slack integration while the bot is running.

## Message history

CrybbBot keeps a local history of managed messages.

The message list supports filtering by states such as:

- all messages;
- scheduled;
- sent;
- errors.

Messages can also be filtered by date and through the advanced filter dialog.

Scheduled messages can be reopened and edited before delivery.

## Channel management

Slack channels can be stored with aliases and organized into reusable bundles.

This makes it possible to maintain logical groups of channels and select an entire group when composing a message instead of choosing every channel individually.

## Slack bot control

The main screen exposes the current bot state and allows the Slack client to be started or stopped directly from the application.

The Slack Bot User OAuth Token is stored in the local `appsettings.json` file and can be edited from the application settings screen.

Do not commit a real Slack token to source control.

## Local data

CrybbBot creates its SQLite database automatically on first run.

Default database location:

```text
DB/app.db
```

The data layer stores channels, bundles, messages, scheduling information, attachments, and delivery-related state.

## Architecture

The solution is divided into three main projects:

```text
src/
├── CrybbBot/         WPF desktop application
├── SlackBotSender/   Slack API and background scheduling layer
├── DataLayer/        Database access and persistence
└── CrybbBot.sln
```

### CrybbBot

The main Windows application contains:

- WPF views and view models;
- message composition UI;
- channel and bundle management;
- message history;
- dialogs and settings;
- Material Design resources;
- application logging.

### SlackBotSender

The Slack integration layer handles:

- Slack API authentication;
- posting text and structured Block Kit messages;
- file and image upload;
- multi-channel delivery;
- scheduled-message background processing;
- per-channel send results and errors.

### DataLayer

The persistence layer uses **Dapper** and supports:

- SQLite;
- Microsoft SQL Server.

SQLite is the default application database. SQL Server support is also available through the developer tooling.

## Technology

- C#
- .NET 6
- WPF
- Material Design in XAML
- Slack Web API
- Slack Block Kit
- Dapper
- SQLite
- Microsoft SQL Server
- Newtonsoft.Json
- NLog
- Nerdbank.GitVersioning

## Getting started

### Requirements

- Windows
- .NET 6 SDK
- a Slack app with a Bot User OAuth Token
- Slack bot permissions appropriate for the features you want to use, such as posting messages and uploading files

### Build and run

Clone the repository and open:

```text
src/CrybbBot.sln
```

Or build from the command line:

```bash
cd src
dotnet restore
dotnet build
```

Run the WPF application from Visual Studio or with:

```bash
dotnet run --project CrybbBot/CrybbBot.csproj
```

## Slack configuration

On first launch, CrybbBot creates a local `appsettings.json` from the default configuration when necessary.

You can configure the token from **Settings** inside the application.

The relevant configuration structure is:

```json
{
  "Slack": {
    "BotToken": "",
    "DefaultChannelId": ""
  }
}
```

Keep credentials local and never commit real tokens.

## Developer tools

CrybbBot also includes developer tooling for database-related testing and development.

It can:

- initialize a SQLite database;
- initialize/connect to SQL Server;
- save and restore mock data used during development.

These tools are separate from the normal Slack messaging workflow.

## Design goals

CrybbBot is built around a few practical goals:

- make repeated multi-channel Slack messaging faster;
- keep message composition richer than plain text;
- make channel groups reusable;
- preserve scheduled and previously sent messages locally;
- keep Slack-specific transport logic separated from the desktop UI;
- keep the persistence layer reusable across storage backends.
