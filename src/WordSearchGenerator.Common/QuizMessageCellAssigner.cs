using Wose.Common.WoSeCon.Api;

namespace Wose.Common
{
  internal static class QuizMessageCellAssigner
  {
    private const int InitialAnswerLimit = 2;
    private const int MaximumSearchMessageLength = 128;
    private const int MaximumRefinementMessageLength = 32;

    private sealed record Candidate(
      Board.Cell Cell,
      int Row,
      int Column,
      int Position,
      int[] AnswerIndices);

    private readonly record struct AssignmentScore(
      int MaximumAnswerCount,
      int SquaredAnswerCount,
      double Crowding,
      double TargetDistance)
    {
      public bool IsBetterThan(AssignmentScore other)
      {
        if (MaximumAnswerCount != other.MaximumAnswerCount)
        {
          return MaximumAnswerCount < other.MaximumAnswerCount;
        }

        if (SquaredAnswerCount != other.SquaredAnswerCount)
        {
          return SquaredAnswerCount < other.SquaredAnswerCount;
        }

        if (Math.Abs(Crowding - other.Crowding) > 1e-9)
        {
          return Crowding < other.Crowding;
        }

        return TargetDistance < other.TargetDistance - 1e-9;
      }
    }

    public static void Assign(Board board)
    {
      if (board.Message.Length == 0)
      {
        return;
      }

      var answerIndices = new Dictionary<WordInfo, int>(
        ReferenceEqualityComparer.Instance);

      for (var index = 0; index < board.Words.Count; index++)
      {
        answerIndices.Add(board.Words[index], index);
      }

      var candidates = new List<Candidate>();
      var cellsByCharacter = new Dictionary<char, List<int>>();

      for (var row = 0; row < board.Rows; row++)
      for (var column = 0; column < board.Columns; column++)
      {
        var cell = board.Matrix[row, column];

        if (cell.Type != Board.Cell.CellType.CharFromText)
        {
          continue;
        }

        var candidateIndex = candidates.Count;
        candidates.Add(new Candidate(
          cell,
          row,
          column,
          row * board.Columns + column,
          cell.Words.Select(word => answerIndices[word]).Distinct().ToArray()));

        if (!cellsByCharacter.TryGetValue(cell.Char, out var indices))
        {
          indices = [];
          cellsByCharacter.Add(cell.Char, indices);
        }

        indices.Add(candidateIndex);
      }

      var choices = new int[board.Message.Length][];
      var targets = new double[board.Message.Length];

      for (var index = 0; index < board.Message.Length; index++)
      {
        if (!cellsByCharacter.TryGetValue(board.Message[index], out var indices))
        {
          throw CannotPlace(index);
        }

        targets[index] = board.Message.Length == 1
          ? (board.Rows * board.Columns - 1) / 2.0
          : (double)index * (board.Rows * board.Columns - 1) /
            (board.Message.Length - 1);
        choices[index] = indices
          .OrderBy(candidateIndex =>
            Math.Abs(candidates[candidateIndex].Position - targets[index]))
          .ThenBy(candidateIndex => candidates[candidateIndex].Position)
          .ToArray();
      }

      // Keep a valid assignment even if the bounded balancing search runs out of time.
      var assignment = CreateOriginalAssignment(choices, candidates.Count);
      var greedy = CreateBalancedGreedy(
        board,
        candidates,
        choices,
        targets);

      if (Score(board, candidates, targets, greedy).IsBetterThan(
            Score(board, candidates, targets, assignment)))
      {
        assignment = greedy;
      }

      var answerCounts = CountAnswers(assignment, candidates, board.Words.Count);
      var originalMaximum = answerCounts.Max();

      if (originalMaximum > InitialAnswerLimit &&
          board.Message.Length <= MaximumSearchMessageLength)
      {
        for (var limit = InitialAnswerLimit; limit < originalMaximum; limit++)
        {
          var balanced = FindWithinAnswerLimit(
            board,
            candidates,
            choices,
            targets,
            limit);

          if (balanced == null)
          {
            continue;
          }

          assignment = balanced;
          break;
        }
      }

      ImproveAssignment(board, candidates, choices, targets, assignment);

      for (var index = 0; index < assignment.Length; index++)
      {
        candidates[assignment[index]].Cell.MessageIndex = index + 1;
      }
    }

