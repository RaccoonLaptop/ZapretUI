# Zapret UI — описание проекта

## Что это

**Zapret UI** — бесплатная программа для Windows с графическим интерфейсом. Она упрощает работу с готовыми конфигурациями [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) (форк адаптации [zapret](https://github.com/bol-van/zapret) для Discord, YouTube и других сервисов).

Пользователь не редактирует командную строку вручную: выбирает `.bat`-стратегию и нажимает **Запустить** — в фоне стартует `winws.exe`.

## Автор и репозиторий

| | |
|---|---|
| Автор UI | Niko |
| GitHub | https://github.com/RaccoonLaptop/ZapretUI |
| Сайт-руководство | https://raccoonlaptop.github.io/ZapretUI/ |
| Текущая версия | 1.6.8 |

## Для кого

- Пользователи Windows 10/11 (x64), которым нужен обход блокировок через zapret/Flowseal
- Разработчики, дорабатывающие UI в Cursor / Visual Studio
- AI-агенты — см. [AGENT_CONTEXT.md](AGENT_CONTEXT.md)

## Что программа делает

- Скачивает и обновляет пакет Flowseal при первом запуске
- Запускает и останавливает `winws.exe` по выбранной стратегии
- Редактирует `.bat` и списки `lists/*.txt`
- Настраивает game filter (TCP/UDP), IPSet, язык RU/EN
- Работает из системного трея (запуск/остановка, статус, зелёная иконка при работе)
- Проверяет обновления Zapret UI и Flowseal (с подтверждением)
- Мастер исключений Defender и брандмауэра

## Что программа не делает

- Не заменяет ядро zapret и не пишет правила DPI сама
- Не гарантирует обход на всех провайдерах — нужна подходящая стратегия
- Не работает без прав администратора (UAC)

## Технологии

- .NET 8, WPF, WinForms (трей)
- PowerShell (`ui-bridge.ps1`) для запуска winws
- Inno Setup — установщик `ZapretUI-Setup.exe`
- AvalonEdit — редактор стратегий

## Единая папка проекта

Весь актуальный код и документация — **только** в:

`C:\Users\Aero\Desktop\ZapretUI\`

Старая копия в `zapret-discord-youtube-1.9.8c\ZapretUI` **не используется**.
