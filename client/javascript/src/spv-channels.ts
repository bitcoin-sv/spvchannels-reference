import axios, {AxiosRequestConfig, AxiosInstance} from '../node_modules/axios';
import {w3cwebsocket} from '../node_modules/websocket';
import * as libsodiumWrapper from '../node_modules/libsodium-wrappers'

/** @internal */
const SPV_CHANNELS_VERSION = "v1";

/**
 * Channels client used to create and manage account channels and access tokens for channels.
 */
export class ChannelsClient
{
    private _url:string;
    private _accountId:string;
    private _username:string;
    private _password:string;

    private _clientConfig: AxiosRequestConfig;
    private _restClient: AxiosInstance;

    /** 
     * Channel management client constructor.
     * 
     * @param url Base url of SPV Channels service
     * @param accountId Id of the account being managed
     * @param username Username used to authenticate to the service
     * @param password Password used to authenticate to the service
     */
    constructor(url: string, accountId: string, username: string, password: string)
    {
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
        this._restClient = axios.create(this._clientConfig);
    }
    
    /**
     * Lists all channels of the account.
     * 
     * @returns List of channels.
     */
    async listChannels(): Promise<Array<Channel>>
    {
        console.debug(`Calling listChannels to ${this.getServiceUrl()} using username ${this._username}`);

        let resp = await this._restClient.get<ChannelList>("channel/list");
        if (resp.status != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);

        return resp.data.channels;
    };

    /**
     * Get channel with selected id
     * 
     * @returns Selected channel details.
     */
    async getChannel(channelId: string): Promise<Channel> 
    { 
        console.debug(`Calling getChannel to ${this.getServiceUrl()} for channel with ${channelId} using username ${this._username}`);

        let resp = await this._restClient.get<Channel>(`channel/${channelId}`);

        if (resp.status != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);

        return resp.data;         
    };

    /**
     * Create new channel
     * 
     * @param publicRead If false only owner can read from channel.
     * @param publicWrite If false only owner can write to channel.
     * @param sequenced If true than all messages mus be marked read for given token before new message can be written with that token.
     * @param retention Channel retention options.
     * @returns Details of new channel.
     */
    async createChannel(publicRead: boolean, publicWrite: boolean, sequenced: boolean, retention: Retention): Promise<Channel>
    { 
        console.debug(`Calling createChannel to ${this.getServiceUrl()} using username ${this._username}`);
        
        let resp = await this._restClient.post<Channel>("channel", {
            public_read: publicRead,
            public_write: publicWrite,
            sequenced: sequenced,
            retention: retention
        });

        if (resp.status != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);

        return resp.data; 
    }; 

