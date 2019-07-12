using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CodeGenerator;

public static class CodeGen
{
    public static void GenerateWPFDotnetCoreProject(string outputFolder, string projectName, IEnumerable<Type> types)
    {
        string projFolder = Path.Combine(outputFolder, projectName);
        string csprojName = Path.Combine(outputFolder, projectName, projectName + ".csproj");
        string namespaceName = projectName;

        GenerateClassFiles(projFolder, namespaceName, types);
        File.WriteAllText(csprojName, GenerateCSProj());

        string GenerateCSProj() =>
        $@"
            <Project Sdk=""Microsoft.NET.Sdk.WindowsDesktop"">
                <PropertyGroup>
                    <OutputType>WinExe</OutputType>
                    <TargetFramework>netcoreapp3.0</TargetFramework>
                    <RootNamespace>{namespaceName}</RootNamespace>
                    <UseWPF>true</UseWPF>
                 </PropertyGroup>
            </Project>
        ";
    }

    public static void GenerateDotnetFrameworkProject(string outputFolder, string projectName, IEnumerable<Type> types)
    {
        // TODO
    }

    public static void GenerateClassFiles(string outputFolder, string namespaceName, IEnumerable<Type> types)
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        foreach (Type t in types)
        {
            Console.WriteLine($"Generating {t.Name}Extensions.cs");

            string outputFilePath = Path.Combine(outputFolder, $"{t.Name}Extensions.cs");
            string text = GenerateExtensionClassFor(namespaceName, t);
            File.WriteAllText(outputFilePath, text);
        }
    }

    public static string GenerateExtensionClassFor(string namespaceName, Type T)
    {
        var namespaces = new HashSet<string>();
        namespaces.Add(T.Namespace);

        // GeneratePropertyExtensions and GenerateEventExtenions both update "namespaces"
        // as a side-effect.
        string classBody = GeneratePropertyExtensions() + GenerateEventExtensions();
        string usingsSection = GenerateUsingsSection();

        return 
        $@"
            {usingsSection}

            namespace {namespaceName}
            {{
                public static class {T.Name}Extensions
                {{
                    {classBody}
                }}
            }}
        ";

        string GeneratePropertyExtensions()
        {
            var builder = new StringBuilder();
            var settableProperties = T
                .GetProperties()
                .Where(p => p.DeclaringType == T)   // Skip properties added by parent class
                .Where(p => p.CanWrite && p.SetMethod.IsPublic);

            foreach (PropertyInfo p in settableProperties)
            {
                namespaces.Add(p.PropertyType.Namespace);
                builder.AppendLine(GenerateSinglePropertyExtension(p));
            }

            return builder.ToString();
        }

        string GenerateEventExtensions()
        {
            var builder = new StringBuilder();
            var events = T
                .GetEvents()
                .Where(e => e.DeclaringType == T);

            foreach (EventInfo e in events)
            {
                namespaces.Add(e.EventHandlerType.Namespace);
                builder.AppendLine(GenerateSingleEventExtension(e));
            }

            return builder.ToString();
        }

        string GenerateUsingsSection()
        {
            var builder = new StringBuilder();
            var sortedNamespaces = namespaces
                .OrderBy(s => s);

            foreach (string ns in sortedNamespaces)
                builder.AppendLine($"using {ns};");

            return builder.ToString();
        }

        string GenerateSinglePropertyExtension(PropertyInfo p) =>
        $@"
            public static TObject With{p.Name}<TObject>(this TObject obj, {p.PropertyType.GenericName()} value)
                where TObject : {T.Name}
            {{
                obj.{p.Name} = value;
                return obj;
            }}
        ";

        string GenerateSingleEventExtension(EventInfo e) =>
        $@"
            public static TObject Handle{e.Name}<TObject>(this TObject obj, {e.EventHandlerType.GenericName()} handler)
                where TObject : {T.Name}
            {{
                obj.{e.Name} += handler;
                return obj;
            }}
        ";
    }
}