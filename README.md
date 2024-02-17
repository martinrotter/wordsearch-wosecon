# wordsearch-wosecon
Simple .NET/WPF library/app for creating word search puzzles via WoSeCon algorithm. See the [paper](docs/68-IJSES-V6N1.pdf) for more info about it.

The algorithm uses backtracking approach and is exhaustive. In other words if there is at least one solution to given list of words and matrix size, the algorithm will find it, but it may run very long for crazy huge number of words with small matrix size.

For regular lists of words (like 30 words) and normally sized matrix (like 20x30 rows/columns), the results are fast and within seconds.

## Console version
Console version is able to generate word search puzzle according to input parameters. To save puzzle to file, use output redirection.

```
WordSearchGenerator.Console --help

Copyright (C) 2024 WordSearchGenerator.Console
  -d             (Default: false) Also print solution and other information for debugging purposes.
  -c             (Default: 20) Specify number of columns for the puzzle.
  -r             (Default: 20) Specify number of rows for the puzzle.
  -w, --words    Required. Specify words file name. One word per line is expected.
  -m             Target message to be found when puzzle is completed.
  --help         Display this help screen.
  --version      Display version information.
```