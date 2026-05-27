<p align="center">
  <img src="Assets/icon-readme.png" alt="Zapret UI" width="128">
</p>

# Zapret UI

Современный WPF-интерфейс для запуска и управления конфигами Zapret на Windows.

Проект помогает проще пользоваться стратегиями обхода блокировок для Discord, YouTube и других сервисов.

| | |
|---|---|
| **Скачать** | [ZapretUI-Setup.exe (последний релиз)](https://github.com/RaccoonLaptop/ZapretUI/releases/latest) |
| **Руководство** | https://raccoonlaptop.github.io/ZapretUI/ |
| **Для разработчиков / AI** | [AGENTS.md](AGENTS.md) · [docs/project/](docs/project/) |

## Быстрый старт

1. Скачайте `ZapretUI-Setup.exe` из [Releases](https://github.com/RaccoonLaptop/ZapretUI/releases)
2. Установите программу
3. Откройте Zapret UI, выберите стратегию на главной и нажмите **ЗАПУСТИТЬ**

## Возможности

- Язык интерфейса: **Русский / English** (Сервис → Язык)
- Запуск/остановка стратегии одной кнопкой + меню в **системном трее**
- Редактор стратегий `.bat` с подсветкой
- Списки Flowseal (`lists/*.txt`)
- Game Filter, IPSet, обновления, мастер безопасности
- Анимированные фоны главной (по умолчанию Wavy)

## Документация

| Документ | Описание |
|----------|----------|
| [docs/project/USER_GUIDE.md](docs/project/USER_GUIDE.md) | Руководство пользователя (RU) |
| [docs/project/USER_GUIDE_EN.md](docs/project/USER_GUIDE_EN.md) | User guide (EN) |
| [docs/project/CHANGELOG.md](docs/project/CHANGELOG.md) | История изменений |
| [docs/project/ARCHITECTURE.md](docs/project/ARCHITECTURE.md) | Архитектура |
| [AGENTS.md](AGENTS.md) | Контекст для Cursor AI |

## Сборка из исходников

```powershell
.\build-installer.ps1
```

Результат: `ZapretUI-dist\ZapretUI-Setup.exe`

Подробнее: [docs/project/BUILD.md](docs/project/BUILD.md)

## Требования

- Windows 10/11 (x64)
- Права администратора (запрашиваются автоматически)

## Благодарности

- [bol-van/zapret](https://github.com/bol-van/zapret)
- [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube)
- Всем, кто тестирует и улучшает Zapret UI
