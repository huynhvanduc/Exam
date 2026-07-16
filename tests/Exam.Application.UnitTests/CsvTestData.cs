using System.Globalization;
using CsvHelper;

namespace Exam.Application.UnitTests;

// Đọc case test từ file CSV trong TestData/ (copy ra output dir qua .csproj) thành hàng cho xUnit
// [Theory]/[MemberData] - mỗi cột chuyển kiểu qua 1 converter tương ứng theo đúng thứ tự tham số test.
public static class CsvTestData
{
    public static IEnumerable<object?[]> Read(string fileName, params Func<string, object?>[] converters)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var row = new object?[converters.Length];
            for (var i = 0; i < converters.Length; i++)
                row[i] = converters[i](csv.GetField(i)!);

            yield return row;
        }
    }

    public static string Str(string s) => s;
    public static object? NullableStr(string s) => string.IsNullOrEmpty(s) ? null : s;
    public static object Bool(string s) => bool.Parse(s);
    public static object? NullableBool(string s) => string.IsNullOrEmpty(s) ? null : bool.Parse(s);
    public static object Int(string s) => int.Parse(s, CultureInfo.InvariantCulture);
    public static object? NullableInt(string s) => string.IsNullOrEmpty(s) ? null : int.Parse(s, CultureInfo.InvariantCulture);
    public static object Decimal(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);
}
