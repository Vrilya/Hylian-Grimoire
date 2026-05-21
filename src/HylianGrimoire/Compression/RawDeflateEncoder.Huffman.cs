namespace HylianGrimoire.Compression;

internal static partial class RawDeflateEncoder
{
    private static int LengthSymbol(int length)
    {
        if (length == 258)
        {
            return 285;
        }

        for (int i = 0; i < 29; i++)
        {
            int extra = LenExtra[i];
            if ((extra == 0 && length == LenBase[i])
                || (extra != 0 && length >= LenBase[i] && length < LenBase[i] + (1 << extra)))
            {
                return 257 + i;
            }
        }

        return -1;
    }

    private static int DistanceSymbol(int distance)
    {
        for (int i = 0; i < 30; i++)
        {
            int extra = DistExtra[i];
            if ((extra == 0 && distance == DistBase[i])
                || (extra != 0 && distance >= DistBase[i] && distance < DistBase[i] + (1 << extra)))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool SmallerNode(uint[] freq, byte[] depth, int a, int b) =>
        freq[a] < freq[b] || (freq[a] == freq[b] && depth[a] <= depth[b]);

    private static void DownHeap(int[] heap, int heapLength, int index, uint[] freq, byte[] depth)
    {
        int value = heap[index];
        while (index <= heapLength / 2)
        {
            int child = index * 2;
            if (child < heapLength && SmallerNode(freq, depth, heap[child + 1], heap[child]))
            {
                child++;
            }

            if (SmallerNode(freq, depth, value, heap[child]))
            {
                break;
            }

            heap[index] = heap[child];
            index = child;
        }

        heap[index] = value;
    }

    private static byte[] HuffmanLengths(uint[] frequencies, int n, int maxBits)
    {
        int maxNodes = (n * 2) + 1;
        uint[] nodeFreq = new uint[600];
        byte[] depth = new byte[600];
        int[] parent = new int[600];
        int[] heap = new int[600];
        int[] heapOrder = new int[600];
        int[] active = new int[300];
        byte[] output = new byte[n];
        Array.Fill(parent, -1);

        int activeCount = 0;
        int highestSymbol = -1;
        for (int i = 0; i < n; i++)
        {
            nodeFreq[i] = frequencies[i];
            if (frequencies[i] != 0)
            {
                active[activeCount++] = i;
                highestSymbol = i;
            }
        }

        if (activeCount == 0)
        {
            return output;
        }

        heap[0] = 0;
        int heapLength = 0;
        for (int i = 0; i < activeCount; i++)
        {
            heap[++heapLength] = active[i];
        }

        while (heapLength < 2)
        {
            int dummy = highestSymbol < 2 ? highestSymbol + 1 : 0;
            bool exists = true;
            while (exists)
            {
                exists = false;
                for (int j = 0; j < activeCount; j++)
                {
                    if (active[j] == dummy)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    dummy++;
                }
            }

            active[activeCount++] = dummy;
            if (dummy > highestSymbol)
            {
                highestSymbol = dummy;
            }

            nodeFreq[dummy] = 1;
            heap[++heapLength] = dummy;
        }

        for (int i = heapLength / 2; i >= 1; i--)
        {
            DownHeap(heap, heapLength, i, nodeFreq, depth);
        }

        int heapMax = maxNodes;
        int nextNode = n;
        while (heapLength >= 2)
        {
            int n1 = heap[1];
            heap[1] = heap[heapLength--];
            DownHeap(heap, heapLength, 1, nodeFreq, depth);
            heapOrder[--heapMax] = n1;

            int n2 = heap[1];
            heapOrder[--heapMax] = n2;

            int node = nextNode++;
            nodeFreq[node] = nodeFreq[n1] + nodeFreq[n2];
            depth[node] = (byte)(Math.Max(depth[n1], depth[n2]) + 1);
            parent[n1] = node;
            parent[n2] = node;
            heap[1] = node;
            DownHeap(heap, heapLength, 1, nodeFreq, depth);
        }

        heapOrder[--heapMax] = heap[1];

        byte[] nodeLength = new byte[600];
        int[] bitLengthCount = new int[16];
        int overflow = 0;
        for (int h = heapMax + 1; h < maxNodes; h++)
        {
            int node = heapOrder[h];
            int bits = parent[node] >= 0 ? nodeLength[parent[node]] + 1 : 0;
            if (bits > maxBits)
            {
                bits = maxBits;
                overflow++;
            }

            nodeLength[node] = (byte)bits;
            if (node <= highestSymbol)
            {
                bitLengthCount[bits]++;
            }
        }

        if (overflow == 0)
        {
            for (int i = 0; i < n && i <= highestSymbol; i++)
            {
                output[i] = nodeLength[i];
            }

            return output;
        }

        while (overflow > 0)
        {
            int bits = maxBits - 1;
            while (bits > 0 && bitLengthCount[bits] == 0)
            {
                bits--;
            }

            bitLengthCount[bits]--;
            bitLengthCount[bits + 1] += 2;
            bitLengthCount[maxBits]--;
            overflow -= 2;
        }

        int orderIndex = maxNodes;
        for (int bits = maxBits; bits > 0; bits--)
        {
            int count = bitLengthCount[bits];
            while (count > 0)
            {
                int node = heapOrder[--orderIndex];
                if (node > highestSymbol)
                {
                    continue;
                }

                output[node] = (byte)bits;
                count--;
            }
        }

        return output;
    }

    private static void FrequenciesForBlock(List<Token> tokens, int start, int end, uint[] literalFrequency, uint[] distanceFrequency)
    {
        Array.Clear(literalFrequency);
        Array.Clear(distanceFrequency);

        for (int i = start; i < end; i++)
        {
            Token token = tokens[i];
            if (token.Type == TokenType.Literal)
            {
                literalFrequency[token.Literal]++;
            }
            else
            {
                literalFrequency[LengthSymbol(token.Length)]++;
                distanceFrequency[DistanceSymbol(token.Distance)]++;
            }
        }

        literalFrequency[256]++;
        for (int i = 0; i < 30; i++)
        {
            if (distanceFrequency[i] != 0)
            {
                return;
            }
        }

        distanceFrequency[0] = 1;
    }

    private static (int LiteralCount, int DistanceCount) TrimLengths(byte[] literalLengths, byte[] distanceLengths)
    {
        int lastLiteral = 256;
        int lastDistance = 0;
        for (int i = 0; i < 286; i++)
        {
            if (literalLengths[i] != 0)
            {
                lastLiteral = i;
            }
        }

        for (int i = 0; i < 30; i++)
        {
            if (distanceLengths[i] != 0)
            {
                lastDistance = i;
            }
        }

        int literalCount = Math.Max(lastLiteral + 1, 257);
        int distanceCount = Math.Max(lastDistance + 1, 1);
        return (literalCount, distanceCount);
    }

    private static List<CodeLengthItem> EncodeCodeLengths(byte[] lengths, int count)
    {
        var output = new List<CodeLengthItem>();
        int previousLength = -1;
        int nextLength = count > 0 ? lengths[0] : 0xffff;
        int runCount = 0;
        int repeatMax = 7;
        int repeatMin = 4;
        if (nextLength == 0)
        {
            repeatMax = 138;
            repeatMin = 3;
        }

        for (int i = 0; i < count; i++)
        {
            int currentLength = nextLength;
            nextLength = i + 1 < count ? lengths[i + 1] : 0xffff;
            runCount++;

            if (runCount < repeatMax && currentLength == nextLength)
            {
                continue;
            }

            if (runCount < repeatMin)
            {
                for (int j = 0; j < runCount; j++)
                {
                    output.Add(new CodeLengthItem(currentLength, 0, 0));
                }
            }
            else if (currentLength != 0)
            {
                if (currentLength != previousLength)
                {
                    output.Add(new CodeLengthItem(currentLength, 0, 0));
                    runCount--;
                }

                output.Add(new CodeLengthItem(16, runCount - 3, 2));
            }
            else if (runCount <= 10)
            {
                output.Add(new CodeLengthItem(17, runCount - 3, 3));
            }
            else
            {
                output.Add(new CodeLengthItem(18, runCount - 11, 7));
            }

            runCount = 0;
            previousLength = currentLength;
            if (nextLength == 0)
            {
                repeatMax = 138;
                repeatMin = 3;
            }
            else if (currentLength == nextLength)
            {
                repeatMax = 6;
                repeatMin = 3;
            }
            else
            {
                repeatMax = 7;
                repeatMin = 4;
            }
        }

        return output;
    }

    private static void FixedLengths(byte[] literalLengths, byte[] distanceLengths)
    {
        Array.Clear(literalLengths);
        Array.Clear(distanceLengths);
        for (int i = 0; i < 144; i++)
        {
            literalLengths[i] = 8;
        }

        for (int i = 144; i < 256; i++)
        {
            literalLengths[i] = 9;
        }

        for (int i = 256; i < 280; i++)
        {
            literalLengths[i] = 7;
        }

        for (int i = 280; i < 288; i++)
        {
            literalLengths[i] = 8;
        }

        for (int i = 0; i < 32; i++)
        {
            distanceLengths[i] = 5;
        }
    }
}
