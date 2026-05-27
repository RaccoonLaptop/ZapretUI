# Zapret UI

Современный WPF-интерфейс для запуска и управления конфигами Zapret на Windows.

Проект помогает проще пользоваться стратегиями обхода блокировок для Discord, YouTube и других сервисов.

## Быстрый старт

1. Скачайте `ZapretUI-Setup.exe` из [Releases](https://github.com/RaccoonLaptop/ZapretUI/releases)
2. Установите программу
3. Откройте `Zapret UI`, выберите стратегию на главной и нажмите `ЗАПУСТИТЬ`

## Возможности

- Запуск/остановка стратегии одной кнопкой
- Редактор стратегий `.bat` с подсветкой
- Работа со списками Flowseal (`lists/*.txt`)
- Управление сервисом и диагностикой
- Проверка обновлений Zapret UI и Flowseal при запуске (только с подтверждением)

## Сборка из исходников

```powershell
.\build-installer.ps1
```

Результат:

- `ZapretUI-dist\ZapretUI-Setup.exe` — установщик
- `ZapretUI-update\ZapretUI-Program.zip` — пакет для in-app обновления

## Требования

- Windows 10/11 (x64)
- Права администратора (запрашиваются автоматически)

## Благодарности

Огромная благодарность:

- создателю проекта [zapret](https://github.com/bol-van/zapret) за фундаментальную работу;
- команде [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) за адаптацию и поддержку конфигов для Windows;
- всем пользователям и тестерам, которые помогают улучшать Zapret UI.
