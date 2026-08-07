<p align="center">
  <img src="src/WordSearchGenerator.Desktop/WordSearchGenerator.Desktop.png" width="128" alt="WoSeCon application icon">
</p>

# WoSeCon Word Search Generator

WoSeCon is a Windows application for creating polished word-search puzzles. Add
your words, choose the board size, and let WoSeCon find a suitable layout.

**[Download the development build for Windows x64](https://github.com/martinrotter/wordsearch-wosecon/releases/download/dev/wosecon-win64.zip)**

The development build follows the latest successful build of the `master`
branch. Extract the ZIP and run the application; installation is not required.

## What it can do

- Create ordinary word searches and question-based quizzes.
- Hide a secret message in the unused letters.
- Leave remaining unused cells as black boxes.
- Preview both the puzzle and its solution.
- Print puzzles or export them as PDF, HTML, or PNG.
- Save projects and import word or question lists.
- Display the application in English or Czech.

Generation can be almost instant for simple puzzles, while very dense or
difficult puzzles may take considerably longer. Progress is shown while the
application searches, and generation can be cancelled at any time.

## For developers

The solution requires Windows and the .NET 10 SDK:

```powershell
dotnet build src/WordSearchGenerator.sln -c Release
dotnet test src/WordSearchGenerator.sln -c Release --filter "TestCategory!=LongRunning"
```

The reusable generator is in `WordSearchGenerator.Common`; the desktop
application is in `WordSearchGenerator.Desktop`. The algorithm is based on the
[included WoSeCon paper](docs/68-IJSES-V6N1.pdf).

## License

Licensed under the [GNU General Public License v3.0](LICENSE).
