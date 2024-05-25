// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
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
            using (var disposable = cancellationTokenSource)
            {
                cancellationTokenSource = null;
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

        [Parameter(HelpMessage = "Text encoding")]
        public Encoding Encoding
        {
            get
            {
                return encoding;
            }

            set
            {
                encoding = value;
            }
        }
        private Encoding encoding = System.Console.OutputEncoding;

        private Stream stream;
        private byte[] newLine;

        protected override void BeginProcessing()
        {
            stream = System.Console.OpenStandardOutput();

            if (!noNewLine)
            {
                newLine = encoding.GetBytes(Environment.NewLine);
            }
        }

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
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            if (InputString != null)
            {
                if (InputString is string str)
                {
                    if (str.Length > 0)
                    {
                        byte[] output = encoding.GetBytes(str);
                        stream.Write(output, 0, output.Length);
                    }
                }
                else
                {
                    if (!(InputString is char[] chars))
                    {
                        chars = InputString.ToArray();
                    }

                    if (chars.Length > 0)
                    {
                        byte[] output = encoding.GetBytes(chars);
                        stream.Write(output, 0, output.Length);
                    }
                }

                if (!noNewLine)
                {
                    stream.Write(newLine, 0, newLine.Length);
                }
            }
        }

        protected override void EndProcessing() => Dispose();

        public void Dispose()
        {
            using (var s = stream)
            {
                stream = null;
            }
        }
    }
}
