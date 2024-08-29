using Microsoft.Extensions.Logging;
using System;

namespace ApiApplication.Helpers
{
    public static class GenericHelper
    {
        public static int TryParseValue(string value)
        {
            if (int.TryParse(value, out var result) && result >= 1)
            {
                return result;
            }
            else
            {
                Console.WriteLine("Could not parse value. Returning default value of 10.");
                return 10;
            }
        }
    }
}
