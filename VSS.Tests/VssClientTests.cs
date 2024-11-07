using System.Net;
using Google.Protobuf;
using Moq;
using Moq.Protected;
using VSSProto;

namespace VSS.Tests;

public class VssClientTests
{
    private Mock<HttpMessageHandler> SetupMockHttpMessageHandler(HttpResponseMessage response, ByteString expectedRequestContent = null)
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    expectedRequestContent == null || req.Content.ReadAsByteArrayAsync().Result.SequenceEqual(expectedRequestContent.ToByteArray())
                ),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return mockHttpHandler;
    }

    private IVSSAPI CreateClient(Mock<HttpMessageHandler> handler)
    {
        var client = new HttpClient(handler.Object);
        return new HttpVSSAPIClient(new Uri("https://vss.example.com"), client);
    }

    [Fact]
    public async Task TestGetObject_Success()
    {
        // Arrange
        var expectedResponse = new GetObjectResponse
        {
            Value = new KeyValue
            {
                Key = "k1",
                Version = 2,
                Value = ByteString.CopyFromUtf8("k1v2")
            }
        };
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedResponse.ToByteArray())
        };

        var mockHttpHandler = SetupMockHttpMessageHandler(responseMessage);
        var client = CreateClient(mockHttpHandler);

        var request = new GetObjectRequest { StoreId = "store", Key = "k1" };

        // Act
        var actualResponse = await client.GetObjectAsync(request);

        // Assert
        Assert.Equal(expectedResponse, actualResponse);
    }

    [Fact]
    public async Task TestPutObject_Success()
    {
        // Arrange
        var expectedResponse = new PutObjectResponse();
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedResponse.ToByteArray())
        };

        var mockHttpHandler = SetupMockHttpMessageHandler(responseMessage);
        var client = CreateClient(mockHttpHandler);

        var request = new PutObjectRequest
        {
            StoreId = "store",
            GlobalVersion = 4,
            TransactionItems = { new KeyValue { Key = "k1", Version = 2, Value = ByteString.CopyFromUtf8("k1v3") } }
        };

        // Act
        var actualResponse = await client.PutObjectAsync(request);

        // Assert
        Assert.Equal(expectedResponse, actualResponse);
    }

    [Fact]
    public async Task TestGetObject_NotFoundError()
    {
        // Arrange
        var errorResponse = new ErrorResponse
        {
            ErrorCode = ErrorCode.NoSuchKeyException,
            Message = "NoSuchKeyException"
        };
        var responseMessage = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new ByteArrayContent(errorResponse.ToByteArray())
        };

        var mockHttpHandler = SetupMockHttpMessageHandler(responseMessage);
        var client = CreateClient(mockHttpHandler);

        var request = new GetObjectRequest { StoreId = "store", Key = "non_existent_key" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<VSSClientException>(() => client.GetObjectAsync(request));
        
        Assert.Equal(ErrorCode.NoSuchKeyException,exception.Error.ErrorCode);
    }

    [Fact]
    public async Task TestInternalServerError()
    {
        // Arrange
        var errorResponse = new ErrorResponse
        {
            ErrorCode = ErrorCode.InternalServerException,
            Message = "InternalServerException"
        };
        var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new ByteArrayContent(errorResponse.ToByteArray())
        };

        var mockHttpHandler = SetupMockHttpMessageHandler(responseMessage);
        var client = CreateClient(mockHttpHandler);

        var request = new GetObjectRequest { StoreId = "store", Key = "k1" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<VSSClientException>(() => client.GetObjectAsync(request));
        Assert.Equal(ErrorCode.InternalServerException,exception.Error.ErrorCode);
    }
    
    [Fact]
    public async Task DeleteObjectAsync_ShouldReturnValidResponse()
    {
        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var request = new DeleteObjectRequest { StoreId = "store", KeyValue = new KeyValue { Key = "key1" } };
        var expectedResponse = new DeleteObjectResponse();

        mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().EndsWith("/deleteObject") &&
                    req.Content.ReadAsByteArrayAsync().Result.SequenceEqual(request.ToByteArray())
                ),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(expectedResponse.ToByteArray())
            });


        var client = CreateClient(mockHttpHandler);

        // Act
        var response = await client.DeleteObjectAsync(request);

        // Assert
        Assert.Equal(expectedResponse, response);
        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListKeyVersionsAsync_ShouldReturnValidResponse()
    {
        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var request = new ListKeyVersionsRequest { StoreId = "store", PageSize = 5 };
        var expectedResponse = new ListKeyVersionsResponse
        {
            KeyVersions =
            {
                new KeyValue { Key = "key1", Version = 1 },
                new KeyValue { Key = "key2", Version = 2 }
            }
        };

        mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().EndsWith("/listKeyVersions") &&
                    req.Content.ReadAsByteArrayAsync().Result.SequenceEqual(request.ToByteArray())
                ),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(expectedResponse.ToByteArray())
            });

        var client = CreateClient(mockHttpHandler);

        // Act
        var response = await client.ListKeyVersionsAsync(request);

        // Assert
        Assert.Equal(expectedResponse, response);
        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}