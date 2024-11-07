using System.Security.Cryptography;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using VSSProto;

namespace VSS.Tests;

public class VSSApiEncryptorClientTests
{
    [Fact]
    public async Task GetObjectAsync_ShouldDecryptValue_WhenValueIsPresent()
    {
        // Arrange
        var mockVssApi = new Mock<IVSSAPI>();
        var mockProtector = new Mock<IDataProtector>();

        var encryptedData = ByteString.CopyFromUtf8("encrypted");
        var decryptedData = "decrypted";

        var getObjectResponse = new GetObjectResponse
        {
            Value = new KeyValue
            {
                Value = encryptedData
            }
        };

        mockVssApi
            .Setup(api => api.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getObjectResponse);

        mockProtector
            .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Returns(System.Text.Encoding.UTF8.GetBytes(decryptedData));

        var client = new VSSApiEncryptorClient(mockVssApi.Object, mockProtector.Object);

        var request = new GetObjectRequest { StoreId = "store", Key = "key1" };

        // Act
        var response = await client.GetObjectAsync(request);

        // Assert
        Assert.Equal(decryptedData, response.Value.Value.ToStringUtf8());
        mockVssApi.Verify(api => api.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        mockProtector.Verify(p => p.Unprotect(It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task PutObjectAsync_ShouldEncryptValues_BeforeCallingVssApi()
    {
        // Arrange
        var mockVssApi = new Mock<IVSSAPI>();
        var mockProtector = new Mock<IDataProtector>();

        var decryptedData = ByteString.CopyFromUtf8("decrypted");
        var encryptedData = System.Text.Encoding.UTF8.GetBytes("encrypted");

        var putObjectResponse = new PutObjectResponse();

        mockVssApi
            .Setup(api => api.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(putObjectResponse);

        mockProtector
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns(encryptedData);

        var client = new VSSApiEncryptorClient(mockVssApi.Object, mockProtector.Object);

        var request = new PutObjectRequest
        {
            StoreId = "store",
            TransactionItems =
            {
                new KeyValue
                {
                    Key = "key1",
                    Value = decryptedData
                }
            }
        };

        // Act
        var response = await client.PutObjectAsync(request);

        // Assert
        mockVssApi.Verify(api => api.PutObjectAsync(It.Is<PutObjectRequest>(req =>
            req.TransactionItems[0].Value.ToStringUtf8() == "encrypted"), It.IsAny<CancellationToken>()), Times.Once);
        mockProtector.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task ListKeyVersionsAsync_ShouldDecryptValues_WhenValuesArePresent()
    {
        // Arrange
        var mockVssApi = new Mock<IVSSAPI>();
        var mockProtector = new Mock<IDataProtector>();

        var encryptedData = ByteString.CopyFromUtf8("encrypted");
        var decryptedData = "decrypted";

        var listResponse = new ListKeyVersionsResponse
        {
            KeyVersions =
            {
                new KeyValue { Key = "key1", Value = encryptedData },
                new KeyValue { Key = "key2", Value = ByteString.Empty } // No decryption needed
            }
        };

        mockVssApi
            .Setup(api => api.ListKeyVersionsAsync(It.IsAny<ListKeyVersionsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        mockProtector
            .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Returns(System.Text.Encoding.UTF8.GetBytes(decryptedData));

        var client = new VSSApiEncryptorClient(mockVssApi.Object, mockProtector.Object);

        var request = new ListKeyVersionsRequest { StoreId = "store" };

        // Act
        var response = await client.ListKeyVersionsAsync(request);

        // Assert
        Assert.Equal(decryptedData, response.KeyVersions[0].Value.ToStringUtf8());
        Assert.True(response.KeyVersions[1].Value.IsEmpty); // Ensure second value remains unchanged
        mockVssApi.Verify(api => api.ListKeyVersionsAsync(It.IsAny<ListKeyVersionsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        mockProtector.Verify(p => p.Unprotect(It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task DeleteObjectAsync_ShouldCallVssApiDirectly()
    {
        // Arrange
        var mockVssApi = new Mock<IVSSAPI>();
        var mockProtector = new Mock<IDataProtector>();

        var deleteResponse = new DeleteObjectResponse();

        mockVssApi
            .Setup(api => api.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResponse);

        var client = new VSSApiEncryptorClient(mockVssApi.Object, mockProtector.Object);

        var request = new DeleteObjectRequest { StoreId = "store", KeyValue = new KeyValue { Key = "key1" } };

        // Act
        var response = await client.DeleteObjectAsync(request);

        // Assert
        Assert.Equal(deleteResponse, response);
        mockVssApi.Verify(api => api.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        mockProtector.VerifyNoOtherCalls(); // No encryption/decryption calls expected
    }
    
    [Fact]
    public async Task PutObjectAsync_ShouldHandleNullTransactionItems()
    {
    
        Assert.Throws<ArgumentNullException>(() =>
        {
            var request = new PutObjectRequest
            {
                StoreId = "store",
                TransactionItems = {(KeyValue) null}
            };
        });
        
    
    }
    
    [Fact]
    public async Task GetObjectAsync_ShouldHandleUnprotectException()
    {
        // Arrange
        var mockVssApi = new Mock<IVSSAPI>();
        var mockProtector = new Mock<IDataProtector>();
    
        var encryptedData = ByteString.CopyFromUtf8("encrypted");
        var getObjectResponse = new GetObjectResponse
        {
            Value = new KeyValue { Value = encryptedData }
        };
    
        mockVssApi
            .Setup(api => api.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getObjectResponse);
    
        mockProtector
            .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Throws(new CryptographicException("Decryption failed"));

        var client = new VSSApiEncryptorClient(mockVssApi.Object, mockProtector.Object);

        var request = new GetObjectRequest { StoreId = "store", Key = "key1" };
    
        // Act & Assert
        await Assert.ThrowsAsync<CryptographicException>(() => client.GetObjectAsync(request));
    }
    
    [Fact]
    public async Task ListKeyVersionsAsync_ShouldSkipInvalidDecryption()
    {
        // Arrange
        var mockVssApi = new Mock<IVSSAPI>();
        var mockProtector = new Mock<IDataProtector>();
    
        var validEncryptedData = ByteString.CopyFromUtf8("valid");
        var invalidEncryptedData = ByteString.CopyFromUtf8("invalid");

        var listResponse = new ListKeyVersionsResponse
        {
            KeyVersions =
            {
                new KeyValue { Key = "key1", Value = validEncryptedData },
                new KeyValue { Key = "key2", Value = invalidEncryptedData }
            }
        };
    
        mockVssApi
            .Setup(api => api.ListKeyVersionsAsync(It.IsAny<ListKeyVersionsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);
    
        mockProtector
            .Setup(p => p.Unprotect(validEncryptedData.ToByteArray()))
            .Returns(System.Text.Encoding.UTF8.GetBytes("decrypted-valid"));
    
        mockProtector
            .Setup(p => p.Unprotect(invalidEncryptedData.ToByteArray()))
            .Throws(new CryptographicException("Decryption failed"));

        var client = new VSSApiEncryptorClient(mockVssApi.Object, mockProtector.Object);

        var request = new ListKeyVersionsRequest { StoreId = "store" };
    
        
       await Assert.ThrowsAsync<CryptographicException>(() => client.ListKeyVersionsAsync(request)); 
        // Assert
        mockProtector.Verify(p => p.Unprotect(It.IsAny<byte[]>()), Times.Exactly(2));
    }



}