# Как выложить Zapret UI на GitHub

На этом ПК **не установлен Git**. Ниже — три способа загрузить проект.

Папка для GitHub: **`C:\Users\Aero\Desktop\ZapretUI`** (только исходники программы, без zapret/bin).

---

## Способ 1 — через сайт GitHub (без Git)

1. Зайдите на https://github.com и войдите в аккаунт
2. Нажмите **+** → **New repository**
3. Имя: `ZapretUI` (или другое)
4. Public → **Create repository**
5. **Add file** → **Upload files**
6. Перетащите **всё содержимое** папки `C:\Users\Aero\Desktop\ZapretUI`
7. Commit message: `Initial commit` → **Commit changes**

---

## Способ 2 — GitHub Desktop

1. Установите https://desktop.github.com/
2. File → Add local repository → выберите `C:\Users\Aero\Desktop\ZapretUI`
3. Publish repository → выберите имя `ZapretUI`

---

## Способ 3 — Git в командной строке

1. Установите Git: https://git-scm.com/download/win
2. В PowerShell:

```powershell
cd C:\Users\Aero\Desktop\ZapretUI
git init
git add .
git commit -m "Initial release: Zapret UI by Niko"
git branch -M main
git remote add origin https://github.com/RaccoonLaptop/ZapretUI.git
git push -u origin main
```

Замените `ВАШ_ЛОГИН` на свой GitHub username.

---

## Releases (для автообновления)

1. Соберите: `.\build-portable.ps1` (из папки zapret-discord-youtube\ZapretUI)
2. На GitHub: **Releases** → **Create a new release**
3. Tag: `v1.0.0`
4. Прикрепите файлы:
   - `ZapretUI-Program.zip` (из `ZapretUI-update`)
   - `update.json` (версия в файле = версии релиза)
5. Publish release

После этого в `AppSelfUpdateService.cs` укажите URL манифеста:

`https://raw.githubusercontent.com/RaccoonLaptop/ZapretUI/main/update.json`

или положите `update.json` в корень репозитория.

---

## Важно

- **Не загружайте** папки `bin\`, `winws.exe`, WinDivert — это часть zapret/Flowseal, не вашего UI
- В репозитории только папка **ZapretUI** с исходниками
