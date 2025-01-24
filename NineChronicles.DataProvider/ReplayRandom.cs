namespace NineChronicles.DataProvider
{
    using Lib9c.ActionEvaluatorCommonComponents;
    using Libplanet.Action;

    public class ReplayRandom : IRandom
    {
        private readonly System.Random _random;

        public ReplayRandom(int seed)
        {
            _random = new Random(seed);
            Seed = seed;
        }

        public int Seed { get; }

        public int Next()
        {
            return _random.Next();
        }

        public int Next(int upperBound)
        {
            return _random.Next(upperBound);
        }

        public int Next(int lowerBound, int upperBound)
        {
            return _random.Next(lowerBound, upperBound);
        }

        public void NextBytes(byte[] buffer)
        {
            _random.NextBytes(buffer);
        }
    }
}