    /**
     * Amend certain properties of the channel.
     * 
     * @param publicRead If false only owner can read from channel.
     * @param publicWrite If false only owner can write to channel.
     * @param locked If true than now new messages can be posted to the channel.
     */
    async amendChannel(channel: Channel, publicRead: boolean, publicWrite: boolean, locked: boolean)
    { 
        console.debug(`Calling amendChannel to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);
        
        let resp = await this._restClient.post<ChannelAmend>(`channel/${channel.id}`, {
            public_read: publicRead,
            public_write: publicWrite,
            locked: locked
        });

        if (resp.status != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);     
    };

    /**
     * Delete selected channel.
     * 
     * @param channel Channel to delete.
     */
    async deleteChannel(channel: Channel)
    {
        console.debug(`Calling deleteChannel to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);
        
        let resp = await this._restClient.delete(`channel/${channel.id}`);

        if (resp.status != 204)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);   
    }

    /**
     * List tokens for selected channel.
     * 
     * @param channel Selected channel.
     * @returns List of access tokens that belong to selected channel.
     */
    async listTokens(channel: Channel): Promise<Array<AccessToken>>
    {
        console.debug(`Calling listTokens to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);

        let resp = await this._restClient.get<Array<AccessToken>>(`channel/${channel.id}/api-token`);

        if (resp.status != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`); 

        return resp.data; 
    };

    /**
     * Get details of token issued for selected channel by token string.
     * 
     * @param channel Selected channel.
     * @param token Token string.
     * @returns Token details.
     */
    async getToken(channel: Channel, token: string): Promise<AccessToken|null>
    {
        console.debug(`Calling listTokens to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);
        
        let resp = await this._restClient.get<Array<AccessToken>>(`channel/${channel.id}/api-token?token=${token}`);
        
        if (resp.status != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`); 

        if (resp.data.length === 1)
            return resp.data[0];

        return null;
    }

    /**
     * Generate new token for the channel
     * 
     * @param channel Selected channel.
     * @param description Description of the token (e.g. token owner).
     * @param canRead If true than this token can be used to read from the channel.
     * @param canWrite If true than this token can be used to write to the channel.
     * @returns Generated token details.
     */
    async generateToken(channel: Channel, description: string, canRead: boolean, canWrite: boolean): Promise<AccessToken>
    { 
        console.debug(`Calling generateToken to ${this.getServiceUrl()} for channel with id ${channel.id} using username ${this._username}`);
        
        let resp = await this._restClient.post<AccessToken>(`channel/${channel.id}/api-token`, {
            description: description,
            can_read: canRead,
            can_write: canWrite
        });

        if (resp.status != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`); 
        
        return resp.data;         
    };

    /**
     * Revoke selected token.
     * 
     * @param channel Selected channel.
     * @param token Token to revoke.
     */
    async revokeToken(channel: Channel, token: AccessToken)
    {
        console.debug(`Calling revokeToken to ${this.getServiceUrl()} for token with id ${token.id} using username ${this._username}`);
        
        let resp = await this._restClient.delete(`channel/${channel.id}/api-token/${token.id}`);

        if (resp.status != 204)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);
    }

    /**
     * Serialize channels client object to JSON.
     * 
     * @returns Channels client object serialized to JSON.
     */
    public getState(): string 
    {
        return JSON.stringify({ 
            url: this._url,
            accountId: this._accountId,
            username: this._username,
            password: this._password
        });
    }

    /**
     * Create channels client instance from JSON state.
     * 
     * @param state Channels client object JSON state.
     * @returns Channels client object.
     */
    public static fromState(state: string): ChannelsClient
    {   
        let stateObj = JSON.parse(state);
        return new ChannelsClient(stateObj["url"], stateObj["accountId"], stateObj["username"], stateObj["password"]);
    }

    private getServiceUrl(): string
    {
        return this._url.concat("/", SPV_CHANNELS_VERSION, "/account/", this._accountId);
    }
}

/**
 * Messaging client used to post and read messages from channel using access token.
 */
export class MessagingClient
{
    private _url:string;
    private _channel:string;
    private _token:string;

    private _clientConfig: AxiosRequestConfig;
    private _restClient: AxiosInstance;

    private _webSocket: w3cwebsocket;
    private _webSocketOpen: boolean = false;
    private _encryption : IEncryption;

    /** 
     * Messaging client constructor.
     * 
     * @param url Base url of SPV Channels service.
     * @param channel Id of the channel.
     * @param token Token used to authenticate to the service.
     * @param encryption Optional encryption instance to use for secure communication.
     */
    constructor(url: string, channel: string, token: string, encryption: IEncryption = null) 
    {
        this._url = url;
        this._channel = channel;
        this._token = token;

        this._clientConfig = {
            baseURL: this.getServiceUrl(),
            headers: {
                'Authorization': `Bearer ${this._token}`
            }
        };
        this._restClient = axios.create(this._clientConfig);
        this._encryption = encryption ?? new NoEncryption();
    }

    /** 
     * Test Channel for new messages.
     * 
     * @returns Sequence of the most recent message in channel.
     */
    async getMessagesHead(): Promise<number>
    {        
        console.debug(`Calling getMessagesHead to ${this.getServiceUrl()} using token ${this._token}`);
        
        let resp = await this._restClient.head("");

        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`); 
        
        return Number.parseInt(resp.headers.etag.toString());    
    }

    
    /** 
     * Get messages from the channel.
     * 
     * @param onlyUnread Return only unread messages.
     * @returns List of messages.
     */
    async getMessages(onlyUnread: boolean = true): Promise<Array<Message>>
    { 
        console.debug(`Calling getMessages to ${this.getServiceUrl()} using token ${this._token}`);
        
        let resp = await this._restClient.get<Array<Message>>(`?unread=${onlyUnread.valueOf().toString()}`);
        
        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);             

        // decode buffer from utf-8 encoded base64   
        resp.data.forEach(msg => {
            let base64Decoded = Buffer.from(msg.payload.toString("utf8"), "base64"); 
            msg.payload = this._encryption.decrypt(base64Decoded);            
        });

        return resp.data;
    }

    /** 
     * Write new message to the channel.
     * 
     * @param contentType Content type of the payload.
     * @param payload Message payload.
     * @returns Posted message.
     */
    async writeMessage(contentType: string, payload: Buffer) : Promise<Message>   
    { 
        console.debug(`Calling writeMessage to ${this.getServiceUrl()} for message of type ${contentType} using token ${this._token}`);      
                    
        let config : AxiosRequestConfig =  {
            headers: {
                'content-type': contentType
            }
        };
        let resp = await this._restClient.post<Message>("", this._encryption.encrypt(payload), config);

        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);        

        // decode buffer from utf-8 encoded base64   
        resp.data.payload = Buffer.from(resp.data.payload.toString("utf8"), "base64");

        return resp.data;
    }

    /** 
     * Mark message as read
     * 
     * @param message Message to mark.
     * @param markOlder Also mark older messages.
     */
    async markAsRead (message:Message, markOlder: boolean)
    {
        console.debug(`Calling markMessage to ${this.getServiceUrl()} for sequence ${message.sequence} using token ${this._token}`);      

        let resp = await this._restClient.post<Message>(
            `${message.sequence.toString()}?older=${(markOlder ? "true" : "false")}`, 
            { read : true });
        
        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);     
    }

    /** 
     * Mark message as unread
     * 
     * @param message Message to mark.
     * @param markOlder Also mark older messages.
     */
    async markAsUnread (message:Message, markOlder: boolean)
    {
        console.debug(`Calling markMessage to ${this.getServiceUrl()} for sequence ${message.sequence} using token ${this._token}`);      

        let resp = await this._restClient.post<Message>(
            `${message.sequence.toString()}?older=${(markOlder ? "true" : "false")}`, 
            { read : false });
        
        if (resp.status.valueOf() != 200)
            throw new Error(`Error calling SPV channels API. Invalid status code received: ${resp.status.toString()}`);    
    }

    /** 
     * Subscribe to push notifications over websockets.
     * 
     * @param onOpenCB Notifications websocket open callback function.
     * @param onMessageReceivedCB New notification received callback function.
     * @param onDisconnectCB Notifications websocket closed callback function.
     */
    public subscribeNotifications(
        onOpenCB: (
            channelId: string) => any,
        onMessageReceivedCB: (
            channelId: string, 
            notification: string, 
            received: Date) => any,
        onDisconnectCB: (
            channelId: string) => any) 
    {
        console.debug(`Subscribing to notifications to ${this.getWebSocketUrl()} using token ${this._token}`);  

        this._webSocket = new w3cwebsocket(
            this.getWebSocketUrl().concat("/notify?token=", this._token));

        this._webSocket.onopen = () => {
            console.debug(`Notifications web socket open for channel ${this._channel}.`);
            this._webSocketOpen = true;
            if (onOpenCB)
            {
                onOpenCB(
                    this._channel
                );
            }                    
        };

        this._webSocket.onmessage = (e) => {
            console.debug(`Notifications message received: ${e.data}`);
            let notification: Notification = JSON.parse(e.data);
            if (onMessageReceivedCB)
            {
                onMessageReceivedCB(
                    notification.channel_id, 
                    notification.notification, 
                    notification.received);
            }
        };

        this._webSocket.onclose = (e) => {
            console.debug(`Notifications web socket disconnected for channel ${this._channel}.`);
            this._webSocketOpen = false;
            if (onDisconnectCB)
            {
                onDisconnectCB(
                    this._channel
                )
            }
        };
    }

    /** 
     * Unsubscribe from push notifications over websockets.
     * 
     */
    public unsubscribeNotifications() : void
    {
        if (this._webSocket && this._webSocketOpen)
        {
            console.debug(`Unsubscribing from notifications.`);  
            this._webSocket.close();
        }
    }

    /** 
     * Check that notifications are active.
     * 
     * @returns True if notifications wesocket is open.
     */
    public notificationsActive() : boolean
    {
        return this._webSocketOpen;
    }

    /**
     * Serialize messaging client object to JSON.
     * 
     * @returns Channels client object serialized to JSON.
     */
    public getState(): string 
    {
        return JSON.stringify({ 
            url: this._url,
            channel: this._channel,
            token: this._token,
            encryption: this._encryption.exportKeys() 
        });
    }

    /**
     * Create messaging client instance from JSON state. 
     * 
     * Note: This will not restore push notifications.
     * 
     * @param state Messaging client object JSON state.
     * @returns Messaging client object.
     */
    public static async fromState(state: string): Promise<MessagingClient>
    {   
        let stateObj = JSON.parse(state);
        let encryption:IEncryption = null;        
        if (stateObj["encryption"])
        {
            encryption = await EncryptionFactory.ImportKeys(stateObj["encryption"])
        }            
        return new MessagingClient(stateObj["url"], stateObj["channel"], stateObj["token"], encryption);
    }

    private getServiceUrl(): string
    {
        return this._url.concat("/", SPV_CHANNELS_VERSION, "/channel/", this._channel);
    }

    private getWebSocketUrl(): string
    {
        return this.getServiceUrl().replace("http", "ws");
    }
}

