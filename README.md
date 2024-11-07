# Versioned Storage System (VSS) for dotnet 

Client-side library to interact with Versioned Storage Service (VSS).

VSS is an open-source project designed to offer a server-side cloud storage solution specifically tailored for noncustodial Lightning supporting mobile wallets. Its primary objective is to simplify the development process for Lightning wallets by providing a secure means to store and manage the essential state required for Lightning Network (LN) operations.

Learn more [here](https://github.com/lightningdevkit/vss-server/blob/main/README.md).

## Usage

At its core, the interface IVSSAPI defines all the methods required for both server and client. The library then provides two concrete implementations of the IVSSAPI interface:
* HttpVSSAPIClient - a client that communicates with the VSS server over HTTP. Any authentication mechanism can be hooked up to the provided HttpClient.
* VSSApiEncryptorClient - a client that allows encrypting the data before sending/receiving it from the VSS server. This does not handle actual communication, but rather wraps around another IVSSAPI implementation.

## Proto Models
The current included vss.proto file is from [here](https://github.com/lightningdevkit/vss-server/blob/main/proto/vss.proto). There is one modification to the original file, which is the addition of
```proto
option csharp_namespace = "VSSProto";
```

which allows us to automatically generate the C# models from the proto file.