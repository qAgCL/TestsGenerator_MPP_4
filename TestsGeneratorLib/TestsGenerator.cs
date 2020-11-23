using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace TestsGeneratorLib
{
    public static class TestsGenerator
    {
        public static Tests[] GenerateTests(string textProgram)
        {
            List<Tests> tests = new List<Tests>();
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(textProgram);
            CompilationUnitSyntax Root = syntaxTree.GetCompilationUnitRoot();
            var namespaces = Root.ChildNodes().Where(@namespace => @namespace is NamespaceDeclarationSyntax);
            foreach (NamespaceDeclarationSyntax @namespace in namespaces)
            {
                FindMethod(@namespace, Root, ref tests);
            }
            return tests.ToArray();
        }
        private static string GetFullNameSpace(SyntaxNode @class)
        {
            string ClssNameSpace = "";

            while (!(@class.Parent is CompilationUnitSyntax))
            {
                if (@class.Parent is ClassDeclarationSyntax)
                {
                    ClssNameSpace = ClssNameSpace.Insert(0, '.' + (@class.Parent as ClassDeclarationSyntax).Identifier.Text);
                }
                if (@class.Parent is NamespaceDeclarationSyntax)
                {
                    ClssNameSpace = ClssNameSpace.Insert(0, '.' + ((@class.Parent as NamespaceDeclarationSyntax).Name as IdentifierNameSyntax).Identifier.Text);
                }
                @class = @class.Parent;
            }
            return ClssNameSpace.Remove(0, 1);
        }
        private static MethodDeclarationSyntax[] TestMethodsGenerate(ClassDeclarationSyntax @class)
        {
            var members = @class.Members;
            var methods = members.Where(mem => mem is MethodDeclarationSyntax);
            methods = methods.Where(method => method.Modifiers.Where(modifier => modifier.Kind() == SyntaxKind.PublicKeyword).Any());
            List<MethodDeclarationSyntax> testMethods = new List<MethodDeclarationSyntax>();

            foreach (MemberDeclarationSyntax method in methods)
            {
                var teshMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), (method as MethodDeclarationSyntax).Identifier.Text + "Test")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAttributeLists(SyntaxFactory.SingletonList<AttributeListSyntax>(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                                    SyntaxFactory.Attribute(
                                        SyntaxFactory.IdentifierName("Test"))))).ToArray()).WithBody(SyntaxFactory.Block());
                string paramString="";
                foreach(ParameterSyntax parameter in (method as MethodDeclarationSyntax).ParameterList.Parameters)
                {
                    if (parameter.Type.ToString()[0] == 'I')
                    {
                        paramString += "_" + parameter.Identifier.Text + "Dependency.Object, ";
                    }
                    else {
                        teshMethod = teshMethod.AddBodyStatements(GenerateVar(parameter.Type.ToString(), parameter.Identifier.Text));
                        paramString += parameter.Identifier.Text + ", ";
                    }
                }
                if (paramString.Length > 0) {
                    paramString = paramString.Remove(paramString.Length - 2, 1);
                }
                string methodText = "";
                if ((method as MethodDeclarationSyntax).ReturnType.ToString() == "void")
                {              
                    if (method.Modifiers.Any(SyntaxKind.StaticKeyword))
                    {
                        methodText += @class.Identifier.Text;
                    }
                    else
                    {
                        methodText += "_" + @class.Identifier.Text + "UnderTest";
                    }
                    methodText += "." + (method as MethodDeclarationSyntax).Identifier.Text + "(" + paramString + ");";
                    teshMethod = teshMethod.AddBodyStatements(SyntaxFactory.ParseStatement(methodText));
                }
                else
                {
                    methodText += (method as MethodDeclarationSyntax).ReturnType.ToString() + " actual = ";
                    if (method.Modifiers.Any(SyntaxKind.StaticKeyword))
                    {
                        methodText += @class.Identifier.Text;
                    }
                    else
                    {
                        methodText += "_" + @class.Identifier.Text + "UnderTest";
                    }
                    methodText += "." + (method as MethodDeclarationSyntax).Identifier.Text + "(" + paramString + ");";
                    teshMethod = teshMethod.AddBodyStatements(SyntaxFactory.ParseStatement(methodText));
                    teshMethod = teshMethod.AddBodyStatements(GenerateVar((method as MethodDeclarationSyntax).ReturnType.ToString(), "expected"));
                    teshMethod = teshMethod.AddBodyStatements(SyntaxFactory.ParseStatement("Assert.That(actual, Is.EqualTo(expected));"));
                }
                teshMethod = teshMethod.AddBodyStatements(SyntaxFactory.ParseStatement("Assert.Fail(\"autogenerated\");"));
                testMethods.Add(teshMethod);
            }
            return testMethods.ToArray();
        }

        private static MemberDeclarationSyntax[] SetUpGenerate(ClassDeclarationSyntax @class)
        {
            List<MemberDeclarationSyntax> fields = new List<MemberDeclarationSyntax>();
            MethodDeclarationSyntax SetUp = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "SetUp")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAttributeLists(SyntaxFactory.SingletonList<AttributeListSyntax>(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                                    SyntaxFactory.Attribute(
                                        SyntaxFactory.IdentifierName("SetUp"))))).ToArray()).WithBody(SyntaxFactory.Block());
            if (@class.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                fields.Add(SetUp);
                return fields.ToArray();
            }
            fields.Add(GenerateField(SyntaxKind.PrivateKeyword, "_"+@class.Identifier.Text+"UnderTest",@class.Identifier.Text));

            List<ConstructorDeclarationSyntax> constructors = new List<ConstructorDeclarationSyntax>();
            foreach(MemberDeclarationSyntax member in @class.Members.Where(cstr => (cstr is ConstructorDeclarationSyntax)))
            {
                constructors.Add(member as ConstructorDeclarationSyntax);
            }

            string constString = "_" + @class.Identifier.Text + "UnderTest = new " + @class.Identifier.Text + "(";
            if (constructors.Count > 0)
            {
                ConstructorDeclarationSyntax constructor = constructors.OrderBy(x => x.ParameterList.Parameters.Count).First();

                ParameterSyntax[] parametrs = constructor.ParameterList.Parameters.ToArray();

                foreach (ParameterSyntax param in parametrs)
                {
                    if (param.Type.ToString()[0] == 'I')
                    {
                        fields.Add(
                        GenerateField(
                            SyntaxKind.PrivateKeyword,
                            "_" + param.Identifier.Text + "Dependency",
                            "Mock<" + param.Type.ToString() + ">"));
                        SetUp = SetUp.AddBodyStatements(GenerateMockClass(param.Type.ToString(), "_" + param.Identifier.Text + "Dependency"));
                        constString += "_" + param.Identifier.Text + "Dependency.Object, ";
                    }
                    else
                    {
                        SetUp = SetUp.AddBodyStatements(GenerateVar(param.Type.ToString(), param.Identifier.Text));
                        constString += param.Identifier.Text + ", ";
                    }
                }
                constString = constString.Remove(constString.Length - 2, 1);
            }
            constString += ");";
            SetUp = SetUp.AddBodyStatements(SyntaxFactory.ParseStatement(constString));
            fields.Add(SetUp);
            return fields.ToArray();

        }
        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
        private static StatementSyntax GenerateMockClass(string VarType, string VarName)
        {
            string result = string.Format("{0} = new Mock<{1}>();", VarName, VarType);
            return SyntaxFactory.ParseStatement(result);
        }
        private static StatementSyntax GenerateVar(string VarType, string VarName)
        {                                                               
            string result = string.Format("{0} {1} = default;", VarType, VarName);
            return SyntaxFactory.ParseStatement(result);
        }
        private static FieldDeclarationSyntax GenerateField(SyntaxKind kind, string IdentifierName, string IdentifierType)
        {
            return SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.IdentifierName(IdentifierType))
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(IdentifierName)))))
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(kind)));
        }
        private static void FindMethod(SyntaxNode namespac, CompilationUnitSyntax root, ref List<Tests> Tests)
        {
            var classes = namespac.ChildNodes().Where(@class => @class is ClassDeclarationSyntax);
            foreach (ClassDeclarationSyntax @class in classes)
            {
                if (!(@class.Modifiers.Any(SyntaxKind.AbstractKeyword))) { 
                    var members = @class.Members;
                    var methods = members.Where(mem => mem is MethodDeclarationSyntax);
                    methods = methods.Where(method => method.Modifiers.Where(modifier => modifier.Kind() == SyntaxKind.PublicKeyword).Any());
                    if (methods.Count() > 0)
                    {
                        var usings = root.Usings;
                        var syntaxFactory = SyntaxFactory.CompilationUnit();
                        syntaxFactory = syntaxFactory.AddUsings(usings.ToArray());
                        syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("NUnit.Framework")));
                        syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Moq")));
                        syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(GetFullNameSpace(@class))));
                        var namespaceName = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(GetFullNameSpace(@class) + ".Test"));


                        ClassDeclarationSyntax TestClass = SyntaxFactory.ClassDeclaration(@class.Identifier.Text + "Tests")
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
                        TestClass = TestClass.AddAttributeLists(SyntaxFactory.SingletonList<AttributeListSyntax>(
                                SyntaxFactory.AttributeList(
                                    SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                                        SyntaxFactory.Attribute(
                                            SyntaxFactory.IdentifierName("TestFixture"))))).ToArray());

                        TestClass = TestClass.AddMembers(SetUpGenerate(@class));

                        TestClass = TestClass.AddMembers(TestMethodsGenerate(@class));


                        namespaceName = namespaceName.AddMembers(TestClass);
                        syntaxFactory = syntaxFactory.AddMembers(namespaceName);
                        string fileName = GetFullNameSpace(@class) + "." + @class.Identifier.Text + ".Test.cs";
                        Tests.Add(new Tests(fileName, syntaxFactory.NormalizeWhitespace().ToFullString()));
                    }
                    foreach (MemberDeclarationSyntax member in members)
                    {
                        if (member is ClassDeclarationSyntax)
                        {
                            FindMethod(@class, root, ref Tests);
                        }
                    }
                }
            }
            var namespaces = namespac.ChildNodes().Where(@class => @class is NamespaceDeclarationSyntax);
            foreach (NamespaceDeclarationSyntax @namespace in namespaces)
            {
                FindMethod(@namespace, root, ref Tests);
            }
        }
    }
}
