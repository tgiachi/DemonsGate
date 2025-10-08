using DemonsGate.Core.Enums;
using DemonsGate.Core.Utils;
using DemonsGate.Entities.Interfaces;
using DemonsGate.Entities.Models;
using DemonsGate.Services.Interfaces;
using Serilog;

namespace DemonsGate.Services.Impl;

public class SeedService : ISeedService
{
    private readonly ILogger _logger = Log.ForContext<SeedService>();

    private readonly IEntityDataAccess<UserEntity> _userDataAccess;

    public SeedService(IEntityDataAccess<UserEntity> userDataAccess)
    {
        _userDataAccess = userDataAccess;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await VerifyAdminUserAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
    }

    private async Task VerifyAdminUserAsync()
    {
        var adminCount = await _userDataAccess.SearchAsync(s => s.UserLevel == UserLevelType.SuperAdmin);
        if (adminCount.ToList().Count == 0)
        {
            _logger.Information("No SuperAdmin user found. Creating default SuperAdmin user.");

            var generatedPassword = PasswordGeneratorUtils.GeneratePassword(8);

            var user = new UserEntity
            {
                Email = "admin@demonsgame.io",
                IsLocked = false,
                UserLevel = UserLevelType.SuperAdmin,
                PasswordHash = HashUtils.CreatePassword(generatedPassword)
            };

            await _userDataAccess.InsertAsync(user);

            _logger.Information(
                "Default SuperAdmin user created with email: {Email} and password: {Password}",
                user.Email,
                generatedPassword
            );
        }
    }
}
