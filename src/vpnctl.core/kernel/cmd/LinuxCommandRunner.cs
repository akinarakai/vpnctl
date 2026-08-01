using System.Diagnostics;

public class LinuxCommandRunner : ICommandRunner
{
    public CmdResult Run(string command, string arguments, bool useSudo = false, bool showOutput = true)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = useSudo ? "sudo" : command,
                Arguments = useSudo ? $"{command} {arguments}" : arguments,

                UseShellExecute = false,
                CreateNoWindow = false
            };

            if (!showOutput)
            {
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
            }
            else
            {
                processInfo.RedirectStandardOutput = false;
                processInfo.RedirectStandardError = false;
            }

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return new CmdResult { Success = false, Text = "Failed start process." };
            }

            string resultText = string.Empty;

            if (!showOutput)
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                bool success = process.ExitCode == 0;
                resultText = success ? output.Trim() : error.Trim();
            }
            else
            {
                process.WaitForExit();
                resultText = process.ExitCode == 0 ? "Success" : $"Error code: {process.ExitCode}";
            }

            return new CmdResult
            {
                Success = process.ExitCode == 0,
                Text = resultText
            };
        }
        catch (Exception ex)
        {
            return new CmdResult
            {
                Success = false,
                Text = ex.Message
            };
        }
    }
}