using Newtonsoft.Json.Linq;
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

            var helper = new PowershellHelper().WithProcessCleanup(false);
            helper.AddInputObject("testObject", new TestClass { TestProperty = "myValue" });
            helper.AddOutputObject("testObject");

            
            var exitcode = await helper.Run(new System.Threading.CancellationToken());
            var returnedClass = helper.Output["testObject"].ToObject<TestClass>();
            Assert.Equal("myValue", returnedClass.TestProperty);
        }

        [Fact]
        public async void It_Sets_And_Retrieves_Value_Odd_VarName()
        {

            var helper = new PowershellHelper().WithProcessCleanup(false);
            helper.AddInputObject("te``st! Ob`{je}ct", new TestClass { TestProperty = "myValue" });
            helper.AddOutputObject("te``st! Ob`{je}ct");


            var exitcode = await helper.Run(new System.Threading.CancellationToken());
            var returnedClass = helper.Output["te``st! Ob`{je}ct"].ToObject<TestClass>();
            Assert.Equal("myValue", returnedClass.TestProperty);
        }

        [Fact]
        public async void It_Handles_Null_Output()
        {

            var helper = new PowershellHelper().WithProcessCleanup(false);
            helper.AddOutputObject("notSet");


            var exitcode = await helper.Run(new System.Threading.CancellationToken());
            Assert.Equal(null, helper.Output["notSet"]);
        }
        public class TestClass
        {
            public string TestProperty { get; set; }
        }
    }
}
