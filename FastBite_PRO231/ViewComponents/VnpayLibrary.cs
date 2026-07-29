using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FastBite_PRO231.Helpers;

public class VnpayLibrary
{
    private readonly SortedList<string, string> _requestData = new(StringComparer.Ordinal);
    private readonly SortedList<string, string> _responseData = new(StringComparer.Ordinal);

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _requestData.Add(key, value);
        }
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _responseData.Add(key, value);
        }
    }

    public string GetResponseData(string key)
    {
        return _responseData.TryGetValue(key, out var value) ? value : "";
    }

    // Tạo URL thanh toán, kèm chữ ký HMAC-SHA512
    public string CreateRequestUrl(string baseUrl, string hashSecret)
    {
        var queryBuilder = new StringBuilder();

        foreach (var kv in _requestData)
        {
            if (queryBuilder.Length > 0) queryBuilder.Append('&');

            // ĐỔI: dùng WebUtility.UrlEncode (chuẩn form-urlencoded) thay vì Uri.EscapeDataString
            queryBuilder.Append(WebUtility.UrlEncode(kv.Key));
            queryBuilder.Append('=');
            queryBuilder.Append(WebUtility.UrlEncode(kv.Value));
        }

        var queryString = queryBuilder.ToString();
        var secureHash = HmacSha512(hashSecret, queryString);

        return $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";
    }

    // Kiểm tra chữ ký trả về từ VNPay (chống giả mạo callback)
    public bool ValidateSignature(string inputHash, string hashSecret)
    {
        var queryBuilder = new StringBuilder();

        foreach (var kv in _responseData)
        {
            if (kv.Key == "vnp_SecureHash" || kv.Key == "vnp_SecureHashType")
                continue;

            if (queryBuilder.Length > 0) queryBuilder.Append('&');

            queryBuilder.Append(WebUtility.UrlEncode(kv.Key));
            queryBuilder.Append('=');
            queryBuilder.Append(WebUtility.UrlEncode(kv.Value));
        }

        var computedHash = HmacSha512(hashSecret, queryBuilder.ToString());

        return computedHash.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string HmacSha512(string key, string inputData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);

        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}