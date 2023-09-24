# BinaryTcpProxy
C# 7 Tcp proxy - Redirecting binary data to another endpoint

## Binary data format/order
> 4 bytes + Message bytes

Each binary message should start with 4 bytes (int) defining the message size (message header)
After that all bytes of the message follow in the size/length of the message size (message body)

## Configuration
Configure the proxy by changing the constant values at `Program.cs`

| Plugin | README |
| ------ | ------ |
| ProxyIp | Server Ip to redirect binary data |
| ProxyPort | Port of the server above |
| ProxySendDelay | Delay (milliseconds) until message gets redirected |
| MaxMessageSize | Max. size of an binary message (excluded header size) |
| ConnectPort | Port to this Proxy where you need to connect |
| NoDelay | Proxy Nagles algorithm |
| SendTimeout | Proxy sending timeout |
| ReceiveTimeout | Proxy receiving timeout |