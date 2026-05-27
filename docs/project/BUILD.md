# Сборка и публикация Zapret UI

## Требования

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (для установщика)
- Git, GitHub CLI (`gh`) — для релизов

## Быстрая сборка установщика

```powershell
cd C:\Users\Aero\Desktop\ZapretUI
.\build-installer.ps1
```

Результат: `C:\Users\Aero\Desktop\ZapretUI-dist\ZapretUI-Setup.exe`

## Версия

Обновить перед релизом:

1. `ZapretUI.csproj` → `<Version>1.x.x</Version>`
2. `packaging/update.json` и `update.json` → `version` и URL тега

## Публикация на GitHub

```powershell
git add .
git commit -m "release: v1.x.x"
git push origin main
gh release delete v1.x.x --yes --cleanup-tag   # если пересобираете
gh release create v1.x.x "..\ZapretUI-dist\ZapretUI-Setup.exe" --title "ZapretUI v1.x.x" --notes "..."
```

На Releases — **только** файл `ZapretUI-Setup.exe`.

## GitHub Pages (сайт-руководство)

Сайт лежит в `docs/index.html`. Включение:

1. Repo → **Settings** → **Pages**
2. Source: **Deploy from branch**
3. Branch: `main`, folder: `/docs`
4. Сайт: `https://raccoonlaptop.github.io/ZapretUI/`

## Структура выходных папок (не в Git)

| Папка | Назначение |
|-------|------------|
| `bin/`, `obj/` | Сборка .NET (в .gitignore) |
| `ZapretUI-dist/` | Готовый Setup.exe |
| `ZapretUI-installer-staging/` | Staging для Inno Setup |

## Локальный запуск без установщика

```powershell
dotnet run --project ZapretUI.csproj
```

Нужна папка zapret: `%LocalAppData%\ZapretUI\zapret` или bootstrap при первом запуске.
