using System.Text;
using Org.BouncyCastle.Crypto.Digests; // NuGet: BouncyCastle

namespace DouYin;
public static class StringProcessor
{
    private static readonly Random _rnd = new Random();

    public static string ToOrdStr(byte[] bytes)
    {
        char[] chars = bytes.Select(b => (char)b).ToArray();
        return new string(chars);
    }

    public static List<int> ToOrdArray(string s)
    {
        return s.Select(c => (int)c).ToList();
    }

    public static string ToCharStr(IEnumerable<int> arr)
    {
        return new string(arr.Select(i => (char)i).ToArray());
    }

    public static List<int> ToCharArray(string s)
    {
        return s.Select(c => (int)c).ToList();
    }

    // 模拟 JS 的无符号右移
    public static int JsShiftRight(long val, int n)
    {
        uint u = (uint)(val & 0xFFFFFFFF);
        return (int)(u >> n);
    }

    // 生成伪随机字节字符串（与 Python 版本算法等价）
    public static string GenerateRandomBytes(int length = 3)
    {
        List<char> result = new List<char>(length * 4);

        for (int t = 0; t < length; t++)
        {
            int _rd = (int)(_rnd.NextDouble() * 10000);
            char c1 = (char)(((_rd & 255) & 170) | 1);
            char c2 = (char)(((_rd & 255) & 85) | 2);
            char c3 = (char)((JsShiftRight(_rd, 8) & 170) | 5);
            char c4 = (char)((JsShiftRight(_rd, 8) & 85) | 40);

            result.Add(c1);
            result.Add(c2);
            result.Add(c3);
            result.Add(c4);
        }

        return new string(result.ToArray());
    }
}

public class CryptoUtility
{
    private readonly string salt;
    private readonly List<string> base64Alphabet;

    private readonly List<int> bigArray = new List<int>
    {
        121,243,55,234,103,36,47,228,30,231,106,6,115,95,78,101,250,207,198,50,
        139,227,220,105,97,143,34,28,194,215,18,100,159,160,43,8,169,217,180,120,
        247,45,90,11,27,197,46,3,84,72,5,68,62,56,221,75,144,79,73,161,
        178,81,64,187,134,117,186,118,16,241,130,71,89,147,122,129,65,40,88,150,
        110,219,199,255,181,254,48,4,195,248,208,32,116,167,69,201,17,124,125,104,
        96,83,80,127,236,108,154,126,204,15,20,135,112,158,13,1,188,164,210,237,
        222,98,212,77,253,42,170,202,26,22,29,182,251,10,173,152,58,138,54,141,
        185,33,157,31,252,132,233,235,102,196,191,223,240,148,39,123,92,82,128,109,
        57,24,38,113,209,245,2,119,153,229,189,214,230,174,232,63,52,205,86,140,
        66,175,111,171,246,133,238,193,99,60,74,91,225,51,76,37,145,211,166,151,
        213,206,0,200,244,176,218,44,184,172,49,216,93,168,53,21,183,41,67,85,
        224,155,226,242,87,177,146,70,190,12,162,19,137,114,25,165,163,192,23,59,
        9,94,179,107,35,7,142,131,239,203,149,136,61,249,14,156
    };

    public CryptoUtility(string salt, List<string> customBase64Alphabet)
    {
        this.salt = salt ?? "";
        this.base64Alphabet = customBase64Alphabet ?? new List<string>();
    }

    // 使用 BouncyCastle 的 SM3 实现
    public static List<int> Sm3ToArray(byte[] inputBytes)
    {
        SM3Digest digest = new SM3Digest();
        digest.BlockUpdate(inputBytes, 0, inputBytes.Length);
        byte[] output = new byte[digest.GetDigestSize()];
        digest.DoFinal(output, 0);

        // 返回与 Python 原代码等价的 int 列表（0-255）
        return output.Select(b => (int)(b & 0xFF)).ToList();
    }

    public static List<int> Sm3ToArray(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        return Sm3ToArray(bytes);
    }

    public string AddSalt(string param)
    {
        return param + this.salt;
    }

    private object ProcessParamObject(object param, bool addSalt)
    {
        // 在 Python 中只有字符串会被追加 salt
        if (param is string s && addSalt)
        {
            return AddSalt(s);
        }

