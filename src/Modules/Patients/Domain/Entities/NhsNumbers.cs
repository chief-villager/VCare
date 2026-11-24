namespace VCare.Modules.Patients.Domain.Entities;

/// <summary>NHS numbers: ten digits, the last a modulus 11 check digit over the first nine.</summary>
internal static class NhsNumbers
{
    /// <summary>The ten digits, with spaces and dashes taken out; null when it isn't a valid NHS number.</summary>
    public static string? Normalise(string written)
    {
        var digits = new string([.. written.Where(c => c is not ' ' and not '-')]);
        if (digits.Length != 10 || !digits.All(char.IsAsciiDigit))
            return null;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (digits[i] - '0') * (10 - i);
        var check = 11 - sum % 11;
        if (check == 11)
            check = 0;
        // A check digit of 10 means no valid number has these first nine digits.
        return check != 10 && check == digits[9] - '0' ? digits : null;
    }
}
