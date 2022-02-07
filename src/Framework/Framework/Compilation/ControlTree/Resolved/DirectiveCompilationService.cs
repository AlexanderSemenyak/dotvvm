﻿using System;
using System.Linq;
using System.Linq.Expressions;
using DotVVM.Framework.Compilation.Parser.Dothtml.Parser;
using DotVVM.Framework.Compilation.Parser.Binding.Parser;
using DotVVM.Framework.Compilation.Binding;
using System.Collections.Immutable;
using DotVVM.Framework.Configuration;

namespace DotVVM.Framework.Compilation.ControlTree.Resolved
{
    public class DirectiveCompilationService
    {
        private readonly CompiledAssemblyCache compiledAssemblyCache;
        private readonly ExtensionMethodsCache extensionMethodsCache;
        private readonly DotvvmConfiguration configuration;

        public DirectiveCompilationService(CompiledAssemblyCache compiledAssemblyCache, ExtensionMethodsCache extensionMethodsCache, DotvvmConfiguration configuration)
        {
            this.compiledAssemblyCache = compiledAssemblyCache;
            this.extensionMethodsCache = extensionMethodsCache;
            this.configuration = configuration;
        }

        public ResolvedTypeDescriptor? ResolveType(DothtmlDirectiveNode directive, BindingParserNode nameSyntax, ImmutableList<NamespaceImport> imports)
        {
            if (CompileDirectiveExpression(directive, nameSyntax, imports) is not StaticClassIdentifierExpression expression)
            {
                directive.AddError($"Could not resolve type '{nameSyntax.ToDisplayString()}'.");
                return null;
            }
            else return new ResolvedTypeDescriptor(expression.Type);
        }

        public Type? ResolveTypeOrNamespace(DothtmlDirectiveNode directive, BindingParserNode nameSyntax)
        {
            var expression = CompileDirectiveExpression(directive, nameSyntax, ImmutableList<NamespaceImport>.Empty);

            if (expression is UnknownStaticClassIdentifierExpression unknownStaticClassIdentifier)
            {
                var namespaceValid = compiledAssemblyCache.IsAssemblyNamespace(unknownStaticClassIdentifier.Name);

                if (!namespaceValid)
                {
                    directive.AddError($"{nameSyntax.ToDisplayString()} is unknown type or namespace.");
                }

                return null;

            }
            else if (expression is StaticClassIdentifierExpression)
            {
                return expression.Type;
            }

            directive.AddError($"{nameSyntax.ToDisplayString()} is not a type or namespace.");
            return null;
        }

        public object? ResolvePropertyInitializer(DothtmlDirectiveNode directive, Type propertyType, BindingParserNode? initializer, ImmutableList<NamespaceImport> imports)
        {
            if (initializer == null) { return null; }

            var registry = RegisterImports(TypeRegistry.DirectivesDefault(compiledAssemblyCache), imports);
                
            var visitor = new ExpressionBuildingVisitor(registry, new MemberExpressionFactory(extensionMethodsCache, imports)) {
                ResolveOnlyTypeName = false,
                Scope = null,
                ExpectedType = propertyType
            };

            try {
                var initializerExpression = visitor.Visit(initializer);

                var funcType = typeof(Func<>).MakeGenericType(propertyType);

                var lambda = Expression.Lambda(funcType, Expression.Convert(Expression.Block(initializerExpression), propertyType));
                var lambdaDelegate = lambda.Compile(true);

                return lambdaDelegate.DynamicInvoke() ?? CreateDefaultValue(propertyType);
            }
            catch (Exception ex)
            {
                directive.AddError("Could not initialize property value.");
                directive.AddError(ex.Message);
                return CreateDefaultValue(propertyType);
            }
        }

        private object? CreatePropertyInitializerValue(DothtmlDirectiveNode directive, Type? propertyType, LiteralExpressionBindingParserNode? initializer)
        {
            if (initializer?.Value is null || propertyType is null) { return null; }

            var originalLiteralType = initializer.Value.GetType();

            if (originalLiteralType != typeof(string)) { return initializer.Value; }

            var initializerValueString = initializer.Value.ToString() ?? "";

            if (propertyType == typeof(char))
            {
                if (initializerValueString.Length != 1)
                {
                    directive.AddError($"Could not convert \"{initializerValueString}\" to char when initializing property {directive.Name}.");
                    return default(char);
                }

                return initializerValueString.Single();
            }

            if (propertyType == typeof(Guid))
            {
                if (Guid.TryParse(initializerValueString, out var guid))
                {
                    return guid;
                }
                directive.AddError($"Could not convert \"{initializerValueString}\" to Guid when initializing property {directive.Name}.");
                return default(Guid);
            }

            return initializerValueString;
        }

        private object? CreateDefaultValue(Type? type)
        {
            if (type != null && type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        private Expression? CompileDirectiveExpression(DothtmlDirectiveNode directive, BindingParserNode expressionSyntax, ImmutableList<NamespaceImport> imports)
        {
            TypeRegistry registry;
            if (expressionSyntax is TypeOrFunctionReferenceBindingParserNode typeOrFunction)
                expressionSyntax = typeOrFunction.ToTypeReference();

            if (expressionSyntax is AssemblyQualifiedNameBindingParserNode assemblyQualifiedName)
            {
                registry = TypeRegistry.DirectivesDefault(compiledAssemblyCache, assemblyQualifiedName.AssemblyName.ToDisplayString());
            }
            else
            {
                registry = TypeRegistry.DirectivesDefault(compiledAssemblyCache);
            }

            registry = RegisterImports(registry, imports);

            var visitor = new ExpressionBuildingVisitor(registry, new MemberExpressionFactory(extensionMethodsCache, imports)) {
                ResolveOnlyTypeName = true,
                Scope = null
            };

            try
            {
                return visitor.Visit(expressionSyntax);
            }
            catch (Exception ex)
            {
                directive.AddError($"{expressionSyntax.ToDisplayString()} is not a valid type or namespace: {ex.Message}");
                return null;
            }
        }

        private TypeRegistry RegisterImports(TypeRegistry registry, ImmutableList<NamespaceImport> imports)
        {
            var allImports = imports.Concat(configuration.Markup.ImportedNamespaces).ToImmutableArray();

            registry = registry
                .AddImportedTypes(compiledAssemblyCache, allImports);
            return registry;
        }
    }
}
