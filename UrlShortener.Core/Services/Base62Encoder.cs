using System.Text;

namespace URLShortener.Core.Services
{
    public interface IBase62Encoder
    {
        string Encode(long number);
        long Decode(string encoded);
        string GenerateShortCode();
    }

    public class Base62Encoder : IBase62Encoder
    {
        private const string Characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private const int Base = 62;

        private const long CustomEpoch = 1640995200000L;
        private const int TimestampBits = 41;
        private const int DatacenterBits = 5;
        private const int WorkerBits = 5;
        private const int SequenceBits = 12;

        private const long MaxDatacenterId = (1L << DatacenterBits) - 1;
        private const long MaxWorkerId = (1L << WorkerBits) - 1;
        private const long MaxSequence = (1L << SequenceBits) - 1;

        private const int WorkerIdShift = SequenceBits;
        private const int DatacenterIdShift = SequenceBits + WorkerBits;
        private const int TimestampShift = SequenceBits + WorkerBits + DatacenterBits;

        private readonly long _datacenterId;
        private readonly long _workerId;

        private long _lastTimestamp = -1L;
        private long _sequence = 0L;

        public Base62Encoder(long datacenterId = 1, long workerId = 1)
        {
            ValidateConfiguration(datacenterId, workerId);
            _datacenterId = datacenterId;
            _workerId = workerId;
        }

        public string GenerateShortCode()
        {
            var snowflakeId = GenerateSnowflakeId();
            return Encode(snowflakeId);
        }

        private long GenerateSnowflakeId()
        {
            while (true)
            {
                var currentTimestamp = GetCurrentTimestamp();
                var lastTimestamp = Interlocked.Read(ref _lastTimestamp);

                if (currentTimestamp < lastTimestamp)
                {
                    throw new InvalidOperationException("Clock moved backwards");
                }

                if (currentTimestamp == lastTimestamp)
                {
                    var currentSequence = Interlocked.Increment(ref _sequence) - 1;

                    if (currentSequence > MaxSequence)
                    {
                        Interlocked.Exchange(ref _sequence, 0L);
                        continue;
                    }

                    return ((currentTimestamp - CustomEpoch) << TimestampShift) |
                           (_datacenterId << DatacenterIdShift) |
                           (_workerId << WorkerIdShift) |
                           currentSequence;
                }
                else
                {
                    if (Interlocked.CompareExchange(ref _lastTimestamp, currentTimestamp, lastTimestamp) == lastTimestamp)
                    {
                        Interlocked.Exchange(ref _sequence, 0L);

                        return ((currentTimestamp - CustomEpoch) << TimestampShift) |
                               (_datacenterId << DatacenterIdShift) |
                               (_workerId << WorkerIdShift) |
                               0L;
                    }
                }
            }
        }

        public string Encode(long number)
        {
            if (number == 0) return Characters[0].ToString();

            var result = new StringBuilder();
            while (number > 0)
            {
                result.Insert(0, Characters[(int)(number % Base)]);
                number /= Base;
            }
            return result.ToString();
        }

        public long Decode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return 0;

            long result = 0;
            for (int i = 0; i < encoded.Length; i++)
            {
                var charIndex = Characters.IndexOf(encoded[i]);
                if (charIndex == -1)
                    throw new ArgumentException($"Invalid character '{encoded[i]}' in Base62 string");

                result = result * Base + charIndex;
            }
            return result;
        }

        private long GetCurrentTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static void ValidateConfiguration(long datacenterId, long workerId)
        {
            if (datacenterId > MaxDatacenterId || datacenterId < 0)
                throw new ArgumentException($"Datacenter ID must be between 0 and {MaxDatacenterId}");

            if (workerId > MaxWorkerId || workerId < 0)
                throw new ArgumentException($"Worker ID must be between 0 and {MaxWorkerId}");
        }
    }
}