/**
 * Channels retention properties.
 */
export class Retention
{
    /**
     * Minimum age of message in days.
     */
    min_age_days: number;

    /**
     * Maximum age of message in days - should be equal or higher than min_age_days.
     */
    max_age_days: number;

    /**
     * Should channel messages be automatically pruned.
     */
    auto_prune: boolean;
}

/**
 * Access token used for messaging.
 */
export class AccessToken
{
    /**
     * Token id.
     */
    id: string; 

    /**
     * Token string that is used in authorization header.
     */
    token: string;

    /**
     * Token description. 
     */
    description: string;

    /**
     * Can this token be used for reading messages.
     */
    can_read: boolean;

    /**
     * Can this token be used for writing messages.
     */
    can_write: boolean;
}

/**
 * Channel details.
 */
export class Channel
{
    /**
     * Channel id.
     */
    id: string;

    /**
     * Channel reference address.
     */
    href: string;    

    /**
     * Can everyone read from this channel or only the ownere of the channel.
     */
    public_read: boolean;

    /**
     * Can everyone write to this channel or only the ownere of the channel.
     */
    public_write: boolean;

    /**
     * Is channel locked for writing.
     */
    locked: boolean;

    /**
     * Is channel sequenced - all messages must be marked as read before new messages can be written to the channel for selected token.
     */
    sequenced: boolean;