        return param;
    }

    // params_to_array 支持 string 或 List<int>
    public List<int> ParamsToArray(string param, bool addSalt = true)
    {
        var processed = (string)ProcessParamObject(param, addSalt);
        return Sm3ToArray(processed);
    }

    public List<int> ParamsToArray(IEnumerable<int> paramEnumerable, bool addSalt = true)
    {
        // Python 中传进来的 List[int] 不会被加盐（因为 isinstance(param,str) 为 False）
        // 这里直接把 IEnumerable<int> 转为 byte[] 然后 SM3
        byte[] bytes = paramEnumerable.Select(i => (byte)(i & 0xFF)).ToArray();
        return Sm3ToArray(bytes);
    }

    // transform_bytes 的等价实现（会改变 bigArray 的内容，与 Python 中保持一致）
    public string TransformBytes(List<int> bytesList)
    {
        // 将 bytesList 转为字符串（每个 int -> char）
        string bytesStr = StringProcessor.ToCharStr(bytesList);

        var resultChars = new List<char>();
        int indexB = bigArray[1];
        int initialValue = 0;
        int valueE = 0;

        for (int index = 0; index < bytesStr.Length; index++)
        {
            int sumInitial;
            if (index == 0)
            {
                initialValue = bigArray[indexB];
                sumInitial = indexB + initialValue;

                bigArray[1] = initialValue;
                bigArray[indexB] = indexB;
            }
            else
            {
                sumInitial = initialValue + valueE;
            }

            int charValue = (int)bytesStr[index];
            sumInitial %= bigArray.Count;
            int valueF = bigArray[sumInitial];
            int encryptedChar = charValue ^ valueF;
            resultChars.Add((char)encryptedChar);

            valueE = bigArray[(index + 2) % bigArray.Count];
            sumInitial = (indexB + valueE) % bigArray.Count;
            initialValue = bigArray[sumInitial];
            bigArray[sumInitial] = bigArray[(index + 2) % bigArray.Count];
            bigArray[(index + 2) % bigArray.Count] = initialValue;
            indexB = sumInitial;
        }

        return new string(resultChars.ToArray());
    }

    // 自定义 Base64 编码（使用自定义字符表）
    public string Base64Encode(string input, int selectedAlphabet = 0)
    {
        if (selectedAlphabet < 0 || selectedAlphabet >= base64Alphabet.Count)
            throw new ArgumentOutOfRangeException(nameof(selectedAlphabet));

        string alphabet = base64Alphabet[selectedAlphabet];

        // 将每个字符转为 8 位二进制并拼接
        StringBuilder binaryBuilder = new StringBuilder();
        foreach (char c in input)
        {
            binaryBuilder.Append(Convert.ToString((int)c, 2).PadLeft(8, '0'));
        }

        string binaryString = binaryBuilder.ToString();
        int paddingLength = (6 - (binaryString.Length % 6)) % 6;
        binaryString = binaryString + new string('0', paddingLength);

        var indices = new List<int>();
        for (int i = 0; i < binaryString.Length; i += 6)
        {
            string slice = binaryString.Substring(i, 6);
            indices.Add(Convert.ToInt32(slice, 2));
        }

        StringBuilder outBuilder = new StringBuilder();
        foreach (int idx in indices)
        {
            outBuilder.Append(alphabet[idx]);
        }

        // Python 代码中: output_string += "=" * (padding_length // 2)
        outBuilder.Append(new string('=', paddingLength / 2));
        return outBuilder.ToString();
    }

