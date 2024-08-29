using ApiApplication.Helpers;

namespace ApiApplication.Tests
{
    public class GenericHelperTests
    {
        [Theory]
        [InlineData("5", 5)]
        [InlineData("10", 10)]
        [InlineData("0", 10)]
        [InlineData("-1", 10)]
        [InlineData("asd", 10)]
        public void TryParseValue_ValidAndInvalidInputs_ReturnsExpectedResult(string input, int expected)
        {
            var result = GenericHelper.TryParseValue(input);

            Assert.Equal(expected, result);
        }
    }
}
