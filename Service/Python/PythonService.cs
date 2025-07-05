using BLAS_HP.DTO;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace BLAS_HP.Service.Python
{
    class PythonService
    {
        static string scriptFolder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"Utils\Python"));
        static string scriptFile = Path.Combine(scriptFolder, "algorithm_matrix.py");
        static string pythonExecutable = @"C:\Python312\python.exe";
        public static IActionResult ResolveImage(ComputeImageRequest req)
        {
            string arguments = $"\"{scriptFile}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = scriptFolder
            };

            try
            {
                using (var process = Process.Start(processStartInfo))
                {
                    string jsonData = JsonSerializer.Serialize(req);

                    Console.WriteLine($"\nEnviando JSON para o Python via stdin:\n{jsonData}\n");

                    process.StandardInput.Write(jsonData);
                    process.StandardInput.Close();

                    string output = process.StandardOutput.ReadToEnd();
                    string errors = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return new OkObjectResult(output);
                }
            }
            catch (Exception ex)
            {
                return new BadRequestResult();
            }
        }

        public static Dictionary<string, string> ParsePythonOutput(string output)
        {
            var dictionary = new System.Collections.Generic.Dictionary<string, string>();
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains(":"))
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    dictionary[parts[0]] = parts[1];
                }
            }
            return dictionary;
        }
    }
}