    // abogus_encode
    public string AbogusEncode(string abogusBytesStr, int selectedAlphabet)
    {
        if (selectedAlphabet < 0 || selectedAlphabet >= base64Alphabet.Count)
            throw new ArgumentOutOfRangeException(nameof(selectedAlphabet));

        string alphabet = base64Alphabet[selectedAlphabet];
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < abogusBytesStr.Length; i += 3)
        {
            int n;
            if (i + 2 < abogusBytesStr.Length)
            {
                n = (ord(abogusBytesStr[i]) << 16) | (ord(abogusBytesStr[i + 1]) << 8) | ord(abogusBytesStr[i + 2]);
            }
            else if (i + 1 < abogusBytesStr.Length)
            {
                n = (ord(abogusBytesStr[i]) << 16) | (ord(abogusBytesStr[i + 1]) << 8);
            }
            else
            {
                n = ord(abogusBytesStr[i]) << 16;
            }

            var shifts = new[] { 18, 12, 6, 0 };
            var masks = new[] { 0xFC0000, 0x03F000, 0x0FC0, 0x3F };

            for (int idx = 0; idx < shifts.Length; idx++)
            {
                int j = shifts[idx];
                int k = masks[idx];
                if (j == 6 && i + 1 >= abogusBytesStr.Length)
                    break;
                if (j == 0 && i + 2 >= abogusBytesStr.Length)
                    break;
                int index = (n & k) >> j;
                sb.Append(alphabet[index]);
            }
        }

        int padCount = (4 - (sb.Length % 4)) % 4;
        sb.Append(new string('=', padCount));
        return sb.ToString();
    }

    // RC4
    public static byte[] Rc4Encrypt(byte[] key, string plaintext)
    {
        int[] S = Enumerable.Range(0, 256).ToArray();
        int j = 0;
        for (int i = 0; i < 256; i++)
        {
            j = (j + S[i] + key[i % key.Length]) & 0xFF;
            int tmp = S[i];
            S[i] = S[j];
            S[j] = tmp;
        }

        int ii = 0;
        j = 0;
        var ciphertext = new List<byte>(plaintext.Length);
        foreach (char ch in plaintext)
        {
            ii = (ii + 1) & 0xFF;
            j = (j + S[ii]) & 0xFF;
            int tmp = S[ii];
            S[ii] = S[j];
            S[j] = tmp;
            int K = S[(S[ii] + S[j]) & 0xFF];
            ciphertext.Add((byte)((int)ch ^ K));
        }

        return ciphertext.ToArray();
    }

    private static int ord(char c) => (int)c;
}

public static class BrowserFingerprintGenerator
{
    private static readonly Random _rnd = new Random();

    public static string GenerateFingerprint(string browserType = "Edge")
    {
        var browsers = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Chrome", GenerateChromeFingerprint },
            { "Firefox", GenerateFirefoxFingerprint },
            { "Safari", GenerateSafariFingerprint },
            { "Edge", GenerateEdgeFingerprint }
        };

        if (browsers.TryGetValue(browserType, out var func))
            return func();
        return GenerateChromeFingerprint();
    }

    public static string GenerateChromeFingerprint() => _GenerateFingerprint("Win32");
    public static string GenerateFirefoxFingerprint() => _GenerateFingerprint("Win32");
    public static string GenerateSafariFingerprint() => _GenerateFingerprint("MacIntel");
    public static string GenerateEdgeFingerprint() => _GenerateFingerprint("Win32");

    private static string _GenerateFingerprint(string platform)
    {
        int innerWidth = _rnd.Next(1024, 1921);
        int innerHeight = _rnd.Next(768, 1081);
        int outerWidth = innerWidth + _rnd.Next(24, 33);
        int outerHeight = innerHeight + _rnd.Next(75, 91);
        int screenX = 0;
        int screenY = _rnd.Next(0, 2) == 0 ? 0 : 30;
        int sizeWidth = _rnd.Next(1024, 1921);
        int sizeHeight = _rnd.Next(768, 1081);
        int availWidth = _rnd.Next(1280, 1921);
        int availHeight = _rnd.Next(800, 1081);

        var fingerprint = string.Join("|", new object[]
        {
            innerWidth, innerHeight, outerWidth, outerHeight,
            screenX, screenY, 0, 0, sizeWidth, sizeHeight,
            availWidth, availHeight, innerWidth, innerHeight, 24, 24, platform
        });

        return fingerprint;
    }
}

public class ABogus
{
    public int Aid { get; set; } = 6383;
    public int PageId { get; set; } = 0;
    public string Salt { get; set; } = "cus";
    public bool Boe { get; set; } = false;
    public double Ddrt { get; set; } = 8.5;
    public double Ic { get; set; } = 8.5;
    public List<string> Paths { get; set; } = new List<string>
    {
        "^/webcast/", "^/aweme/v1/", "^/aweme/v2/", "/v1/message/send", "^/live/", "^/captcha/", "^/ecom/"
    };

