/// <reference types="node" />
export declare class ChannelsClient {
    private _url;
    private _accountId;
    private _username;
    private _password;
    private _clientConfig;
    private _restClient;
    /**
     * Channel management client constructor.
     *
     * @param url Base url of SPV Channels service
     * @param accountId Id of the account being managed
     * @param username Username used to authenticate to the service
     * @param password Password used to authenticate to the service
     */
    constructor(url: string, accountId: string, username: string, password: string);
    /**
     * Lists all channels of the account.
     */
    listChannels(): Promise<Array<Channel>>;
    getChannel(channelId: string): Promise<Channel>;
    createChannel(publicRead: boolean, publicWrite: boolean, sequenced: boolean, retention: Retention): Promise<Channel>;
    amendChannel(channel: Channel, publicRead: boolean, publicWrite: boolean, locked: boolean): Promise<void>;
    deleteChannel(channel: Channel): Promise<void>;
    listTokens(channel: Channel): Promise<Array<AccessToken>>;
    getToken(channel: Channel, token: string): Promise<AccessToken>;
    generateToken(channel: Channel, description: string, canRead: boolean, canWrite: boolean): Promise<AccessToken>;
    revokeToken(channel: Channel, token: AccessToken): Promise<void>;
    getState(): string;
    static fromState(state: string): ChannelsClient;
    private getServiceUrl;
}
export declare class MessagingClient {
    private _url;
    private _channel;
    private _token;
    private _clientConfig;
    private _restClient;
    private _webSocket;
    private _webSocketOpen;
    constructor(url: string, channel: string, token: string);
    getMessagesHead(): Promise<number>;
    getMessages(onlyUnread?: boolean): Promise<Array<Message>>;
    writeMessage(contentType: string, payload: Buffer): Promise<Message>;
    markAsRead(message: Message, markOlder: boolean): Promise<void>;
    markAsUnread(message: Message, markOlder: boolean): Promise<void>;
    subscribeNotifications(onOpenCB: (channelId: string) => any, onMessageReceivedCB: (channelId: string, notification: string, received: Date) => any, onDisconnectCB: (channelId: string) => any): void;
    unsubscribeNotifications(): void;
    notificationsActive(): boolean;
    private getServiceUrl;
    private getWebSocketUrl;
}
export declare class Retention {
    min_age_days: number;
    max_age_days: number;
    auto_prune: boolean;
}
export declare class AccessToken {
    id: string;
    token: string;
    description: string;
    can_read: boolean;
    can_write: boolean;
}
export declare class Channel {
    id: string;
    href: string;
    public_read: boolean;
    public_write: boolean;
    locked: boolean;
    sequenced: boolean;
    head: number;
    retention: Retention;
    access_tokens: AccessToken[];
}
export declare class Message {
    sequence: number;
    received: Date;
    content_type: string;
    payload: Buffer;
}
