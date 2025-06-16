
namespace URLShortener.Application.Services;

    public interface IBase62Encoder
    {
        string Encode(long number);
        long Decode(string encoded);
        string GenerateShortCode();
    }

    
