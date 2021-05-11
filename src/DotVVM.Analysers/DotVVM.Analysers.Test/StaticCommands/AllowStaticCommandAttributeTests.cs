using System;
using System.Collections.Generic;
using System.Text;
using DotVVM.Analysers.Serializability;
using DotVVM.Analysers.StaticCommands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = DotVVM.Analysers.Test.CSharpAnalyzerVerifier<
    DotVVM.Analysers.StaticCommands.AllowStaticCommandUsageAnalyser>;

namespace DotVVM.Analysers.Test.StatiCommands
{
    public class AllowStaticCommandAttributeTests
    {
        [Fact]
        public async void Test_AllowStaticCommandOnInstanceMethod()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
    using DotVVM.Framework.ViewModel;
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        public class DefaultViewModel : DotvvmViewModelBase
        {
            {|#0:[AllowStaticCommand]
            public void Method()
            {

            }|}
        }
    }",

            VerifyCS.Diagnostic(AllowStaticCommandUsageAnalyser.DoNotUseAllowStaticCommandOnInstanceMethods).WithLocation(0));
        }
    }
}
