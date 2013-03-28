using System;
using System.Diagnostics;

namespace TfsCreateBuild
{
    public static class ProcessExtensions
    {
        public static void InputAndOutputToEnd(this Process p, string standardInput, out string standardOutput, out string standardError)
        {
            if (p == null)
                throw new ArgumentException("p must be non-null");
            // Assume p has started. Alas there's no way to check.
            if (p.StartInfo.UseShellExecute)
                throw new ArgumentException("Set StartInfo.UseShellExecute to false");
            if ((p.StartInfo.RedirectStandardInput != (standardInput != null)))
                throw new ArgumentException("Provide a non-null Input only when StartInfo.RedirectStandardInput");
            //
            var outputData = new InputAndOutputToEndData();
            var errorData = new InputAndOutputToEndData();

            //
            if (p.StartInfo.RedirectStandardOutput)
            {
                outputData.Stream = p.StandardOutput;
                outputData.Thread = new System.Threading.Thread(InputAndOutputToEndProc);
                outputData.Thread.Start(outputData);
            }
            if (p.StartInfo.RedirectStandardError)
            {
                errorData.Stream = p.StandardError;
                errorData.Thread = new System.Threading.Thread(InputAndOutputToEndProc);
                errorData.Thread.Start(errorData);
            }
            //
            if (p.StartInfo.RedirectStandardInput)
            {
                p.StandardInput.Write(standardInput);
                p.StandardInput.Close();
            }
            //
            if (p.StartInfo.RedirectStandardOutput)
            {
                outputData.Thread.Join();
                standardOutput = outputData.Output;
            }
            else
                standardOutput = string.Empty;

            if (p.StartInfo.RedirectStandardError)
            {
                errorData.Thread.Join();
                standardError = errorData.Output;
            }
            else
                standardError = string.Empty;

            if (outputData.Exception != null)
                throw outputData.Exception;
            if (errorData.Exception != null)
                throw errorData.Exception;
        }

        private class InputAndOutputToEndData
        {
            public System.Threading.Thread Thread;
            public System.IO.StreamReader Stream;
            public string Output;
            public Exception Exception;
        }

        private static void InputAndOutputToEndProc(object data)
        {
            var ioData = (InputAndOutputToEndData)data;
            try
            {
                ioData.Output = ioData.Stream.ReadToEnd();
            }
            catch (Exception e)
            {
                ioData.Exception = e;
            }
        }
    }
}