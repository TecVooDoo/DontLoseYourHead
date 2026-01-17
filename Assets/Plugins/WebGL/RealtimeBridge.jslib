// RealtimeBridge.jslib
// JavaScript bridge for WebSocket realtime connections in WebGL builds
// Created: January 16, 2026
// Developer: TecVooDoo LLC
//
// Implements Supabase Realtime (Phoenix Channels protocol) for WebGL

mergeInto(LibraryManager.library, {

    // Active WebSocket connections
    _realtimeConnections: {},
    _nextConnectionId: 1,
    _realtimeGameObject: 'RealtimeClient',

    // Create a new WebSocket connection
    // Returns connection ID, or 0 on failure
    RealtimeConnect: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        var connectionId = this._nextConnectionId++;

        console.log('[RealtimeBridge] Connecting to: ' + url);

        try {
            var ws = new WebSocket(url);

            var connection = {
                id: connectionId,
                ws: ws,
                isConnected: false,
                messageRef: 0
            };

            this._realtimeConnections[connectionId] = connection;

            ws.onopen = function() {
                console.log('[RealtimeBridge] Connected: ' + connectionId);
                connection.isConnected = true;
                try {
                    SendMessage(this._realtimeGameObject, 'OnWebGLConnected', connectionId.toString());
                } catch (e) {
                    console.error('[RealtimeBridge] SendMessage error on connect: ' + e);
                }
            }.bind(this);

            ws.onmessage = function(event) {
                var data = event.data;
                // Format: connectionId|message
                var payload = connectionId + '|' + data;
                try {
                    SendMessage(this._realtimeGameObject, 'OnWebGLMessage', payload);
                } catch (e) {
                    console.error('[RealtimeBridge] SendMessage error on message: ' + e);
                }
            }.bind(this);

            ws.onerror = function(error) {
                console.error('[RealtimeBridge] Error on connection ' + connectionId + ': ' + error);
                try {
                    SendMessage(this._realtimeGameObject, 'OnWebGLError', connectionId + '|Connection error');
                } catch (e) {
                    console.error('[RealtimeBridge] SendMessage error on error: ' + e);
                }
            }.bind(this);

            ws.onclose = function(event) {
                console.log('[RealtimeBridge] Closed: ' + connectionId + ' (code: ' + event.code + ')');
                connection.isConnected = false;
                try {
                    SendMessage(this._realtimeGameObject, 'OnWebGLDisconnected', connectionId + '|' + event.code);
                } catch (e) {
                    console.error('[RealtimeBridge] SendMessage error on close: ' + e);
                }
            }.bind(this);

            return connectionId;
        } catch (e) {
            console.error('[RealtimeBridge] Failed to create WebSocket: ' + e);
            return 0;
        }
    },

    // Disconnect a WebSocket
    RealtimeDisconnect: function(connectionId) {
        var connection = this._realtimeConnections[connectionId];
        if (!connection) {
            console.warn('[RealtimeBridge] Connection not found: ' + connectionId);
            return;
        }

        console.log('[RealtimeBridge] Disconnecting: ' + connectionId);

        try {
            connection.ws.close(1000, 'Client disconnect');
        } catch (e) {
            console.warn('[RealtimeBridge] Error closing connection: ' + e);
        }

        delete this._realtimeConnections[connectionId];
    },

    // Send a message on a WebSocket
    // Returns 1 on success, 0 on failure
    RealtimeSend: function(connectionId, messagePtr) {
        var connection = this._realtimeConnections[connectionId];
        if (!connection) {
            console.warn('[RealtimeBridge] Connection not found for send: ' + connectionId);
            return 0;
        }

        if (!connection.isConnected || connection.ws.readyState !== WebSocket.OPEN) {
            console.warn('[RealtimeBridge] Connection not open for send: ' + connectionId);
            return 0;
        }

        var message = UTF8ToString(messagePtr);

        try {
            connection.ws.send(message);
            return 1;
        } catch (e) {
            console.error('[RealtimeBridge] Send error: ' + e);
            return 0;
        }
    },

    // Check if connection is open
    // Returns 1 if connected, 0 otherwise
    RealtimeIsConnected: function(connectionId) {
        var connection = this._realtimeConnections[connectionId];
        if (!connection) {
            return 0;
        }
        return connection.isConnected && connection.ws.readyState === WebSocket.OPEN ? 1 : 0;
    },

    // Get connection ready state
    // Returns WebSocket.readyState value, or -1 if not found
    RealtimeGetReadyState: function(connectionId) {
        var connection = this._realtimeConnections[connectionId];
        if (!connection) {
            return -1;
        }
        return connection.ws.readyState;
    },

    // Set the GameObject name that receives realtime callbacks
    SetRealtimeCallbackGameObject: function(gameObjectNamePtr) {
        this._realtimeGameObject = UTF8ToString(gameObjectNamePtr);
        console.log('[RealtimeBridge] Callback target set to: ' + this._realtimeGameObject);
    },

    // Phoenix Channels helpers

    // Build a Phoenix join message
    // Returns the message as a string
    PhoenixJoin: function(topicPtr, payloadPtr) {
        var topic = UTF8ToString(topicPtr);
        var payload = UTF8ToString(payloadPtr);

        // Phoenix message format: [join_ref, ref, topic, event, payload]
        var message = JSON.stringify([
            null,
            this._getNextRef(),
            topic,
            'phx_join',
            payload ? JSON.parse(payload) : {}
        ]);

        var bufferSize = lengthBytesUTF8(message) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(message, buffer, bufferSize);
        return buffer;
    },

    // Build a Phoenix leave message
    PhoenixLeave: function(topicPtr) {
        var topic = UTF8ToString(topicPtr);

        var message = JSON.stringify([
            null,
            this._getNextRef(),
            topic,
            'phx_leave',
            {}
        ]);

        var bufferSize = lengthBytesUTF8(message) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(message, buffer, bufferSize);
        return buffer;
    },

    // Build a Phoenix heartbeat message
    PhoenixHeartbeat: function() {
        var message = JSON.stringify([
            null,
            this._getNextRef(),
            'phoenix',
            'heartbeat',
            {}
        ]);

        var bufferSize = lengthBytesUTF8(message) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(message, buffer, bufferSize);
        return buffer;
    },

    // Build a Phoenix push message
    PhoenixPush: function(topicPtr, eventPtr, payloadPtr) {
        var topic = UTF8ToString(topicPtr);
        var event = UTF8ToString(eventPtr);
        var payload = UTF8ToString(payloadPtr);

        var message = JSON.stringify([
            null,
            this._getNextRef(),
            topic,
            event,
            payload ? JSON.parse(payload) : {}
        ]);

        var bufferSize = lengthBytesUTF8(message) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(message, buffer, bufferSize);
        return buffer;
    },

    // Parse a Phoenix message
    // Returns JSON object with topic, event, payload
    PhoenixParse: function(messagePtr) {
        var message = UTF8ToString(messagePtr);

        try {
            var parsed = JSON.parse(message);

            // Phoenix format: [join_ref, ref, topic, event, payload]
            if (Array.isArray(parsed) && parsed.length >= 5) {
                var result = JSON.stringify({
                    join_ref: parsed[0],
                    ref: parsed[1],
                    topic: parsed[2],
                    event: parsed[3],
                    payload: parsed[4]
                });

                var bufferSize = lengthBytesUTF8(result) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(result, buffer, bufferSize);
                return buffer;
            }
        } catch (e) {
            console.error('[RealtimeBridge] Parse error: ' + e);
        }

        // Return empty on failure
        var buffer = _malloc(1);
        stringToUTF8('', buffer, 1);
        return buffer;
    },

    // Internal: Get next message reference number
    _phoenixRef: 0,
    _getNextRef: function() {
        return (++this._phoenixRef).toString();
    },

    // Cleanup all connections
    RealtimeCleanup: function() {
        console.log('[RealtimeBridge] Cleaning up all connections');
        for (var id in this._realtimeConnections) {
            try {
                this._realtimeConnections[id].ws.close(1000, 'Cleanup');
            } catch (e) {
                // Ignore
            }
        }
        this._realtimeConnections = {};
    }
});
