// Copyright © 2010-2016 The CefSharp Project. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CefSharp.Internals
{
    public class Int64Map<TValue> : IEnumerable<KeyValuePair<Int64, TValue>> {
        int[] _buckets;
        Entry[] _entries;
        int _count;
        int _version;
        int _freeList;
        int _freeCount;

        public int Count { get { return _count - _freeCount; } }

        public TValue this[Int64 key] {
            get {
                var entry = FindEntry(key);
                if (entry >= 0)
                    return _entries[entry].Value;
                throw new KeyNotFoundException();
            }
            set { Insert(key, value, false); }
        }


        public Int64Map()
            : this(0) { }

        public Int64Map(int capacity) {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException();
            if (capacity > 0)
                Initialize(capacity);
        }

        public void Add(Int64 key, TValue value) {
            Insert(key, value, true);
        }

        public void Clear() {
            if (_count <= 0)
                return;
            for (var index = 0; index < _buckets.Length; ++index)
                _buckets[index] = -1;
            Array.Clear(_entries, 0, _count);
            _freeList = -1;
            _count = 0;
            _freeCount = 0;
            _version = _version + 1;
        }

        public bool ContainsKey(Int64 key) {
            return FindEntry(key) >= 0;
        }

        int FindEntry(Int64 key) {
            if (_buckets != null) {
                var num = (int)(key & int.MaxValue);
                for (var index = _buckets[num % _buckets.Length]; index >= 0; index = _entries[index].Next) {
                    if (_entries[index].HashCode == num && (_entries[index].Key == key))
                        return index;
                }
            }
            return -1;
        }

        void Initialize(int capacity) {
            capacity = capacity | 1;
            if (capacity < 131)
                capacity = 131;
            _buckets = new int[capacity];
            for (var index = 0; index < _buckets.Length; ++index)
                _buckets[index] = -1;
            _entries = new Entry[capacity];
            _freeList = -1;
        }

        void Insert(Int64 key, TValue value, bool add) {
            if (_buckets == null)
                Initialize(0);
            var num1 = (int)(key & int.MaxValue);
            var index1 = num1 % _buckets.Length;
            var num2 = 0;
            for (var index2 = _buckets[index1]; index2 >= 0; index2 = _entries[index2].Next) {
                if (_entries[index2].HashCode == num1 && (_entries[index2].Key == key)) {
                    if (add)
                        throw new ArgumentException();
                    _entries[index2].Value = value;
                    _version = _version + 1;
                    return;
                }
                ++num2;
            }
            int index3;
            if (_freeCount > 0) {
                index3 = _freeList;
                _freeList = _entries[index3].Next;
                _freeCount = _freeCount - 1;
            }
            else {
                if (_count == _entries.Length) {
                    Resize();
                    index1 = num1 % _buckets.Length;
                }
                index3 = _count;
                _count = _count + 1;
            }
            _entries[index3].HashCode = num1;
            _entries[index3].Next = _buckets[index1];
            _entries[index3].Key = key;
            _entries[index3].Value = value;
            _buckets[index1] = index3;
            _version = _version + 1;
            if (num2 <= 100)
                return;
            Resize(_entries.Length + _entries.Length / 4 + 1);
        }

        void Resize() {
            Resize(_count * 2 + 1);
        }

        void Resize(int newSize) {
            var numArray = new int[newSize];
            for (var index = 0; index < numArray.Length; ++index)
                numArray[index] = -1;
            var entryArray = new Entry[newSize];
            Array.Copy(_entries, 0, entryArray, 0, _count);
            for (var index1 = 0; index1 < _count; ++index1) {
                if (entryArray[index1].HashCode >= 0) {
                    var index2 = entryArray[index1].HashCode % newSize;
                    entryArray[index1].Next = numArray[index2];
                    numArray[index2] = index1;
                }
            }
            _buckets = numArray;
            _entries = entryArray;
        }

        public bool Remove(Int64 key) {
            if (_buckets != null) {
                var num = (int)(key & int.MaxValue);
                var index1 = num % _buckets.Length;
                var index2 = -1;
                for (var index3 = _buckets[index1]; index3 >= 0; index3 = _entries[index3].Next) {
                    if (_entries[index3].HashCode == num && (_entries[index3].Key == key)) {
                        if (index2 < 0)
                            _buckets[index1] = _entries[index3].Next;
                        else
                            _entries[index2].Next = _entries[index3].Next;
                        _entries[index3].HashCode = -1;
                        _entries[index3].Next = _freeList;
                        _entries[index3].Key = default(Int64);
                        _entries[index3].Value = default(TValue);
                        _freeList = index3;
                        _freeCount = _freeCount + 1;
                        _version = _version + 1;
                        return true;
                    }
                    index2 = index3;
                }
            }
            return false;
        }

        public bool TryGetValue(Int64 key, out TValue value) {
            var entry = FindEntry(key);
            if (entry >= 0) {
                value = _entries[entry].Value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        internal TValue GetValueOrDefault(Int64 key) {
            var entry = FindEntry(key);
            if (entry >= 0)
                return _entries[entry].Value;
            return default(TValue);
        }


        struct Entry {
            public int HashCode;
            public int Next;
            public Int64 Key;
            public TValue Value;
        }

        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<Int64, TValue>> {
            readonly Int64Map<TValue> _parent;
            readonly int _version;
            int _index;
            KeyValuePair<Int64, TValue> _current;
            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            public void Reset() {
                throw new NotImplementedException();
            }

            object IEnumerator.Current { get { return Current; } }

            public KeyValuePair<Int64, TValue> Current { get { return _current; } }

            internal Enumerator(Int64Map<TValue> parent) {
                _parent = parent;
                _version = parent._version;
                _index = 0;
                _current = new KeyValuePair<Int64, TValue>();
            }

            public bool MoveNext() {
                if (_version != _parent._version)
                    throw new InvalidOperationException();
                for (; (uint)_index < (uint)_parent._count; _index = _index + 1) {
                    if (_parent._entries[_index].HashCode >= 0) {
                        _current = new KeyValuePair<Int64, TValue>(_parent._entries[_index].Key, _parent._entries[_index].Value);
                        _index = _index + 1;
                        return true;
                    }
                }
                _index = _parent._count + 1;
                _current = new KeyValuePair<Int64, TValue>();
                return false;
            }

            public void Dispose() { }
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }

        IEnumerator<KeyValuePair<Int64, TValue>> IEnumerable<KeyValuePair<Int64, TValue>>.GetEnumerator() {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class to store TaskCompletionSources indexed by a unique id.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the tasks held.</typeparam>
    public sealed class PendingTaskRepository
    {
        private readonly Int64Map<TaskCompletionSource<JavascriptResponse>> pendingTasks =
            new Int64Map<TaskCompletionSource<JavascriptResponse>>();

        private readonly Int64Map<JavascriptResponseReceiver> pendingReceivers =
            new Int64Map<JavascriptResponseReceiver>();
        
        //should only be accessed by Interlocked.Increment
        private long lastId;

        /// <summary>
        /// Creates a new pending task with a timeout.
        /// </summary>
        /// <param name="timeout">The maximum running time of the task.</param>
        /// <returns>The unique id of the newly created pending task and the newly created <see cref="TaskCompletionSource{TResult}"/>.</returns>
        public KeyValuePair<long, TaskCompletionSource<JavascriptResponse>> CreatePendingTask(TimeSpan? timeout = null)
        {
            var taskCompletionSource = new TaskCompletionSource<JavascriptResponse>();

            var id = Interlocked.Increment(ref lastId);
            id = id << 1;
            lock (pendingTasks) {
                pendingTasks[id] = taskCompletionSource;
            }

            if (timeout.HasValue)
            {
                taskCompletionSource = taskCompletionSource.WithTimeout(timeout.Value, () => RemovePendingTask(id));
            }

            return new KeyValuePair<long, TaskCompletionSource<JavascriptResponse>>(id, taskCompletionSource);
        }

        /// <summary>
        /// Gets and removed pending task by id.
        /// </summary>
        /// <param name="id">Unique id of the pending task.</param>
        /// <returns>
        /// The <see cref="TaskCompletionSource{TResult}"/> associated with the given id.
        /// </returns>
        public TaskCompletionSource<JavascriptResponse> RemovePendingTask(long id)
        {
            TaskCompletionSource<JavascriptResponse> result;
            lock (pendingTasks) {
                if (pendingTasks.TryGetValue(id, out result))
                    pendingTasks.Remove(id);
            }
            return result;
        }


        public void RegisterReceiver(JavascriptResponseReceiver receiver)
        {
            var id = Interlocked.Increment(ref lastId);
            id = (id << 1) | 1;
            lock (pendingReceivers) {
                pendingReceivers[id] = receiver;
            }

            receiver.Key = id;
        }

        public JavascriptResponseReceiver RemoveReceiver(long id)
        {
            JavascriptResponseReceiver result;
            lock (pendingReceivers) {
                if (pendingReceivers.TryGetValue(id, out result))
                    pendingReceivers.Remove(id);
            }
            return result;
        }

        public bool IsTask(long id)
        {
            return (id & 1) == 0;
        }
    }
}