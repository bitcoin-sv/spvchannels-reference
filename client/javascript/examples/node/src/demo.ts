import {ChannelsClient, MessagingClient, Message, EncryptionFactory} from "../../../build/spv-channels";
import { resolve } from "path";

async function channelsDemo(url: string, accountId: string, username: string, password: string)
{
    let channelsClient = new ChannelsClient(url, accountId, username, password);

    // Create channel
    let newChannel = await channelsClient.createChannel(
        false,
        false,
        true,
        { min_age_days: 10, max_age_days: 31, auto_prune: true}
    );
    console.log(`Created channel ${newChannel.href}`);

    // Get details of the channel
    let channelDetails = await channelsClient.getChannel(newChannel.id);
    console.log(`Channel details: ${JSON.stringify(channelDetails)}`);

    // Call list channels
    let channelsList = await channelsClient.listChannels();
    console.log(`List of channels:`);
    channelsList.forEach(c=> {
        console.log(c.href);
    });

    // Call list channels
    let tokensList = await channelsClient.listTokens(channelDetails);
    console.log(`List of tokens:`);
    tokensList.forEach(t=> {
        console.log(`${t.description} : ${t.token}`);
    });

    // Lock new channel
    await channelsClient.amendChannel(channelDetails, false, false, true);
    channelDetails = await channelsClient.getChannel(channelDetails.id);
    console.log(`Channel locked: ${channelDetails.locked}`);

    // Delete new channel
    await channelsClient.deleteChannel(channelDetails);
    channelDetails = await channelsClient.getChannel(channelDetails.id)
    .then( channel => {
        console.log(`Channel ${channelDetails.href} still exists - delete was not successfull.`);
        return channel;
    })
    .catch ( error => {
            console.log(`Cannot fetch channel ${channelDetails.href} - delete was successfull.`);
            return null;
        }
    );
}
       
async function messagingDemo(url: string, accountId: string, username: string, password: string)
{
    let channelsClient = new ChannelsClient(url, accountId, username, password);

    // Create channel
    let channel = await channelsClient.createChannel(
        false,
        false,
        true,
        { min_age_days: 10, max_age_days: 31, auto_prune: true}
    );

    let ownerToken = channel.access_tokens[0];
    let extraToken = await channelsClient.generateToken(channel, "Extra token", true, true);

    // Create messaging client for channel owner
    let messagingClient = new MessagingClient(
        url, 
        channel.id,
        ownerToken.token);

    // Create messaging client for extra token
    let messagingClientEx = new MessagingClient(
        url, 
        channel.id,
        extraToken.token);

    // Subscribe to notifications on extra token
    messagingClientEx.subscribeNotifications(notifyOpen, notifyNewMessage, notifyDisconnect);

    // Wait for websockets to open
    while (!messagingClientEx.notificationsActive())
    {
        await sleep(1000);
    }

    // Write message to channel
    let newMessage = await messagingClient.writeMessage("text/plain", Buffer.from("Test message from channel owner"));
    console.log(`Wrote message: ${newMessage.sequence} Content type: ${newMessage.content_type}`);
    console.log(`Payload: ${newMessage.payload.toString()}`);

    // Get messages head
    let maxSeq = await messagingClient.getMessagesHead();
    console.log(`Max sequence: ${maxSeq}`);

    // List messages - should be empty
    printMessages(await messagingClient.getMessages());

    // Mark message as unread
    await messagingClient.markAsUnread(newMessage, false);    
    // List messages - should display message
    printMessages(await messagingClient.getMessages());

    // Mark message as read
    await messagingClient.markAsRead(newMessage, false);    
    // List messages - should be empty
    printMessages(await messagingClient.getMessages());


    // Mark message as also for other client
    // The first message is unencrypted and we do not want to read it with encryption    
    await messagingClientEx.markAsRead(newMessage, false);    

    /// Encryption

    // Create new keys
    var keysReceiver = await EncryptionFactory.CreateNewKeys();

    // Create messaging client for receiver of encrypted message
    let messagingClientEncryptedReceiver = new MessagingClient(
        url, 
        channel.id,
        extraToken.token, keysReceiver);
    

    var encryptionKey = keysReceiver.getEncryptionKey();
    console.log(`Sender will use the following encryption key ${encryptionKey}`)
    console.log(`Sender will use following authentication token for channels: Bearer ${ownerToken.token}`)
    console.log(`Channel URL:  ${channel.href}`)

    var keySender = await EncryptionFactory.CreateFromEncryptionKey(encryptionKey);

    // Reconnect to channels, this time with encryption
    let messagingClientEncrypted = new MessagingClient(
        url, 
        channel.id,
        ownerToken.token,
        keySender);

    let encryptedMessage = await messagingClientEncrypted.writeMessage("text/plain", Buffer.from("This is a top secret message"));
    console.log(`Wrote encrypted message: ${encryptedMessage.sequence} Content type: ${encryptedMessage.content_type}`);
    
    // Test state saving 
    var clientState = messagingClientEncryptedReceiver.getState();
    console.log(`Saved client state to JSON: ${clientState}`);

    // Restore from state
    var messagingClientEncryptedReceiverCopy = (await MessagingClient.fromState(clientState));

    var messages2 = await messagingClientEncryptedReceiverCopy.getMessages(true);
    printMessages(messages2);
    
    // Delete new channel
    await channelsClient.deleteChannel(channel);

    // Unsubscribe notifications
    messagingClientEx.unsubscribeNotifications();
}

function sleep(ms) {
    return new Promise((resolve) => {
        setTimeout(resolve, ms);
    });
}   

function notifyOpen (channelId: string)
{
    console.log("Notifications socket is open for channel" + channelId);
}

function notifyNewMessage (channelId: string, notification: string, received: Date)
{
    console.log("Notification: " + notification);
}

function notifyDisconnect (channelId: string)
{
    console.log("Notifications socket is closed.");
}

function printMessages(messages: Array<Message>)
{
    console.log(`List of messages:`);
    messages.forEach(m=> {
        console.log(`Message: ${m.sequence} Content type: ${m.content_type}`);
        console.log(`Payload: ${m.payload.toString()}`);
    });

}

if (process.argv.length < 6)
{
    console.info("Missing parameters.");
    console.info("Usage: node demo.js channelsUrl accountId username password");
}
else
{
    var url = process.argv[2];
    var accountId = process.argv[3];
    var username = process.argv[4];
    var password = process.argv[5];

    channelsDemo(url, accountId, username, password); 
    messagingDemo(url, accountId, username, password);
}
