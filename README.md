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

### Для пользователей (рекомендуется)
Скачайте **`ZapretUI-Setup.exe`** из [Releases](https://github.com/RaccoonLaptop/ZapretUI/releases):

1. Запустите установщик — **всё автоматически**
2. Программа сама скачает zapret (Flowseal) с GitHub
3. Ярлык появится в меню Пуск

Ничего распаковывать и никакие папки указывать не нужно. Нужен только интернет при первой установке.

### Portable (без установщика)
Папка **`ZapretUI-Program`** — распакуйте zip в папку zapret и запустите `ZapretUI.exe`.

### Сборка установщика
```powershell
cd ZapretUI
.\build-installer.ps1
```
Результат: `ZapretUI-dist\ZapretUI-Setup.exe`

### Сборка portable-версии
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

- Windows 10/11 (64-bit)
- Папка zapret должна содержать `service.bat` и `bin/`

## Примечания

- Установка/удаление службы требует прав администратора (UAC)
- Тестирование и диагностика выводят результат в раздел «Консоль»
- Антивирус может реагировать на WinDivert — добавьте папку в исключения
