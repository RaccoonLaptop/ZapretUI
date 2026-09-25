<p align="center">
  <img src="Assets/icon-readme.png" alt="Zapret UI" width="128">
</p>

# Zapret UI

Современный интерфейс для запуска конфигов [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) на Windows — Discord, YouTube и другие сервисы.

| | |
|---|---|
| **Скачать** | [ZapretUI-Setup.exe](https://github.com/RaccoonLaptop/ZapretUI/releases/latest) |
| **GitHub** | https://github.com/RaccoonLaptop/ZapretUI |
| **Telegram** | https://t.me/ZapretUI |
| **Донат** | https://raccoonlaptop.github.io/ZapretUI/donate.html |
| **Руководство** | https://raccoonlaptop.github.io/ZapretUI/ |

## Быстрый старт

1. Скачайте и установите **ZapretUI-Setup.exe**
2. При первом запуске дождитесь загрузки компонентов Flowseal
3. Добавьте папку программы в исключения антивируса и разрешите `ZapretUI.exe` и `winws.exe` в брандмауэре — [инструкция ниже](#антивирус-и-брандмауэр)
4. Выберите стратегию на главной и нажмите **ЗАПУСТИТЬ**

## Антивирус и брандмауэр

Исключения в антивирус и правила брандмауэра добавляются вручную. Без них Windows Defender может удалить `WinDivert` и `winws.exe`, а брандмауэр — заблокировать запуск.

После установки программа лежит в папке:

`%LOCALAPPDATA%\ZapretUI`

Обычно это `C:\Users\<имя>\AppData\Local\ZapretUI`. Внутри неё находится `zapret\bin\winws.exe`.

### Windows Defender

1. Откройте **Безопасность Windows**.
2. **Защита от вирусов и угроз** → **Управление настройками**.
3. **Исключения** → **Добавление или удаление исключений** → **Добавить исключение** → **Папка**.
4. Укажите `%LOCALAPPDATA%\ZapretUI`.

Если установлен другой антивирус, добавьте ту же папку в его исключения. Срабатывание с именем `WinDivert` или `Not-a-virus:RiskTool.Multi.WinDivert` относится к драйверу zapret: файл нужно восстановить из карантина и оставить папку в исключениях.

### Брандмауэр Windows

1. **Безопасность Windows** → **Брандмауэр и безопасность сети** → **Разрешить работу с приложением через брандмауэр**.
2. Нажмите **Изменить параметры** (нужны права администратора).
3. **Разрешить другое приложение** → **Обзор** и добавьте оба файла:
   - `%LOCALAPPDATA%\ZapretUI\ZapretUI.exe`
   - `%LOCALAPPDATA%\ZapretUI\zapret\bin\winws.exe`
4. Для каждого файла отметьте частную и публичную сеть.

## Возможности

- Русский и English
- Запуск/остановка одной кнопкой и из системного трея
- Редактор стратегий, списки, обновления
- Game Filter, IPSet

## Требования

- Windows 10/11 (x64)
- Права администратора

## Благодарности

- [bol-van/zapret](https://github.com/bol-van/zapret)
- [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube)
