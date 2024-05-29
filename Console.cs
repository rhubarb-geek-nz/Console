// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace RhubarbGeekNz.Console
{
    [Cmdlet(VerbsCommunications.Read, "Console")]
    sealed public class ReadConsole : PSCmdlet, IDisposable
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        [Parameter(HelpMessage = "Buffer length when reading")]
        public int ReadCount = 4096;

        [Parameter(HelpMessage = "Treat input as binary")]
        public SwitchParameter AsByteStream
        {
            get
            {
                return asByteStream;
            }

            set
            {
                asByteStream = value;
            }
        }
        private bool asByteStream;

        protected override void ProcessRecord()
        {
            var cancellationToken = cancellationTokenSource.Token;

            if (asByteStream)
            {
                using (Stream stream = System.Console.OpenStandardInput())
                {
                    byte[] buffer = new byte[ReadCount];

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        int result = stream.Read(buffer, 0, buffer.Length);

                        if (result == 0)
                        {
                            break;
                        }

                        if (result == buffer.Length)
                        {
                            WriteObject(buffer);

                            buffer = new byte[result];
                        }
                        else
                        {
                            byte[] packet = new byte[result];

                            Buffer.BlockCopy(buffer, 0, packet, 0, result);

                            WriteObject(packet);
                        }
                    }
                }
            }
            else
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    string line = System.Console.ReadLine();

                    if (line == null)
                    {
                        break;
                    }

                    WriteObject(line);
                }
            }
        }

        protected override void StopProcessing()
        {
            cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            IDisposable disposable = cancellationTokenSource;

            cancellationTokenSource = null;

            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }

    [Cmdlet(VerbsCommunications.Write, "Console")]
    sealed public class WriteConsole : PSCmdlet, IDisposable
    {
        [Parameter(ValueFromPipeline = true, HelpMessage = "Bytes to write")]
        public IEnumerable<byte> InputObject;

        [Parameter(ValueFromPipeline = true, HelpMessage = "Text to write")]
        public IEnumerable<char> InputString;

        [Parameter(HelpMessage = "No newline after string")]
        public SwitchParameter NoNewline
        {
            get
            {
                return noNewLine;
            }

            set
            {
                noNewLine = value;
            }
        }
        private bool noNewLine;

        private Stream stream;

        [Parameter(ValueFromPipeline = true, HelpMessage = "Error data for stderr")]
        public ErrorRecord InputErrorRecord;

        [Parameter(ValueFromPipeline = true, HelpMessage = "Information data for stderr")]
        public InformationRecord InputInformationRecord;

        [Parameter(ValueFromPipeline = true, HelpMessage = "Warning, debug and verbose data for stderr")]
        public InformationalRecord InputInformationalRecord;

        protected override void ProcessRecord()
        {
            if (InputObject != null)
            {
                if (!(InputObject is byte[] bytes))
                {
                    bytes = InputObject.ToArray();
                }

                if (bytes.Length > 0)
                {
                    if (stream == null)
                    {
                        stream = System.Console.OpenStandardOutput();
                    }

                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            if (InputString != null)
            {
                Dispose();

                if (InputString is string str)
                {
                    if (noNewLine)
                    {
                        if (str.Length > 0)
                        {
                            System.Console.Write(str);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine(str);
                    }
                }
                else
                {
                    if (!(InputString is char[] chars))
                    {
                        chars = InputString.ToArray();
                    }

                    if (noNewLine)
                    {
                        if (chars.Length > 0)
                        {
                            System.Console.Write(chars);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine(chars);
                    }
                }
            }

            if (InputErrorRecord != null)
            {
                WriteConsoleError(InputErrorRecord);
            }

            if (InputInformationalRecord != null)
            {
                WriteConsoleError(InputInformationalRecord);
            }

            if (InputInformationRecord != null)
            {
                WriteConsoleError(InputInformationRecord);
            }
        }

        protected override void EndProcessing() => Dispose();

        public void Dispose()
        {
            IDisposable s = stream;

            stream = null;

            if (s != null)
            {
                s.Dispose();
            }
        }

        private void WriteConsoleError(object record)
        {
            using (var shell = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                var result = shell.AddCommand("Out-String").Invoke(new object[] { record });

                foreach (var item in result)
                {
                    if (item.BaseObject is string s)
                    {
                        System.Console.Error.Write(s);
                    }
                }
            }
        }
    }
}