    private static MessageCannotBePlacedException CannotPlace(int messageIndex)
    {
      return new MessageCannotBePlacedException(
        $"message character at index {messageIndex} cannot be assigned to a distinct answer cell");
    }

    private static int[] CreateOriginalAssignment(int[][] choices, int candidateCount)
    {
      var used = new bool[candidateCount];
      var assignment = new int[choices.Length];

      for (var index = 0; index < choices.Length; index++)
      {
        var chosen = -1;

        foreach (var candidateIndex in choices[index])
        {
          if (!used[candidateIndex])
          {
            chosen = candidateIndex;
            break;
          }
        }

        if (chosen < 0)
        {
          throw CannotPlace(index);
        }

        assignment[index] = chosen;
        used[chosen] = true;
      }

      return assignment;
    }

    private static int[] CreateBalancedGreedy(
      Board board,
      IReadOnlyList<Candidate> candidates,
      int[][] choices,
      double[] targets)
    {
      var assignment = Enumerable.Repeat(-1, choices.Length).ToArray();
      var used = new bool[candidates.Count];
      var counts = new int[board.Words.Count];
      var order = Enumerable.Range(0, choices.Length)
        .OrderBy(index => choices[index].Length)
        .ThenBy(index => index);

      foreach (var messageIndex in order)
      {
        var available = choices[messageIndex]
          .Where(candidateIndex => !used[candidateIndex])
          .OrderBy(candidateIndex => candidates[candidateIndex].AnswerIndices
            .Sum(answerIndex => 2 * counts[answerIndex] + 1));
        var chosen = choices.Length <= MaximumRefinementMessageLength
          ? available
            .ThenBy(candidateIndex => CrowdingWithSelected(
              candidates,
              assignment,
              candidateIndex))
            .ThenBy(candidateIndex => Math.Abs(
              candidates[candidateIndex].Position - targets[messageIndex]))
            .ThenBy(candidateIndex => candidateIndex)
            .First()
          : available
            .ThenBy(candidateIndex => Math.Abs(
              candidates[candidateIndex].Position - targets[messageIndex]))
            .ThenBy(candidateIndex => candidateIndex)
            .First();

        assignment[messageIndex] = chosen;
        used[chosen] = true;

        foreach (var answerIndex in candidates[chosen].AnswerIndices)
        {
          counts[answerIndex]++;
        }
      }

      return assignment;
    }

    private static int[] CountAnswers(
      int[] assignment,
      IReadOnlyList<Candidate> candidates,
      int answerCount)
    {
      var counts = new int[answerCount];

      foreach (var candidateIndex in assignment)
      foreach (var answerIndex in candidates[candidateIndex].AnswerIndices)
      {
        counts[answerIndex]++;
      }

      return counts;
    }

    private static int[] FindWithinAnswerLimit(
      Board board,
      IReadOnlyList<Candidate> candidates,
      int[][] choices,
      double[] targets,
      int limit)
    {
      if ((long)board.Message.Length > (long)limit * board.Words.Count)
      {
        return null;
      }

      var order = Enumerable.Range(0, choices.Length)
        .OrderBy(index => choices[index].Length)
        .ThenBy(index => index)
        .ToArray();
      var previousSameCharacter = new int[choices.Length];
      var lastIndexByCharacter = new Dictionary<char, int>();

      for (var index = 0; index < choices.Length; index++)
      {
        previousSameCharacter[index] = lastIndexByCharacter.TryGetValue(
          board.Message[index], out var previous) ? previous : -1;
        lastIndexByCharacter[board.Message[index]] = index;
      }

      var assignment = Enumerable.Repeat(-1, choices.Length).ToArray();
      var used = new bool[candidates.Count];
      var counts = new int[board.Words.Count];
      var visitedNodes = 0;
      var nodeLimit = choices.Length <= 16 ? 100_000 : 30_000;

      bool Search(int depth)
      {
        if (depth == order.Length)
        {
          return true;
        }

        if (visitedNodes >= nodeLimit)
        {
          return false;
        }

        visitedNodes++;

        var messageIndex = order[depth];
        var previousIndex = previousSameCharacter[messageIndex];
        var options = choices[messageIndex]
          .Where(candidateIndex =>
            !used[candidateIndex] &&
            (previousIndex < 0 || candidateIndex > assignment[previousIndex]) &&
            candidates[candidateIndex].AnswerIndices.All(
              answerIndex => counts[answerIndex] < limit))
          .OrderBy(candidateIndex => candidates[candidateIndex].AnswerIndices
            .Sum(answerIndex => 2 * counts[answerIndex] + 1))
          .ThenBy(candidateIndex => CrowdingWithSelected(
            candidates,
            assignment,
            candidateIndex))
          .ThenBy(candidateIndex => Math.Abs(
            candidates[candidateIndex].Position - targets[messageIndex]))
          .ThenBy(candidateIndex => candidateIndex);

        foreach (var candidateIndex in options)
        {
          assignment[messageIndex] = candidateIndex;
          used[candidateIndex] = true;

          foreach (var answerIndex in candidates[candidateIndex].AnswerIndices)
          {
            counts[answerIndex]++;
          }

          if (Search(depth + 1))
          {
            return true;
          }

          foreach (var answerIndex in candidates[candidateIndex].AnswerIndices)
          {
            counts[answerIndex]--;
          }

          used[candidateIndex] = false;
          assignment[messageIndex] = -1;

          if (visitedNodes >= nodeLimit)
          {
            return false;
          }
        }

        return false;
      }

      return Search(0) ? assignment : null;
    }

