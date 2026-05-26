# Zapret UI

**Автор: Niko**

Современный графический интерфейс для [Zapret](https://github.com/bol-van/zapret) с конфигами [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube).

## Возможности

- **Главная** — выбор стратегии, запуск/остановка, проверка обновлений
- **Стратегии** — просмотр и редактирование `.bat` конфигов, создание своих
- **Списки** — редактор доменов и IP (`lists/*.txt`)
- **Сервис** — функции `service.bat`: служба, Game Filter, IPSet, обновления
- **Консоль** — вывод диагностики и тестирования в реальном времени

## Запуск

### Для пользователей (передаваемая версия)
Папка **`ZapretUI-Program`** — отдельный каталог программы:
- `ZapretUI.exe` — запуск
- `Запуск ZapretUI.bat` — то же самое
- `ПРОЧТИ МЕНЯ.txt` — инструкция

Положите `ZapretUI-Program` рядом с папкой zapret (где `service.bat` и `bin\`).

### Сборка передаваемой версии
```powershell
cd ZapretUI
.\build-portable.ps1
```

### Разработка
```bash
cd ZapretUI
dotnet run
```

## Требования

- Windows 10/11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Папка zapret должна содержать `service.bat` и `bin/`

## Примечания

- Установка/удаление службы требует прав администратора (UAC)
- Тестирование и диагностика выводят результат в раздел «Консоль»
- Антивирус может реагировать на WinDivert — добавьте папку в исключения
