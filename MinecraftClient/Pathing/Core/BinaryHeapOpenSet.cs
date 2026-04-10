using System;

namespace MinecraftClient.Pathing.Core
{
    /// <summary>
    /// Min-heap of PathNodes ordered by FCost, used as the A* open set.
    /// </summary>
    public sealed class BinaryHeapOpenSet
    {
        private PathNode[] _heap;
        private int _size;

        public int Count => _size;

        public BinaryHeapOpenSet(int initialCapacity = 1024)
        {
            _heap = new PathNode[initialCapacity];
            _size = 0;
        }

        public void Insert(PathNode node)
        {
            if (_size == _heap.Length)
                Array.Resize(ref _heap, _heap.Length * 2);

            node.HeapIndex = _size;
            _heap[_size] = node;
            _size++;
            SiftUp(_size - 1);
        }

        public PathNode RemoveMin()
        {
            var min = _heap[0];
            _size--;
            if (_size > 0)
            {
                _heap[0] = _heap[_size];
                _heap[0].HeapIndex = 0;
                SiftDown(0);
            }
            _heap[_size] = null!;
            min.IsOpen = false;
            return min;
        }

        public void Update(PathNode node)
        {
            SiftUp(node.HeapIndex);
        }

        private void SiftUp(int i)
        {
            var node = _heap[i];
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (Compare(node, _heap[parent]) >= 0)
                    break;
                _heap[i] = _heap[parent];
                _heap[i].HeapIndex = i;
                i = parent;
            }
            _heap[i] = node;
            node.HeapIndex = i;
        }

        private void SiftDown(int i)
        {
            var node = _heap[i];
            int half = _size >> 1;
            while (i < half)
            {
                int left = (i << 1) + 1;
                int right = left + 1;
                int best = left;
                if (right < _size && Compare(_heap[right], _heap[left]) < 0)
                    best = right;
                if (Compare(node, _heap[best]) <= 0)
                    break;
                _heap[i] = _heap[best];
                _heap[i].HeapIndex = i;
                i = best;
            }
            _heap[i] = node;
            node.HeapIndex = i;
        }

        private static int Compare(PathNode a, PathNode b)
        {
            int cmp = a.FCost.CompareTo(b.FCost);
            if (cmp != 0) return cmp;
            return a.HCost.CompareTo(b.HCost);
        }
    }
}