    public List<int> Array1 { get; private set; } = new List<int>();
    public List<int> Array2 { get; private set; } = new List<int>();
    public List<int> Array3 { get; private set; } = new List<int>();

    public List<int> Options { get; set; } = new List<int> { 0, 1, 14 };
    public byte[] UaKey { get; set; } = new byte[] { 0x00, 0x01, 0x0E };

    private readonly string character =
        "Dkdpgh2ZmsQB80/MfvV36XI1R45-WUAlEixNLwoqYTOPuzKFjJnry79HbGcaStCe";
    private readonly string character2 =
        "ckdp1h4ZKsUB80/Mfvw36XIgR25+WQAlEi7NLboqYTOPuzmFjJnryx9HVGDaStCe";
    private readonly List<string> characterList;

    private readonly CryptoUtility cryptoUtility;
    public string UserAgent { get; set; }
    public string BrowserFp { get; set; }

    private readonly List<int> sortIndex = new List<int>
    {
        18,20,52,26,30,34,58,38,40,53,42,21,27,54,55,31,35,57,39,41,43,22,28,
        32,60,36,23,29,33,37,44,45,59,46,47,48,49,50,24,25,65,66,70,71
    };

    private readonly List<int> sortIndex2 = new List<int>
    {
        18,20,26,30,34,38,40,42,21,27,31,35,39,41,43,22,28,32,36,23,29,33,37,
        44,45,46,47,48,49,50,24,25,52,53,54,55,57,58,59,60,65,66,70,71
    };

    public ABogus(string fp = "", string userAgent = "", List<int> options = null)
    {
        if (options != null)
            this.Options = new List<int>(options);

        this.characterList = new List<string> { character, character2 };
        this.cryptoUtility = new CryptoUtility(this.Salt, this.characterList);

        this.UserAgent = !string.IsNullOrEmpty(userAgent)
            ? userAgent
            : "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0";

        this.BrowserFp = !string.IsNullOrEmpty(fp)
            ? fp
            : BrowserFingerprintGenerator.GenerateFingerprint("Edge");
    }

    public string EncodeData(string data, int alphabetIndex = 0)
    {
        return cryptoUtility.AbogusEncode(data, alphabetIndex);
    }

