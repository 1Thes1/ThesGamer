# Thes Gamer

**PC care · boost · drivers · game tuning** for Windows.

Сделано **Thes** · [github.com/1Thes1](https://github.com/1Thes1)

Интерфейс в стиле Thes VPN: тёмный UI, icon-rail, карточки, teal-акценты.

---

## Скачать

Готовый Windows x64 билд — во вкладке [**Releases**](https://github.com/1Thes1/ThesGamer/releases):

1. Скачай `ThesGamer-*-win-x64.zip`
2. Распакуй куда угодно (не обязательно Program Files)
3. Запусти `ThesGamer.exe`

.NET runtime ставить **не нужно** (self-contained).

---

## Возможности / Features

| RU | EN |
|----|----|
| Обзор CPU/RAM/диск, Defender, процессы | Overview CPU/RAM/disk, Defender, processes |
| Очистка RAM + автопорог + до/после | RAM cleanup + auto threshold + before/after |
| Чистка Temp + до/после | Temp cleanup + before/after |
| Скан драйверов → Windows Update | Driver scan → Windows Update |
| Игровой буст и пресеты | Game boost and presets |
| Температуры CPU/GPU | CPU/GPU temperatures |
| RU/EN язык | RU/EN language |
| Проверка обновлений GitHub | GitHub update checker |
| Кастомный title bar | Custom title bar |


---

## Сборка из исходников

Нужны Windows 10/11 и [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bat
dotnet build ThesGamer\ThesGamer.csproj -c Release
dotnet publish ThesGamer\ThesGamer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist\ThesGamer-win-x64
```

Или после Release-сборки: `RUN Thes Gamer.bat`

---

## Настройки

`%APPDATA%\ThesGamer\settings.json`

---

## Важно

- Это companion-утилита, **не замена** Windows Defender
- Драйверы обновляются только через **официальные** каналы Windows
- Точка восстановления перед бустом рекомендуется (нужны права админа и защита системы)
- Не связан с Pearl Abyss и издателями игр

## Лицензия

MIT © Thes
