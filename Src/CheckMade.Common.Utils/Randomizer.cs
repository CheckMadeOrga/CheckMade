﻿using CheckMade.Common.Interfaces.Utils;

namespace CheckMade.Common.Utils;

public class Randomizer : IRandomizer
{
    public long GenerateRandomLong()
    {
        var random = new Random();
        var buffer = new byte[8];
        random.NextBytes(buffer);
        var randomLong = BitConverter.ToInt64(buffer, 0);

        return randomLong;
    }
}