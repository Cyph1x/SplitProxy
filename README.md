# SplitProxy
This is just a small hobby project to help improve my home internet speeds and teach me some c# along the way

Please do not use in situations that require high reliability.

## What is Split Proxy?
Split Proxy is a server and client that allows for data to be split equally between multiple connections.


## Why Split Proxy?
This project is designed to be an efficient means of connection aggregation without all the fancy network configuration.
The goal is to increase data throughput by combining all tunnels connection speeds.

## How to use
The classes are designed to be used in a similar way to sockets

For efficiency both the client and server have a target socket. 
This socket once specified will have all data streamed to it.

_NOTE_ You will have to handle all data coming from the target socket.


### Server:
1. Create a server object
2. Start the server with a port
3. Accept connections!
4. With the newly accepted connection you can now specify a target socket

### Client:
1. Create a Client object with the target IP and port combo
2. Connect with all the proxy IP and port combos in an array
3. Set the target socket

## Example code
You can find example code in the "connection split client" and "connection split server" folders
