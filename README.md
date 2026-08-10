<p align="center">
  <img src="src/WordSearchGenerator.Desktop/WordSearchGenerator.Desktop.png" width="128" alt="WoSe application icon">
</p>

# WoSe

WoSe is a Windows application for creating word searches and question-based
crosswords. Enter the content, choose a board size, and generate.

**[Download the development build for Windows x64](https://github.com/martinrotter/wordsearch-wosecon/releases/tag/dev)**

The development build follows the latest successful build of the `master`
branch. Extract the ZIP and run the application; installation is not required.

## What it can do

- Create word searches or question-based crosswords.
- Hide a secret message in unused cells or numbered answer cells.
- Set the board size, headings, content, and black boxes.
- Preview the printable puzzle and its solution.
- Print or export as Word, PDF, HTML, or PNG.
- Save projects and import word or question lists from text files.
- Work in English or Czech.

Simple puzzles can generate instantly, while dense layouts may take much longer.
WoSe estimates difficulty, shows progress, runs parallel searches, and lets
you cancel generation at any time.

## For developers

The solution requires Windows and the .NET 10 SDK:

```powershell
dotnet build src/WordSearchGenerator.sln -c Release
dotnet test src/WordSearchGenerator.sln -c Release --filter "TestCategory!=LongRunning"
```

The reusable `WoSe.Common` library is in `WordSearchGenerator.Common`; the
`WoSe` application is in `WordSearchGenerator.Desktop`. The algorithm is based on the
[included WoSeCon paper](docs/68-IJSES-V6N1.pdf).

## License

Licensed under the [GNU General Public License v3.0](LICENSE).
