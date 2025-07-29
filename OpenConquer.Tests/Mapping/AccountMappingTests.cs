using FluentAssertions;
using Mapster;
using OpenConquer.Domain.Entities;
using OpenConquer.Domain.Enums;
using OpenConquer.Infrastructure.Enums;
using OpenConquer.Infrastructure.Models;
using Xunit.Abstractions;

namespace OpenConquer.Tests.Mapping
{
    public class AccountMappingTests
    {
        private readonly ITestOutputHelper _output;

        public AccountMappingTests(ITestOutputHelper output)
        {
            _output = output;
            MapsterTestConfig.RegisterMappings();
        }

        [Fact]
        public void CanMapAccountEntityToAccount()
        {
            AccountEntity entity = new()
            {
                UID = 123,
                Username = "TestUser",
                Password = "Hash",
                EMail = "test@example.com",
                Question = "Favorite color?",
                Answer = "Blue",
                Permission = AccountPermissionCode.Player,
                Hash = 456,
                Timestamp = 789
            };

            Account account = entity.Adapt<Account>();

            _output.WriteLine("Mapped Account: {0}", System.Text.Json.JsonSerializer.Serialize(account));

            account.Should().BeEquivalentTo(entity, opts => opts
                .ExcludingMissingMembers()
                .Using<PlayerPermission>(ctx =>
                    ctx.Subject.Should().Be(ctx.Expectation))
                .WhenTypeIs<PlayerPermission>());
        }

        [Fact]
        public void CanMapAccountToAccountEntity()
        {
            Account account = new()
            {
                UID = 321,
                Username = "ReverseUser",
                Password = "ReverseHash",
                EMail = "rev@example.com",
                Question = "Pet's name?",
                Answer = "Fluffy",
                Permission = PlayerPermission.GM,
                Hash = 654,
                Timestamp = 987
            };

            AccountEntity entity = account.Adapt<AccountEntity>();

            _output.WriteLine("Mapped AccountEntity: {0}", System.Text.Json.JsonSerializer.Serialize(entity));

            entity.Should().BeEquivalentTo(account, opts => opts
                .ExcludingMissingMembers()
                .Using<AccountPermissionCode>(ctx =>
                    ctx.Subject.Should().Be(ctx.Expectation))
                .WhenTypeIs<AccountPermissionCode>());
        }

        [Fact]
        public void PermissionEnums_Should_Be_Synchronized()
        {
            foreach (AccountPermissionCode value in Enum.GetValues<AccountPermissionCode>())
            {
                Assert.True(Enum.IsDefined(typeof(PlayerPermission), (uint)value),
                    $"Missing PlayerPermission value for {value} ({(uint)value})");
            }

            foreach (PlayerPermission value in Enum.GetValues<PlayerPermission>())
            {
                Assert.True(Enum.IsDefined(typeof(AccountPermissionCode), (uint)value),
                    $"Missing AccountPermissionCode value for {value} ({(uint)value})");
            }
        }
    }
}
