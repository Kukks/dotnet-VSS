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

## Gotchas!

On Mac, there is an issue around the source generator for protobuf to c# code. While I expect this to be fixed at some point from the library vendor, we can't wait on this. There is a folder `MacHax` which contains the generates library but is only included when compiling on a Mac. This is a temporary solution until the issue is resolved but may result in missing/different definitions when on mac (regenerate the machax folder when this is the case).
