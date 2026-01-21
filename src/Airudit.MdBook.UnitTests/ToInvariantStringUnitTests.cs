
namespace Airudit.MdBook.UnitTests
{
    using Airudit.MdBook.Core.Internals;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    public class ExtensionsTests
    {
        [Fact]
        public void ToInvariantString_Invariability()
        {
            var cultures = new CultureInfo[]
            {
                CultureInfo.InvariantCulture,
                new CultureInfo("fr-FR"),
                new CultureInfo("en-US"),
            };

            for (int i = 0; i < cultures.Length; i++)
            {
                // set a problematic culture
                Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = cultures[i];

                // verify invariability
                Assert.Equal("-2147483648", ToStringExtensions.ToInvariantString(int.MinValue));
                Assert.Equal("2147483647", ToStringExtensions.ToInvariantString(int.MaxValue));
                Assert.Equal("3.40282347E+38", ToStringExtensions.ToInvariantString(float.MaxValue));
                Assert.Equal("NaN", ToStringExtensions.ToInvariantString(float.NaN));
                Assert.Equal("1.40129846E-45", ToStringExtensions.ToInvariantString(float.Epsilon));
                Assert.Equal("1.7976931348623157E+308", ToStringExtensions.ToInvariantString(double.MaxValue));
                Assert.Equal("NaN", ToStringExtensions.ToInvariantString(double.NaN));
                Assert.Equal("4.9406564584124654E-324", ToStringExtensions.ToInvariantString(double.Epsilon));
                Assert.Equal("79228162514264337593543950335", ToStringExtensions.ToInvariantString(decimal.MaxValue));
                Assert.Equal("123456789.123456789", ToStringExtensions.ToInvariantString(123456789.123456789M));
                Assert.Equal("2020-08-08T16:35:59.9110000Z", ToStringExtensions.ToInvariantString(new DateTime(2020, 08, 08, 16, 35, 59, 911, DateTimeKind.Utc)));
            }
        }
    }
}
