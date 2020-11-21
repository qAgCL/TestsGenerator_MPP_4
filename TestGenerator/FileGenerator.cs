using System.IO;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TestsGeneratorLib;
using System.Text;
using System.Collections.Generic;
namespace TestGenerator
{
    public class FileGenerator
    {
        public Task Generate(List<string> inputFiles, string outputPath, int maxReadText, int maxGenerateTest, int maxWriteText)
        {
            var ExecReaxOption = new ExecutionDataflowBlockOptions();
            ExecReaxOption.MaxDegreeOfParallelism = maxReadText;

            var ExecGenerateOption = new ExecutionDataflowBlockOptions();
            ExecGenerateOption.MaxDegreeOfParallelism = maxGenerateTest;
            var ExecWriteOption = new ExecutionDataflowBlockOptions();
            ExecWriteOption.MaxDegreeOfParallelism = maxWriteText;

            var readText = new TransformBlock<string, string>(async textReadPath => await ReadTextAsync(textReadPath), ExecReaxOption);
            var generateTests = new TransformManyBlock<string, Tests>(textCode => TestsGenerator.GenerateTests(textCode), ExecGenerateOption);
            var writeText = new ActionBlock<Tests>(async test => await WriteTextAsync(Path.Combine(outputPath, test.FileName), test.TestCode), ExecWriteOption);


            var linkOptions = new DataflowLinkOptions();
            linkOptions.PropagateCompletion = true;

            readText.LinkTo(generateTests, linkOptions);
            generateTests.LinkTo(writeText, linkOptions);


            foreach (string file in inputFiles) {
                readText.Post(file);
            }

            readText.Complete();

            return writeText.Completion;
        }

        private static async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.UTF8.GetBytes(text);

            using (var sourceStream =
                new FileStream(
                    filePath,
                    FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            }
        }
        private static async Task<string> ReadTextAsync(string filePath)
        {
            using (var sourceStream =
                new FileStream(
                    filePath,
                    FileMode.Open, FileAccess.Read, FileShare.Read,
                    bufferSize: 4096, useAsync: true))
            {
                var sb = new StringBuilder();
                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string text = Encoding.UTF8.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }
                return sb.ToString();
            }
        }
    }
}
