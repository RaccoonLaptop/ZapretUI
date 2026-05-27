# Zapret UI — контекст для AI-агентов (Cursor)

**Читай этот файл первым**, если работаешь над проектом в новой сессии.

## Репозиторий

- **GitHub:** https://github.com/RaccoonLaptop/ZapretUI
- **Актуальная версия:** 1.6.8
- **Стек:** C# / .NET 8 / WPF + WinForms (трей)
- **Назначение:** графический интерфейс для [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) на Windows

## Где вся документация

| Файл | Содержание |
|------|------------|
| [docs/project/README.md](docs/project/README.md) | Оглавление документации |
| [docs/project/DESCRIPTION.md](docs/project/DESCRIPTION.md) | Описание проекта |
| [docs/project/AGENT_CONTEXT.md](docs/project/AGENT_CONTEXT.md) | Полный контекст для агента |
| [docs/project/ARCHITECTURE.md](docs/project/ARCHITECTURE.md) | Архитектура и потоки данных |
| [docs/project/FILE_INDEX.md](docs/project/FILE_INDEX.md) | Индекс всех исходных файлов |
| [docs/project/USER_GUIDE.md](docs/project/USER_GUIDE.md) | Руководство пользователя (RU) |
| [docs/project/USER_GUIDE_EN.md](docs/project/USER_GUIDE_EN.md) | User guide (EN) |
| [docs/project/CHANGELOG.md](docs/project/CHANGELOG.md) | История изменений |
| [docs/project/BUILD.md](docs/project/BUILD.md) | Сборка и релиз |

## Сайт для пользователей

https://raccoonlaptop.github.io/ZapretUI/

## Важные пути

- Исходники: корень репозитория `ZapretUI/`
- Установка пользователя: `%LocalAppData%\ZapretUI`
- Zapret (Flowseal): `%LocalAppData%\ZapretUI\zapret`
- Настройки: `%LocalAppData%\ZapretUI\settings.json`
- Установщик: `ZapretUI-dist\ZapretUI-Setup.exe` (после `build-installer.ps1`)

## Правила при изменениях

1. Не ломать запуск `winws.exe` через `ui-bridge.ps1` / `StrategyService`
2. Локализация: ключи в `Resources/Strings.ru.json` и `Strings.en.json`, вызов `Loc.T()` / `Loc.F()`
3. Релизы: только `ZapretUI-Setup.exe` на GitHub Releases
4. Версию поднимать в `ZapretUI.csproj` и `packaging/update.json`
