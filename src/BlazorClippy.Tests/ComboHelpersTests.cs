
using BlazorClippyWatson.Common;

namespace BlazorClippy.Tests
{
    public class ComboHelpersTests
    {

        [Fact]
        public void TestGetKCombos()
        {
            var list = new List<string>() { "1A", "1B", "1C" };

            var result = ComboHelpers.GetKCombsWithRept<string>(list, 2).ToList();

            var finals = new List<string>();
            foreach( var item in result )
            {
                var actual = string.Empty;
                foreach (var k in item)
                {
                    actual += k;
                }
                finals.Add(actual);
            }
            Assert.Equal( 6, finals.Count);
            Assert.Equal("1A1A", finals[0]);
            Assert.Equal("1A1B", finals[1]);
            Assert.Equal("1A1C", finals[2]);
            Assert.Equal("1B1B", finals[3]);
            Assert.Equal("1B1C", finals[4]);
            Assert.Equal("1C1C", finals[5]);
        }

    }
}