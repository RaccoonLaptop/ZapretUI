from pathlib import Path

p = Path(__file__).resolve().parents[1] / "Pages" / "StrategiesPage.xaml.cs"
text = p.read_text(encoding="utf-8")
replacements = [
    ('Text = "Стратегии"', 'Text = Loc.T("strategies.title")'),
    ('Text = "Конфиги запуска Zapret (.bat). Выберите файл слева, отредактируйте и сохраните."', 'Text = Loc.T("strategies.subtitle")'),
    ('Text = "Конфиг Niko_ALT11 — авторская стратегия Niko (из CDPi UI). «Списки» — .txt из выбранного .bat."', 'Text = Loc.T("strategies.hint")'),
    ('Content = "Запустить"', 'Content = Loc.T("strategies.run")'),
    ('Content = "Новый конфиг"', 'Content = Loc.T("strategies.new")'),
    ('MakeToolbarBtn("Сохранить"', 'MakeToolbarBtn(Loc.T("strategies.save")'),
    ('MakeToolbarBtn("Отменить"', 'MakeToolbarBtn(Loc.T("strategies.revert")'),
    ('MakeToolbarBtn("Создать копию"', 'MakeToolbarBtn(Loc.T("strategies.copy")'),
    ('MakeToolbarBtn("Переименовать"', 'MakeToolbarBtn(Loc.T("strategies.rename")'),
    ('MakeToolbarBtn("Удалить"', 'MakeToolbarBtn(Loc.T("strategies.delete")'),
    ('MakeToolbarBtn("Списки"', 'MakeToolbarBtn(Loc.T("strategies.lists")'),
    ('MakeToolbarBtn("Найти / заменить"', 'MakeToolbarBtn(Loc.T("strategies.find")'),
    ('UiHelpers.ShowError("Сначала выберите конфиг (.bat) слева")', 'UiHelpers.ShowError(Loc.T("strategies.select_first"))'),
    ('UiHelpers.ShowInfo("Файл сохранён")', 'UiHelpers.ShowInfo(Loc.T("strategies.saved"))'),
    ('_runBtn.Content = "Запускается..."', '_runBtn.Content = Loc.T("strategies.running")'),
    ('_runStatus.Text = "Подготовка запуска..."', '_runStatus.Text = Loc.T("strategies.prep")'),
    ('_runStatus.Text = "Запускаем winws, подождите..."', '_runStatus.Text = Loc.T("strategies.wait")'),
    ('ConsoleLog.Instance.Write($"Запущена стратегия: {file}")', 'ConsoleLog.Instance.Write(Loc.F("strategies.log_run", file))'),
    ('_runStatus.Text = "Запуск завершен"', '_runStatus.Text = Loc.T("strategies.done")'),
    ('_runStatus.Text = "Ошибка запуска"', '_runStatus.Text = Loc.T("strategies.run_error")'),
    ('_runBtn.Content = "Запустить"', '_runBtn.Content = Loc.T("strategies.run")'),
]
for old, new in replacements:
    text = text.replace(old, new)
p.write_text(text, encoding="utf-8")
print('done')
