

using System.Collections;

namespace Tungsten;
public class Range : IEnumerator, IEnumerable
{
    private int i;
    private int start;
    private int end;
    private int step;

    public Range(int start, int end, int step)
    {
        this.start = start;
        i = start;
        this.end = end;
        this.step = step;
    }

    public object Current
    {
        get
        {
            return i++;
        }
    }

    public IEnumerator GetEnumerator()
    {
        return this;
    }

    public bool MoveNext()
    {
        return i < end;
    }

    public void Reset()
    {
        i = start;
    }
}
