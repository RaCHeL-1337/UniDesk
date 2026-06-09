# UniDesk

UniDesk to aplikacja webowa ASP.NET Core MVC/API do obslugi zgloszen technicznych. Projekt bazuje na rozwiazaniu rozwijanym podczas laboratoriow 1-12 i zostal rozszerzony w ramach projektu zaliczeniowego z laboratorium 13 do poziomu v1.1 STRETCH.

## Wymagania

- .NET 8 SDK
- SQLite
- baza danych domyslnie zapisywana w `src/UniDesk.Web/unidesk.db`

## Baza danych

Connection string znajduje sie w `src/UniDesk.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=unidesk.db"
  }
}
```

Aplikacja podczas startu wykonuje oczekujace migracje przed zaladowaniem danych startowych. Migracje mozna tez uruchomic recznie:

```powershell
dotnet ef database update --project .\src\UniDesk.Web\UniDesk.Web.csproj
```

Schemat zawiera tabele zgloszen, komentarzy, ASP.NET Core Identity, role, tokeny oraz powiazania uzytkownik-rola.

## Dane startowe

Przy starcie pustego systemu aplikacja tworzy:

- role `Admin` i `User`;
- domyslne konto administratora;
- testowe zgloszenia;
- komentarze przypisane do testowych zgloszen.

Dane domyslnego administratora:

```text
admin@unidesk.local
Admin123!
```

Dane startowe sa konfigurowane przez Options Pattern w sekcji `SeedData` w pliku `appsettings.json`.

## Dyskusja, prywatnosc i Markdown

Komentarze w zgloszeniach obsluguja bezpieczny Markdown renderowany po stronie serwera:

- `**bold**`;
- kod inline w backtickach;
- bloki kodu w potrojnych backtickach.

Surowy HTML jest kodowany przed nalozeniem formatowania Markdown, dlatego wejscie typu `<script>` zostaje wyswietlone jako tekst i nie wykonuje sie w przegladarce.

Dostep do osi komentarzy ma tylko autor zgloszenia oraz administrator. Inny zalogowany uzytkownik moze wejsc na szczegoly zgloszenia, ale interfejs dyskusji jest dla niego zablokowany.

## Rate Limiting

UniDesk korzysta z wbudowanego middleware Rate Limiting w ASP.NET Core. Globalna polityka jest konfigurowana w sekcji `RateLimiting` w `appsettings.json`.

Po przekroczeniu limitu zadan aplikacja zwraca:

```text
429 Too Many Requests
```

## Uruchomienie

```powershell
dotnet run --launch-profile https --project .\src\UniDesk.Web\UniDesk.Web.csproj
```

Domyslne adresy lokalne:

- `https://localhost:7002`
- `http://localhost:5174`

## Punkty wejscia

MVC:

- `/`
- `/Account/Login`
- `/Account/Register`
- `/Tickets`
- `/Tickets/Create`
- `/Tickets/Details/{id}`

API:

- `/api/tickets`
- `/api/tickets/{id}`
- `/api/v2/tickets`
- `/register`
- `/login`
- `/refresh`

Diagnostyka:

- `/health/live`
- `/health/ready`
- `/swagger` tylko w srodowisku Development

## Logowanie i Health Checks

UniDesk uzywa Serilog do logowania strukturalnego w formacie JSON.

Logi runtime sa zapisywane w:

```text
src/UniDesk.Web/logs/unidesk-YYYYMMDD.json
```

Endpoint `/health/live` sprawdza, czy aplikacja dziala jako proces. Endpoint `/health/ready` zwraca JSON i sprawdza gotowosc aplikacji, w tym dostep do SQLite.

## Testy

```powershell
dotnet test .\src\UniDesk.UnitTests\UniDesk.UnitTests.csproj -p:UseAppHost=false
dotnet test .\src\UniDesk.IntegrationTests\UniDesk.IntegrationTests.csproj -p:UseAppHost=false
```
