using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotVVM.Analysers.StaticCommands
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AllowStaticCommandUsageAnalyser : DiagnosticAnalyzer
    {
        private static readonly LocalizableResourceString allowStaticCommandUsageTitle = new LocalizableResourceString(nameof(Resources.StaticCommands_DoNotUseAllowStaticCommand_Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableResourceString allowStaticCommandUsageInstanceMethodMessage = new LocalizableResourceString(nameof(Resources.StaticCommands_DoNotUseAllowStaticCommandInstanceMethod_Message), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableResourceString allowStaticCommandUsageStaticMethodMessage = new LocalizableResourceString(nameof(Resources.StaticCommands_DoNotUseAllowStaticCommandStaticMethod_Message), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableResourceString allowStaticCommandUsageDescription = new LocalizableResourceString(nameof(Resources.StaticCommands_DoNotUseAllowStaticCommand_Description), Resources.ResourceManager, typeof(Resources));
        private const string dotvvmViewModelInterfaceMetadataName = "DotVVM.Framework.ViewModel.IDotvvmViewModel";
        private const string dotvvmAllowStaticCommandAttributeMetadataName = "DotVVM.Framework.ViewModel.AllowStaticCommandAttribute";

        public static DiagnosticDescriptor DoNotUseAllowStaticCommandOnStaticMethods = new DiagnosticDescriptor(
            DotvvmDiagnosticIds.UseSerializablePropertiesRuleId,
            allowStaticCommandUsageTitle,
            allowStaticCommandUsageStaticMethodMessage,
            DiagnosticCategory.StaticCommands,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            allowStaticCommandUsageDescription);

        public static DiagnosticDescriptor DoNotUseAllowStaticCommandOnInstanceMethods = new DiagnosticDescriptor(
            DotvvmDiagnosticIds.UseSerializablePropertiesRuleId,
            allowStaticCommandUsageTitle,
            allowStaticCommandUsageInstanceMethodMessage,
            DiagnosticCategory.StaticCommands,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            allowStaticCommandUsageDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                DoNotUseAllowStaticCommandOnStaticMethods,
                DoNotUseAllowStaticCommandOnInstanceMethods);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSemanticModelAction(AnalyzeViewModelProperties);
        }

        private static void AnalyzeViewModelProperties(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var viewModelInterface = semanticModel.Compilation.GetTypeByMetadataName(dotvvmViewModelInterfaceMetadataName);
            var allowStaticCommandAttribute = semanticModel.Compilation.GetTypeByMetadataName(dotvvmAllowStaticCommandAttributeMetadataName);

            // Check all classes
            foreach (var classDeclaration in syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                // Filter out non-ViewModels
                var classInfo = semanticModel.GetDeclaredSymbol(classDeclaration);
                if (!classInfo.AllInterfaces.Any(symbol => SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, viewModelInterface)))
                    continue;

                // Check all methods
                foreach (var method in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                    var attribute = methodSymbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, allowStaticCommandAttribute));
                    if (attribute != null)
                    {
                        var isStatic = methodSymbol.IsStatic;
                        var diagnostic = Diagnostic.Create((isStatic) ? DoNotUseAllowStaticCommandOnStaticMethods : DoNotUseAllowStaticCommandOnInstanceMethods, method.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
