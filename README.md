# MeetingTranscriber

Десктопное приложение для Windows на базе Avalonia/.NET 8, которое в реальном времени транскрибирует встречи локально через Whisper, генерирует AI-резюме и экспортирует результат в TXT / PDF / SRT.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Avalonia](https://img.shields.io/badge/Avalonia-11.2-teal)
[![CI](https://github.com/090TYPE/MeetingTranscriber/actions/workflows/ci.yml/badge.svg)](https://github.com/090TYPE/MeetingTranscriber/actions/workflows/ci.yml)

---

## Возможности

- **Запись с микрофона** — захват аудио в реальном времени, разбивка на чанки и транскрипция
- **Транскрипция файлов** — открыть готовый аудиофайл (MP3, WAV, MP4, M4A)
- **Локальный Whisper** — транскрипция полностью на устройстве, без облака
- **AI-резюме** — краткое изложение через Claude или OpenAI (опционально)
- **История сессий** — все записи хранятся в локальной SQLite, поиск по названию
- **Экспорт** — TXT, SRT (субтитры), PDF
- **Neon Terminal UI** — тёмный интерфейс в стиле терминала

---

## Скриншоты

> Вкладка записи

```
┌─────────────────────────────────────────────────────┐
│ ● MEETING_TRANSCRIBER          ● REC   00:01:23     │
├──┬──────────────────┬──────────────────────────────┤
│  │ SOURCE           │ TRANSCRIPT                    │
│🎙│ [● MIC] [▲ FILE] │ [00:00] Hello, let's begin... │
│📋│                  │ [00:08] Today we discuss...    │
│⚙ │ WAVEFORM         │ [00:16] The main topic is...  │
│  │ ▁▃▅▇▅▃▁▃▅▇      │                               │
│  │ [■ STOP REC]     │                               │
│  │ AI SUMMARY       │                               │
│  │ [▶ GENERATE]     │                               │
│  │ EXPORT           │                               │
│  │ [TXT][PDF][SRT]  │                               │
└──┴──────────────────┴───────────────────────────────┘
```

---

## Требования

- Windows 10/11 x64
- .NET 8 Runtime ([скачать](https://dotnet.microsoft.com/download/dotnet/8.0))
- Микрофон (для записи)
- Интернет — только для первого скачивания модели Whisper и AI-резюме

---

## Установка и запуск

### Из исходников

```bash
git clone https://github.com/090TYPE/MeetingTranscriber.git
cd MeetingTranscriber
dotnet run --project MeetingTranscriber/MeetingTranscriber.csproj
```

### Сборка self-contained exe

```bash
dotnet publish MeetingTranscriber/MeetingTranscriber.csproj \
  -r win-x64 \
  -c Release \
  -p:PublishSingleFile=true \
  --self-contained
```

Исполняемый файл появится в `MeetingTranscriber/bin/Release/net8.0/win-x64/publish/`.

---

## Первый запуск

При первом старте приложение автоматически скачает модель Whisper (`tiny`, ~74 MB). Прогресс отображается в отдельном окне. После завершения загрузки приложение готово к работе.

Модель сохраняется в `%APPDATA%\MeetingTranscriber\models\`.

---

## Настройка

Открой вкладку **⚙** в боковой панели:

| Параметр | Описание |
|---|---|
| **Whisper Model** | `tiny` (быстро) → `base` → `small` → `medium` → `large-v3` (точно) |
| **Language** | Язык речи или `auto` для автоопределения |
| **AI Provider** | `disabled` / `claude` / `openai` |
| **Claude API Key** | Ключ Anthropic (`sk-ant-...`) |
| **OpenAI API Key** | Ключ OpenAI (`sk-...`) |

> При смене модели — удали старый файл из `%APPDATA%\MeetingTranscriber\models\` и перезапусти.

---

## Архитектура

```
MeetingTranscriber/
├── App/                        # UI-слой (Avalonia)
│   ├── Controls/               # WaveformControl — анимированная форма сигнала
│   ├── Styles/                 # NeonTheme.axaml — цвета, шрифты, стили кнопок
│   ├── ViewModels/             # MVVM: Main, History, Settings
│   └── Views/                  # AXAML-окна и UserControl-ы
├── Core/
│   ├── Audio/                  # MicRecorder (WaveInEvent 16kHz), FileAudioReader
│   ├── Export/                 # TxtExporter, SrtExporter, PdfExporter
│   ├── Storage/                # EF Core + SQLite (AppDbContext, SessionRepository)
│   ├── Summary/                # ClaudeProvider, OpenAIProvider, NullSummaryProvider
│   └── Transcription/          # WhisperService, ChunkQueue
├── Models/                     # AppSettings, Session, TranscriptLine, AudioChunk
└── Program.cs
```

**Поток данных:**
```
Микрофон → WaveInEvent (16kHz/16bit/mono)
         → ChunkQueue (буферизация по N секунд)
         → WhisperService.ProcessChunkAsync()
         → TranscriptLine → UI + SQLite
```

---

## Стек технологий

| Компонент | Библиотека |
|---|---|
| UI | Avalonia 11.2 + ReactiveUI 20 |
| Транскрипция | Whisper.net 1.7 (ggml) |
| Запись звука | NAudio 2.2 |
| AI-резюме | Anthropic.SDK 4 / Betalgo.OpenAI 8 |
| База данных | EF Core 8 + SQLite |
| PDF | QuestPDF 2024 |

---

## Лицензия

MIT