    /**
     * Sequence of the latest message in the channel.
     */
    head: number;

    /**
     * Channel retention properties.
     */
    retention: Retention;

    /**
     * List of access tokens issued for this channel.
     */
    access_tokens: AccessToken[];
}

/**
 * Message details
 */
export class Message
{
    /**
     * Sequence of this message
     */
    sequence: number;

    /**
     * Date received.
     */
    received: Date;

    /**
     * Content type.
     */
    content_type: string;

    /**
     * Message content.
     */
    payload: Buffer;
}

/**
 * Encryption interface used for secure communication
 */
export interface IEncryption
{
    /**
     * Encrypt message.
     * @param payload Message to be encrypted.
     * @returns Encrypted message.
     */
    encrypt(payload:Buffer) : Buffer;

    /**
     * Decrypt message.
     * @param encrypted Encrypted message to be decrypted.
     * @returns Decrypted message.
     */    
    decrypt(encrypted:Buffer) : Buffer;

    /**
     * Export decrypted enryption key pair.
     * @returns Decrypted enryption key pair.
     */ 
    exportKeys() : string;
  
    /**
     * Get key used to encrypt messages.
     * @returns Key used to encrypt messages.
     */ 
    getEncryptionKey() : string
}

/**
 * Factory class for encryption interface using libsodium.
 */
export class EncryptionFactory
{
    private static sodium :  libsodiumWrapper.ISodium;

    private static async ensureSodium() : Promise<libsodiumWrapper.ISodium> 
    {
        if (EncryptionFactory.sodium == null)
        {
            await libsodiumWrapper.ready;
            EncryptionFactory.sodium =  libsodiumWrapper;
        }
        return EncryptionFactory.sodium;    
    }

    /**
     * Generate new key pair for encryption.
     * @returns Encryption interface using new encryption key pair.
     */ 
    static async CreateNewKeys() : Promise<IEncryption>
    {
        const sodium=await EncryptionFactory.ensureSodium();
        return new Encryption(sodium);
    }