    public (string Params, string ABogus, string UserAgent, string Body) GenerateAbogus(string paramsStr, string body = "")
    {
        // ab_dir 初始（在 Python 源中有部分是复杂对象，但后续并未用到）
        // 我们把需要的整数项初始化；未使用的项保留为 0
        var abDir = new Dictionary<int, int>();
        abDir[8] = 3;
        abDir[18] = 44;
        abDir[19] = 0; // 原为列表，但未用于计算流程中（占位）
        abDir[66] = 0; abDir[69] = 0; abDir[70] = 0; abDir[71] = 0;

        long startEncryption = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // params 参数加盐加密（注意：Python 中两次 params_to_array 嵌套）
        var inner1 = cryptoUtility.ParamsToArray(paramsStr);
        var array1 = cryptoUtility.ParamsToArray(inner1);
        var inner2 = cryptoUtility.ParamsToArray(body);
        var array2 = cryptoUtility.ParamsToArray(inner2);

        // UA 加密流程：RC4 -> bytes->string(每字节->char) -> base64 with custom alphabet index=1 -> SM3 without salt
        byte[] rc4Ua = CryptoUtility.Rc4Encrypt(UaKey, this.UserAgent);
        string uaOrdStr = StringProcessor.ToOrdStr(rc4Ua);
        string uaBase64 = cryptoUtility.Base64Encode(uaOrdStr, 1);
        var array3 = cryptoUtility.ParamsToArray(uaBase64, addSalt: false);

        long endEncryption = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 插入加密开始时间（分解为字节）
        abDir[20] = (int)((startEncryption >> 24) & 255);
        abDir[21] = (int)((startEncryption >> 16) & 255);
        abDir[22] = (int)((startEncryption >> 8) & 255);
        abDir[23] = (int)(startEncryption & 255);
        abDir[24] = (int)((startEncryption / (256L * 256 * 256 * 256)) & 0xFFFFFFFF);
        abDir[25] = (int)((startEncryption / (256L * 256 * 256 * 256 * 256)) & 0xFFFFFFFF);

        // 请求头配置
        abDir[26] = (Options[0] >> 24) & 255;
        abDir[27] = (Options[0] >> 16) & 255;
        abDir[28] = (Options[0] >> 8) & 255;
        abDir[29] = Options[0] & 255;

        // 请求方法
        abDir[30] = (int)((Options[1] / 256) & 255);
        abDir[31] = (int)((Options[1] % 256) & 255);
        abDir[32] = (Options[1] >> 24) & 255;
        abDir[33] = (Options[1] >> 16) & 255;

        // 请求头加密
        abDir[34] = (Options[2] >> 24) & 255;
        abDir[35] = (Options[2] >> 16) & 255;
        abDir[36] = (Options[2] >> 8) & 255;
        abDir[37] = Options[2] & 255;

        // 请求体加密/插入部分 (array1 & array2 & array3)
        if (array1.Count > 22)
        {
            abDir[38] = array1[21];
            abDir[39] = array1[22];
        }
        else
        {
            abDir[38] = 0; abDir[39] = 0;
        }
        if (array2.Count > 22)
        {
            abDir[40] = array2[21];
            abDir[41] = array2[22];
        }
        else
        {
            abDir[40] = 0; abDir[41] = 0;
        }
        if (array3.Count > 24)
        {
            abDir[42] = array3[23];
            abDir[43] = array3[24];
        }
        else
        {
            abDir[42] = array3.ElementAtOrDefault(23);
            abDir[43] = array3.ElementAtOrDefault(24);
        }

        // 加密结束时间
        abDir[44] = (int)((endEncryption >> 24) & 255);
        abDir[45] = (int)((endEncryption >> 16) & 255);
        abDir[46] = (int)((endEncryption >> 8) & 255);
        abDir[47] = (int)(endEncryption & 255);
        abDir[48] = abDir.ContainsKey(8) ? abDir[8] : 0;
        abDir[49] = (int)((endEncryption / (256L * 256 * 256 * 256)) & 0xFFFFFFFF);
        abDir[50] = (int)((endEncryption / (256L * 256 * 256 * 256 * 256)) & 0xFFFFFFFF);

        // 固定值
        abDir[51] = (PageId >> 24) & 255;
        abDir[52] = (PageId >> 16) & 255;
        abDir[53] = (PageId >> 8) & 255;
        abDir[54] = PageId & 255;
        abDir[55] = PageId;
        abDir[56] = Aid;
        abDir[57] = Aid & 255;
        abDir[58] = (Aid >> 8) & 255;
        abDir[59] = (Aid >> 16) & 255;
        abDir[60] = (Aid >> 24) & 255;

        // 浏览器指纹长度
        abDir[64] = BrowserFp.Length;
        abDir[65] = BrowserFp.Length;

        // 获取 ab_dir 中 sort_index 的值
        var sortedValues = sortIndex.Select(i => abDir.ContainsKey(i) ? abDir[i] : 0).ToList();

        // 浏览器指纹 ASCII 列表
        var edgeFpArray = StringProcessor.ToCharArray(BrowserFp);

        // ab_xor 的初值与计算（与 Python 等价）
        int abXor = ((BrowserFp.Length & 255) >> 8) & 255; // 通常为 0
        for (int idx = 0; idx < sortIndex2.Count - 1; idx++)
        {
            if (idx == 0)
                abXor = abDir.ContainsKey(sortIndex2[idx]) ? abDir[sortIndex2[idx]] : 0;
            abXor ^= abDir.ContainsKey(sortIndex2[idx + 1]) ? abDir[sortIndex2[idx + 1]] : 0;
        }

        sortedValues.AddRange(edgeFpArray);
        sortedValues.Add(abXor);

        string abogusBytesStr = StringProcessor.GenerateRandomBytes() + cryptoUtility.TransformBytes(sortedValues);
        string abogus = cryptoUtility.AbogusEncode(abogusBytesStr, 0);

        string newParams = $"{paramsStr}&a_bogus={abogus}";
        return (newParams, abogus, this.UserAgent, body);
    }
}