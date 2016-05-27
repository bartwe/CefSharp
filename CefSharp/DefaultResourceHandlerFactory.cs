// Copyright © 2010-2016 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;

namespace CefSharp {
    public class DefaultResourceHandlerFactory : IResourceHandlerFactory {
        readonly Dictionary<string, IResourceHandler> Handlers;

        public DefaultResourceHandlerFactory(IEqualityComparer<string> comparer = null) {
            Handlers = new Dictionary<string, IResourceHandler>(comparer ?? StringComparer.OrdinalIgnoreCase);
        }

        public virtual bool RegisterHandler(string url, IResourceHandler handler) {
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri)) {
                lock (Handlers) {
                    Handlers[uri.ToString()] = handler;
                }
                return true;
            }
            return false;
        }

        public virtual bool UnregisterHandler(string url) {
            lock (Handlers) {
                IResourceHandler handler;
                if (!Handlers.TryGetValue(url, out handler)) {
                    return false;
                }
                Handlers.Remove(url);
                return true;
            }
        }

        /// <summary>
        /// Are there any <see cref="ResourceHandler"/>'s registered?
        /// </summary>
        public bool HasHandlers {
            get {
                lock (Handlers) {
                    return Handlers.Count > 0;
                }
            }
        }

        /// <summary>
        /// Called before a resource is loaded. To specify a handler for the resource return a <see cref="ResourceHandler"/> object
        /// </summary>
        /// <param name="browserControl">The browser UI control</param>
        /// <param name="browser">the browser object</param>
        /// <param name="frame">the frame object</param>
        /// <param name="request">the request object - cannot be modified in this callback</param>
        /// <returns>To allow the resource to load normally return NULL otherwise return an instance of ResourceHandler with a valid stream</returns>
        public virtual IResourceHandler GetResourceHandler(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request) {
            try {
                IResourceHandler handler;
                lock (Handlers) {
                    Handlers.TryGetValue(request.Url, out handler);
                }

                return handler;
            }
            finally {
                request.Dispose();
            }
        }
    }
}
