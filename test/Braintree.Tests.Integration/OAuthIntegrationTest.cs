using Braintree.Exceptions;
using Braintree.TestUtil;
using NUnit.Framework;

namespace Braintree.Tests.Integration
{
    [TestFixture]
    public class OAuthIntegrationTest
    {
        private BraintreeGateway gateway;

        [SetUp]
        public void Setup()
        {
            gateway = new BraintreeGateway(
                "client_id$development$integration_client_id",
                "client_secret$development$integration_client_secret"
            );
        }

        [Test]
        public void CreateTokenFromCode_ReturnsOAuthCredentials()
        {
            
            string code = OAuthTestHelper.CreateGrant(gateway, "integration_merchant_id", "read_write");

            ResultImpl<OAuthCredentials> result = gateway.OAuth.CreateTokenFromCode(new OAuthCredentialsRequest {
                Code = code,
                Scope = "read_write"
            });

            Assert.IsTrue(result.IsSuccess());
            Assert.IsNotNull(result.Target.AccessToken);
            Assert.IsNotNull(result.Target.RefreshToken);
            Assert.IsNotNull(result.Target.ExpiresAt);
            Assert.AreEqual("bearer", result.Target.TokenType);
            
        }

        [Test]
        public void CreateTokenFromRefreshToken_ExchangesRefreshTokenForAccessToken()
        {
            string code = OAuthTestHelper.CreateGrant(gateway, "integration_merchant_id", "read_write");

            ResultImpl<OAuthCredentials> accessTokenResult = gateway.OAuth.CreateTokenFromCode(new OAuthCredentialsRequest {
                Code = code,
                Scope = "read_write"
            });

            ResultImpl<OAuthCredentials> refreshTokenResult = gateway.OAuth.CreateTokenFromRefreshToken(new OAuthCredentialsRequest {
                RefreshToken = accessTokenResult.Target.RefreshToken,
                Scope = "read_write"
            });

            Assert.IsTrue(refreshTokenResult.IsSuccess());
            Assert.IsNotNull(refreshTokenResult.Target.AccessToken);
            Assert.IsNotNull(refreshTokenResult.Target.RefreshToken);
            Assert.IsNotNull(refreshTokenResult.Target.ExpiresAt);
            Assert.AreEqual("bearer", refreshTokenResult.Target.TokenType);
        }

        [Test]
        public void CreateTokenFromBadCode_ReturnsFailureCode()
        {
            ResultImpl<OAuthCredentials> result = gateway.OAuth.CreateTokenFromCode(new OAuthCredentialsRequest {
                Code = "bad_code",
                Scope = "read_write"
            });

            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(
                ValidationErrorCode.OAUTH_INVALID_GRANT,
                result.Errors.ForObject("Credentials").OnField("Code")[0].Code
            );
            Assert.AreEqual(
                "Invalid grant: code not found",
                result.Errors.ForObject("Credentials").OnField("Code")[0].Message
            );
        }

        [Test]
        public void RevokeAccessToken_RevokesAccessToken()
        {
            string code = OAuthTestHelper.CreateGrant(gateway, "integration_merchant_id", "read_write");

            ResultImpl<OAuthCredentials> accessTokenResult = gateway.OAuth.CreateTokenFromCode(new OAuthCredentialsRequest {
                Code = code,
                Scope = "read_write"
            });

            string accessToken = accessTokenResult.Target.AccessToken;
            ResultImpl<OAuthResult> result = gateway.OAuth.RevokeAccessToken(accessToken);
            Assert.IsTrue(result.Target.Result.Value);

            try {
                gateway = new BraintreeGateway(
                    accessToken
                );

                gateway.Customer.Create();

                Assert.Fail("Should throw AuthenticationException");
            } catch (AuthenticationException) {}
        }
    }
}
