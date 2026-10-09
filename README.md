# Szablon modułu .NET dla Zapqio

Punkt startowy dla repozytorium typu **.NET**, wykonywanego przez zewnętrznego
[Zapqio Runner](https://github.com/zapqio/dotnet-runner). Metoda `template-dotnet-hello`
przyjmuje imię, zapisuje log i zwraca powitanie jako JSON.

Moduł używa publicznego `Zapqio.Runner.Module.Core` **1.2.0** z nuget.org. Nie wymaga
klonowania źródeł runnera ani lokalnego źródła NuGet. Obsługuje budowanie
na runnerze i gotowe paczki z GitHub Actions.

## Szybki start

1. Na GitHubie wybierz **Use this template → Create a new repository** i sklonuj nowe repozytorium.
2. Zainstaluj **.NET SDK 8**. Nowsze SDK też może kompilować projekt; lokalne uruchamianie
   przykładu i testów nadal wymaga środowiska uruchomieniowego .NET 8.
3. Nadaj modułowi własne nazwy:
   - `AssemblyName` w [Template.Module.csproj](Template.Module.csproj), np. `MyCompany.Orders`;
   - wartość zwracaną przez `NameMethod()` w [HelloMethod.cs](Methods/HelloMethod.cs), np. `orders-hello`.

   Nazwa DLL i nazwy metod muszą być unikalne wśród modułów ładowanych na jednym runnerze.
   Nazwy projektu i przestrzeni nazw możesz na początek pozostawić bez zmian. Marker `##Dll`
   powstaje automatycznie na podstawie `AssemblyName`.

4. Z katalogu głównego uruchom:

   ```powershell
   dotnet test tests/Template.Module.Tests/Template.Module.Tests.csproj -c Release
   dotnet run --project tools/LocalRunner/LocalRunner.csproj -- examples/input.json
   dotnet publish Template.Module.csproj -c Release -o artifacts/publish
   ```

5. Gotowy moduł znajdziesz w **`artifacts/module.zip`**. Możesz sprawdzić jego zawartość:

   ```powershell
   ./tools/Test-Package.ps1 -PackagePath artifacts/module.zip
   ```

   Skrypt wymaga PowerShella (Windows PowerShell 5.1 albo PowerShell 7).
   Jeżeli Windows blokuje uruchamianie skryptów, wykonaj jednorazowo:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./tools/Test-Package.ps1 -PackagePath artifacts/module.zip
   ```

Wejście przykładu:

```json
{"Name":"Anna"}
```

Wynik:

```json
{"Message":"Cześć, Anna!"}
```

`dotnet` sam przywraca zależności z nuget.org. Własne prywatne źródła możesz dodać
do [NuGet.Config](NuGet.Config). Token do repozytorium Git ustawiasz osobno w Webie.

## Wdrożenie przez Web

Wspólny początek dla obu trybów:

1. Zainstaluj aktualnego runnera według [instrukcji instalacji](https://github.com/zapqio/dotnet-runner#instalacja-jako-usługa-windows-windows-service),
   połącz go tokenem z Webem i sprawdź, czy jest online. Dla Windows wariant .NET 8
   wymaga **.NET Desktop Runtime 8 x64**. Sam ten szablon jest biblioteką `net8.0`.
2. Wykonaj commit i push swojego modułu.
3. W Webie dodaj **Repozytorium**: adres swojego repo, rodzaj **.NET**, właściwą gałąź,
   unikalną nazwę paczki i wybrany sposób przygotowania modułu.
4. W **Konfiguracji repozytorium** zapisz token GitHub z odczytem repozytorium;
   dla trybu CI potrzebny jest również odczyt Actions. Token przydaje się także dla
   publicznych repozytoriów ze względu na limit anonimowych zapytań API.
5. Kliknij **Sprawdź aktualizacje**, a następnie pobierz migawkę wybranego commita.
   Otwarcie strony repozytorium pokazuje ostatnio zapisany stan.
6. W **Runnery → Zewnętrzne → wybrany runner → Wdrożenia** znajdź repozytorium
   i kliknij **Wdróż** dla sprawdzonego commita gałęzi. Aby wybrać inną wersję,
   wpisz jej pełny hash w polu wdrażania konkretnego commita i kliknij **Wdróż commit**.

### Budowanie na runnerze

W repozytorium wybierz **Budowanie na ExternalRunnerze** (`Runner`). Web dostarcza
źródła konkretnego commita. Konto usługi runnera musi mieć dostęp do `dotnet` z SDK
8 lub nowszym, nuget.org oraz prawa zapisu do katalogów budowania i cache NuGet.
Sam Desktop Runtime służy do uruchamiania i nie zawiera kompilatora.

Dla tego ogólnego modułu wystarczy systemowo zainstalowane .NET SDK i poprawne
środowisko usługi. Jeśli SDK zainstalowano po uruchomieniu usługi, uruchom ją ponownie.

Na serwerze, w PowerShellu administratora, przejdź do katalogu runnera:

```powershell
Set-Location C:\zapqio\runner
./Zapqio.Runner.exe deploy list
./Zapqio.Runner.exe deploy show <deployment-id>
./Zapqio.Runner.exe deploy approve <deployment-id>
```

Po potwierdzeniu zgody przez Web usługa uruchomi się ponownie, zbuduje moduł
i go załaduje. Poczekaj na **Applied**, następnie dodaj metodę do kroku pipeline'u,
wybierz tego runnera i wykonaj próbę z [examples/input.json](examples/input.json).
Restart przerywa trwające zadania — zaplanuj zatwierdzenie w odpowiednim momencie.

### ZIP z CI

W repozytorium wybierz **ZIP z CI** (`CiZip`). Workflow [build.yml](.github/workflows/build.yml)
uruchamia testy i publikuje moduł na Windows oraz Linux. Po udanym pushu albo ręcznym
uruchomieniu workflowu zadanie Windows przesyła jeden artefakt **`zapqio-module`**,
zawierający **`module.zip`** w katalogu głównym. Pull requesty uruchamiają weryfikację,
ale nie przesyłają artefaktu do wdrożenia.

Poczekaj na zakończenie całego workflowu dla **dokładnie tego commita**, który wybierasz
w Webie. Artefakt ma retencję 30 dni. Po wygaśnięciu ponów workflow dla tego samego
commita, np. ponawiając jego wcześniejszy przebieg; nowy build nowszego commita go nie zastąpi.

Zatwierdzenie na serwerze odbywa się tak samo przez `deploy approve`. W tym trybie
serwer potrzebuje runtime'u i zależności uruchomieniowych modułu, a narzędzia kompilacji
zapewnia CI. GitHub Release ani plik `.nupkg` nie zastępują artefaktu `zapqio-module`.

Workflow korzysta z oficjalnych akcji [checkout](https://github.com/actions/checkout),
[setup-dotnet](https://github.com/actions/setup-dotnet) i [upload-artifact](https://github.com/actions/upload-artifact).
Nie wymaga sekretów NuGet i nie wdraża samodzielnie kodu na runnera.

## Struktura

```text
Template.Module.csproj           # jedyny projekt w katalogu głównym
Methods/HelloMethod.cs           # IRunnerMethod: wejście, wykonanie, wynik
Models/HelloInput.cs             # typ wejścia i schemat dla Weba
Models/HelloOutput.cs            # typ wyniku
Services/GreetingService.cs      # własna usługa z IRunnerInjection
examples/input.json
examples/output.json
tools/LocalRunner/               # lokalne wywołanie przykładu
tools/Test-Package.ps1           # kontrola ZIP-a
tests/Template.Module.Tests/     # testy metody
.github/workflows/build.yml      # testy i artefakt dla Weba
NuGet.Config                    # publiczne zależności
```

Projekty narzędziowe i testowe pozostają w podkatalogach. Główny `.csproj` wyklucza
`tools/`, `tests/` i `artifacts/` z automatycznego zbierania plików.
Web wymaga dokładnie jednego `.csproj` w katalogu głównym repozytorium.

## Jak napisać własną metodę

Dodaj publiczną, konkretną klasę implementującą `IRunnerMethod`:

- `NameMethod()` zwraca unikalną nazwę widoczną w platformie.
- `InData()` i `OutData()` zwracają typy używane do opisu wejścia i wyniku w Webie.
- `Run(string data)` otrzymuje tekst wejścia i zwraca `Task<string>` z wynikiem.
  Dla JSON samodzielnie deserializuj wejście i serializuj wynik.

W przykładzie nazwy pól są rozróżniane wielkością liter: użyj `Name`, tak jak
w modelu wejściowym. Niepoprawny JSON, brak imienia i puste imię kończą metodę błędem.
Adnotacje modelu opisują schemat; walidację rzeczywistych danych wykonuje metoda.

`GreetingService` pokazuje wstrzykiwanie własnej usługi. Runner znajduje klasy
z `IRunnerInjection` i przekazuje je do konstruktorów metod. Konstruktor powinien
przygotować obiekt; operacje biznesowe wykonuj w `Run`.

Metody i usługi są współdzielone przez kolejne wywołania, także równoległe.
Dane pojedynczego zadania trzymaj w zmiennych lokalnych. Unikaj pól z wejściem,
wynikiem lub kontekstem bieżącej próby. Przy tworzeniu dokumentów lub innych
skutkach zewnętrznych uwzględnij możliwość ponowienia zadania.

## Logi i kontekst zadania

```csharp
RunnerLog.Info("Rozpoczęto przetwarzanie");
RunnerLog.Warning("Użyto wartości domyślnej");
var job = JobContext.Current;
```

`RunnerLog` jest częścią Core. Log trafia do bieżącego zadania. `Debug` zależy od
ustawionego poziomu logowania runnera; nie jest wynikiem kroku. `Console.WriteLine`
również zapisuje log. Wynikiem jest tekst zwrócony przez `Run`.

`JobContext.Current` udostępnia `JobId`, `AttemptId` i `MethodName`. `JobId` pozwala
rozpoznać tę samą operację po ponowieniu, a `AttemptId` identyfikuje konkretną próbę.
Poza wywołaniem metody kontekst może być pusty. Nie zapisuj danych dostępowych w logach.

Lokalny program w `tools/LocalRunner` tworzy przykładową metodę i jej zależność,
ustawia sztuczny kontekst oraz kieruje logi na stderr. JSON wyniku zapisuje na stdout.
Nie łączy się z Webem ani nie instaluje usługi. Po dodaniu kolejnej metody dostosuj
ten program lub wywołaj ją z własnego testu. Lokalny program nie zastępuje próby
załadowania ZIP-a i wykonania metody na docelowym runnerze.

## Pakowanie i zależności

`dotnet publish Template.Module.csproj -c Release -o artifacts/publish` tworzy
`artifacts/module.zip`. Bez `-o` paczka trafia do `bin/Release/net8.0/module.zip`.
ZIP powstaje obok `PublishDir`, dlatego działa także komenda uruchamiana przez runnera.

Marker `##Dll` wskazuje DLL z metodami. Core ma `ExcludeAssets="runtime"`, ponieważ
runner dostarcza własną kopię tego kontraktu. Program lokalny i projekt testów
referencjonują Core także do uruchamiania, ale ich pliki nie trafiają do paczki modułu.
Inne biblioteki potrzebne modułowi w runtime powinny trafić do publish normalnie.

Jeśli zmienisz nazwę pliku `Template.Module.csproj`, zaktualizuj odwołania projektów
w `tools/` i `tests/` oraz polecenie publish w workflowie. Samą nazwę DLL możesz zmienić
przez `AssemblyName`, bez zmiany tych ścieżek.

## Najczęstsze problemy

| Objaw | Co sprawdzić |
| --- | --- |
| `dotnet` nie istnieje lub nie ma SDK | Zainstaluj .NET SDK na komputerze budującym; sam runtime nie wystarcza. |
| Program lokalny wymaga .NET 8 | Doinstaluj runtime 8 lub SDK 8, nawet jeśli masz już nowsze SDK. |
| Niepoprawna struktura repozytorium | W katalogu głównym musi być jeden `.csproj`; narzędzia i testy umieść niżej. |
| Błąd pobierania Core | Sprawdź dostęp do nuget.org oraz `NuGet.Config`; nie buduj Core ze źródeł runnera. |
| Brak artefaktu CI | Sprawdź commit, sukces całego workflowu, nazwę `zapqio-module` i retencję. |
| Wdrożenie czeka | Sprawdź `deploy list` i zatwierdź właściwe ID na serwerze. |
| Metoda nie pojawia się | Sprawdź `Applied`, logi runnera, marker `##Dll`, zależności konstruktora i unikalność nazw. |
| Konflikt DLL po drugim module z szablonu | Nadaj modułom różne `AssemblyName` i różne nazwy metod. |

Kod szablonu jest udostępniany na licencji [Apache-2.0](LICENSE).
