NAPRAWIONO:
- dodano using Microsoft.Extensions.DependencyInjection; dla Services.Configure(...)
- dodano using System.IO; tam, gdzie używane są Path/File/Directory/FileStream/FileInfo
- usunięto CharacterSpacing z TextBlock, bo WPF go nie obsługuje
- wymuszono nie-null _app przy mapowaniu endpointów
- błędy InitializeComponent / StatusText / QrImage itd. były błędami kaskadowymi po nieudanym kompilowaniu XAML

Jeśli Visual Studio nadal pokazuje stare błędy:
1. Zamknij aplikację.
2. Build > Clean Solution.
3. Usuń foldery bin i obj z projektu.
4. Build > Rebuild Solution.
