using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestsGeneratorLib;
namespace TestGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> inputFiles = new List<string>();
            inputFiles.Add(@"..\..\test.cs");
            string outputPath = @"..\..\GeneratedTests";
            new FileGenerator().Generate(inputFiles, outputPath,2,2,2);
            Console.WriteLine("Done");


            Console.ReadLine();
        }
    }
}

