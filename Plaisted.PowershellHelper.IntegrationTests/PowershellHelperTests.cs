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

            
            var output = await helper.Run(new System.Threading.CancellationToken());
            var returnedClass = ((JObject)output["testObject"]).ToObject<TestClass>();
            Assert.Equal("myValue", returnedClass.TestProperty);
        }
        public class TestClass
        {
            public string TestProperty { get; set; }
        }
    }
}
