function log(s)
{
    var logTime = new Date().toLocaleString();
    document.getElementById("divLog").innerHTML += "<p><code>" + logTime + " " + s + "</code></p><br />";
}

async function sendMessage(client, messageText)
{
    client.writeMessage("text/plain",messageText);
}

async function printAndMarkNewMessages(client)
{
    var messages = await client.getMessages(true);
    messages.forEach(async message => {
        log(message.payload.toString() + " RECEIVED: " + message.received);       
        await client.markAsRead(message, false);
    });
    
}

var messagingClientOwner = null;
async function connectToChannel(serviceUrl, channel, ownerToken, secondPartyToken, encryptionKeyPair)
{
    document.getElementById("channelUrl").value = channel.href;   
    document.getElementById("ownerToken").value = ownerToken.token;
    document.getElementById("secondPartyToken").value = secondPartyToken.token;
    if (encryptionKeyPair)
    {
        document.getElementById("encryptionKeyPair").value = encryptionKeyPair.exportKeys();
        document.getElementById("encryptionKey").value = encryptionKeyPair.getEncryptionKey();
    }
    else 
    {
        document.getElementById("encryptionKeyPair").value = "";
        document.getElementById("encryptionKey").value = "";
    }
    document.getElementById("channelLog").style.display = "block";
    

    messagingClientOwner = new SPVChannels.MessagingClient(
        serviceUrl, 
        channel.id,
        ownerToken.token,
        encryptionKeyPair);

    var encryptionKey = null;
    if (encryptionKeyPair)
        encryptionKey = await SPVChannels.EncryptionFactory.CreateFromEncryptionKey(encryptionKeyPair.getEncryptionKey());

    let messagingClientSecondParty = new SPVChannels.MessagingClient(
        serviceUrl, 
        channel.id,
        secondPartyToken.token,
        encryptionKey);
    
    document.getElementById("btnSendMessage").onclick = async function() {
        if (document.getElementById("messageText").value)
            await sendMessage(messagingClientSecondParty, document.getElementById("messageText").value);
        return false;
    }

    // get messages already waiting on the channel
    await printAndMarkNewMessages(messagingClientOwner);

    messagingClientOwner.subscribeNotifications(notifyOpen, notifyNewMessage, notifyDisconnect);    
}

function notifyOpen (channelId)
{
    log("Connected to channel " + channelId);
}

function notifyNewMessage (channelId, notification, received)
{
    printAndMarkNewMessages(messagingClientOwner);
}

function notifyDisconnect (channelId)
{
    log("Notifications socket is closed.");
}

async function loadChannel(serviceUrl, accountId, username, password, channelId, encryptionKey)
{
    var channelsClient = new SPVChannels.ChannelsClient(serviceUrl, accountId, username, password);
    var channel = await channelsClient.getChannel(channelId);
    var ownerToken = "";
    var secondPartyToken = "";
    channel.access_tokens.forEach(async token => {
        if (token.description == "Owner")
        {
            ownerToken = token;
        }
        else
        {
            secondPartyToken = token;
        }
    });

    var key = null
    if (encryptionKey)
        key = await SPVChannels.EncryptionFactory.ImportKeys(encryptionKey);
    await connectToChannel(serviceUrl, channel, ownerToken, secondPartyToken, key);
}

async function createChannel(serviceUrl, accountId, username, password)
{    
    var channelsClient = new SPVChannels.ChannelsClient(serviceUrl, accountId, username, password);
    var channel = await channelsClient.createChannel(
        false,
        false,
        true,
        { min_age_days: 10, max_age_days: 31, auto_prune: true}
    );
    
    var ownerToken = channel.access_tokens[0];
    var secondPartyToken = await channelsClient.generateToken(channel, "Second party", true, true);

    var encryptionKeyPair = await SPVChannels.EncryptionFactory.CreateNewKeys();

    await connectToChannel(serviceUrl, channel, ownerToken, secondPartyToken, encryptionKeyPair);
}

function onConnectChannelClick()
{
    var serviceUrl = document.getElementById("serviceUrl").value;
    var accountId = document.getElementById("accountId").value;
    var username = document.getElementById("username").value;
    var password = document.getElementById("password").value;    
    var channelId = document.getElementById("channelId").value; 
    var encryptionKey = document.getElementById("encryptionKeyPairExisting").value; 

    loadChannel(serviceUrl, accountId, username, password, channelId, encryptionKey);    

    document.getElementById("modal-control").checked = false; 
}

function onCreateChannelClick()
{
    var serviceUrl = document.getElementById("serviceUrl").value;
    var accountId = document.getElementById("accountId").value;
    var username = document.getElementById("username").value;
    var password = document.getElementById("password").value;    

    createChannel(serviceUrl, accountId, username, password);    
}

