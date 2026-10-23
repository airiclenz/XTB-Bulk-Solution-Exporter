using Com.AiricLenz.BulkSolutionExporter.Core.IO;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The git phase of <see cref="RunEngine"/>: stages the files the run
	/// exported, commits them with the request's message and optionally
	/// pushes. The caller skips the whole phase on abort.
	/// </summary>
	public sealed partial class RunEngine
	{

		// ============================================================================
		/// <summary>
		/// Stages, commits and optionally pushes this run's exported files. Does
		/// nothing when the request does not commit or no configured file path
		/// lies inside a git repository. A git failure is logged and the phase
		/// goes on: a failed stage still commits, a failed commit still pushes.
		/// </summary>
		private void HandleGit()
		{
			if (!_request.ShouldCommitToGit)
			{
				return;
			}

			var repositoryRootPath = FindRepositoryRootPath();

			if (string.IsNullOrEmpty(repositoryRootPath))
			{
				return;
			}

			var gitClient = _createGitClient(repositoryRootPath);

			_sink.Progress(
				"Committing files to Git...",
				null,
				isNewStep: true);

			_runLog.Log("##### Commiting the new files to Git:");
			_runLog.IncreaseIndent();

			if (_sessionFiles.Count == 0)
			{
				_runLog.Log("No files need to be committed.");
				_runLog.DecreaseIndent();
				_runLog.Log();
				return;
			}

			CommitSessionFiles(gitClient);

			if (_request.ShouldPushCommit)
			{
				PushCommit(gitClient);
			}

			_runLog.DecreaseIndent();
			_runLog.Log();
		}


		// ============================================================================
		/// <summary>
		/// The root of the git repository that holds the first configured file
		/// path of the run's solutions (managed before unmanaged, per solution);
		/// empty when there is no such path or it is not inside a repository.
		/// </summary>
		private string FindRepositoryRootPath()
		{
			foreach (var runSolution in _solutions)
			{
				var configuration = runSolution.Configuration;

				if (configuration == null)
				{
					continue;
				}

				if (!string.IsNullOrWhiteSpace(configuration.FileNameManaged))
				{
					return
						FileAndFolderHelper.FindRepositoryRootPath(
							configuration.FileNameManaged,
							out _);
				}

				if (!string.IsNullOrWhiteSpace(configuration.FileNameUnmanaged))
				{
					return
						FileAndFolderHelper.FindRepositoryRootPath(
							configuration.FileNameUnmanaged,
							out _);
				}
			}

			return string.Empty;
		}


		// ============================================================================
		private void CommitSessionFiles(
			IGitClient gitClient)
		{
			string errorMessage;

			foreach (var file in _sessionFiles)
			{
				if (!gitClient.AddFile(file, out errorMessage))
				{
					_runLog.LogError("Error staging: *" + errorMessage + "*");
				}
			}

			var message = _request.CommitMessage ?? string.Empty;

			if (!gitClient.Commit(message, out errorMessage))
			{
				_runLog.LogError("Error commiting: *" + errorMessage + "*");
				return;
			}

			_runLog.Log(_sessionFiles.Count + " File(s) was/weres commited: " + message);
		}


		// ============================================================================
		private void PushCommit(
			IGitClient gitClient)
		{
			if (!gitClient.Push(out string errorMessage))
			{
				_runLog.LogError("Error pushing: *" + errorMessage + "*");
				return;
			}

			_runLog.Log("The commit has been pushed to the remote origin.");
		}

	}
}
