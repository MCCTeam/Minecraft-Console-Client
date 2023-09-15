# Web Socket Chat Bot documentation

This is a documentation page on the Web Socket chat bot and on how to make a library that uses web socket to execute commands in the MCC and processes events sent by the MCC.

Please read the [Important things](#important-things) before everything.

# Page index

- [Important things](#important-things)
  - [Prerequisites](#prerequisites)
  - [Limitations](#limitations)
  - [Precision of information](#precisionvalidity-of-the-information-in-this-guide)
- [How does it work?](#how-does-it-work)
- [Sending commands](#sending-commands-to-mcc)
- [Websocket Commands](Commands.md)
- [Websocket Events](Events.md)
- [Reference Implementation](#reference-implementation)

## Reference implementation

I have made a reference implementation in TypeScript/JavaScript, it is avaliable here: 

[https://github.com/milutinke/MCC.js](https://github.com/milutinke/MCC.js)

It is great for better understanding how this works.

## Important things

### Prerequisites 

This guide/documentation assumes that you have enough of programming knowledge to know:

  - What Web Socket is
  - Basics of networking and concurency
  - What JSON is
  - What are the various data types such as boolean, integer, long, float, double, object, dictionary/hash map

Without knowing those, I highly recommend learning about those concepts before trying to implement your own library.

### Limitations

The Web Socket chat bot should be considered experimental and prone to change, it has not been fully tested and might change, keep an eye on updates on our official Discord server.

### Precision/Validity of the information in this guide

This guide has been mostly generated from the code itself, so the types are C# types, except in few cases where I have manually changed them. 

For some thing you will have to dig in to the MCC C# code of the Chat Bot and various helper classes.

**Some information sent by the MCC, for example entity metadata, block ids, item ids, or various other data is different for each Minecraft Version, thus you need to map it for each minecraft version.**

Some events might not be that useful, eg. `OnNetworkPacket`

## How does it work?

So, basically, this Web Socket Chat Bot is a chat bot that has a Web Socket server running while you're connected to a minecraft server.

It sends events, and listens for commands and responds to commands.

It has build in authentication, which requires you to send a command to authenticate if the the password is set, if it is not set, it should automatically authenticate you on the first command.

You also can name every connection (session) with an alias.

The flow of the protocol is the following:

```
Connect to the chat bot via web socket

            |
            |
           \ /
            `

Optionally set a session alias/name with "ChangeSessionId" command 
(this can be done multiple times at any point)

            |
            |
           \ /
            `

Send an "Authenticate" command if there is a password set 

            |
            |
           \ /
            `

Send commands and listen for events
```

In order to implement a library that communicates witht this chat bot, you need to make a way to send commands, remember the sent commands via the `requestId` value, and listen for `OnWsCommandResponse` event in which you need to detect if your command has been executed by looking for the `requestId` that matches the one you've sent. I also recommend you put a 5-10 seconds command execution timeout, where you discard the command if it has not been executed in the given timeout range.

## Sending commands to MCC

You can send text in the chat, execute client commands or execute remote procedures (WebSocket Chat Bot commands).

Each thing that is sent to the chat bot results in a response through the [`OnWsCommandResponse`](#onwscommandresponse) event.

### Sending chat messages

To send a chat message just send a plain text with your message to via the web socket.

### Executing client commands

To execute a client command, just send plain text with your command.

Example: `/move suth`

### Execution remote procedures (WebSocket Chat Bot commands)

In order to execute a remote procedure, you need to send a json encoded string in the following format:

```json
{
  "command": "<command name here>",
  "requestId": "<randomly generated string for identification>",
  "parameters": [ 1, "some string", true, "etc.." ]
}
```

#### `command` 

  Refers to the name of the command

#### `requestId`

  Is a unique indentifier you generate on each command, it will be returned in the response of the command execution ([`OnWsCommandResponse`](#onwscommandresponse)), use it to track if a command has been successfully executed or not, and to get the return value if it has been successfully executed. (*It's recommended to generate at least 7 characters to avoid collision, best to use an UUID format*).

#### `parameters`
  
  Are parameters (attibutes) of the procedure you're executing, they're sent as an array of data of various types, the Web Socket chat bot does parsing and conversion and returns an error if you have sent a wrong type for the given parameters, of if you haven't send enough of them.

  **Example:**

  ```json
  {
    "command": "Authenticate",
    "requestId": "8w9u60-q39ik",
    "parameters": ["wspass12345"]
  }
  ```