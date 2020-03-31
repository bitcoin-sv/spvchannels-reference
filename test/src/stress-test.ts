import { MessagingClient, Message} from "../lib/spv-channels";
import { exit } from "process";

process.env['NODE_TLS_REJECT_UNAUTHORIZED'] = '0';
       
async function sendMessages(url: string, channelId: string, token: string, numberOfMessages: number)
{
    let messagingClient = new MessagingClient(
        url, 
        channelId,
        token);

    let totalMessages = 0;
    while (totalMessages < numberOfMessages)
    {
        // Post some new messages
        var generateInThisLoop = randomInt(3) + 1;
        for (let i=0; i < generateInThisLoop && totalMessages < numberOfMessages; i++)
        {
            let payload = `Test message ${totalMessages}`;
            try 
            {
                await messagingClient.writeMessage("text/plain", Buffer.from(payload));
                totalMessages++;
            } 
            catch (ex)
            {
                await readMessages(messagingClient);    
            }
            console.log(payload);
        }
        // Sleep for random time (up to a second)
        var sleepTime = randomInt(100) * 10;
        console.log(`Sleeping for ${sleepTime}ms`);
        await sleep(sleepTime);
    }
}

function randomInt(max) {
    return Math.floor(Math.random() * Math.floor(max));
}

function sleep(ms) {
    return new Promise((resolve) => {
        setTimeout(resolve, ms);
    });
}   

async function readMessages(messagingClient: MessagingClient)
{
    // Fetch messages
    var messages = await messagingClient.getMessages();
    if (messages.length > 0)
    {
        // Mark all as read 
        for (const message of messages) 
        {        
            await messagingClient.markAsRead(message, false); 
        }
        // Display messages
        printMessages(messages);
    }
}

function printMessages(messages: Array<Message>)
{
    console.log(`List of messages:`);
    messages.forEach(m=> {
        console.log(`Message: ${m.sequence} Content type: ${m.content_type}`);
        console.log(`Payload: ${m.payload.toString()}`);
    });

}

var cmdArgs = process.argv.slice(2);
if (cmdArgs.length != 4)
{
  console.info("Usage: node stress-test.js url channelId token numberOfMessages");
  exit(0);
}

var url = cmdArgs[0];
var channelId = cmdArgs[1];
var token = cmdArgs[2];
var numberOfMessages = parseInt(cmdArgs[3]);

sendMessages(url, channelId, token, numberOfMessages);