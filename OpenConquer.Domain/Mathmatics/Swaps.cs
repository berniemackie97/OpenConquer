
namespace OpenConquer.Domain.Mathmatics
{
    public static class Swaps
    {
        public static void Swap(this ref uint n1, ref uint n2)
        {
            (n2, n1) = (n1, n2);
        }
    }
}
