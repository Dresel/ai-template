namespace FocusTemplate.Admin.Client;

public union ApiOutcome<T>(T, ApiFailure);

// For a caller that handles one of the operation's problems itself: a form its ValidationProblem, a load its NotFoundProblem
public union ApiOutcome<T, TProblem>(T, TProblem, ApiFailure);