// Copyright © 2010-2016 The CefSharp Project. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CefSharp.Internals
{
    /// <summary>
    /// Class to store TaskCompletionSources indexed by a unique id.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the tasks held.</typeparam>
    public sealed class PendingTaskRepository
    {
        private readonly Dictionary<long, TaskCompletionSource<JavascriptResponse>> pendingTasks =
            new Dictionary<long, TaskCompletionSource<JavascriptResponse>>();

        private readonly Dictionary<long, JavascriptResponseReceiver> pendingReceivers =
            new Dictionary<long, JavascriptResponseReceiver>();
        
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