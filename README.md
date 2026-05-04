# UniDesk

Репозиторий содержит ASP.NET Core веб‑приложение и тесты.

## Требования

- .NET SDK 10+ (в решении есть проект с `TargetFramework=net10.0`)

## Быстрый старт

Сборка:

```powershell
dotnet build .\UniDesk.sln
```

Запуск веб‑приложения:

```powershell
dotnet run --project .\src\UniDesk.Web\UniDesk.Web.csproj
```

Тесты:

```powershell
dotnet test .\UniDesk.sln
```
