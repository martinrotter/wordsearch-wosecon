using WordSearchGenerator.Desktop.Localization;

namespace WordSearchGenerator.Desktop.ViewModels
{
  public sealed class QuizEntryViewModel : ValidatableViewModelBase
  {
    #region Fields

    private string _answer = string.Empty;
    private string _question = string.Empty;

    #endregion

    #region Properties

    public string Answer
    {
      get => _answer;
      set
      {
        if (SetProperty(ref _answer, value ?? string.Empty))
        {
          Validate();
        }
      }
    }

    public string Question
    {
      get => _question;
      set
      {
        if (SetProperty(ref _question, value ?? string.Empty))
        {
          Validate();
        }
      }
    }

    public bool IsEmpty =>
      string.IsNullOrWhiteSpace(Answer) &&
      string.IsNullOrWhiteSpace(Question);

    #endregion

    #region Constructors

    public QuizEntryViewModel()
    {
      Validate();
    }

    #endregion

    #region Other Stuff

    private void Validate()
    {
      var answer = Answer.Trim();
      var question = Question.Trim();
      var answerErrors = new List<string>();
      var questionErrors = new List<string>();

      if (answer.Length == 0 && question.Length == 0)
      {
        SetErrors(nameof(Answer), []);
        SetErrors(nameof(Question), []);
      }
      else
      {
        if (answer.Length == 0)
        {
          answerErrors.Add(AppStrings.Get("AnswerRequired"));
        }
        else if (answer.Length < 2)
        {
          answerErrors.Add(AppStrings.Get("AnswerMinimumLength"));
        }

        if (question.Length == 0)
        {
          questionErrors.Add(AppStrings.Get("QuestionRequired"));
        }

        SetErrors(nameof(Answer), answerErrors);
        SetErrors(nameof(Question), questionErrors);
      }

      OnPropertyChanged(nameof(IsEmpty));
    }

    #endregion
  }
}