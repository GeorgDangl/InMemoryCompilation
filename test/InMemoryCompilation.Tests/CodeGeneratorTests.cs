using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using InMemoreCompilation;
using Xunit;

namespace InMemoryCompilation.Tests
{
    public class CodeGeneratorTests
    {
        public CodeGeneratorTests()
        {
            GenerateCode();
            CreateCompilation();
            CompileAndLoadAssembly();
        }

        private string _generatedCode;
        private CSharpCompilation _compilation;
        private Assembly _generatedAssembly;

        [Theory]
        [InlineData(1,1,2)]
        [InlineData(5,5,10)]
        [InlineData(9,9,18)]
        public void GenerateCompileAndTestCode(int x, int y, int expectedResult)
        {
            var calculatedResult = CallCalculatorMethod(x, y);
            Assert.Equal(expectedResult, calculatedResult);
        }

        private void GenerateCode()
        {
            _generatedCode = CodeGenerator.GenerateCalculator();
        }

        private int CallCalculatorMethod(int x, int y)
        {
            var calculatorType = _generatedAssembly.GetType("Calculator.Calculator");
            var calculatorInstance = Activator.CreateInstance(calculatorType);
            var calculateMethod = calculatorType.GetTypeInfo().GetDeclaredMethod("AddIntegers");
            var calculationResult = calculateMethod.Invoke(calculatorInstance, new object[] { x, y });
            Assert.IsType(typeof(int), calculationResult);
            return (int)calculationResult;
        }

        private void CreateCompilation()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(_generatedCode);
            string assemblyName = Guid.NewGuid().ToString();
            var references = GetAssemblyReferences();
            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            _compilation = compilation;
        }

        private static IEnumerable<MetadataReference> GetAssemblyReferences()
        {
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
                // A bit hacky, if you need it
                //MetadataReference.CreateFromFile(Path.Combine(typeof(object).GetTypeInfo().Assembly.Location, "..", "mscorlib.dll")),
            };
            return references;
        }

        private void CompileAndLoadAssembly()
        {
            using (var ms = new MemoryStream())
            {
                var result = _compilation.Emit(ms);
                ThrowExceptionIfCompilationFailure(result);
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(ms);
#if NET46
                // Different in full .Net framework
                var assembly = Assembly.Load(ms.ToArray());
#endif
                _generatedAssembly = assembly;
            }
        }

        private void ThrowExceptionIfCompilationFailure(EmitResult result)
        {
            if (!result.Success)
            {
                var compilationErrors = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error)
                    .ToList();
                if (compilationErrors.Any())
                {
                    var firstError = compilationErrors.First();
                    var errorNumber = firstError.Id;
                    var errorDescription = firstError.GetMessage();
                    var firstErrorMessage = $"{errorNumber}: {errorDescription};";
                    throw new Exception($"Compilation failed, first error is: {firstErrorMessage}");
                }
            }
        }
    }
}
