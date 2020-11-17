"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Message = exports.Channel = exports.AccessToken = exports.Retention = exports.MessagingClient = exports.ChannelsClient = void 0;
const axios_1 = require("../node_modules/axios");
const websocket_1 = require("../node_modules/websocket");
/** @internal */
const spv_CHANNELS_VERSION = "v1";
class ChannelsClient {
    /**
     * Channel management client constructor.
     *
     * @param url Base url of SPV Channels service
     * @param accountId Id of the account being managed
     * @param username Username used to authenticate to the service
     * @param password Password used to authenticate to the service
     */
    constructor(url, accountId, username, password) {
        this._url = url;
        this._accountId = accountId;
        this._username = username;
        this._password = password;
        this._clientConfig = {
            baseURL: this.getServiceUrl(),
            auth: {
                username: this._username,
                password: this._password
            }
        };
        this._restClient = axios_1.default.create(this._clientConfig);
    }
    /**
     * Lists all channels of the account.
     */
    async listChannels() {
        console.debug(`Calling listChannels to ${this.getServiceUrl()} using username ${this._username}`);
        let resp = await this._restClient.get("channel/list");
        if (resp.status != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
        return resp.data.channels;
    }
    ;
    async getChannel(channelId) {
        console.debug(`Calling getChannel to ${this.getServiceUrl()} for channel with ${channelId} using username ${this._username}`);
        let resp = await this._restClient.get(`channel/${channelId}`);
        if (resp.status != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
        return resp.data;
    }
    ;
    async createChannel(publicRead, publicWrite, sequenced, retention) {
        console.debug(`Calling createChannel to ${this.getServiceUrl()} using username ${this._username}`);
        let resp = await this._restClient.post("channel", {
            public_read: publicRead,
            public_write: publicWrite,
            sequenced: sequenced,
            retention: retention
        });
        if (resp.status != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
        return resp.data;
    }
    ;
    async amendChannel(channel, publicRead, publicWrite, locked) {
        console.debug(`Calling amendChannel to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);
        let resp = await this._restClient.post(`channel/${channel.id}`, {
            public_read: publicRead,
            public_write: publicWrite,
            locked: locked
        });
        if (resp.status != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
    }
    ;
    async deleteChannel(channel) {
        console.debug(`Calling deleteChannel to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);
        let resp = await this._restClient.delete(`channel/${channel.id}`);
        if (resp.status != 204)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
    }
    async listTokens(channel) {
        console.debug(`Calling listTokens to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);
        let resp = await this._restClient.get(`channel/${channel.id}/api-token`);
        if (resp.status != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
        return resp.data;
    }
    ;
    async getToken(channel, token) {
        console.debug(`Calling listTokens to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);
        let resp = await this._restClient.get(`channel/${channel.id}/api-token?token=${token}`);
        if (resp.status != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
        if (resp.data.length = 1)
            return resp.data[0];
        return null;
    }
    async generateToken(channel, description, canRead, canWrite) {
        console.debug(`Calling generateToken to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);
        let resp = await this._restClient.post(`channel/${channel.id}/api-token`, {
            description: description,
            can_read: canRead,
            can_write: canWrite
        });
        if (resp.status != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
        return resp.data;
    }
    ;
    async revokeToken(channel, token) {
        console.debug(`Calling revokeToken to ${this.getServiceUrl()} for token with id ${token.id} using username ${this._username}`);
        let resp = await this._restClient.delete(`channel/${channel.id}/api-token/${token.id}`);
        if (resp.status != 204)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
    }
    getState() {
        let stateObj = {
            url: this._url,
            accountId: this._accountId,
            username: this._username,
            password: this._password
        };
        return JSON.stringify(stateObj);
    }
    static fromState(state) {
        let stateObj = JSON.parse(state);
        return new ChannelsClient(stateObj["url"], stateObj["accountId"], stateObj["username"], stateObj["password"]);
    }
    getServiceUrl() {
        return this._url.concat("/", spv_CHANNELS_VERSION, "/account/", this._accountId);
    }
}
exports.ChannelsClient = ChannelsClient;
class MessagingClient {
    constructor(url, channel, token) {
        this._webSocketOpen = false;
        this._url = url;
        this._channel = channel;
        this._token = token;
        this._clientConfig = {
            baseURL: this.getServiceUrl(),
            headers: {
                'Authorization': `Bearer ${this._token}`
            }
        };
        this._restClient = axios_1.default.create(this._clientConfig);
    }
    async getMessagesHead() {
        console.debug(`Calling getMessagesHead to ${this.getServiceUrl()} using token ${this._token}`);
        let resp = await this._restClient.head("");
        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
        return Number.parseInt(resp.headers.etag.toString());
    }
    async getMessages(onlyUnread = true) {
        console.debug(`Calling getMessages to ${this.getServiceUrl()} using token ${this._token}`);
        let resp = await this._restClient.get(`?unread=${onlyUnread.valueOf().toString()}`);
        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
        // decode buffer from utf-8 encoded base64   
        resp.data.forEach(msg => {
            msg.payload = Buffer.from(msg.payload.toString("utf8"), "base64");
        });
        return resp.data;
    }
    async writeMessage(contentType, payload) {
        console.debug(`Calling writeMessage to ${this.getServiceUrl()} for message of type ${contentType} using token ${this._token}`);
        let config = {
            headers: {
                'content-type': contentType
            }
        };
        let resp = await this._restClient.post("", payload, config);
        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
        // decode buffer from utf-8 encoded base64   
        resp.data.payload = Buffer.from(resp.data.payload.toString("utf8"), "base64");
        return resp.data;
    }
    async markAsRead(message, markOlder) {
        console.debug(`Calling markMessage to ${this.getServiceUrl()} for sequence ${message.sequence} using token ${this._token}`);
        let resp = await this._restClient.post(`${message.sequence.toString()}?older=${(markOlder ? "true" : "false")}`, { read: true });
        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
    }
    async markAsUnread(message, markOlder) {
        console.debug(`Calling markMessage to ${this.getServiceUrl()} for sequence ${message.sequence} using token ${this._token}`);
        let resp = await this._restClient.post(`${message.sequence.toString()}?older=${(markOlder ? "true" : "false")}`, { read: false });
        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV Channels API. Invalid status code received: ${resp.status.toString()}`);
    }
    subscribeNotifications(onOpenCB, onMessageReceivedCB, onDisconnectCB) {
        console.debug(`Subscribing to notifications to ${this.getWebSocketUrl()} using token ${this._token}`);
        this._webSocket = new websocket_1.w3cwebsocket(this.getWebSocketUrl().concat("/notify?token=", this._token));
        this._webSocket.onopen = () => {
            console.debug(`Notifications web socket open for channel ${this._channel}.`);
            this._webSocketOpen = true;
            if (onOpenCB) {
                onOpenCB(this._channel);
            }
        };
        this._webSocket.onmessage = (e) => {
            console.debug(`Notifications message received: ${e.data}`);
            let notification = JSON.parse(e.data);
            if (onMessageReceivedCB) {
                onMessageReceivedCB(notification.channel_id, notification.notification, notification.received);
            }
        };
        this._webSocket.onclose = (e) => {
            console.debug(`Notifications web socket disconnected for channel ${this._channel}.`);
            this._webSocketOpen = false;
            if (onDisconnectCB) {
                onDisconnectCB(this._channel);
            }
        };
    }
    unsubscribeNotifications() {
        if (this._webSocket && this._webSocketOpen) {
            console.debug(`Unsubscribing from notifications.`);
            this._webSocket.close();
        }
    }
    notificationsActive() {
        return this._webSocketOpen;
    }
    getServiceUrl() {
        return this._url.concat("/", spv_CHANNELS_VERSION, "/channel/", this._channel);
    }
    getWebSocketUrl() {
        return this.getServiceUrl().replace("http", "ws");
    }
}
exports.MessagingClient = MessagingClient;
class Retention {
}
exports.Retention = Retention;
class AccessToken {
}
exports.AccessToken = AccessToken;
class Channel {
}
exports.Channel = Channel;
class Message {
}
exports.Message = Message;
/** @internal */
class Notification {
}
/** @internal */
class ChannelList {
}
/** @internal */
class ChannelAmend {
}
