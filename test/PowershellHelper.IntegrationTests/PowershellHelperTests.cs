﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Plaisted.PowershellHelper.IntegrationTests
{
    public class PowershellHelperTests
    {
        [Fact]
        public async void It_Sets_And_Retrieves_Value()
        {
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.None; }))
            {
                helper.AddInputObject("testObject", new TestClass { TestProperty = "myValue" });
                helper.AddOutputObject("testObject");

                var exitcode = await helper.RunAsync(new System.Threading.CancellationToken());
                var returnedClass = helper.Output["testObject"].ToObject<TestClass>();
                Assert.Equal("myValue", returnedClass.TestProperty);
            }

        }

        [Fact]
        public async void It_Sets_And_Retrieves_Value_Odd_VarName()
        {
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.None; }))
            {
                helper.AddInputObject("te``st! Ob`{je}ct", new TestClass { TestProperty = "myValue" });
                helper.AddOutputObject("te``st! Ob`{je}ct");

                var exitcode = await helper.RunAsync(new System.Threading.CancellationToken());
                var returnedClass = helper.Output["te``st! Ob`{je}ct"].ToObject<TestClass>();
                Assert.Equal("myValue", returnedClass.TestProperty);
            }

        }

        [Fact]
        public async void It_Handles_Null_Output()
        {
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.None; }))
            {
                helper.AddOutputObject("notSet");

                var exitcode = await helper.RunAsync(new System.Threading.CancellationToken());
                Assert.Equal(null, helper.Output["notSet"]);
            }

        }
        public class TestClass
        {
            public string TestProperty { get; set; }
        }
    }
}