    /**
     * Import existing key pair.
     * @param serializedKeys Previously exported key pair.
     * @returns Encryption interface using given key pair.
     */ 
    static async ImportKeys(serializedKeys: string) : Promise<IEncryption>
    {
        const sodium=await EncryptionFactory.ensureSodium();
        return new Encryption(sodium, serializedKeys);
    }

    /**
     * Create interface used for encryption.
     * @param encryptionKey Encryption part of key pair.
     * @returns Encryption interface using given key.
     */ 
    static async CreateFromEncryptionKey(encryptionKey:string)
    {
        const sodium=await EncryptionFactory.ensureSodium();
        return new Encryption(sodium, null, encryptionKey);
    }
}

/**
 * No encryption interface implementation.
 */
class NoEncryption implements IEncryption
{
    getEncryptionKey(): string {
        return null;
    }
    encrypt(payload: Buffer): Buffer {
        return payload;
    }
    decrypt(encrypted: Buffer): Buffer {
        return encrypted;
    }
    exportKeys(): string {
        return null;
    }   
}

/**
 * libsodium encryption interface implementation.
 */
class Encryption implements IEncryption
{
    private sodium : libsodiumWrapper.ISodium;
    public encryptionKey : Uint8Array = null;
    public keyPair : libsodiumWrapper.KeyPair = null;

    private static parseEncryptionKey(encryptionKey : string) : Uint8Array
    {
        const parts = encryptionKey.split(' ');
        if (parts.length != 3)
        {
            throw new Error("Can not parse decryption key - invalid number of parameters")
        }
        if (parts[0] != "libsodium" || parts[1] != "sealed_box")
        {
            throw new Error("Can not parse decryption key - unsupported method")
        }  
        const key = Buffer.from(parts[2], "base64"); 

        return key;
    }

    constructor(sodium:libsodiumWrapper.ISodium, serializedKeys: string = null, encryptionKey :string = null) 
    {
        this.sodium = sodium;      
        if (serializedKeys == null)
        {
            if (encryptionKey == null)
            {
                this.keyPair = sodium.crypto_box_keypair(); 
            }
            else
            {
                this.encryptionKey = Encryption.parseEncryptionKey(encryptionKey);
            }
        }
        else
        {
            this.initKeys(serializedKeys)
        }
    }

    getEncryptionKey(): string 
    {
        var key : Uint8Array;
        
        if (this.keyPair != null)
        {
           key = this.keyPair.publicKey;
        }
        else
        {
            if (this.encryptionKey == null)
            {
                throw new Error("No encryption key specified")
            }
        }
        return "libsodium sealed_box " + Buffer.from(key).toString('base64');
    }

    exportKeys() : string
    {  
        if (this.keyPair == null)
        {
            throw new Error("Can not export keyPair. We only have encryption key.");
        }
        return JSON.stringify({
            "keyType" : this.keyPair.keyType,
            "publicKey" :  Array.from(this.keyPair.publicKey),
            "privateKey"  :  Array.from(this.keyPair.privateKey),
        });
    }
  
    initKeys(serializedKeys:string)
    {
        let keyPairFromJson : libsodiumWrapper.KeyPair =  JSON.parse(serializedKeys);
        keyPairFromJson.publicKey = Uint8Array.from(keyPairFromJson.publicKey);
        keyPairFromJson.privateKey =  Uint8Array.from(keyPairFromJson.privateKey);
        this.keyPair = keyPairFromJson;
    }
    
    encrypt(payload: Buffer): Buffer 
    {
        return this.sodium.crypto_box_seal(payload, this.encryptionKey ??  this.keyPair.publicKey);
    }

    decrypt(encrypted: Buffer): Buffer 
    {
        if (this.keyPair == null)
        {
            throw new Error("Can not decrypt. We only have encryption key.");
        }
        const  decryptedBytes = this.sodium.crypto_box_seal_open(encrypted, this.keyPair.publicKey, this.keyPair.privateKey);

        return Buffer.from(decryptedBytes);
    }
}

/** @internal */
class Notification
{
    channel_id: string;
    notification: string;
    received: Date;
}

/** @internal */
class ChannelList
{
    channels: Channel[];
}

/** @internal */
class ChannelAmend
{
    public_read: boolean;
    public_write: boolean;
    locked: boolean; 
}