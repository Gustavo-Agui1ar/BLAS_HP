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
        static string _scriptFolder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"Utils\Python"));
        static string _scriptFile = Path.Combine(_scriptFolder, "algorithm_matrix.py");
        static string _pythonExecutable = @"C:\Python312\python.exe";
        public static string ResolveImage(ComputeImageRequest req)
        {
            string arguments = $"\"{_scriptFile}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutable,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _scriptFolder
            };

            try
            {
                using (var process = Process.Start(processStartInfo))
                {
                    if(process == null)
                    {
                        throw new InvalidOperationException("Não foi possível iniciar o processo Python.");
                    }

                    var serializerOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    string jsonData = JsonSerializer.Serialize(req, serializerOptions);
                    
                    process.StandardInput.Write(jsonData);
                    process.StandardInput.Close();

                    string output = process.StandardOutput.ReadToEnd();
                    string errors = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    using var jsonDoc = JsonDocument.Parse(output);

                    if (jsonDoc.RootElement.TryGetProperty("imagePath", out var pathElement))
                    {
                        return pathElement.GetString() ?? string.Empty;
                    }

                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao executar o script Python: {ex.Message}");
                return "";
            }
        }
    }
}