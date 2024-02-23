# wordsearch-wosecon
Simple .NET (cross platform) library for creating word search puzzles via WoSeCon algorithm. See the [paper](docs/68-IJSES-V6N1.pdf) for more info about it. The algorithm was enhanced to support diagonal word matching and opposite directions (reversed words) so now this algorithm implementation support full 8 directions.

There are some .NET-specific optimizations made, like some unnecessary allocations were avoided etc.

The algorithm uses backtracking approach and is exhaustive. In other words if there is at least one solution to given list of words and matrix size, the algorithm will find it, but it may run very very long for crazy huge number of words with small matrix size.

For regular lists of words (like 50 words) and normally sized matrix (like 20x30 rows/columns), the results are fast and within seconds.

## GUI frontend
**TODO**

## Console frontend
Console frontend is able to generate word search puzzle according to input parameters. To save puzzle to file, use output redirection.

```
WordSearchGenerator.Console --help

  -c             (Default: 20) Specify number of columns for the puzzle.

  -d             (Default: false) Also print solution and other information for debugging purposes.

  -b             (Default: 0) Number from 0.0 to 1.0. Defines % of cells to be marked as 'hidden' so that puzzle solving person
                 has to determine what character belongs to those hidden spots.

  -h             (Default: false) Output the puzzle in a nicely formatted HTML document. Use redirection to save to some file.

  -p             (Default: true) Process input wordlist - replace accented characters, convert to uppercase and remove spaces
                 and other non-word characters.

  -m             Target message to be found when puzzle is completed.

  -r             (Default: 20) Specify number of rows for the puzzle.

  -w, --words    Specify words file name. One word per line is expected. Words can also be provided via standard input.

  --help         Display this help screen.

  --version      Display version information.
```