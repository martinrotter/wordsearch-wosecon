# WoSeCon Word Search Generator

WoSeCon is a .NET implementation of an exhaustive, backtracking word-search
construction algorithm. The repository contains a reusable cross-platform core
library and a Windows desktop application for designing, generating, previewing,
printing, and exporting word-search puzzles.

The implementation is based on the WoSeCon algorithm described in the
[included paper](docs/68-IJSES-V6N1.pdf), extended to support all eight
horizontal, vertical, and diagonal directions.

## Features

### Core library

- Exhaustive backtracking: if a valid placement exists, the search can find it.
- All eight word directions, including reversed and diagonal words.
- Normal and quiz construction modes.
- Optional caller-provided placement ordering.
- Progress reporting through `IProgress<ConstructionProgress>`.
- Cooperative cancellation through `CancellationToken`.
- Reusable generator instances for sequential construction runs.
- Construction statistics including tested positions and backtracks.
- Fast heuristic difficulty estimation without starting a search.
- Secret-message placement in cells not occupied by words.
- Explicit black boxes for cells unused by words or the secret message.

### Desktop application

- Fluent-themed WPF interface with a CefSharp HTML preview.
- Normal word-search and quiz modes.
- Configurable matrix dimensions, headings, secret message, and worker count.
- Parallel Monte Carlo attempts using independently shuffled placement orders.
- Detailed progress, search-depth, timing, and backtracking information.
- Puzzle and solution previews.
- Printing and export to PDF, HTML, and board-only PNG.
- Versioned `.wosecon` project files that preserve inputs, exact generated
  placements, the winning seed, and generation statistics.
- UTF-8 import for word lists and quiz questions.
- English and Czech user interfaces using native `.resx` satellite resources.
- Versioned JSON application settings.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows x64 for the WPF desktop application
- Any platform supported by .NET 10 for the core library

## Run the desktop application

From the repository root:

```powershell
dotnet run --project src/WordSearchGenerator.Desktop/WordSearchGenerator.Desktop.csproj -c Release
```

The application starts with a small valid 4×5 example that can be generated
immediately.

## Use the core library

```csharp
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Api;

var words = new List<WordInfo>
{
  new() { WordNumber = 1, Text = "cat" },
  new() { WordNumber = 2, Text = "dog" },
  new() { WordNumber = 3, Text = "sun" },
  new() { WordNumber = 4, Text = "map" }
};

var generator = new WoSeCon(
  words,
  rowCount: 4,
  columnCount: 5,
  quizMode: false);

var progress = new Progress<ConstructionProgress>(state =>
{
  Console.WriteLine(
    $"Placed {state.PlacedWordCount}/{state.TotalWordCount}; " +
    $"tested {state.TestedPositions:N0}; " +
    $"backtracks {state.Backtrackings:N0}");
});

using var cancellation = new CancellationTokenSource();
generator.Construct(progress, cancellation.Token);

var board = new Board(
  generator.Words,
  rowCount: 4,
  columnCount: 5,
  quizMode: false,
  message: "hello");

Console.WriteLine(board.PrintBoard());
```

`Construct` is synchronous. Run it on a background thread when calling it from
a UI or server request. Concurrent calls on the same `WoSeCon` instance are
rejected; use one instance per parallel attempt.

Words are case-sensitive, and each word must contain at least two characters.
The constructor clones and sorts the supplied `WordInfo` objects, so generation
does not mutate the caller's objects.

## Placement ordering

The optional `Locator.LocationOrderer` delegate controls the order in which
candidate locations are tested:

```csharp
var random = new Random(12345);

IReadOnlyList<DirectedLocation> Shuffle(
  IReadOnlyList<DirectedLocation> locations)
{
  var shuffled = locations.ToArray();

  for (var index = shuffled.Length - 1; index > 0; index--)
  {
    var otherIndex = random.Next(index + 1);
    (shuffled[index], shuffled[otherIndex]) =
      (shuffled[otherIndex], shuffled[index]);
  }

  return shuffled;
}

var generator = new WoSeCon(words, 12, 12, false, Shuffle);
```

Without an orderer, construction is deterministic for the same input. A
shuffled order can find a solution earlier, but does not change the validity of
the algorithm. The desktop application runs several shuffled attempts in
parallel and accepts the first successful result.

## Quiz mode

In quiz mode, every answer has an associated question:

```csharp
new WordInfo
{
  WordNumber = 1,
  Text = "prague",
  QuizQuestion = "What is the capital of the Czech Republic?"
};
```

One leading cell is reserved for the question number and direction before each
answer. A cell can be the leading question cell for at most one answer.

## Secret messages and black boxes

The secret message is not part of the placement search. After all words have
been placed, `Board` writes the message into vacant cells in row-major order.
Any cells left over become black boxes. Creating the board fails if the message
does not fit in the remaining cells.

This means construction time is primarily affected by the matrix dimensions,
the number and lengths of words, the available crossings, quiz question cells,
and the candidate-location order.

## Difficulty estimation

`WoSeCon.EstimateDifficulty` returns a fast heuristic band:

```csharp
var estimate = WoSeCon.EstimateDifficulty(
  words,
  rowCount: 12,
  columnCount: 12,
  quizMode: false,
  parallelism: Environment.ProcessorCount);
```

Possible values range from `FastInSeconds` through `CrazySlowHours`, plus
`LikelyImpossible`. This is an estimate, not a time guarantee or proof that a
solution exists.

## Files and persistence

### Projects

The desktop application uses versioned JSON `.wosecon` files. A project stores:

- puzzle mode and matrix dimensions;
- words or quiz questions and answers;
- secret message and printable headings;
- generation settings;
- optional exact generated placements and generation statistics.

Reopening a generated project restores its board without repeating the search.

### Imports

Normal-mode UTF-8 files contain one word per line:

```text
cat
dog
sun
```

Quiz-mode files are tab-separated, with one entry per line:

```text
prague<TAB>What is the capital of the Czech Republic?
```

Blank lines are ignored. Answers are case-sensitive, surrounding whitespace is
trimmed, and duplicate answers are removed.

### Application settings

Application preferences are separate from puzzle projects and are stored at:

```text
%LOCALAPPDATA%\WoSeCon\settings.json
```

The selected UI culture is applied during startup. Changing the language in
**File → Settings** takes effect after restarting the application. Missing,
invalid, or unsupported settings safely fall back to a supported operating
system language or English.

## Build and test

Build the complete solution on Windows:

```powershell
dotnet build src/WordSearchGenerator.sln -c Release
```

Build only the cross-platform core library:

```powershell
dotnet build src/WordSearchGenerator.Common/WordSearchGenerator.Common.csproj -c Release
```

Run the test suite:

```powershell
dotnet test src/WordSearchGenerator.Tests/WordSearchGenerator.Tests.csproj -c Release
```

Some construction tests intentionally exercise backtracking and parallel
Monte Carlo generation, so their duration depends on the machine.

## Repository layout

```text
docs/                                  Algorithm paper
src/WordSearchGenerator.Common/        Cross-platform core library
src/WordSearchGenerator.Desktop/       WPF desktop application
src/WordSearchGenerator.Tests/         MSTest test suite
test-data/                             Test word lists
words/                                 Example word lists
```

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