    private static double CrowdingWithSelected(
      IReadOnlyList<Candidate> candidates,
      int[] assignment,
      int candidateIndex)
    {
      var candidate = candidates[candidateIndex];
      var crowding = 0.0;

      foreach (var selectedIndex in assignment)
      {
        if (selectedIndex < 0)
        {
          continue;
        }

        var selected = candidates[selectedIndex];
        var rowDistance = candidate.Row - selected.Row;
        var columnDistance = candidate.Column - selected.Column;
        crowding += 1.0 /
                    (1 + rowDistance * rowDistance +
                     columnDistance * columnDistance);
      }

      return crowding;
    }

    private static void ImproveAssignment(
      Board board,
      IReadOnlyList<Candidate> candidates,
      int[][] choices,
      double[] targets,
      int[] assignment)
    {
      if (assignment.Length > MaximumRefinementMessageLength)
      {
        return;
      }

      var used = new bool[candidates.Count];

      foreach (var candidateIndex in assignment)
      {
        used[candidateIndex] = true;
      }

      for (var iteration = 0;
           iteration < Math.Min(
             assignment.Length * 2,
             MaximumRefinementMessageLength);
           iteration++)
      {
        var bestScore = Score(board, candidates, targets, assignment);
        var bestMessageIndex = -1;
        var bestCandidateIndex = -1;

        for (var messageIndex = 0; messageIndex < assignment.Length; messageIndex++)
        {
          var originalCandidateIndex = assignment[messageIndex];
          used[originalCandidateIndex] = false;

          foreach (var candidateIndex in choices[messageIndex])
          {
            if (used[candidateIndex] || candidateIndex == originalCandidateIndex)
            {
              continue;
            }

            assignment[messageIndex] = candidateIndex;
            var score = Score(board, candidates, targets, assignment);

            if (score.IsBetterThan(bestScore))
            {
              bestScore = score;
              bestMessageIndex = messageIndex;
              bestCandidateIndex = candidateIndex;
            }
          }

          assignment[messageIndex] = originalCandidateIndex;
          used[originalCandidateIndex] = true;
        }

        if (bestMessageIndex < 0)
        {
          return;
        }

        used[assignment[bestMessageIndex]] = false;
        assignment[bestMessageIndex] = bestCandidateIndex;
        used[bestCandidateIndex] = true;
      }
    }

    private static AssignmentScore Score(
      Board board,
      IReadOnlyList<Candidate> candidates,
      double[] targets,
      int[] assignment)
    {
      var counts = CountAnswers(assignment, candidates, board.Words.Count);
      var crowding = 0.0;
      var targetDistance = 0.0;

      for (var first = 0; first < assignment.Length; first++)
      {
        var firstCandidate = candidates[assignment[first]];
        targetDistance += Math.Abs(firstCandidate.Position - targets[first]);

        for (var second = first + 1; second < assignment.Length; second++)
        {
          var secondCandidate = candidates[assignment[second]];
          var rowDistance = firstCandidate.Row - secondCandidate.Row;
          var columnDistance = firstCandidate.Column - secondCandidate.Column;
          crowding += 1.0 /
                      (1 + rowDistance * rowDistance +
                       columnDistance * columnDistance);
        }
      }

      return new AssignmentScore(
        counts.Max(),
        counts.Sum(count => count * count),
        crowding,
        targetDistance);
    }
  }
}
