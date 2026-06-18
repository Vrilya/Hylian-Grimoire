namespace HylianGrimoire.Compression;

internal static partial class RawDeflateEncoder
{
    private static List<Token> Tokenize(byte[] data)
    {
        int[] head = new int[MatchHashSize];
        int[] prev = new int[WindowSize];
        Array.Fill(head, NoPos);
        Array.Fill(prev, NoPos);

        byte[] tail = MakeTailWindow(data, out int tailBase);
        var tokens = new List<Token>(Math.Max(1024, data.Length / 2));
        int pos = 0;
        bool pendingLiteral = false;
        int matchLen = MinMatch - 1;
        int matchStart = NoPos;
        uint outPos = 0;

        while (pos < data.Length)
        {
            int chainHead = pos + MinMatch <= data.Length ? InsertString(data, pos, head, prev) : NoPos;
            int prevLen = matchLen;
            int seedStart = matchStart;
            int matchDist = 0;
            matchLen = MinMatch - 1;
            matchStart = NoPos;

            if (chainHead != NoPos && prevLen < LazyMatchLimit && pos - chainHead <= MatchDistanceLimit)
            {
                (matchLen, matchDist) = FindMatch(data, pos, chainHead, prev, prevLen, seedStart, tail, tailBase);
                matchStart = matchLen >= MinMatch ? pos - matchDist : NoPos;
            }

            if (prevLen >= MinMatch && seedStart != NoPos && matchLen <= prevLen)
            {
                outPos += (uint)prevLen;
                tokens.Add(Token.CreateMatch((ushort)prevLen, (ushort)((pos - 1) - seedStart), outPos));

                int end = pos + prevLen - 1;
                if (end > data.Length)
                {
                    end = data.Length;
                }

                pos++;
                while (pos < end)
                {
                    if (pos + MinMatch <= data.Length)
                    {
                        InsertString(data, pos, head, prev);
                    }

                    pos++;
                }

                pendingLiteral = false;
                matchLen = MinMatch - 1;
            }
            else if (pendingLiteral)
            {
                outPos++;
                tokens.Add(Token.CreateLiteral(data[pos - 1], outPos));
                pos++;
            }
            else
            {
                pendingLiteral = true;
                pos++;
            }
        }

        if (pendingLiteral && data.Length > 0)
        {
            outPos++;
            tokens.Add(Token.CreateLiteral(data[^1], outPos));
        }

        return tokens;
    }

    private static byte[] MakeTailWindow(byte[] data, out int baseOffset)
    {
        int cap = (WindowSize * 2) + MatchProbeTail.Length + 0x4000;
        byte[] window = new byte[cap];
        int readPos = 0;
        int localPos = 0;
        int baseValue = 0;

        while (readPos < data.Length)
        {
            if (localPos >= WindowSize + MatchDistanceLimit)
            {
                Buffer.BlockCopy(window, WindowSize, window, 0, WindowSize);
                baseValue += WindowSize;
                localPos -= WindowSize;
            }

            int available = (WindowSize * 2) - localPos;
            int chunk = Math.Min(data.Length - readPos, available);
            Buffer.BlockCopy(data, readPos, window, localPos, chunk);
            readPos += chunk;
            localPos += chunk;
        }

        if (localPos >= (WindowSize * 2) - 1)
        {
            Buffer.BlockCopy(window, WindowSize, window, 0, WindowSize);
            baseValue += WindowSize;
        }

        Buffer.BlockCopy(MatchProbeTail, 0, window, WindowSize * 2, MatchProbeTail.Length);
        baseOffset = baseValue;
        return window;
    }

    private static byte WindowByte(byte[] data, int pos, byte[] tail, int tailBase)
    {
        if (pos >= 0 && pos < data.Length)
        {
            return data[pos];
        }

        int local = pos - tailBase;
        return local >= 0 && local < tail.Length ? tail[local] : (byte)0;
    }

    private static int InsertString(byte[] data, int pos, int[] head, int[] prev)
    {
        if (pos + MinMatch > data.Length)
        {
            return NoPos;
        }

        int h = Hash3(data, pos);
        int old = head[h];
        prev[pos & (WindowSize - 1)] = old;
        head[h] = pos;
        return old;
    }

    private static int Hash3(byte[] data, int pos)
    {
        uint h = (((uint)data[pos] << MatchHashShift) ^ data[pos + 1]) & MatchHashMask;
        return (int)(((h << MatchHashShift) ^ data[pos + 2]) & MatchHashMask);
    }

    private static (int Length, int Distance) FindMatch(
        byte[] data,
        int pos,
        int chainHead,
        int[] prev,
        int prevLength,
        int seedStart,
        byte[] tail,
        int tailBase)
    {
        (int length, int distance) = FindLongestMatch(
            data,
            pos,
            chainHead,
            prev,
            searchBudget: 4096,
            seedLength: prevLength,
            seedStart,
            tail,
            tailBase);
        int remaining = data.Length - pos;
        if (length > remaining)
        {
            length = remaining;
        }

        if (length == MinMatch && distance > ShortMatchDistanceLimit)
        {
            length--;
        }

        return (length, distance);
    }

    private static (int Length, int Distance) FindLongestMatch(
        byte[] data,
        int pos,
        int chainHead,
        int[] prev,
        int searchBudget,
        int seedLength,
        int seedStart,
        byte[] tail,
        int tailBase)
    {
        if (pos + MinMatch > data.Length)
        {
            return (0, 0);
        }

        int cur = chainHead;
        int limit = pos - MatchDistanceLimit;
        if (limit < 1)
        {
            limit = 1;
        }

        bool includeLimit = pos < WindowSize + MatchDistanceLimit;
        int bestLength = seedLength;
        int bestStart = seedStart;
        int probeEnd = pos + MaxMatch;
        int chainLeft = seedLength >= FastChainMatch ? searchBudget >> 2 : searchBudget;
        int eofEqualUpdates = 0;

        while ((includeLimit ? cur >= limit : cur > limit) && chainLeft > 0 && cur != NoPos)
        {
            chainLeft--;
            if (WindowByte(data, cur + bestLength, tail, tailBase) != WindowByte(data, pos + bestLength, tail, tailBase)
                || data[cur] != data[pos])
            {
                cur = prev[cur & (WindowSize - 1)];
                continue;
            }

            int n = 1;
            int maxN = probeEnd - pos;
            while (n < maxN && WindowByte(data, cur + n, tail, tailBase) == WindowByte(data, pos + n, tail, tailBase))
            {
                n++;
            }

            if (n > bestLength)
            {
                bestLength = n;
                bestStart = cur;
                eofEqualUpdates = 0;
                if (n >= MaxMatch)
                {
                    break;
                }
            }
            else if (n == bestLength
                && bestStart != NoPos
                && bestLength > data.Length - pos
                && eofEqualUpdates < 4)
            {
                bestStart = cur;
                eofEqualUpdates++;
            }

            cur = prev[cur & (WindowSize - 1)];
        }

        if (bestLength < MinMatch || bestStart == NoPos)
        {
            return (0, 0);
        }

        int distance = pos - bestStart;
        if (bestLength == MinMatch && distance > ShortMatchDistanceLimit)
        {
            return (0, 0);
        }

        return (bestLength, distance);
    }
}
